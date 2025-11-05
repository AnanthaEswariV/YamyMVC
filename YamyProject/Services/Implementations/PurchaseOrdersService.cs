namespace YamyProject.Services.Implementations
    {
    public class PurchaseOrdersService(YamyDbContext context, IListServices ListServices) : IPurchaseOrdersService
        {       

        private string Vendors = null;
        private DateOnly Starting = default;
        private DateOnly Ending = default;
        private string PayMethod = null;
        private readonly YamyDbContext _context = context;
        private readonly IListServices _ListServices = ListServices;

        public async Task<IEnumerable<PurchaseRowViewModel>> GetPurchaseAsync(bool Subcontractors = false, string selectVendors = null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true)
            {
            if (Custmer == true)
                Vendors = selectVendors;
            if (Date == true)
                {
                //if (From==default )
                //{ 
                //    From = DateOnly.FromDateTime(DateTime.Today);
                //}
                // if( To==default)
                //{
                //    To = DateOnly.FromDateTime(DateTime.Today);
                //}

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
            var query = _context.TblPurchaseOrders
              .Where(s => s.State == 0)
              .Include(s => s.Vendors)
              .OrderBy(s => s.Date)
              .AsQueryable();
            // Apply Vendors filter if provided
            if (!string.IsNullOrEmpty(Vendors))
                query = query.Where(s => s.VendorId == int.Parse(Vendors) || s.Vendors.Name == Vendors);
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

            var PurchaseOrder = await query
    .Select(s => new
        {
        s.Id,
        s.Date,
        InvNo = s.InvoiceId,
        VendorsName = s.Vendors != null
            ? EF.Functions.Collate(s.Vendors.Code + " - " + s.Vendors.Name, Collation) : "",
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
        x.VendorsName,
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
        Vendors_Name = g.Key.VendorsName,
        Payment_Method = g.Key.PayMethod,
        g.Key.Total,
        g.Key.Vat,
        g.Key.Net
        })
    .OrderBy(r => r.Date)
    .ToListAsync();
            var result = new List<PurchaseRowViewModel>();
            int sn = 1;
            foreach (var s in PurchaseOrder)
                {
                result.Add(new PurchaseRowViewModel
                    {
                    SN = sn,
                    Date = s.Date,
                    Id = s.Id,
                    InvoiceNo = s.INV_NO,
                    VendorName = s.Vendors_Name,
                    PaymentMethod = s.Payment_Method,
                    Total = s.Total,
                    Vat = s.Vat,
                    Net = s.Net,
                    });
                sn = sn + 1;
                }
            return result;
            }
        public async Task<IEnumerable<PurchaseRowViewModel>> GetDetailedReportAsync()
            {
            var query = _context.TblPurchaseOrders
                    .Where(s => s.State == 0)
                    .Include(s => s.PurchaseOrderDetail)
                    .Include(s => s.Vendors)
                    .OrderBy(s => s.Date)
                    .AsQueryable();
            // Apply Vendors filter if provided
            if (!string.IsNullOrEmpty(Vendors))
                query = query.Where(s => s.VendorId == int.Parse(Vendors) || s.Vendors.Name == Vendors);
            // Apply Starting date filter
            if (Starting != default)
                query = query.Where(s => s.Date >= Starting);
            // Apply Ending date filter
            if (Ending != default)
                query = query.Where(s => s.Date <= Ending);
            // Apply Payment Method filter
            if (!string.IsNullOrEmpty(PayMethod))
                query = query.Where(s => s.PaymentMethod == PayMethod);
            const string Collation = "utf8mb4_0900_ai_ci";
            // Project into anonymous type with customer and details
            var PurchaseOrder = await query
                .Select(s => new
                    {
                    Sale = s,
                    Details = _context.TblPurchaseOrderDetails
                                 .Where(sd => sd.PurchaseId == s.Id)
                                 .Select(sd => new
                                     {
                                     VendorsDetails = sd,
                                     Item = _context.TblItems.FirstOrDefault(i => i.Id == sd.ItemId)
                                     }).ToList(),

                    VendorsName = s.Vendors != null
                   ? EF.Functions.Collate(s.Vendors.Code, Collation)
                     + " - "
                     + EF.Functions.Collate(s.Vendors.Name, Collation)
                   : ""
                    })
                .ToListAsync();
            var flat = PurchaseOrder
    .OrderBy(x => x.Sale.Date) // for SN like ROW_NUMBER() OVER (ORDER BY date)
    .SelectMany(x => x.Details.Select(d => new
        {
        x.Sale.Date,
        x.Sale.Id,
        INV_NO = x.Sale.InvoiceId,
        Vendors_Name = x.VendorsName,
        Payment_Method = x.Sale.PaymentMethod,
        x.Sale.Total,
        x.Sale.Vat,
        x.Sale.Net,
        Item_Name = (d.Item != null ? (d.Item.Code + " - " + d.Item.Name) : ""),
        d.VendorsDetails.Qty,
        d.VendorsDetails.Price,
        Item_Vat = d.VendorsDetails.Vat,
        Item_Total = d.VendorsDetails.Total
        }))
    // optional: group if you truly need it (mirrors SQL GROUP BY)
    .GroupBy(r => new
        {
        r.Date,
        r.Id,
        r.INV_NO,
        r.Vendors_Name,
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
        r.Vendors_Name,
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
            foreach (var s in PurchaseOrder)
                {
                foreach (var sd in s.Details)
                    {
                    result.Add(new PurchaseRowViewModel
                        {
                        SN = sn++,
                        Date = s.Sale.Date,
                        Id = s.Sale.Id,
                        InvoiceNo = s.Sale.InvoiceId,
                        VendorName = s.VendorsName,
                        PaymentMethod = s.Sale.PaymentMethod,
                        Total = s.Sale.Total,
                        Vat = s.Sale.Vat,
                        Net = s.Sale.Net,
                        ItemName = sd.Item != null ? sd.Item.Code + " - " + sd.Item.Name : "",
                        Qty = sd.VendorsDetails.Qty,
                        CostPrice = sd.VendorsDetails.Price,
                        ItemVat = sd.VendorsDetails.Vat,
                        ItemTotal = sd.VendorsDetails.Total
                        });
                    }
                sn = sn + 1;
                }
            return result;
            }
        public async Task<string> GenerateVendorsInvoiceNoAsync()
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue = await _context.TblPurchaseOrders
               .Select(s => s.InvoiceId.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;

            return $"PO-{int.Parse(prefix) + 1:D4}";
            }
        public async Task<PurchaseInvoiceViewModel> GetEditAsync(int id, string formType = "")
            {

            var vm = new PurchaseInvoiceViewModel();
            // if (sale == null) throw new KeyNotFoundException($"Sale {id} not found.");

          
                var sale = await _context.TblPurchaseOrders
                                  .Include(s => s.PurchaseOrderDetail)
                                  .ThenInclude(d => d.Items)
                                  .Include(s => s.Vendors)
                                  .FirstOrDefaultAsync(s => s.Id == id);
                vm = MapPurchaseToViewModel(sale);
          
            var Warehouse = await _ListServices.GetWarehousesAsync();
            var WarehouseSelectList = Warehouse.Select(c => new TblWarehouse
                {
                Id = c.Id,
                Name = c.Name
                }).ToList();
            var Vendors = await _ListServices.GetVendorsAsync();
            List<TblVendor>? VendorslectList = Vendors.Select(c => new TblVendor
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
            var FixedAsset = await _ListServices.GetFixedAssetAsync();
            var FixedAssetList = FixedAsset.Select(c => new TblFixedAssetsCategory
                {
                Id = c.Id,
                CategoryName = c.CategoryName
                }).ToList();

            vm.FixedAssets = FixedAssetList;
            vm.Vendors = VendorslectList;
            vm.Warehouses = WarehouseSelectList;
            vm.Accounts = AccountList;
            vm.CostCenters = CostCenterList;
            vm.Vat = VatList;
            //    await PopulateLookupsAsync(vm);
            return vm;
            }
        private static PurchaseInvoiceViewModel MapPurchaseToViewModel(TblPurchaseOrder s)
            {
            // Coalesce details to an empty list to avoid NRE on OrderBy/Select
            var details = s.PurchaseOrderDetail ?? new List<TblPurchaseOrderDetail>();

            return new PurchaseInvoiceViewModel
                {
                Invoce = s.InvoiceId,
                VendorId = s.VendorId,
                VendorCode = s.Vendors?.Code.ToString(),
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

                Items = s.PurchaseOrderDetail
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
                    })
                    .ToList()
                };
            }
        private static PurchaseInvoiceViewModel MapOrdersToViewModel(TblPurchaseOrder s)
            {
            // Coalesce details to an empty list to avoid NRE on OrderBy/Select
            var details = s.PurchaseOrderDetail ?? new List<TblPurchaseOrderDetail>();

            return new PurchaseInvoiceViewModel
                {
                VendorId = s.VendorId,
                VendorCode = s.Vendors?.Code.ToString(),
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

                Items = s.PurchaseOrderDetail
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
                    })
                    .ToList()
                };
            }
        public async Task CreateTaxInvoiceAsync(PurchaseInvoiceViewModel vm, int currentUserId)
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
            //if (vm.Invoce == null)
            //     invoiceNo = GenerateVendorsInvoiceNoAsync();
            // 3) Strategy + Transaction
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    // 4) Header
                    var Purchase = new TblPurchaseOrder
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
                        //Description = vm.Description ?? string.Empty
                        };
                    _context.TblPurchaseOrders.Add(Purchase);
                    await _context.SaveChangesAsync(); // need sale.Id
                    var PurchaseId = Purchase.Id;
                    if (PurchaseId == 0)
                        throw new InvalidOperationException("Failed to create sale record.");

                    //if (vm.From == "PO")
                    //    {
                    //    var PurchaseOrders = _context.TblPurchaseOrders.Find(vm.IdFromOtherTybe);
                    //    PurchaseOrders.TranferStatus = 1;
                    //    //  _context.TblPurchaseOrderOrders.Update();
                    //    await _context.SaveChangesAsync();
                    //    }
                    // 5) Details (batch add), then inventory moves per line
                    await InsertInvItems(vm, PurchaseId, invoiceNo);
                    // 6) Transfer flags + accounting entries
                  
                    await tx.CommitAsync();
                    }
                catch
                    {
                    await tx.RollbackAsync();
                    throw;
                    }
            });
            }
        public async Task InsertInvItems(PurchaseInvoiceViewModel vm, int PurchaseId = 0, string invoiceNo = "")
            {
            var details = new List<TblPurchaseOrderDetail>(vm.Items.Count);
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

                    details.Add(new TblPurchaseOrderDetail
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
                    _context.TblPurchaseOrderDetails.AddRange(details);
                    await _context.SaveChangesAsync();
                    }
                }
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
                    if (Model.InvoiceType != "Cash")
                        {
                        var receiptvoucher = await _context.TblPaymentVoucherDetails
                                        .AsNoTracking()
                                        .Where(r => r.InvId == Model.Id)
                                        .SumAsync(r => (decimal?)r.Payment) ?? 0m;
                        if (receiptvoucher > 0)
                            {
                            paidAmount = receiptvoucher;
                            changeAmount = paidAmount - Model.TotaAmount;
                            }
                        }
                    var PurchaseOrder = _context.TblPurchaseOrders.Find(Model.Id);
                    if (PurchaseOrder == null)
                        {
                        throw new KeyNotFoundException($"Purchase Invoice with ID {Model.Id} not found.");
                        }
                    PurchaseOrder.Date = Model.Date;
                    PurchaseOrder.VendorId = Model.VendorId ?? 0;
                    PurchaseOrder.InvoiceId = Model.Invoce; ;
                    PurchaseOrder.City = Model.Emirate ?? string.Empty; ;
                    PurchaseOrder.WarehouseId = Model.WarehouseId ?? 0;
                    PurchaseOrder.PoNum = Model.PONO ?? string.Empty;
                    PurchaseOrder.BillTo = Model.VendorName ?? string.Empty;
                    PurchaseOrder.ShipDate = Model.ShipDate;
                    PurchaseOrder.SalesMan = Model.SalesMane ?? string.Empty;
                    PurchaseOrder.ShipVia = Model.Via ?? string.Empty;
                    PurchaseOrder.ShipTo = Model.ShipTo ?? string.Empty;
                    PurchaseOrder.PaymentMethod = Model.InvoiceType ?? string.Empty;
                    PurchaseOrder.AccountCashId = Model.AccountId;
                    PurchaseOrder.PaymentTerms = Model.PaymentTerm;
                    PurchaseOrder.PaymentDate = Model.DueTo;
                    PurchaseOrder.Vat = (decimal)Model.TotalVat;
                    PurchaseOrder.Total = (decimal)Model.TotalBeforeVat;
                    PurchaseOrder.Net = (decimal)Model.TotaAmount;
                  //  PurchaseOrder.Description = Model.Description ?? string.Empty;

                    if (Model.Id == 0)
                        {
                        PurchaseOrder.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                        PurchaseOrder.ModifiedBy = currentUserId;
                        }
                    else
                        {
                        PurchaseOrder.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                        PurchaseOrder.ModifiedBy = currentUserId;
                        }

                    PurchaseOrder.Pay = paidAmount;
                    PurchaseOrder.Change = (decimal)changeAmount;
                    await _context.SaveChangesAsync();

                    //Delete existing details and transactions and item transactions and item card details and cost center transactions and 
            var PurchaseOrderDetails = await _context.TblPurchaseOrderDetails
                      .Where(d => d.PurchaseId == Model.Id)
                      .ToListAsync();
                      _context.TblPurchaseOrderDetails.RemoveRange(PurchaseOrderDetails);
                      await _context.SaveChangesAsync();
                    //insert the new details and transactions and item transactions and item card details and cost center transactions
                    await InsertInvItems(Model);
                 await tx.CommitAsync();
                    }
                catch
                    {
                    await tx.RollbackAsync();
                    throw;
                    }
            });
            }
     
        }
    }