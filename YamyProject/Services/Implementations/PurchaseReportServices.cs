
namespace YamyProject.Services.Implementations
    {
    public class PurchaseReportServices(YamyDbContext context) : IPurchaseReportServices
        {
        private readonly YamyDbContext _context = context;

        // Purchase by Vendor Summary
        public async Task<SalesByCustomerSummaryViewModel> GetPurchaseByVendorSummaryAsync(/*DateOnly? fromDate,DateOnly? toDate,string? invoiceType = null, string? sortBy = null*/)
            {
            var vm = new SalesByCustomerSummaryViewModel();
            // If you have multiple tables (tblName logic), you can switch here based on invoiceType.
            var salesQuery = _context.TblPurchases
                 .Include(s => s.Vendors)
                 .Where(s => s.State == 0 && s.Vendors.Type== "Vendor");

            //  if (invoiceType =="Sales")
            //  if (invoiceType =="Quotation")
            //salesQuery = _context.TblSalesQuotations
            //    .Include(s => s.Customer)
            //    .Where(s => s.State == 0);
            //  if (invoiceType =="Order")
            //salesQuery = _context.TblSalesOrders
            //  .Include(s => s.Customer)
            //  .Where(s => s.State == 0);
            //  if (invoiceType =="Return")
            //salesQuery = _context.TblSalesReturns
            //  .Include(s => s.Customer)
            //  .Where(s => s.State == 0);
            //   FILTER BY DATE RANGE
            //   WHERE s.date >= @fromDate AND s.date <= @toDate
            //if (fromDate.HasValue)
            //    {
            //    var from = fromDate.Value.ToDateTime(TimeOnly.MinValue);
            //    salesQuery = salesQuery.Where(s => s.Date >= from);
            //    }

            //if (toDate.HasValue)
            //    {
            //    var to = toDate.Value.ToDateTime(TimeOnly.MaxValue);
            //    salesQuery = salesQuery.Where(s => s.Date <= to);
            //    }

            // GROUP BY c.id, c.name  +  SUM(s.net)
            var grouped = await salesQuery
                .GroupBy(s => new { s.VendorId, s.Vendors.Name })
                .Select(g => new
                    {
                    g.Key.VendorId,
                    vendorName = g.Key.Name,
                    TotalSales = g.Sum(x => x.Net)
                    })
                .OrderBy(x => x.VendorId)
                .ToListAsync();

            //// ORDER BY c.id (you can adjust with SortBy)
            //var ordered = sortBy?.ToLower() switch
            //    {
            //        "name" => grouped.OrderBy(x => x.CustomerName),
            //        "name_desc" => grouped.OrderByDescending(x => x.CustomerName),
            //        "netsales_desc" => grouped.OrderByDescending(x => x.TotalSales),
            //        "netsales" => grouped.OrderBy(x => x.TotalSales),
            //        _ => grouped.OrderBy(x => x.CustomerId) // default like SQL
            //        };

            // ROW_NUMBER() OVER (ORDER BY c.id) AS SN  -> do it in memory
            vm.Rows = grouped
                .Select((x, index) => new SalesByCustomerSummaryRowViewModel
                    {
                    Sn = index + 1,
                    CustomerId = x.VendorId,
                    Name = x.vendorName,
                    NetSales = x.TotalSales
                    })
                .ToList();

            //   vm.DateFilter = BuildDateFilter(fromDate, toDate);

            return vm;
            }

        // Purchase by Subcontractor Summary
        public async Task<SalesByCustomerSummaryViewModel> GetPurchaseBySubcontractorSummaryAsync(/*DateOnly? fromDate,DateOnly? toDate,string? invoiceType = null, string? sortBy = null*/)
            {
            var vm = new SalesByCustomerSummaryViewModel();
            // If you have multiple tables (tblName logic), you can switch here based on invoiceType.
            var salesQuery = _context.TblPurchases
                 .Include(s => s.Vendors)
                 .Where(s => s.State == 0 && s.Vendors.Type== "Subcontractor");

            //  if (invoiceType =="Sales")
            //  if (invoiceType =="Quotation")
            //salesQuery = _context.TblSalesQuotations
            //    .Include(s => s.Customer)
            //    .Where(s => s.State == 0);
            //  if (invoiceType =="Order")
            //salesQuery = _context.TblSalesOrders
            //  .Include(s => s.Customer)
            //  .Where(s => s.State == 0);
            //  if (invoiceType =="Return")
            //salesQuery = _context.TblSalesReturns
            //  .Include(s => s.Customer)
            //  .Where(s => s.State == 0);
            //   FILTER BY DATE RANGE
            //   WHERE s.date >= @fromDate AND s.date <= @toDate
            //if (fromDate.HasValue)
            //    {
            //    var from = fromDate.Value.ToDateTime(TimeOnly.MinValue);
            //    salesQuery = salesQuery.Where(s => s.Date >= from);
            //    }

            //if (toDate.HasValue)
            //    {
            //    var to = toDate.Value.ToDateTime(TimeOnly.MaxValue);
            //    salesQuery = salesQuery.Where(s => s.Date <= to);
            //    }

            // GROUP BY c.id, c.name  +  SUM(s.net)
            var grouped = await salesQuery
                .GroupBy(s => new { s.VendorId, s.Vendors.Name })
                .Select(g => new
                    {
                    g.Key.VendorId,
                    vendorName = g.Key.Name,
                    TotalSales = g.Sum(x => x.Net)
                    })
                .OrderBy(x => x.VendorId)
                .ToListAsync();

            //// ORDER BY c.id (you can adjust with SortBy)
            //var ordered = sortBy?.ToLower() switch
            //    {
            //        "name" => grouped.OrderBy(x => x.CustomerName),
            //        "name_desc" => grouped.OrderByDescending(x => x.CustomerName),
            //        "netsales_desc" => grouped.OrderByDescending(x => x.TotalSales),
            //        "netsales" => grouped.OrderBy(x => x.TotalSales),
            //        _ => grouped.OrderBy(x => x.CustomerId) // default like SQL
            //        };

            // ROW_NUMBER() OVER (ORDER BY c.id) AS SN  -> do it in memory
            vm.Rows = grouped
                .Select((x, index) => new SalesByCustomerSummaryRowViewModel
                    {
                    Sn = index + 1,
                    CustomerId = x.VendorId,
                    Name = x.vendorName,
                    NetSales = x.TotalSales
                    })
                .ToList();

            //   vm.DateFilter = BuildDateFilter(fromDate, toDate);

            return vm;
            }

        // purchase Details by Vendor And Subcontractor
        public async Task<CustomerSalesDetailListViewModel> GetVendorAndSubcontractorPurchaseDetailsAsync(int VendorId/*,DateTime startDate,DateTime endDate*/)
            {
            // Base query for sales headers
            var baseQuery = _context.TblPurchases
                .Where(s =>
                    s.VendorId == VendorId
                    && s.State == 0 //&&
                    //s.Date >= startDate &&
                    //s.Date <= endDate
                    );

            // Flatten to detail rows (no JOIN, only navigation + subquery)
            var rowsQuery = baseQuery
                .SelectMany(s => s.PurchaseDetails
                .Select(d => new CustomerSalesDetailRowViewModel
                    {
                    Id = s.Id,

                    Type = _context.TblTransactions
                        .Where(t => t.TransactionId == s.Id
                          && t.TType.Contains("sales"))
                        .OrderBy(t => t.Id)
                        .Select(t => t.Type)
                        .FirstOrDefault(),

                    Date = s.Date,
                    Num = s.InvoiceId,
                    CustomerName = s.Vendors.Name,

                    // Detail + Item navigation
                    Item = d.Items.Code + " - " + d.Items.Name,
                    Qty = d.Qty,
                    Price = d.Price,
                    Amount = d.Total
                    }))
                .OrderBy(r => r.Date);

            var rows = await rowsQuery.ToListAsync();

            var vm = new CustomerSalesDetailListViewModel
                {
                CustomerId = VendorId,
                CustomerName = rows.Select(r => r.CustomerName).FirstOrDefault() ?? "",
                // StartDate = startDate,
                //EndDate = endDate,
                Rows = rows
                };

            return vm;
            }

        // Purchase by Items Summary
        public async Task<List<ItemSalesReportRowViewModel>> GetPurchaseByItemsSummaryAsync( int? level,string? key,DateOnly startDate,DateOnly endDate)
            {
            var result = new List<ItemSalesReportRowViewModel>();

            // Convert DateOnly to DateTime for EF comparisons
            var from = startDate.ToDateTime(TimeOnly.MinValue);
            var to = endDate.ToDateTime(TimeOnly.MaxValue);
            if (!level.HasValue || level == 0)
                {
                var types = await _context.TblItems
                    .Select(i => i.Type)
                    .Where(t => t != null && t != "")
                    .Distinct()
                    .OrderBy(t => t)
                    .ToListAsync();

                foreach (var type in types)
                    {
                    result.Add(new ItemSalesReportRowViewModel
                        {
                        State = "e",
                        LoadState = "u",
                        Level = 1,
                        Name = type,
                        Key = type // used later to expand categories
                        });
                    }

                return result;
                }

            // --------------------------------------
            // LEVEL 1 → Expand: type → categories
            // (equivalent to currLvl == 1 part in CellClick)
            // --------------------------------------
            if (level == 1 && !string.IsNullOrEmpty(key))
                {
                string type = key;

                var categories = await _context.TblItemCategories
                    .Where(c => c.Item.Type == type)
                    .OrderBy(c => c.Code)
                    .Select(c => new
                        {
                        c.Id,
                        CategoryName = c.Code + " - " + c.Name
                        })
                    .ToListAsync();

                foreach (var cat in categories)
                    {
                    result.Add(new ItemSalesReportRowViewModel
                        {
                        State = "e",
                        LoadState = "u",
                        Level = 2,
                        Name = cat.CategoryName,
                        Key = $"{cat.Id}|{type}" // same idea as catId + "|" + type
                        });
                    }

                return result;
                }

            // --------------------------------------
            // LEVEL 2 → Expand: category → items
            // (equivalent to currLvl == 2 part in CellClick)
            // --------------------------------------
            if (level == 2 && !string.IsNullOrEmpty(key))
                {
                var parts = key.Split('|');
                int catId = int.Parse(parts[0]);
                string itemType = parts[1];

                // Total Purchase for percentage calculation
                var totalSales = await _context.TblPurchaseDetails
                    .Where(sd =>
                        sd.Purchase.State == 0
                        // &&sd.Purchase.Date >= from &&
                        //sd.Purchase.Date <= to
                        )
                    .SumAsync(sd => (decimal?)sd.Total) ?? 0m;

                // Group by item and calculate aggregates
                var items = await _context.TblPurchaseDetails
                    .Where(sd =>
                        sd.Purchase.State == 0 &&
                        //  sd.Purchase.Date >= from &&
                        // sd.Purchase.Date <= to &&
                        sd.Items.CategoryId == catId &&
                        sd.Items.Type == itemType)
                    .GroupBy(sd => new { sd.ItemId, sd.Items.Name })
                    .Select(g => new
                        {
                        g.Key.ItemId,
                        ItemName = g.Key.Name,
                        Qty = g.Sum(x => x.Qty),
                        Amount = g.Sum(x => x.Total),
                        Cogs = g.Sum(x => x.CostPrice * x.Qty)
                        })
                    .OrderBy(x => x.ItemName)
                    .ToListAsync();

                foreach (var row in items)
                    {
                    var qty = row.Qty ?? 0;
                    var amount = row.Amount ?? 0;
                    var cogs = row.Cogs ?? 0;

                    var percentOfSales =
                        totalSales == 0 ? 0 : Math.Round(amount / totalSales * 100, 1);

                    var avgPrice =
                        qty == 0 ? 0 : Math.Round(amount / qty, 2);

                    var avgCogs =
                        qty == 0 ? 0 : Math.Round(cogs / qty, 2);

                    var grossMargin =
                        Math.Round(amount - cogs, 2);

                    var grossMarginPercent =
                        amount == 0 ? 0 : Math.Round((amount - cogs) / amount * 100, 1);

                    result.Add(new ItemSalesReportRowViewModel
                        {
                        State = "n",
                        LoadState = "l",
                        Level = 3,
                        Name = row.ItemName,
                        Key = row.ItemId.ToString(),
                        Qty = qty,
                        Amount = amount,
                        PercentOfSales = percentOfSales,
                        AvgPrice = avgPrice,
                        Cogs = cogs,
                        AvgCogs = avgCogs,
                        GrossMargin = grossMargin,
                        GrossMarginPercent = grossMarginPercent
                        });
                    }

                return result;
                }

            return result;
            }
        public async Task<SalesByItemDetailsViewModel> GetPurchaseByItemDetailsAsync(int Id, string? dateFilter, DateOnly? dateFrom, DateOnly? dateTo)
            {
            //   var (from, to) = ResolveDates(dateFilter, dateFrom, dateTo);

            // This is the EF version of your WinForms SQL:
            // tbl_sales_details + tbl_items + tbl_sales + tbl_item_category
            var query = _context.TblPurchaseDetails
                .Include(sd => sd.Items)
                    .ThenInclude(i => i.Category)
                .Include(sd => sd.Purchase)
                .Where(sd => sd.Purchase.State == 0 && sd.ItemId == Id);  // s.state = 0

            //if (from.HasValue)
            //    query = query.Where(sd => sd.Sales.Date >= from.Value);

            //if (to.HasValue)
            //    query = query.Where(sd => sd.Sales.Date <= to.Value);

            var flat = await query
                .Select(sd => new
                    {
                    ItemType = sd.Items.Type,               
                     sd.Items.CategoryId,         
                    CategoryCode = sd.Items.Category.Code, 
                    CategoryName = sd.Items.Category.Name, 
                    ItemId = sd.Items.Id,                  
                    ItemName = sd.Items.Name,              

                     sd.Purchase.Date,                    
                    Num = sd.Purchase.InvoiceId,          
                    Customer = sd.Purchase.BillTo,        
                    Memo = sd.Items.Type + " - " + sd.Items.Name + " - " + sd.Items.Category.Name,
                    sd.Qty,
                     sd.Price,
                    Amount = sd.Total,
                    Balance = (sd.Qty * sd.Price) - sd.Discount
                    })
                .OrderBy(x => x.ItemType)
                .ThenBy(x => x.CategoryCode)
                .ThenBy(x => x.ItemName)
                .ThenBy(x => x.Date)
                .ToListAsync();

            var model = new SalesByItemDetailsViewModel();
            // {
            // DateFilter = dateFilter
            //// DateFrom = from,
            //// DateTo = to
            // };
            model.Id = Id;
            model.Types = flat
                .GroupBy(x => x.ItemType)
                .Select(typeGroup => new ItemTypeGroupViewModel
                    {
                    TypeName = typeGroup.Key,
                    Categories = typeGroup
                        .GroupBy(c => new { c.CategoryId, c.CategoryCode, c.CategoryName })
                        .Select(catGroup => new ItemCategoryGroupViewModel
                            {
                            CategoryId = (int)catGroup.Key.CategoryId,
                            CategoryName = $"{catGroup.Key.CategoryCode} - {catGroup.Key.CategoryName}",
                            Items = catGroup
                                .GroupBy(i => new { i.ItemId, i.ItemName })
                                .Select(itemGroup => new ItemGroupViewModel
                                    {
                                    ItemId = itemGroup.Key.ItemId,
                                    ItemName = itemGroup.Key.ItemName,
                                    Rows = itemGroup.Select(r => new ItemInvoiceRowViewModel
                                        {
                                        Date = r.Date,
                                        Num = r.Num,
                                        Memo = r.Memo,
                                        Customer = r.Customer,
                                        Qty = (decimal)r.Qty,
                                        Price = (decimal)r.Price,
                                        Amount = (decimal)r.Amount,
                                        Balance = (decimal)r.Balance
                                        }).ToList()
                                    }).ToList()
                            }).ToList()
                    }).ToList();

            return model;
            }


        }
    }