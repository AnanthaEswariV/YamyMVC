namespace YamyProject.Services.Implementations
    {
    public class SalesReportServices(YamyDbContext context): ISalesReportServices
        {
        private readonly YamyDbContext _context = context;

        public async Task<SalesByCustomerSummaryViewModel> GetSalesByCustomerSummaryAsync(/*DateOnly? fromDate,DateOnly? toDate,string? invoiceType = null, string? sortBy = null*/)
            {
            var vm = new SalesByCustomerSummaryViewModel();
            // If you have multiple tables (tblName logic), you can switch here based on invoiceType.
           var  salesQuery = _context.TblSales
                .Include(s => s.Customer)
                .Where(s => s.State == 0);
            //  if (invoiceType =="Sales")
            //  if (invoiceType =="Quotation")
            //salesQuery = _context.TblSalesQuotations
            //    .Include(s => s.Customer)
            //    .Where(s => s.State == 0);
            //  if (invoiceType =="Performa")
            //salesQuery = _context.TblSalesProformas
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
            //// WHERE s.date >= @fromDate AND s.date <= @toDate
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
                .GroupBy(s => new { s.CustomerId, s.Customer.Name })
                .Select(g => new
                    {
                    g.Key.CustomerId,
                    CustomerName = g.Key.Name,
                    TotalSales = g.Sum(x => x.Net)
                    })
                .OrderBy(x => x.CustomerId) 
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
                    CustomerId = x.CustomerId,
                    Name = x.CustomerName,
                    NetSales = x.TotalSales
                    })
                .ToList();

         //   vm.DateFilter = BuildDateFilter(fromDate, toDate);

            return vm;
            }

        //private static string? BuildDateFilter(DateOnly? fromDate, DateOnly? toDate)
        //   {
        //   if (!fromDate.HasValue && !toDate.HasValue) return null;

        //   if (fromDate.HasValue && toDate.HasValue)
        //       {
        //       if (fromDate.Value == toDate.Value)
        //           return $"On {fromDate:yyyy-MM-dd}";

        //       return $"From {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}";
        //       }

        //   if (fromDate.HasValue)
        //       return $"From {fromDate:yyyy-MM-dd}";

        //   return $"To {toDate:yyyy-MM-dd}";
        //   }

        public async Task<CustomerSalesDetailListViewModel> GetCustomerSalesDetailsAsync(int customerId/*,DateTime startDate,DateTime endDate*/)
            {
            // Base query for sales headers
            var baseQuery = _context.TblSales
                .Where(s =>
                    s.CustomerId == customerId 
                    //&& s.State == 0 &&
                    //s.Date >= startDate &&
                    //s.Date <= endDate
                    );

            // Flatten to detail rows (no JOIN, only navigation + subquery)
            var rowsQuery = baseQuery
                .SelectMany(s => s.TblSalesDetails.Select(d => new CustomerSalesDetailRowViewModel
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
                    CustomerName = s.Customer.Name,

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
                CustomerId = customerId,
                CustomerName = rows.Select(r => r.CustomerName).FirstOrDefault() ?? "",
               // StartDate = startDate,
                //EndDate = endDate,
                Rows = rows
                };

            return vm;
            }
        }
    }
    