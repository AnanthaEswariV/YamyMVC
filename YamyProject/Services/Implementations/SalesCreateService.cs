namespace YamyProject.Services.Implementations
{
    public class SalesCreateService : ISalesCreateService
    {
        private readonly YamyDbContext _context;
        private readonly ISalesCenterService _salesService;
        public SalesCreateService(YamyDbContext _context, ISalesCenterService salesService)
        {
            _context = _context;
            _salesService = salesService;
        }
        public async Task<int> CreateTaxInvoiceAsync(TaxInvoiceViewModel vm, int currentUserId)
        {
            // 0) Guards
            if (vm is null) throw new ArgumentNullException(nameof(vm));
            if (vm.Items is null || vm.Items.Count == 0)
                throw new InvalidOperationException("Invoice must contain at least one item.");

            // 1) Server-side totals
            decimal totalBeforeVat = 0m, totalVat = 0m, netAmount = 0m, totalDiscount = 0m;

            foreach (var r in vm.Items.Where(i => i != null))
            {
                var linePrice = r.Price * r.QTY;
                var lineDisc = r.Disc;
                var lineBase = linePrice - lineDisc;
                var vatPct = (r.VatPersint) / 100m;

                var lineVat = decimal.Round(lineBase * vatPct, 2, MidpointRounding.AwayFromZero);
                var lineTotal = lineBase + lineVat;

                totalBeforeVat += lineBase;
                totalVat += lineVat;
                totalDiscount += lineDisc;
                netAmount += lineTotal;
            }
            // 2) Generate invoice no (async)
            var invoiceNo = string.IsNullOrWhiteSpace(vm.Invoce)
                ? await _salesService.GenerateInvoiceNoAsync()
                : vm.Invoce.Trim();

            // 3) Strategy + Transaction
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 4) Header
                    var sale = new TblSale
                    {
                        Date = vm.Date,
                        CustomerId = vm.CustomerId ?? 0,
                        InvoiceId = invoiceNo,
                        WarehouseId = vm.WarehousesId ?? 0,
                        PoNum = vm.PONO ?? string.Empty,
                        BillTo = vm.CustomerName ?? string.Empty,
                        City = vm.Emirate ?? string.Empty,
                        SalesMan = vm.SalesMane ?? string.Empty,
                        ShipDate = vm.ShipDate,
                        ShipVia = vm.Val ?? string.Empty,
                        ShipTo = vm.Ship ?? string.Empty,
                        PaymentMethod = vm.InvoiceType ?? string.Empty,
                        AccountCashId = vm.AccountsId ?? 0,
                        PaymentTerms = vm.PaymentTerms ?? string.Empty,
                        PaymentDate = vm.DueTo,
                        Total = totalBeforeVat,
                        Vat = totalVat,
                        Net = netAmount,
                        Pay = string.Equals(vm.InvoiceType, "Cash", StringComparison.OrdinalIgnoreCase) ? netAmount : 0m,
                        Change = string.Equals(vm.InvoiceType, "Cash", StringComparison.OrdinalIgnoreCase) ? 0m : netAmount,
                        CreatedBy = currentUserId,
                        CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        State = 0,
                        Discount = totalDiscount
                    };

                    _context.TblSales.Add(sale);
                    await _context.SaveChangesAsync(); // need sale.Id
                    var saleId = sale.Id;

                    // 5) Details (batch add), then inventory moves per line
                    var details = new List<TblSalesDetail>(vm.Items.Count);

                    foreach (var r in vm.Items.Where(i => i != null))
                    {
                        var qty = r.QTY;
                        var price = r.Price;
                        var disc = r.Disc;
                        var vatPct = r.VatPersint;
                        var lineBase = (price * qty) - disc;
                        var lineVat = decimal.Round(lineBase * (vatPct / 100m), 2);
                        var lineTot = lineBase + lineVat;

                        details.Add(new TblSalesDetail
                        {
                            SalesId = saleId,
                            ItemId = r.Id,
                            Qty = qty,
                            CostPrice = r.CostPrice,
                            Price = price,
                            Discount = disc,
                            Vatp = lineVat,
                            Vat = (int?)Math.Round(vatPct, 0),
                            Total = lineTot,
                            CostCenterId = r.CostCenterId ?? 0
                        });

                        // Inventory logic (no overlap, fully awaited)
                        if (!string.IsNullOrWhiteSpace(r.Type))
                        {
                            if (r.Type.Contains("inventory part", StringComparison.OrdinalIgnoreCase))
                            {
                                await InsertItemTransaction(r, r.Id, qty, r.Method, vm.Date, saleId, invoiceNo);
                            }
                            else if (r.Type.Contains("inventory assembly", StringComparison.OrdinalIgnoreCase))
                            {
                                var components = await _context.TblItemAssemblies
                                    .AsNoTracking()
                                    .Where(a => a.AssemblyId == r.Id)
                                    .Select(a => new AssemblyComponentViewModel
                                    {
                                        ItemId = a.ItemId ?? 0,
                                        Qty = a.Qty ?? 0m,
                                        Method = a.Item!.Method
                                    })
                                    .ToListAsync();

                                foreach (var comp in components)
                                {
                                    var compQty = comp.Qty * qty;
                                    await InsertItemTransaction(r, comp.ItemId, compQty, comp.Method, vm.Date, saleId, invoiceNo);
                                }
                            }
                        }

                        // Cost Center per row (if present)
                        if (r.CostCenterId is not null)
                        {
                            await InsertCostCenterTransactionAsync(
                                vm.Date.ToDateTime(TimeOnly.MinValue),
                                debit: 0m,
                                credit: lineTot,
                                refId: saleId.ToString(),
                                type: "SALE",
                                description: "",
                                costCenterId: r.CostCenterId.Value
                            );
                        }
                    }

                    // Batch insert details then save once
                    _context.TblSalesDetails.AddRange(details);
                    await _context.SaveChangesAsync();

                    // 6) Transfer flags + accounting entries
                    await TransferSaleAsync(vm.InvoiceType ?? "", saleId, vm.PONO ?? "");
                    await Transaction(vm,
                        level4PaymentCreditMethodId: 2,
                        invid: saleId,
                        invoiceNo: vm.Invoce ?? invoiceNo,
                        level4VatId: 3);

                    await tx.CommitAsync();
                    return saleId;
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });
        }

        public async Task InsertCostCenterTransactionAsync(
            DateTime date, decimal debit, decimal credit, string refId, string type, string description, int costCenterId)
        {
            var trx = new TblCostCenterTransaction
            {
                Type = type,
                Date = date,
                RefId = int.Parse(refId),
                Debit = debit,
                Credit = credit,
                Description = description,
                CostCenterId = costCenterId,
                ProjectId = 0
            };

            _context.TblCostCenterTransactions.Add(trx);
            await _context.SaveChangesAsync();
        }

        // Computes cost, writes item transaction (qtyOut), updates OnHand, adds card details
        public async Task<decimal> InsertItemTransaction(
            SalesRowDataViewModel vmRow, int? itemId, decimal qty, string? method, DateOnly SelseDate, int invId, string invoiceNo)
        {
            var id = itemId ?? vmRow.Id;

            var costPrice = await _context.TblItemTransactions
                .AsNoTracking()
                .Where(t => t.ItemId == id && t.Date <= SelseDate)
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.Id)
                .Select(t => (decimal?)t.CostPrice)
                .FirstOrDefaultAsync() ?? 0m;

            var totalCost = costPrice * qty;

            await InsertItemTransaction(
                date: SelseDate,
                type: vmRow.Type ?? "SALE",
                invId: invId.ToString(),
                itemId: id,
                costPrice: costPrice,
                qtyIn: 0,
                salesPrice: vmRow.Price,
                qtyOut: qty,
                description: $"Sales Invoice No. {invoiceNo}",
                warehouseId: vmRow.WarehouseId
            );

            return totalCost;
        }

        public async Task InsertItemTransaction(
            DateOnly date, string type, string invId, int? itemId, decimal costPrice, int qtyIn, decimal salesPrice, decimal qtyOut,
            string description, int warehouseId)
        {
            var row = new TblItemTransaction
            {
                Date = date,
                Type = type,
                Reference = invId,
                ItemId = itemId,
                CostPrice = costPrice,
                QtyIn = qtyIn,
                SalesPrice = salesPrice,
                QtyOut = qtyOut,
                Description = description,
                WarehouseId = warehouseId
            };

            _context.TblItemTransactions.Add(row);
            await _context.SaveChangesAsync();

            await UpdateOnHandItem(itemId);
            await AddItemCardDetails(date, type, invId, itemId ?? 0, costPrice, qtyIn, salesPrice, qtyOut, description, warehouseId);
        }

        public async Task UpdateOnHandItem(int? itemId)
        {
            if (itemId is null) return;

            var qty = await _context.TblItemTransactions
                .Where(t => t.ItemId == itemId)
                .SumAsync(t => (decimal?)(t.QtyIn - t.QtyOut)) ?? 0m;

            var affected = await _context.TblItems
                .Where(i => i.Id == itemId)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.OnHand, _ => qty));

            if (affected == 0)
                throw new InvalidOperationException($"Item {itemId} not found.");
        }

        public async Task AddItemCardDetails(
            DateOnly date, string type, string invoce, int itemId, decimal costPrice, decimal qtyIn, decimal salesPrice, decimal qtyOut,
            string description, int warehouseId)
        {
            var invoiceNo = "INV-" + invoce;

            var debit = qtyIn > 0 ? costPrice * qtyIn : 0m;
            var credit = qtyOut > 0 ? costPrice * qtyOut : 0m;

            var agg = await _context.TblItemCardDetails
                .Where(d => d.ItemId == itemId)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    QtyBalance = g.Sum(d => (decimal?)(d.QtyIn - d.QtyOut)) ?? 0m,
                    Balance = g.Sum(d => (decimal?)(d.Debit - d.Credit)) ?? 0m
                })
                .FirstOrDefaultAsync();

            var prevQtyBal = agg?.QtyBalance ?? 0m;
            var prevBal = agg?.Balance ?? 0m;

            var card = new TblItemCardDetail
            {
                Date = date,
                ItemId = itemId,
                TransType = type,
                InvNo = invoiceNo,
                TransNo = int.TryParse(invoce, out var t) ? t : 0,
                Description = description,
                WharehouseId = warehouseId,
                QtyIn = qtyIn,
                QtyOut = qtyOut,
                QtyBalance = prevQtyBal + (qtyIn - qtyOut),
                Price = costPrice,
                Debit = debit,
                Credit = credit,
                Balance = prevBal + (debit - credit),
                FifoQty = 0m,
                FifoCost = 0m
            };

            _context.TblItemCardDetails.Add(card);
            await _context.SaveChangesAsync();
        }

        public async Task TransferSaleAsync(string formType, int invId, string poNoOrId)
        {
            if (formType == "SQ")
            {
                await _context.TblSalesQuotations
                    .Where(q => q.Id.Equals(poNoOrId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(q => q.TranferStatus, 1)
                        .SetProperty(q => q.SalesId, invId));
            }
            else if (formType == "SO")
            {
                await _context.TblSalesOrders
                    .Where(q => q.Id.Equals(poNoOrId))
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(q => q.TranferStatus, 1)
                        .SetProperty(q => q.SalesId, invId));
            }
        }

        public async Task Transaction(TaxInvoiceViewModel model, int level4PaymentCreditMethodId, int invid, string invoiceNo, int level4VatId)
        {
            if (model.TotaAmount <= 0) return;

            var accountId = (model.InvoiceType == "Credit")
                ? level4PaymentCreditMethodId
                : model.AccountsId;

            // AR / Cash
            await addTransactionEntry(
                model.Date, accountId, model.TotaAmount, 0,
                invid, model.CustomerId ?? 0,
                model.InvoiceType == "Credit" ? "Sales Invoice" : "Sales Invoice Cash",
                "SALES", $"Sales Invoice NO. {invoiceNo}",
                createdBy: 1, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                VoucherNo: model.NextCode
            );

            // Revenue
            await addTransactionEntry(
                model.Date, level4PaymentCreditMethodId, 0, model.TotalBeforeVat,
                invid, 0,
                model.InvoiceType == "Credit" ? "Sales Invoice" : "Sales Invoice Cash",
                "SALES", $"Sales Revenue For Invoice No. {invoiceNo}",
                createdBy: 1, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                VoucherNo: model.NextCode
            );

            // VAT
            if (model.TotalVat > 0)
            {
                await addTransactionEntry(
                    model.Date, level4VatId, 0, model.TotalVat,
                    invid, 0,
                    model.InvoiceType == "Credit" ? "Sales Invoice" : "Sales Invoice Cash",
                    "SALES", $"Vat Output For Invoice No. {invoiceNo}",
                    createdBy: 1, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                    VoucherNo: model.NextCode
                );
            }
        }
        public async Task addTransactionEntry(
            DateOnly date, int? accountId, decimal debit, decimal credit,
            int transactionId, int humId, string type, string voucher_name,
            string description, int createdBy, DateOnly createdDate, string VoucherNo)
        {
            var trx = new TblTransaction
            {
                Date = date,
                AccountId = accountId,
                Debit = debit,
                Credit = credit,
                TransactionId = transactionId,
                HumId = humId,
                TType = voucher_name,
                Type = type,
                Description = description,
                CreatedBy = createdBy,
                VoucherNo = voucher_name,
                CreatedDate = createdDate,
                State = 0
            };

            _context.TblTransactions.Add(trx);
            await _context.SaveChangesAsync();
        }
    }
}
