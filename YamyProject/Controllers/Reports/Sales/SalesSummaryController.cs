namespace YamyProject.Controllers.Reports.Sales
    {
    public class SalesSummaryController (ISalesReportServices salesReportServices) : Controller
        {
        private readonly ISalesReportServices _salesReportServices = salesReportServices;
        public async Task<IActionResult> SalesByCustomerSummary()
            {
            var SalesByCustomerSummary =await _salesReportServices.GetSalesByCustomerSummaryAsync();
            return View(SalesByCustomerSummary);
            }
        public async Task<IActionResult> SalesByCustomerSummaryDetails(int Id)
            {
            var SalesByCustomerSummaryDetails = await _salesReportServices.GetCustomerSalesDetailsAsync(Id);
            return View(SalesByCustomerSummaryDetails);
            }
  
        //public async Task<IActionResult> SalesByItemsSummary(int level, string key, DateOnly fromDate, DateOnly toDate)
        //    {
        //    var rows = await _salesReportServices.GetPurchaseByItemsSummaryAsync(level, key, fromDate, toDate);
        //    return Json(rows);
        //    }
        public async Task<IActionResult> SalesByItemsSummaryDetails(int Id,string? dateFilter,DateOnly? dateFrom,DateOnly? dateTo)
            {
            var model = await _salesReportServices
                 .GetSalesByItemDetailsAsync(Id,dateFilter, dateFrom, dateTo);

            return View(model);
            }

        [HttpPost]
        public async Task<IActionResult> SalesByItemDetails(SalesByItemDetailsViewModel filter)
            {
            var model = await _salesReportServices
                .GetSalesByItemDetailsAsync(filter.Id,filter.DateFilter, filter.DateFrom, filter.DateTo);

            return View(model);
            }
        //
        [HttpGet]
        public async Task<IActionResult> ItemSummary(string? dateFilter,DateOnly? fromDate,DateOnly? toDate,string? showColumns,string? sortBy)
            {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var (from, to) = ResolveDateRange(dateFilter, fromDate, toDate, today);

            var rows = await _salesReportServices.GetPurchaseByItemsSummaryAsync(null, null, from, to);

            var vm = new ItemSalesReportViewModel
                {
                DateFilter = dateFilter ?? "All",
                FromDate = from,
                ToDate = to,
                ShowColumns = showColumns ?? "Default",
                SortBy = sortBy ?? "Item Name",
                ItemRows = rows
                };

            return View("SalesByItemsSummary", vm); // view name: ItemSummary.cshtml (below)
            }
        [HttpGet]
        public async Task<IActionResult> LoadRows(int level, string key, DateOnly fromDate, DateOnly toDate)
            {
            var rows = await _salesReportServices.GetPurchaseByItemsSummaryAsync(level, key, fromDate, toDate);
            return Json(rows);
            }

        private static (DateOnly from, DateOnly to) ResolveDateRange(string? filter,DateOnly? fromDate,DateOnly? toDate,DateOnly today)
            {
            filter = filter ?? "All";

            if (string.Equals(filter, "Today", StringComparison.OrdinalIgnoreCase))
                return (today, today);

            if (string.Equals(filter, "This Week", StringComparison.OrdinalIgnoreCase))
                {
                var diff = (int)today.DayOfWeek;
                var startOfWeek = today.AddDays(-diff);
                var endOfWeek = startOfWeek.AddDays(6);
                return (startOfWeek, endOfWeek);
                }

            if (string.Equals(filter, "This Month", StringComparison.OrdinalIgnoreCase))
                {
                var start = new DateOnly(today.Year, today.Month, 1);
                var end = start.AddMonths(1).AddDays(-1);
                return (start, end);
                }

            if (string.Equals(filter, "This Year", StringComparison.OrdinalIgnoreCase))
                {
                var start = new DateOnly(today.Year, 1, 1);
                var end = new DateOnly(today.Year, 12, 31);
                return (start, end);
                }

            // Custom or All: use passed dates, or sensible defaults
            var from = fromDate ?? new DateOnly(today.Year, 1, 1);
            var to = toDate ?? today;
            return (from, to);
            }

        }
    }
