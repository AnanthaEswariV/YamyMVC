namespace YamyProject.Services.Implementations
{
    public class SalesCreateService : ISalesCreateService
        {
        private readonly YamyDbContext _context;
        private readonly ISalesCenterService _salesService;
        private readonly IListServices _ListServices;

        public SalesCreateService(YamyDbContext context, ISalesCenterService salesService, IListServices listServices)
            {
            _context = context;
            _salesService = salesService;
            _ListServices = listServices;
            }
        public async Task CreateTaxInvoiceAsync(TaxInvoiceViewModel vm, int currentUserId)
            {
            // 0) Guards
            if (vm is null) throw new ArgumentNullException(nameof(vm));
            if (vm.Items is null || vm.Items.Count == 0)
                throw new InvalidOperationException("Invoice must contain at least one item.");

            // 1) Server-side totals
            decimal totalBeforeVat = 0m, totalVat = 0m, netAmount = 0m, totalDiscount = 0m;
            foreach (var r in vm.Items.Where(i => i != null))
                {
                if (r.ItemId != null && r.ItemId != 0)
                    {
                    var linePrice = r.Price * r.QTY;
                    var lineDisc = r.Disc;
                    var lineBase = linePrice - lineDisc;
                    var vatPct = (r.VatPersint ?? 0) / 100m;

                    var lineVat = (decimal)(lineBase * vatPct);
                    var lineTotal = lineBase + lineVat;

                    totalBeforeVat += (decimal)lineBase;
                    totalVat += lineVat;
                    totalDiscount += lineDisc;
                    netAmount += (decimal)lineTotal;
                    }
                }
            // 2) Generate invoice no (async)
            var invoiceNo = string.IsNullOrWhiteSpace(vm.Invoce)
                ? await _salesService.GenerateInvoiceNoAsync()
                : vm.Invoce.Trim();

            // 3) Strategy + Transaction
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
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
                       Discount = totalDiscount,
                       Description = vm.Description ?? string.Empty
                       };
                   _context.TblSales.Add(sale);
                   await _context.SaveChangesAsync(); // need sale.Id
                   var saleId = sale.Id;
                   if (saleId == 0)
                       throw new InvalidOperationException("Failed to create sale record.");
                   if(vm.From== "QU")
                       {
                       var SalesQuotations = _context.TblSalesQuotations.Find(vm.IdFromOtherTybe);
                       SalesQuotations.TranferStatus = 1;
                     //  _context.TblSalesQuotations.Update();
                       await _context.SaveChangesAsync();
                       }
                   else if (vm.From == "SO")
                       {
                       var SalesQuotations = _context.TblSalesOrders.Find(vm.IdFromOtherTybe);
                       SalesQuotations.TranferStatus = 1;
                       //  _context.TblSalesOrders.Update();
                       await _context.SaveChangesAsync();                   
                       }
                   // 5) Details (batch add), then inventory moves per line
                   await InsertInvItems(vm, saleId, invoiceNo);
                   // 6) Transfer flags + accounting entries
                   await TransferSaleAsync(vm.InvoiceType ?? "", saleId, vm.PONO ?? "");
                   await Transaction(vm,
                       level4PaymentCreditMethodId: 2,
                       invid: saleId,
                       invoiceNo: vm.Invoce ?? invoiceNo,
                       level4VatId: 3);

                   await tx.CommitAsync();
                   }
               catch
                   {
                   await tx.RollbackAsync();
                   throw;
                    }
           });
            }
        public async Task InsertInvItems(TaxInvoiceViewModel vm, int saleId = 0, string invoiceNo = "")
            {
            var details = new List<TblSalesDetail>(vm.Items.Count);
            saleId = saleId != 0 ? saleId : vm.Id;
            invoiceNo = !string.IsNullOrWhiteSpace(invoiceNo) ? invoiceNo : vm.Invoce;
            foreach (var r in vm.Items.Where(i => i != null))
                {
                if (r.ItemId != null && r.ItemId != 0)
                    {
                    var qty = r.QTY;
                    var price = r.Price;
                    var disc = r.Disc;
                    var vatPct = r.VatPersint ?? 0;
                    var lineBase = (price * qty) - disc;
                    var lineVat = (decimal)(lineBase * (vatPct / 100m));
                    var lineTot = lineBase + lineVat;

                    details.Add(new TblSalesDetail
                        {
                        SalesId = saleId,
                        ItemId = r.ItemId,
                        Qty = qty,
                        CostPrice = r.CostPrice,
                        Price = price,
                        Discount = disc,
                        Vatp = lineVat,
                        Vat = (int)vatPct,
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


                        // Cost Center per row (if present)
                        if (r.CostCenterId is not null)
                            {
                            await InsertCostCenterTransactionAsync(
                                vm.Date.ToDateTime(TimeOnly.MinValue),
                                debit: 0m,
                                credit: (decimal)lineTot,
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
                    }
                }
            }
        public async Task InsertCostCenterTransactionAsync(DateTime date, decimal debit, decimal credit, string refId, string type, string description, int costCenterId)
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
            SalesRowDataViewModel vmRow, int? itemId, decimal? qty, string? method, DateOnly SelseDate, int invId, string invoiceNo)
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
                salesPrice: (decimal)vmRow.Price,
                qtyOut: (decimal)qty,
                description: $"Sales Invoice No. {invoiceNo}",
                warehouseId: vmRow.WarehouseId
            );

            return (decimal)totalCost;
            }
        public async Task InsertItemTransaction(DateOnly date, string type, string invId, int? itemId, decimal costPrice, int qtyIn, decimal salesPrice, decimal qtyOut,
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
        public async Task AddItemCardDetails(DateOnly date, string type, string invoce, int itemId, decimal costPrice, decimal qtyIn, decimal salesPrice, decimal qtyOut,
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
            if (string.IsNullOrEmpty(formType)) return;
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
        public async Task Transaction(TaxInvoiceViewModel model, int level4PaymentCreditMethodId, int invid = 0, string invoiceNo = "", int level4VatId = 0)
            {
            invid = invid != 0 ? invid : model.Id;
            invoiceNo = !string.IsNullOrWhiteSpace(invoiceNo) ? invoiceNo : model.Invoce;
            level4VatId = level4VatId != 0 ? level4VatId : (int)model.AccountsId;
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
        public async Task addTransactionEntry(DateOnly date, int? accountId, decimal debit, decimal credit, int transactionId, int humId, string type, string voucher_name,
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
        public async Task<TaxInvoiceViewModel> GetEditAsync(int id, string formType = "")
            {
            var vm = new TaxInvoiceViewModel();
           // if (sale == null) throw new KeyNotFoundException($"Sale {id} not found.");

            if (formType == "SO")
                {
                var sale = await _context.TblSalesOrders
                                   .Include(s => s.SalesOrderDetail)
                                   .ThenInclude(d => d.Items)
                                   .Include(s => s.Customer)
                                   .FirstOrDefaultAsync(s => s.Id == id);
                vm = MapOrdersToViewModel(sale);

                }
            else if (formType == "QU")
                {  var sale = await _context.TblSalesQuotations
                                   .Include(s => s.SalesQuotationDetails)
                                   .ThenInclude(d => d.Items)
                                   .Include(s => s.Customer)
                                   .FirstOrDefaultAsync(s => s.Id == id);
          vm= MapQuotationsToViewModel(sale);
                }
            else
             {   var sale = await _context.TblSales
                               .Include(s => s.TblSalesDetails)
                               .ThenInclude(d => d.Items)
                               .Include(s => s.Customer)
                               .FirstOrDefaultAsync(s => s.Id == id); 
          vm= MapSaleToViewModel(sale);
                }

            var Warehouse = await _ListServices.GetWarehousesAsync();
            var WarehouseSelectList = Warehouse.Select(c => new TblWarehouse
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList();
            var customers = await _ListServices.GetCustomersAsync();
            var customerSelectList = customers.Select(c => new TblCustomer
                {

                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name
                }).ToList();
            var Account = await _ListServices.GetAccountsAsync();
            var AccountList = Account.Select(c => new TblCoaLevel4
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList();
            var Vat = await _ListServices.GetVatAsync();
            var VatList = Vat.Select(c => new SelectListItem
                {
                    Value = c.Value.ToString(),
                    Text = c.Name
                }).OrderBy(c => c.Value).ToList();
            var CostCenter = await _ListServices.GetCostCenterAsync();
            var CostCenterList = CostCenter.Select(c => new TblCostCenter
                {
                    Id = c.Id,
                    Code = c.Code
                }).ToList();

            vm.Customers = customerSelectList;
            vm.Warehouses = WarehouseSelectList;
            vm.Accounts = AccountList;
            vm.CostCenter = CostCenterList;
            vm.Vat = VatList;
        //    await PopulateLookupsAsync(vm);
            return vm;
        }
        private static TaxInvoiceViewModel MapSaleToViewModel(TblSale s)
        {
            // Coalesce details to an empty list to avoid NRE on OrderBy/Select
            var details = s.TblSalesDetails ?? new List<TblSalesDetail>();

            return new TaxInvoiceViewModel
            {
                Id = s.Id,
                CustomerId = s.CustomerId,
                CustomerCode=s.Customer?.Code,
                CustomerName = s.BillTo,
                Emirate = s.City,
                SalesMane = s.SalesMan,
                ShipDate = s.ShipDate ?? default,
                Ship = s.ShipTo,
                Val = s.ShipVia ?? string.Empty,
                PaymentMethod = s.PaymentMethod,
                AccountsId = s.AccountCashId,

                PaymentTerms = s.PaymentTerms,
                DueTo = s.PaymentDate,
                Date = s.Date,
                Invoce = s.InvoiceId,

                PONO = s.PoNum,
                TotalBeforeVat = s.Total,
                TotalVat = s.Vat,
                TotaDiscount = s.Discount,
                TotaAmount = s.Net,

                Items = s.TblSalesDetails
                    .OrderBy(d => d.Id)

                    .Select(d =>
                    {
                        // null-safe access for the navigation 'Items'

                        // If your schema has nullable decimals, coalesce to 0m as needed.
                        var qty = d.Qty;
                        var price = d.Price;
                        var disc = d.Discount;
                        var vatAmt = d.Vatp;   // amount (keep as-is if that’s your schema)
                        var netPrice = (price * qty) - disc + vatAmt;

                        return new SalesRowDataViewModel
                        {
                            Id = d.Id,
                            ItemId = d.ItemId ?? 0,                         // if non-nullable in DB, make model int (not int?) and remove ??
                            ItemCode = d.Items.Code ?? string.Empty,       // null-safe
                            ItemName = d.Items.Name ?? string.Empty,       // null-safe
                            Method = d.Items.Method ?? string.Empty,       // null-safe
                            Type = d.Items.Type ?? string.Empty,       // null-safe
                            QTY = d.Qty,
                            Price = d.Price,
                            Disc = d.Discount,
                            NetPrice = netPrice,
                            VatPersint = d.Vat,                                  // percent (if that’s your schema)
                            VatAmonut = d.Vatp,                                 // amount
                            Amount = d.Total,
                            CostPrice = d.CostPrice,
                            WarehouseId = d.Items.WarehouseId,                  // if VM property is int? use: it?.WarehouseId
                            CostCenterId = d.CostCenterId                          // if nullable in DB, use: d.CostCenterId ?? 0
                        };
                    })
                    .ToList()
            };
        }
        private static TaxInvoiceViewModel MapOrdersToViewModel(TblSalesOrder s)
        {
            // Coalesce details to an empty list to avoid NRE on OrderBy/Select
            var details = s.SalesOrderDetail ?? new List<TblSalesOrderDetail>();

            return new TaxInvoiceViewModel
            {   From="SO",
                IdFromOtherTybe = s.Id,
                CustomerId = s.CustomerId,
                CustomerCode=s.Customer?.Code,
                CustomerName = s.BillTo,
                Emirate = s.City,
                SalesMane = s.SalesMan,
                ShipDate = s.ShipDate ?? default,
                Ship = s.ShipTo,
                Val = s.ShipVia ?? string.Empty,
                PaymentMethod = s.PaymentMethod,
                AccountsId = s.AccountCashId,
                PaymentTerms = s.PaymentTerms,
                DueTo = s.PaymentDate,
                Date = s.Date,
                PONO = s.PoNum,
                TotalBeforeVat = s.Total,
                TotalVat = s.Vat,
                TotaDiscount = 0,
                TotaAmount = s.Net,

                Items = s.SalesOrderDetail
                    .OrderBy(d => d.Id)

                    .Select(d =>
                    {
                        // null-safe access for the navigation 'Items'

                        // If your schema has nullable decimals, coalesce to 0m as needed.
                        var qty = d.Qty;
                        var price = d.Price;
                        var disc = 0;
                        var vatAmt = d.Vatp;   // amount (keep as-is if that’s your schema)
                        var netPrice = (price * qty) - disc + vatAmt;

                        return new SalesRowDataViewModel
                        {
                            Id = d.Id,
                            ItemId = d.ItemId ?? 0,                         // if non-nullable in DB, make model int (not int?) and remove ??
                            ItemCode = d.Items.Code ?? string.Empty,       // null-safe
                            ItemName = d.Items.Name ?? string.Empty,       // null-safe
                            Method = d.Items.Method ?? string.Empty,       // null-safe
                            Type = d.Items.Type ?? string.Empty,       // null-safe
                            QTY = d.Qty,
                            Price = d.Price,
                            Disc = 0,
                            NetPrice = netPrice,
                            VatPersint = d.Vat,                                  // percent (if that’s your schema)
                            VatAmonut = d.Vatp,                                 // amount
                            Amount = d.Total,
                            CostPrice = d.CostPrice,
                            WarehouseId = d.Items.WarehouseId,                  // if VM property is int? use: it?.WarehouseId
                            CostCenterId = d.CostCenterId                          // if nullable in DB, use: d.CostCenterId ?? 0
                        };
                    })
                    .ToList()
            };
        }
        private static TaxInvoiceViewModel MapQuotationsToViewModel(TblSalesQuotation s)
        {
            // Coalesce details to an empty list to avoid NRE on OrderBy/Select
            var details = s.SalesQuotationDetails ?? new List<TblSalesQuotationDetail>();

            return new TaxInvoiceViewModel
            {
                From = "QU",
                IdFromOtherTybe = s.Id,
                CustomerId = s.CustomerId,
                CustomerCode=s.Customer?.Code,
                CustomerName = s.BillTo,
                Emirate = s.City,
                SalesMane = s.SalesMan,
                ShipDate = s.ShipDate ?? default,
                Ship = s.ShipTo,
                Val = s.ShipVia ?? string.Empty,
                PaymentMethod = s.PaymentMethod,
                AccountsId = s.AccountCashId,
                PaymentTerms = s.PaymentTerms,
                DueTo = s.PaymentDate,
                Date = s.Date,
                PONO = s.PoNum,
                TotalBeforeVat = s.Total,
                TotalVat = s.Vat,
                TotaDiscount = 0,
                TotaAmount = s.Net,
                Items = s.SalesQuotationDetails
                    .OrderBy(d => d.Id)
                    .Select(d =>
                    {
                        // null-safe access for the navigation 'Items'
                        // If your schema has nullable decimals, coalesce to 0m as needed.
                        var qty = d.Qty;
                        var price = d.Price;
                        var disc = 0;
                        var vatAmt = d.Vatp;   // amount (keep as-is if that’s your schema)
                        var netPrice = (price * qty) - disc + vatAmt;

                        return new SalesRowDataViewModel
                        {
                            Id = d.Id,
                            ItemId = d.ItemId ?? 0,                         // if non-nullable in DB, make model int (not int?) and remove ??
                            ItemCode = d.Items.Code ?? string.Empty,       // null-safe
                            ItemName = d.Items.Name ?? string.Empty,       // null-safe
                            Method = d.Items.Method ?? string.Empty,       // null-safe
                            Type = d.Items.Type ?? string.Empty,       // null-safe
                            QTY = d.Qty,
                            Price = d.Price,
                            Disc = 0,
                            NetPrice = netPrice,
                            VatPersint = d.Vat,                                  // percent (if that’s your schema)
                            VatAmonut = d.Vatp,                                 // amount
                            Amount = d.Total,
                            CostPrice = d.CostPrice,
                            WarehouseId = d.Items.WarehouseId,                  // if VM property is int? use: it?.WarehouseId
                            CostCenterId = d.CostCenterId                          // if nullable in DB, use: d.CostCenterId ?? 0
                        };
                    })
                    .ToList()
            };
        }
        private async Task PopulateLookupsAsync(TaxInvoiceViewModel vm)
        {
            var Warehouse = await _ListServices.GetWarehousesAsync();
            var WarehouseSelectList = Warehouse.Select(c => new TblWarehouse
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList();
            var customers = await _ListServices.GetCustomersAsync();
            var customerSelectList = customers.Select(c => new TblCustomer
                {

                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name
                }).ToList();
            var Account = await _ListServices.GetAccountsAsync();
            var AccountList = Account.Select(c => new TblCoaLevel4
                {
                    Id = c.Id,
                    Name = c.Name
                }).ToList();
            var Vat = await _ListServices.GetVatAsync();
            var VatList = Vat.Select(c => new SelectListItem
            {
                    Value = c.Value.ToString(),
                    Text = c.Name
                }).OrderBy(c => c.Value).ToList();
            var CostCenter = await _ListServices.GetCostCenterAsync();
            var CostCenterList = CostCenter.Select(c => new TblCostCenter
                {
                    Id = c.Id,
                    Code = c.Code
                }).ToList();
            vm.Customers = customers;
            vm.Vat = VatList;
            vm.CostCenter = CostCenterList;
            vm.Accounts = AccountList;
            vm.Warehouses = WarehouseSelectList;
        }
        public async Task<bool> CheckItemValidity(TaxInvoiceViewModel vm, int itemId)
        {

            decimal OnHand = 0;
            string? Type = "";
            var result = await _context.TblItems
                .AsNoTracking()
                .Where(i => i.Id == itemId)
                .Select(i => new
                {
                    OnHand = (
                            _context.TblItemTransactions
                                .Where(t =>
                                    t.ItemId == i.Id &&
                                    t.Reference != vm.Id.ToString() &&
                                    t.Type != "Sales Invoice")
                                .Select(t => (decimal?)((t.QtyIn ?? 0m) - (t.QtyOut ?? 0m)))
                                .Sum()
                        ) ?? (i.OnHand),
                    i.Type
                })
                .SingleOrDefaultAsync();
            decimal totalQtyInGrid = GetTotalQtyInVm(vm.Items, itemId);

            if (result != null)
            {
                OnHand = result.OnHand;
                Type = result.Type ?? "";
            }
            if (Type.Contains("Inventory Part", StringComparison.OrdinalIgnoreCase))
            {
                if (totalQtyInGrid > OnHand)
                {
                    return false;
                }
            }
            else if (Type.Contains("Inventory Assembly", StringComparison.OrdinalIgnoreCase))
            {
                var components = await _context.TblItemAssemblies
                    .AsNoTracking()
                    .Where(a => a.AssemblyId == itemId)

                    .Select(a => new AssemblyComponentViewModel
                    {
                        ItemId = a.ItemId ?? 0,
                        Qty = a.Qty ?? 0m
                    })
                    .ToListAsync();
                foreach (var comp in components)
                {
                    decimal compOnHand = 0m;
                    var compResult = await _context.TblItemAssemblies
                        .AsNoTracking()
                        .Where(i => i.AssemblyId == comp.ItemId)
                        .Include(i => i.Item)
                        .SingleOrDefaultAsync();
                    if (compResult != null)
                    {
                        compOnHand = compResult.Item.OnHand;
                    }
                    decimal requiredQty = comp.Qty * totalQtyInGrid;
                    if (requiredQty > compOnHand)
                    {
                        return false;
                    }
                }
            }            
            return true;
        }
        decimal GetTotalQtyInVm(IEnumerable<SalesRowDataViewModel> Items, int itemId)
        {
            //decimal total = 0m;

            decimal total = Items?
              .Where(i => i != null && ((i.ItemId ?? i.Id) == itemId))
              .Sum(i => i.QTY ?? 0m) ?? 0m;

            return total;
        }
        public async Task UpdateTaxInvoiceAsync(TaxInvoiceViewModel Model, int currentUserId)
        {
            decimal paidAmount = 0;
            decimal changeAmount = 0;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (Model.InvoiceType != "Cash")
                      {
                          var receiptvoucher = await _context.TblReceiptVoucherDetails
                                          .AsNoTracking()
                                          .Where(r => r.InvId == Model.Id)
                                          .SumAsync(r => (decimal?)r.Payment) ?? 0m;
                          if (receiptvoucher > 0)
                          {
                              paidAmount = receiptvoucher;
                              changeAmount = paidAmount - Model.TotaAmount;
                          }
                    }       
                 var Sales = _context.TblSales.Find(Model.Id);
                    if (Sales == null)
                    {
                        throw new KeyNotFoundException($"Sales Invoice with ID {Model.Id} not found.");
                        }
                    Sales.Date = Model.Date;
                    Sales.CustomerId = Model.CustomerId ?? 0;
                    Sales.InvoiceId = Model.Invoce; ;
                    Sales.City = Model.Emirate ?? string.Empty; ;
                    Sales.WarehouseId = Model.WarehousesId ?? 0;
                    Sales.PoNum = Model.PONO ?? string.Empty;
                    Sales.BillTo = Model.CustomerName ?? string.Empty;
                    Sales.ShipDate = Model.ShipDate;
                    Sales.SalesMan = Model.SalesMane ?? string.Empty;
                    Sales.ShipVia = Model.Val ?? string.Empty;
                    Sales.ShipTo = Model.Ship ?? string.Empty;
                    Sales.PaymentMethod = Model.InvoiceType ?? string.Empty;
                    Sales.AccountCashId = Model.AccountsId ?? 0;
                    Sales.PaymentTerms = Model.PaymentTerms ?? string.Empty;
                    Sales.PaymentDate = Model.DueTo;
                    Sales.Vat = Model.TotalVat;
                    Sales.Total = Model.TotalBeforeVat;
                    Sales.Net = Model.TotaAmount;
                    Sales.Description = Model.Description ?? string.Empty;

                    if (Model.Id == 0)
                    {
                        Sales.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                        Sales.ModifiedBy = currentUserId;
                    }
                    else
                    {
                        Sales.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                        Sales.ModifiedBy = currentUserId;
                    }

                    Sales.Pay = paidAmount;
                    Sales.Change = changeAmount;
                    await _context.SaveChangesAsync();

                    //Delete existing details and transactions and item transactions and item card details and cost center transactions and 
                    await ReturnItemsToInventory(Model.Id);
                    await DeleteItemsFromInventory(Model.Id);
                    //insert the new details and transactions and item transactions and item card details and cost center transactions
                    await InsertInvItems(Model);
                    await Transaction(Model,
                        level4PaymentCreditMethodId: 2,                      
                        invoiceNo: Model.Invoce ,
                        level4VatId: 3);

                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }

            });

        }
        public async Task ReturnItemsToInventory(int salesId)
        {
            var rows = await _context.TblSalesDetails
                 .AsNoTracking()
                 .Where(sd => sd.SalesId == salesId)
                 .Select(sd => new
                 {
                     Id=sd.ItemId,
                     qty=sd.Qty ?? 0m,
                     Type = sd.Items.Type
                 })
                 .ToListAsync();
            foreach (var r in rows)
            {

                if (!string.IsNullOrWhiteSpace(r.Type))
                {
                    if (r.Type.Contains("inventory part", StringComparison.OrdinalIgnoreCase))
                    {
                        await _context.TblItems
                       .Where(i => i.Id == r.Id)
                       .ExecuteUpdateAsync(setters => setters
                           .SetProperty(i => i.OnHand, i => i.OnHand + r.qty)
                       );
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
                            var compQty = comp.Qty * r.qty;

                            await _context.TblItems
                             .Where(i => i.Id == comp.ItemId)
                             .ExecuteUpdateAsync(setters => setters
                                 .SetProperty(i => i.OnHand, i => i.OnHand  + compQty)
                             );
                        }
                    }

                }
            }
        }
        public async Task DeleteItemsFromInventory(int salesId)
        {
            var SalesDetails = await _context.TblSalesDetails
                      .Where(d => d.SalesId == salesId)
                      .ToListAsync();
            _context.TblSalesDetails.RemoveRange(SalesDetails);
            await _context.SaveChangesAsync();

            
            var ItemTransactions = await _context.TblItemTransactions
                     .Where(d => d.Reference == salesId.ToString())
                     .ToListAsync();
            _context.TblItemTransactions.RemoveRange(ItemTransactions);
            await _context.SaveChangesAsync();
            var ItemCardDetails = await _context.TblItemCardDetails
                     .Where(d => d.TransNo == salesId && d.TransType == "Sales Invoice")
                     .ToListAsync();
            _context.TblItemCardDetails.RemoveRange(ItemCardDetails);
            await _context.SaveChangesAsync();
            
            var CostCenterTransactions = await _context.TblCostCenterTransactions
                     .Where(d => d.RefId == salesId && d.Type== "Sales")
                     .ToListAsync();
            _context.TblCostCenterTransactions.RemoveRange(CostCenterTransactions);
            await _context.SaveChangesAsync();

            var Transactions = await _context.TblTransactions
                     .Where(d => d.TransactionId == salesId && d.Type== "Sales")
                     .ToListAsync();
            _context.TblTransactions.RemoveRange(Transactions);
            await _context.SaveChangesAsync();

        }
    }
}