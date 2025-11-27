namespace YamyProject.Services.Implementations
    {
    public class PurchaseReturnService(YamyDbContext context, IListServices listServices) : IPurchaseReturnService
        {
        private string Vendor = null;
        private DateOnly Starting = default;
        private DateOnly Ending = default;
        private string PayMethod = null;
        private readonly YamyDbContext _context = context;
        private readonly IListServices _ListServices = listServices;
        public async Task<IEnumerable<PurchaseRowViewModel>> GetPurchaseREturnAsync(string selectVendor = null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true)
            {
            if (Custmer == true)
                Vendor = selectVendor;
            if (Date == true)
                {
                if (From == default)
                    {
                    From = DateOnly.FromDateTime(DateTime.Today);
                    }
                if (To == default)
                    {
                    To = DateOnly.FromDateTime(DateTime.Today);
                    }

                Starting = From;
                Ending = To;
                }
            if (Pay == true)
                PayMethod = selectionMethodPay;

            if (selectionMethod == "Default")
                return await GetDefaultReportAsync();
            else
                return await GetDetailedReportAsync();
            }
        public async Task<IEnumerable<PurchaseRowViewModel>> GetDefaultReportAsync()
            {
            //var transactions = await _context.TblTransactions.ToListAsync();

            var query = _context.TblPurchaseReturns
              .Where(s => s.State == 0)
              .Include(s => s.Transaction)
              .Include(s => s.Vendor)
              .OrderBy(s => s.Date)
              .AsQueryable();

            // Apply Vendor filter if provided
            if (!string.IsNullOrEmpty(Vendor))
                query = query.Where(s => s.VendorId == int.Parse(Vendor) || s.Vendor.Name == Vendor);

            // Apply Starting date filter if provided
            if (Starting != default)
                query = query.Where(s => s.Date >= Starting);

            // Apply Ending date filter if provided
            if (Ending != default)
                query = query.Where(s => s.Date <= Ending);

            // Apply Payment Method filter if provided
            if (!string.IsNullOrEmpty(PayMethod))
                query = query.Where(s => s.PaymentMethod == PayMethod);

            const string Collation = "utf8mb4_0900_ai_ci"; // or your column’s exact collation

            var sales = await query
    .Select(s => new
        {
        s.Id,
        s.Date,
        InvNo = s.InvoiceId,
        TransactionId = (int?)(s.Transaction != null ? s.Transaction.TransactionId : (int?)null),
        VendorName = s.Vendor != null
            ? EF.Functions.Collate(s.Vendor.Code + " - " + s.Vendor.Name, Collation) : "",
        PayMethod = s.PaymentMethod,
        s.Total,
        s.Vat,
        s.Net
        })
    .GroupBy(x => new
        {
        x.Id,
        x.Date,
        x.InvNo,
        x.VendorName,
        x.PayMethod,
        x.Total,
        x.Vat,
        x.Net
        })
    .Select(g => new
        {
        g.Key.Id,
        g.Key.Date,
        INV_NO = g.Key.InvNo,
        Jv_No = "000" + (g.Max(x => x.TransactionId) ?? 0),
        Vendor_Name = g.Key.VendorName,
        Payment_Method = g.Key.PayMethod,
        g.Key.Total,
        g.Key.Vat,
        g.Key.Net
        })
    .OrderBy(r => r.Date)
    .ToListAsync();
            var result = new List<PurchaseRowViewModel>();

            int sn = 1;
            foreach (var s in sales)
                {
                result.Add(new PurchaseRowViewModel
                    {
                    SN = sn,
                    Date = s.Date,
                    Id = s.Id,
                    InvoiceNo = s.INV_NO,
                    VendorName = s.Vendor_Name,
                    PaymentMethod = s.Payment_Method,
                    Total = s.Total,
                    Vat = s.Vat,
                    Net = s.Net,
                    JvNo = s.Jv_No
                    });
                sn ++;
                }
            // result.ForEach(x => x.Vendors = customerSelectList);
            return result;
            }
        public async Task<IEnumerable<PurchaseRowViewModel>> GetDetailedReportAsync()
            {
            var query = _context.TblPurchaseReturns
                    .Where(s => s.State == 0)
                    .Include(s => s.PurchaseReturnDetail)
                    .Include(s => s.Vendor)
                    .OrderBy(s => s.Date)
                    .AsQueryable();
            // Apply Vendor filter if provided
            if (!string.IsNullOrEmpty(Vendor))
                query = query.Where(s => s.VendorId == int.Parse(Vendor) || s.Vendor.Name == Vendor);

            // Apply Starting date filter
            if (Starting != default)
                query = query.Where(s => s.Date >= Starting);

            // Apply Ending date filter
            if (Ending != default)
                query = query.Where(s => s.Date <= Ending);

            // Apply Payment Method filter
            if (!string.IsNullOrEmpty(PayMethod))
                query = query.Where(s => s.PaymentMethod == PayMethod);

            const string Collation = "utf8mb4_0900_ai_ci"; // or your column’s exact collation

            // Project into anonymous type with customer and details
            var sales = await query
                .Select(s => new
                    {
                    Sale = s,
                    Details = _context.TblPurchaseReturnDetails
                                 .Where(sd => sd.PurchaseId == s.Id)
                                 .Select(sd => new
                                     {
                                     SalesDetails = sd,
                                     Item = _context.TblItems.FirstOrDefault(i => i.Id == sd.ItemId)
                                     }).ToList(),

                    VendorName = s.Vendor != null
                   ? EF.Functions.Collate(s.Vendor.Code, Collation)
                     + " - "
                     + EF.Functions.Collate(s.Vendor.Name, Collation)
                   : ""
                    })
                .ToListAsync();

            var flat = sales
    .OrderBy(x => x.Sale.Date) // for SN like ROW_NUMBER() OVER (ORDER BY date)
    .SelectMany(x => x.Details.Select(d => new
        {
        x.Sale.Date,
         x.Sale.Id,
        INV_NO = x.Sale.InvoiceId,
        Vendor_Name = x.VendorName,
        Payment_Method = x.Sale.PaymentMethod,
         x.Sale.Total,
        x.Sale.Vat,
        x.Sale.Net,
        Item_Name = (d.Item != null ? (d.Item.Code + " - " + d.Item.Name) : ""),
        d.SalesDetails.Qty,
        d.SalesDetails.Price,
        Item_Vat = d.SalesDetails.Vat,
        Item_Total = d.SalesDetails.Total
        }))
    // optional: group if you truly need it (mirrors SQL GROUP BY)
    .GroupBy(r => new
        {
        r.Date,
        r.Id,
        r.INV_NO,
        r.Vendor_Name,
        r.Payment_Method,
        r.Total,
        r.Vat,
        r.Net,
        r.Item_Name,
        r.Qty,
        r.Price,
        r.Item_Vat,
        r.Item_Total
        })
    .Select(g => g.Key) // keys are the grouped rows
    .Select((r, idx) => new
        {
        SN = idx + 1,
        r.Date,
        r.Id,
        r.INV_NO,
        r.Vendor_Name,
        r.Payment_Method,
        r.Total,
        r.Vat,
        r.Net,
        r.Item_Name,
        r.Qty,
        r.Price,
        r.Item_Vat,
        r.Item_Total
        })
    .ToList();

            var result = new List<PurchaseRowViewModel>();
            int sn = 1;
            foreach (var s in sales)
                {
                foreach (var sd in s.Details)
                    {
                    result.Add(new PurchaseRowViewModel
                        {
                        SN = sn++,
                        Date = s.Sale.Date,
                        Id = s.Sale.Id,
                        InvoiceNo = s.Sale.InvoiceId,
                        VendorName = s.VendorName,
                        PaymentMethod = s.Sale.PaymentMethod,
                        Total = s.Sale.Total,
                        Vat = s.Sale.Vat,
                        Net = s.Sale.Net,
                        ItemName = sd.Item != null ? sd.Item.Code + " - " + sd.Item.Name : "",
                        Qty = sd.SalesDetails.Qty,
                        CostPrice = sd.SalesDetails.CostPrice,
                        ItemVat = sd.SalesDetails.Vat,
                        ItemTotal = sd.SalesDetails.Total
                        });
                    }
                sn ++ ;
                }
            return result;
            }
        public async Task<string> GenerateReturnInvoiceNoAsync() 
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue =await _context.TblPurchaseReturns
               .Select(s => s.InvoiceId.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;
            return $"PR-{int.Parse(prefix) + 1:D4}";
            }
        public async  Task<PurchaseInvoiceViewModel> GetEditAsync(int id)
        {

            var vm = new PurchaseInvoiceViewModel();
            // if (sale == null) throw new KeyNotFoundException($"Sale {id} not found.");

                var sale = await _context.TblPurchaseReturns
                                  .Include(s => s.PurchaseReturnDetail)
                                  .ThenInclude(d => d.Items)
                                  .Include(s => s.Vendor)
                                  .FirstOrDefaultAsync(s => s.Id == id);
                vm = MapPurchaseToViewModel(sale);

            var Warehouse = await _ListServices.GetWarehousesAsync();
            var WarehouseSelectList = Warehouse.Select(c => new TblWarehouse
                {
                Id = c.Id,
                Name = c.Name
                }).ToList();
            var Vendors = await _ListServices.GetVendorsRetrunAsync();
            List<TblVendor>? VendorslectList = [.. Vendors.Select(c => new TblVendor
                {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name
                })];
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
        
            vm.Vendors = VendorslectList;
            vm.Warehouses = WarehouseSelectList;
            vm.Accounts = AccountList;
            vm.CostCenters = CostCenterList;
            vm.Vat = VatList;
            //    await PopulateLookupsAsync(vm);
            return vm;
            }
        public async Task CreateTaxInvoiceAsync(PurchaseInvoiceViewModel vm, int currentUserId)
            {
            // 0) Guards
            if (vm is null)
                {
                throw new ArgumentNullException(nameof(vm));
                }

            if (vm.Items is null || vm.Items.Count == 0)
                throw new InvalidOperationException("Invoice must contain at least one item.");
            // 1) Server-side totals
            decimal totalBeforeVat = 0m, totalVat = 0m, netAmount = 0m, totalDiscount = 0m;
            foreach (var r in vm.Items.Where(i => i != null))
                {
                if (r.ItemId != null && r.ItemId != 0)
                    {
                    var linePrice = r.CostPrice * r.QTY;
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
            var invoiceNo = vm.Invoce;
            if (vm.Invoce == null)
                 invoiceNo = await GenerateReturnInvoiceNoAsync();
            // 3) Strategy + Transaction
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    // 4) Header
                    var Purchase = new TblPurchaseReturn
                        {
                        Date = vm.Date,
                        VendorId = vm.VendorId ?? 0,
                        InvoiceId = invoiceNo,
                        WarehouseId = vm.WarehouseId ?? 0,
                        PoNum = vm.PONO ?? string.Empty,
                        BillTo = vm.VendorName ?? string.Empty,
                        City = vm.Emirate ?? string.Empty,
                        SalesMan = vm.SalesMane ?? string.Empty,
                        ShipDate = vm.ShipDate,
                        ShipVia = vm.Via ?? string.Empty,
                        ShipTo = vm.ShipTo ?? string.Empty,
                        PaymentMethod = vm.InvoiceType ?? string.Empty,
                        AccountCashId = vm.AccountId,
                        PaymentTerms = vm.PaymentTerm ?? string.Empty,
                        PaymentDate = vm.DueTo,
                        Total = totalBeforeVat,
                        Vat = totalVat,
                        Net = netAmount,
                        Pay = string.Equals(vm.InvoiceType, "Cash", StringComparison.OrdinalIgnoreCase) ? netAmount : 0m,
                        Change = string.Equals(vm.InvoiceType, "Cash", StringComparison.OrdinalIgnoreCase) ? 0m : netAmount,
                        CreatedBy = currentUserId,
                        CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        State = 0,
                        //  Discount = totalDiscount,
                        Description = vm.Description ?? string.Empty
                        };
                    _context.TblPurchaseReturns.Add(Purchase);
                    await _context.SaveChangesAsync(); // need sale.Id
                    var PurchaseId = Purchase.Id;
                    if (PurchaseId == 0)
                        throw new InvalidOperationException("Failed to create sale record.");
                                      
                    // 5) Details (batch add), then inventory moves per line
                    await InsertInvItems(vm, PurchaseId, invoiceNo);
                    // 6) Transfer flags + accounting entries
                    await Transaction(vm,
                        level4PaymentCreditMethodId: await _ListServices.DefaultAccountsSet("Vendor"),
                        level4PurchaseReturnId: await _ListServices.DefaultAccountsSet("PurchaseReturn"),
                        invid: PurchaseId,
                        invoiceNo: vm.Invoce ?? invoiceNo,
                        level4VatId: await _ListServices.DefaultAccountsSet("Vat Input"));
                 
                    await tx.CommitAsync();
                    }
                catch
                    {
                    await tx.RollbackAsync();
                    throw;
                    }
            });
            }

        public async Task UpdateTaxInvoiceAsync(PurchaseInvoiceViewModel Model, int currentUserId)
            {
            decimal paidAmount = 0;
            decimal? changeAmount = 0;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {                
                    var Purchase = _context.TblPurchaseReturns.Find(Model.Id) ?? throw new KeyNotFoundException($"Purchase Invoice with ID {Model.Id} not found.");
                    Purchase.Date = Model.Date;
                    Purchase.VendorId = Model.VendorId ?? 0;
                    Purchase.InvoiceId = Model.Invoce; ;
                    Purchase.City = Model.Emirate ?? string.Empty; ;
                    Purchase.WarehouseId = Model.WarehouseId ?? 0;
                    Purchase.PoNum = Model.PONO ?? string.Empty;
                    Purchase.BillTo = Model.VendorName ?? string.Empty;
                    Purchase.ShipDate = Model.ShipDate;
                    Purchase.SalesMan = Model.SalesMane ?? string.Empty;
                    Purchase.ShipVia = Model.Via ?? string.Empty;
                    Purchase.ShipTo = Model.ShipTo ?? string.Empty;
                    Purchase.PaymentMethod = Model.InvoiceType ?? string.Empty;
                    Purchase.AccountCashId = Model.AccountId;
                    Purchase.PaymentTerms = Model.PaymentTerm;
                    Purchase.PaymentDate = Model.DueTo;
                    Purchase.Vat = (decimal)Model.TotalVat;
                    Purchase.Total = (decimal)Model.TotalBeforeVat;
                    Purchase.Net = (decimal)Model.TotaAmount;
                    Purchase.Description = Model.Description ?? string.Empty;

                    if (Model.Id == 0)
                        {
                        Purchase.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                        Purchase.ModifiedBy = currentUserId;
                        }
                    else
                        {
                        Purchase.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                        Purchase.ModifiedBy = currentUserId;
                        }

                    Purchase.Pay = paidAmount;
                    Purchase.Change = (decimal)changeAmount;
                    await _context.SaveChangesAsync();

                    //Delete existing details and transactions and item transactions and item card details and cost center transactions and 
                    await ReturnItemsToInventory(Model.Id);
                    await DeleteItemsFromInventory(Model.Id);
                    //insert the new details and transactions and item transactions and item card details and cost center transactions
                    await InsertInvItems(Model);
                    await Transaction(Model,
                         level4PaymentCreditMethodId: await _ListServices.DefaultAccountsSet("Vendor"),
                        level4PurchaseReturnId: await _ListServices.DefaultAccountsSet("PurchaseReturn"),
                        invoiceNo: Model.Invoce,
                        level4VatId: await _ListServices.DefaultAccountsSet("Vat Input"));

                    await tx.CommitAsync();
                    }
                catch
                    {
                    await tx.RollbackAsync();
                    throw;
                    }
            });
            }

        private static PurchaseInvoiceViewModel MapPurchaseToViewModel(TblPurchaseReturn s)
            {
            // Coalesce details to an empty list to avoid NRE on OrderBy/Select
            var details = s.PurchaseReturnDetail ?? [];

            return new PurchaseInvoiceViewModel
                {
                Invoce = s.InvoiceId,
                VendorId = s.VendorId,
                VendorCode = s.Vendor?.Code.ToString(),
                VendorName = s.BillTo,
                Emirate = s.City,
                SalesMane = s.SalesMan,
                ShipDate = s.ShipDate ?? default,
                ShipTo = s.ShipTo,
                Via = s.ShipVia ?? string.Empty,
                InvoiceType = s.PaymentMethod,
                AccountId = s.AccountCashId,
                PaymentTerm = s.PaymentTerms,
                DueTo = s.PaymentDate,
                Date = s.Date,
                PONO = s.PoNum,
                TotalBeforeVat = s.Total,
                TotalVat = s.Vat,
                TotaDiscount = 0,
                TotaAmount = s.Net,

                Items = [.. s.PurchaseReturnDetail
                    .OrderBy(d => d.Id)

                    .Select(d =>
                    {
                        // null-safe access for the navigation 'Items'

                        // If your schema has nullable decimals, coalesce to 0m as needed.
                        var qty = d.Qty;
                        var price = d.CostPrice;
                        var disc = 0;
                        var vatAmt = d.Vatp;   // amount (keep as-is if that’s your schema)
                        var netPrice = (price * qty) - disc + vatAmt;

                        return new PurchaseItemViewModel
                            {
                            Id = d.Id,
                            ItemId = d.ItemId ?? 0,                         // if non-nullable in DB, make model int (not int?) and remove ??
                            ItemCode = d.Items.Code ?? string.Empty,       // null-safe
                            ItemName = d.Items.Name ?? string.Empty,       // null-safe
                            Method = d.Items.Method ?? string.Empty,       // null-safe
                            Type = d.Items.Type ?? string.Empty,       // null-safe
                            QTY = d.Qty,
                            CostPrice = d.CostPrice,
                            Disc = 0,
                            NetPrice = netPrice,
                            VatPersint = d.Vat,                                  // percent (if that’s your schema)
                            VatAmonut = d.Vatp,                                 // amount
                            Amount = d.Total,
                            Price = d.Price,
                            WarehouseId = d.Items.WarehouseId,                  // if VM property is int? use: it?.WarehouseId
                            CostCenterId = d.CostCenterId                          // if nullable in DB, use: d.CostCenterId ?? 0
                            };
                    })]
                };
            }
        public async Task InsertInvItems(PurchaseInvoiceViewModel vm, int PurchaseId = 0, string invoiceNo = "")
            {
            var details = new List<TblPurchaseReturnDetail>(vm.Items.Count);
            PurchaseId = PurchaseId != 0 ? PurchaseId : vm.Id;
            invoiceNo = !string.IsNullOrWhiteSpace(invoiceNo) ? invoiceNo : vm.Invoce;
            foreach (var r in vm.Items.Where(i => i != null))
                {
                if (r.ItemId != null && r.ItemId != 0)
                    {
                    var qty = r.QTY;
                    var price = r.CostPrice;
                    var disc = r.Disc;
                    var vatPct = r.VatPersint ?? 0;
                    var lineBase = (price * qty) - disc;
                    var lineVat = (decimal)(lineBase * (vatPct / 100m));
                    var lineTot = lineBase + lineVat;

                    details.Add(new TblPurchaseReturnDetail
                        {
                        PurchaseId = PurchaseId,
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
                    // Batch insert details then save once
                    _context.TblPurchaseReturnDetails.AddRange(details);
                    await _context.SaveChangesAsync();

                    // Inventory logic (no overlap, fully awaited)
                    if (!string.IsNullOrWhiteSpace(r.Type))
                        {
                        if (r.Type.Contains("inventory part", StringComparison.OrdinalIgnoreCase))
                            {
                            await InsertItemTransaction(r, vm.Date, PurchaseId.ToString(), invoiceNo, (int)vm.WarehouseId);
                            }
                        // Cost Center per row (if present)
                        if (r.CostCenterId is not null)
                            {
                            await InsertCostCenterTransactionAsync(
                                vm.Date,
                                debit: 0,
                                credit: (decimal)lineTot,
                                refId: PurchaseId.ToString(),
                                type: "Purchase Return",
                                description: "",
                                costCenterId: r.CostCenterId.Value
                            );
                            }
                        }
                    
                    }
                }

            }
        public async Task InsertItemTransaction(PurchaseItemViewModel vmRow, DateOnly date, string invId,string invoiceNo,int warehouseId)
            {
            var row = new TblItemTransaction
                {
                Date = date,
                Type = vmRow.Type ?? "Purchase Return Invoice",
                Reference = invId,
                ItemId = vmRow.ItemId,
                CostPrice = vmRow.CostPrice,
                QtyIn = 0,
                SalesPrice = vmRow.Price,
                QtyOut = vmRow.QTY,
                Description = $"Purchase Return Invoice No. {invoiceNo}",
                WarehouseId = warehouseId
                };

            _context.TblItemTransactions.Add(row);
            await _context.SaveChangesAsync();

             }
        public async Task InsertCostCenterTransactionAsync(DateOnly date, decimal debit, decimal credit, string refId, string type, string description, int costCenterId)
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

        public async Task Transaction(PurchaseInvoiceViewModel model, int level4PaymentCreditMethodId,int level4PurchaseReturnId, int invid = 0, string invoiceNo = "", int level4VatId = 0)
            {
            invid = invid != 0 ? invid : model.Id;
            invoiceNo = !string.IsNullOrWhiteSpace(invoiceNo) ? invoiceNo : model.Invoce;
            level4VatId = level4VatId != 0 ? level4VatId : (int)model.AccountId;
            if (model.TotaAmount <= 0) return;

            var accountId = (model.InvoiceType == "Credit")
                ? level4PaymentCreditMethodId
                : model.AccountId;
            // AR / Cash
            await AddTransactionEntry(
                model.Date, accountId, (int)model.TotaAmount, 0, 
                invid, model.VendorId ?? 0,
                model.InvoiceType == "Credit" ? "Purchase Return Invoice" : "Purchase Return Invoice ",
                "Purchase Return", $"Purchase Return Invoice NO. {invoiceNo}",
                createdBy: 1, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                VoucherNo: model.NextCode
            );
            // Revenue
            await AddTransactionEntry(
                model.Date, level4PurchaseReturnId,  0, (int)model.TotalBeforeVat,
                invid, 0,
                model.InvoiceType == "Credit" ? "Purchase Return Invoice" : "Purchase Return Invoice ",
                "Purchase Return", $"Purchase Return For Invoice No. {invoiceNo}",
                createdBy: 1, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                VoucherNo: model.NextCode
            );
            // VAT
            if (model.TotalVat > 0)
                {
                await AddTransactionEntry(
                    model.Date, level4VatId,  0, (decimal)model.TotalVat,
                    invid, 0,
                    model.InvoiceType == "Credit" ? "Purchase Return Invoice" : "Purchase Return Invoice ",
                    "Purchase Return", $"Vat Input For Return Invoice No. {invoiceNo}",
                    createdBy: 1, createdDate: DateOnly.FromDateTime(DateTime.UtcNow),
                    VoucherNo: model.NextCode
                );
                }
            }
        public async Task AddTransactionEntry(DateOnly date, int? accountId, decimal debit, decimal credit, int transactionId, int humId, string type, string voucher_name,
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
                VoucherNo = VoucherNo,
                CreatedDate = createdDate,
                State = 0
                };

            _context.TblTransactions.Add(trx);
            await _context.SaveChangesAsync();
            }

        public async Task ReturnItemsToInventory(int PurchaseId)
            {
            var rows = await _context.TblPurchaseDetails
                 .AsNoTracking()
                 .Where(sd => sd.PurchaseId == PurchaseId)
                 .Select(sd => new
                     {
                     Id = sd.ItemId,
                     qty = sd.Qty ?? 0m,
                     sd.Items.Type
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
                    }
                }
            }
        public async Task DeleteItemsFromInventory(int PurchaseId)
            {
            var SalesDetails = await _context.TblPurchaseReturnDetails
                      .Where(d => d.PurchaseId == PurchaseId)
                      .ToListAsync();
            _context.TblPurchaseReturnDetails.RemoveRange(SalesDetails);
            await _context.SaveChangesAsync();

            var ItemTransactions = await _context.TblItemTransactions
                     .Where(d => d.Reference == PurchaseId.ToString() && d.Type == "Purchase Return Invoice")
                     .ToListAsync();
            _context.TblItemTransactions.RemoveRange(ItemTransactions);
            await _context.SaveChangesAsync();
            var ItemCardDetails = await _context.TblItemCardDetails
                     .Where(d => d.TransNo == PurchaseId && d.TransType == "Purchase Return Invoice")
                     .ToListAsync();
            _context.TblItemCardDetails.RemoveRange(ItemCardDetails);
            await _context.SaveChangesAsync();

            var CostCenterTransactions = await _context.TblCostCenterTransactions
                     .Where(d => d.RefId == PurchaseId && d.Type == "Purchase Return")
                     .ToListAsync();
            _context.TblCostCenterTransactions.RemoveRange(CostCenterTransactions);
            await _context.SaveChangesAsync();

            var Transactions = await _context.TblTransactions
                     .Where(d => d.TransactionId == PurchaseId && d.Type == "Purchase Return")
                     .ToListAsync();
            _context.TblTransactions.RemoveRange(Transactions);
            await _context.SaveChangesAsync();
            }

        }
    }