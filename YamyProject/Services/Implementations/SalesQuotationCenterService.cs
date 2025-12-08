namespace YamyProject.Services.Implementations
    {
    public class SalesQuotationCenterService(YamyDbContext context, IListServices listServices, IHttpContextAccessor httpContextAccessor, IGlobalService GlobalService) : ISalesQuotationCenterService
        {
        private string Customer ;
        private DateOnly Starting = default;
        private DateOnly Ending = default;
        private string PayMethod ;
        private readonly YamyDbContext _context = context;
        private readonly IListServices _ListServices = listServices;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IGlobalService _GlobalService = GlobalService;

        //the main method to get sales with filters
        public async Task<IEnumerable<SalesCenterViewModel>> GetSalesAsync(string selectCustomer = null, bool Custmer = true, DateOnly From = default, DateOnly To = default, bool Date = true, string selectionMethod = "Default", string selectionMethodPay = null, bool Pay = true)
            {
            if (Custmer == true)
                Customer = selectCustomer;
            if (!Date == true)
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

        //the default report method
        public async Task<IEnumerable<SalesCenterViewModel>> GetDefaultReportAsync()
            {
            //var transactions = await _context.TblTransactions.ToListAsync();

            var query = _context.TblSalesQuotations
              .Where(s => s.State == 0)
              .Include(s => s.Customer)
              .OrderBy(s => s.Date)
              .AsQueryable();

            // Apply Customer filter if provided
            if (!string.IsNullOrEmpty(Customer))
                query = query.Where(s => s.CustomerId == int.Parse(Customer) || s.Customer.Name == Customer);

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
                 // make it nullable so MAX works
                 CustomerName = s.Customer != null
            ? EF.Functions.Collate(s.Customer.Code + " - " + s.Customer.Name, Collation) : "",
                 PayMethod = s.PaymentMethod,
                 s.TranferStatus,
                 s.Total,
                 s.Vat,
                 s.Net
                 })
             .GroupBy(x => new
                 {
                 x.Id,
                 x.Date,
                 x.InvNo,
                 x.CustomerName,
                 x.PayMethod,
                 x.Total,
                 x.TranferStatus,
                 x.Vat,
                 x.Net
                 })
             .Select(g => new
                 {
                 g.Key.Id,
                 g.Key.Date,                 
                 INV_NO = g.Key.InvNo,
                 //     Jv_No = "000" + (g.Max(x => x.TransactionId) ?? 0),
                 Customer_Name = g.Key.CustomerName,
                 Payment_Method = g.Key.PayMethod,
                 g.Key.TranferStatus,
                 g.Key.Total,
                 g.Key.Vat,
                 g.Key.Net
                 })
             .OrderBy(r => r.Date).ToListAsync();
            var result = new List<SalesCenterViewModel>();

            int sn = 1;
            foreach (var s in sales)
                {
                result.Add(new SalesCenterViewModel
                    {
                    SN = sn,
                    Date = s.Date,
                    Id = s.Id,
                    InvoiceId = s.INV_NO,
                    CustomerName = s.Customer_Name,
                    PaymentMethod = s.Payment_Method,
                    TranferStatus=(int)s.TranferStatus,
                    Total = s.Total,
                    Vat = s.Vat,
                    Net = s.Net,
                    });
                sn++;
                }
            // result.ForEach(x => x.Customers = customerSelectList);
            return result;
            }
        //the detailed report method
        public async Task<IEnumerable<SalesCenterViewModel>> GetDetailedReportAsync()
            {
            var query = _context.TblSalesQuotations
                .Where(s => s.State == 0)
                .Include(s => s.SalesQuotationDetails)
                .ThenInclude(d => d.Items)
                .Include(s => s.Customer)
                .OrderBy(s => s.Date)
                .AsQueryable();

            // Apply Customer filter if provided
            if (!string.IsNullOrEmpty(Customer))
                query = query.Where(s => s.CustomerId == int.Parse(Customer) || s.Customer.Name == Customer);

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
                    Details = _context.TblSalesQuotationDetails
                                 .Where(sd => sd.SalesId == s.Id)
                                 .Select(sd => new
                                     {
                                     SalesDetails = sd,
                                     Item = _context.TblItems.FirstOrDefault(i => i.Id == sd.ItemId)
                                     }).ToList(),

                    CustomerName = s.Customer != null
                   ? EF.Functions.Collate(s.Customer.Code, Collation)
                     + " - "
                     + EF.Functions.Collate(s.Customer.Name, Collation)
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
        Customer_Name = x.CustomerName,
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
        r.Customer_Name,
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
        r.Customer_Name,
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

            var result = new List<SalesCenterViewModel>();
            int sn = 1;
            foreach (var s in sales)
                {
                foreach (var sd in s.Details)
                    {
                    result.Add(new SalesCenterViewModel
                        {
                        SN = sn++,
                        Date = s.Sale.Date,
                        Id = s.Sale.Id,
                        InvoiceId = s.Sale.InvoiceId,
                        CustomerName = s.CustomerName,
                        PaymentMethod = s.Sale.PaymentMethod,
                        Total = s.Sale.Total,
                        TranferStatus=(int)s.Sale.TranferStatus,
                        Vat = s.Sale.Vat,
                        Net = s.Sale.Net,
                        ItemName = sd.Item != null ? sd.Item.Code + " - " + sd.Item.Name : "",
                        Qty = sd.SalesDetails.Qty,
                        Price = sd.SalesDetails.Price,
                        ItemVat = sd.SalesDetails.Vat,
                        ItemTotal = sd.SalesDetails.Total
                        });
                    }
                sn++;
                }
            return result;
            }
        //  method to generate invoice number
        public async Task<string> GenerateInvoiceNoAsync()
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue =await _context.TblSalesQuotations
               .Select(s => s.InvoiceId.Substring(3))
               .MaxAsync(); 
            prefix = lastCodeValue ?? prefix;

            return $"QU-{int.Parse(prefix ) + 1:D4}";
            }
        //method to map sales proforma to view model
        private static TaxInvoiceViewModel MapToViewProformaModel(TblSalesProforma s)
            {
            // Coalesce details to an empty list to avoid NRE on OrderBy/Select
            var details = s.SalesProformaDetails ?? new List<TblSalesProformaDetail>();

            return new TaxInvoiceViewModel
                {
                Id = s.Id,
                CustomerId = s.CustomerId,
                CustomerCode = s.Customer?.Code,
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
                PONO = s.InvoiceId,
                TotalBeforeVat = s.Total,
                TotalVat = s.Vat,
                TotaAmount = s.Net,
                Items = s.SalesProformaDetails
                    .OrderBy(d => d.Id)
                    .Select(d =>
                    {
                        // If your schema has nullable decimals, coalesce to 0m as needed.
                        var qty = d.Qty;
                        var price = d.Price;
                        var vatAmt = d.Vatp;   // amount (keep as-is if that’s your schema)
                        var netPrice = (price * qty) + vatAmt;

                        return new SalesRowDataViewModel
                            {
                            Id = d.Id,
                            ItemId = d.ItemId ?? 0,                         // if non-nullable in DB, make model int (not int?) and remove ??
                            ItemCode = d.Items.Code ?? string.Empty,       // null-safe
                            ItemName = d.Items.Name ?? string.Empty,       // null-safe
                            Method = d.Items.Method ?? string.Empty,       // null-safe
                            Type = d.Items.Type ?? string.Empty,       // null-safe
                            QTY = qty,
                            Price = price,
                            NetPrice = netPrice,
                            VatPersint = d.Vat,                                  // percent (if that’s your schema)
                            VatAmonut = vatAmt,                                 // amount
                            Amount = d.Total,
                            CostPrice = d.CostPrice,
                            WarehouseId = d.Items.WarehouseId,                  // if VM property is int? use: it?.WarehouseId
                            CostCenterId = d.CostCenterId                          // if nullable in DB, use: d.CostCenterId ?? 0
                            };
                    }).ToList() };
            }

        //method to get sales proforma data by id
        public async Task<TaxInvoiceViewModel> GetSalesProformaDataAsync(int id)
            {
            var sale = await _context.TblSalesProformas
                .Include(s => s.SalesProformaDetails)
                .ThenInclude(d => d.Items)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sale == null) throw new KeyNotFoundException($"Sale {id} not found.");
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
            var vm = MapToViewProformaModel(sale);
            vm.IdFromOtherTybe = id;
            vm.Customers = customerSelectList;
            vm.Warehouses = WarehouseSelectList;
            vm.Accounts = AccountList;
            vm.CostCenter = CostCenterList;
            vm.Vat = VatList;
            return vm;
            }

        //method to create quotation from proforma + New quotation
        public async Task CreateQuotationCenterAsync(TaxInvoiceViewModel vm)
            {
            decimal totalBeforeVat = 0m, totalVat = 0m, netAmount = 0m;
            foreach (var r in vm.Items.Where(i => i != null))
                {
                if (r.ItemId != null && r.ItemId != 0)
                    {
                    var linePrice = r.Price * r.QTY;
                    var lineDisc = r.Disc;
                    var lineBase = linePrice - lineDisc;
                    var vatPct = (r.VatPersint) / 100m;

                    var lineVat = (decimal)(lineBase * vatPct);
                    var lineTotal = lineBase + lineVat;

                    totalBeforeVat += (decimal)lineBase;
                    totalVat += lineVat;
                    netAmount += (decimal)lineTotal;
                    }
                }  // 2) Generate invoice no (async)
            var invoiceNo = await GenerateInvoiceNoAsync() ;

            // 3) Strategy + Transaction
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    // 4) Header

                    var sale = new TblSalesQuotation
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
                        CreatedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0,
                        CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        State = 0,
                        Description = vm.Description ?? string.Empty
                        };

                    _context.TblSalesQuotations.Add(sale);
                    await _context.SaveChangesAsync(); // need sale.Id

                    var saleId = sale.Id;
                    if (saleId == 0)
                        throw new InvalidOperationException("Failed to create sale record.");
                    if (vm.From == "SP")
                        {
                        var SalesProforma = await _context.TblSalesProformas.FirstOrDefaultAsync(p => p.Id == vm.IdFromOtherTybe);
                        SalesProforma.TranferStatus = 1;
                        await _context.SaveChangesAsync();
                        }
                    var details = new List<TblSalesQuotationDetail>();

                    foreach (var r in vm.Items.Where(i => i != null && i.ItemId.GetValueOrDefault() > 0))
                        {
                        if (r.ItemId != null && r.ItemId != 0)
                            {
                            var qty = r.QTY;
                            var price = r.Price;
                            var vatPct = r.VatPersint;
                            var lineBase = (price * qty);
                            var lineVat = (lineBase * (vatPct / 100m));
                            var lineTot = lineBase + lineVat;
                            details.Add(new TblSalesQuotationDetail
                                {
                                SalesId = saleId,
                                ItemId = r.ItemId,
                                Qty = qty,
                                Discount = 0,
                                CostPrice = r.CostPrice,
                                Price = price,
                                Vatp = lineVat,
                                Vat = (int)vatPct,
                                Total = lineTot
                                });
                            }
                        }
                    if (details.Count == 0)
                        throw new InvalidOperationException("No valid detail rows to save (ItemId missing).");
                    _context.TblSalesQuotationDetails.AddRange(details);
                    await _context.SaveChangesAsync();
                    _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Add Quotation", "Quotation", saleId, "Added Quotation: " + invoiceNo);
                    await tx.CommitAsync();
                    }
                catch (Exception ex)
                    {
                    await tx.RollbackAsync();
                    throw new InvalidOperationException($"CreateQuotation failed: {ex.GetBaseException().Message}", ex);
                    }
            });
            }

        public async Task<TaxInvoiceViewModel> GetEditAsync(int id, string formType = "")
            {
            var vm = new TaxInvoiceViewModel();

            if (formType == "SP")
                {
                var sale = await _context.TblSalesProformas
                                   .Include(s => s.SalesProformaDetails)
                                   .ThenInclude(d => d.Items)
                                   .Include(s => s.Customer)
                                   .FirstOrDefaultAsync(s => s.Id == id);
                vm = MapProforamToViewModel(sale);

                }
            else
                {
                var sale = await _context.TblSalesQuotations
                    .Include(s => s.SalesQuotationDetails)
                    .ThenInclude(d => d.Items)
                    .Include(s => s.Customer)
                    .FirstOrDefaultAsync(s => s.Id == id);
                 vm = MapToViewModel(sale);
                }
          //  if (sale == null) throw new KeyNotFoundException($"Sale {id} not found.");

            var Warehouse = await _ListServices.GetWarehousesAsync();
            var WarehouseSelectList = Warehouse.Select(c => new TblWarehouse
                {
                Id = c.Id,
                Name = c.Name
                }).ToList();
            var customers = await _ListServices.GetCustomersAsync();
            var customerSelectList = customers.Select(c => new TblCustomer {
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
            return vm;
            }
        private static TaxInvoiceViewModel MapProforamToViewModel(TblSalesProforma s)
            {
            // Coalesce details to an empty list to avoid NRE on OrderBy/Select
            var details = s.SalesProformaDetails ?? new List<TblSalesProformaDetail>();

            return new TaxInvoiceViewModel
                {
                From = "SP",
                IdFromOtherTybe = s.Id,
                CustomerId = s.CustomerId,
                CustomerCode = s.Customer?.Code,
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
                Items = s.SalesProformaDetails
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
                            CostCenterId = d.CostCenterId                         // if nullable in DB, use: d.CostCenterId ?? 0
                            };
                    }).ToList()
                };
            }
        private static TaxInvoiceViewModel MapToViewModel(TblSalesQuotation s)
            {
            // Coalesce details to an empty list to avoid NRE on OrderBy/Select
            var details = s.SalesQuotationDetails ?? new List<TblSalesQuotationDetail>();

            return new TaxInvoiceViewModel
                {
                Id = s.Id,
                Invoce = s.InvoiceId,
                CustomerId = s.CustomerId,
                CustomerCode = s.Customer?.Code,
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
                PONO = s.InvoiceId,
                TotalBeforeVat = s.Total,
                TotalVat = s.Vat,
                TotaAmount = s.Net,
                Items = s.SalesQuotationDetails
                    .OrderBy(d => d.Id)
                    .Select(d =>
                    {
                        // If your schema has nullable decimals, coalesce to 0m as needed.
                        var qty = d.Qty;
                        var price = d.Price;
                        var vatAmt = d.Vatp;   // amount (keep as-is if that’s your schema)
                        var netPrice = (price * qty) + vatAmt;

                        return new SalesRowDataViewModel
                            {
                            Id = d.Id,
                            ItemId = d.ItemId ?? 0,                         // if non-nullable in DB, make model int (not int?) and remove ??
                            ItemCode = d.Items.Code ?? string.Empty,       // null-safe
                            ItemName = d.Items.Name ?? string.Empty,       // null-safe
                            Method = d.Items.Method ?? string.Empty,       // null-safe
                            Type = d.Items.Type ?? string.Empty,       // null-safe
                            QTY = qty,
                            Price = price,
                            NetPrice = netPrice,
                            VatPersint = d.Vat,                                  // percent (if that’s your schema)
                            VatAmonut = vatAmt,                                 // amount
                            Amount = d.Total,
                            CostPrice = d.CostPrice,
                            WarehouseId = d.Items.WarehouseId,                  // if VM property is int? use: it?.WarehouseId
                            CostCenterId = d.CostCenterId                          // if nullable in DB, use: d.CostCenterId ?? 0
                            };
                    }).ToList()
                };
            }
        public async Task UpdateTaxInvoiceAsync(TaxInvoiceViewModel Model)
            {
            decimal paidAmount = 0;
            decimal changeAmount = 0;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    var Sales = _context.TblSalesQuotations.Find(Model.Id);
                    if (Sales == null)
                        { }
                    Sales.Date = Model.Date;
                    Sales.CustomerId = Model.CustomerId ?? 0;
                    Sales.InvoiceId = Model.Invoce;
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
                        Sales.ModifiedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                        }
                    else
                        {
                        Sales.ModifiedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                        Sales.ModifiedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                        }

                    Sales.Pay = paidAmount;
                    Sales.Change = changeAmount;
                    await _context.SaveChangesAsync();

                    var SalesDetails = await _context.TblSalesQuotationDetails
                        .Where(d => d.SalesId == Model.Id)
                        .ToListAsync();
                    _context.TblSalesQuotationDetails.RemoveRange(SalesDetails);
                    await _context.SaveChangesAsync();

                    var details = new List<TblSalesQuotationDetail>();

                    foreach (var r in Model.Items.Where(i => i != null && i.ItemId.GetValueOrDefault() > 0))
                        {
                        if (r.ItemId != null && r.ItemId != 0)
                            {
                            var qty = r.QTY;
                            var price = r.Price;
                            var vatPct = r.VatPersint;
                            var lineBase = (price * qty);
                            var lineVat = (lineBase * (vatPct / 100m));
                            var lineTot = lineBase + lineVat;
                            details.Add(new TblSalesQuotationDetail
                                {
                                SalesId = Model.Id,
                                ItemId = r.ItemId,
                                Qty = qty,
                                Discount = 0,
                                CostPrice = r.CostPrice,
                                Price = price,
                                Vatp = lineVat,
                                Vat = (int)vatPct,
                                Total = lineTot
                                });
                            }
                        }
                    if (details.Count == 0)
                        throw new InvalidOperationException("No valid detail rows to save (ItemId missing).");
                    _context.TblSalesQuotationDetails.AddRange(details);
                    await _context.SaveChangesAsync();
                    _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Update Quotation", "Quotation", Model.Id, "Added Quotation: " + Model.Invoce);


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