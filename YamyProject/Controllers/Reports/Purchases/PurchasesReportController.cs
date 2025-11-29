namespace YamyProject.Controllers.Reports.Purchases
    {
    public class PurchasesReportController(IPurchaseReportServices purchaseReportServices) : Controller
        {
        private IPurchaseReportServices _purchaseReportServices= purchaseReportServices;
        public async Task<IActionResult> PurchaseByVendorSummary()
            {
            var PurchaseByVendorSummary =await _purchaseReportServices.GetPurchaseByVendorSummaryAsync();
            return View(PurchaseByVendorSummary);
            }
        public IActionResult PurchaseByVendorSummaryDetails(int Id)
            {
            var PurchaseByVendorSummaryDetails = _purchaseReportServices.GetVendorAndSubcontractorPurchaseDetailsAsync(Id);
            return View(PurchaseByVendorSummaryDetails);
            }
        public IActionResult PurchaseBySubcontractorSummary()
            {
            var PurchaseBySubcontractorSummary =_purchaseReportServices.GetPurchaseBySubcontractorSummaryAsync();
            return View(PurchaseBySubcontractorSummary);
            }
        public IActionResult PurchaseBySubcontractorSummaryDetails(int Id)
            {
            var PurchaseBySubcontractorSummaryDetails = _purchaseReportServices.GetVendorAndSubcontractorPurchaseDetailsAsync(Id);
            return View(PurchaseBySubcontractorSummaryDetails);
            }
      
        public async Task<IActionResult> PurchaseByItemsSummary()
            {
            return View();
            }

        [HttpGet]
        public async Task<IActionResult> ItemSummary(string? dateFilter, DateOnly? fromDate, DateOnly? toDate, string? showColumns, string? sortBy)
            {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var (from, to) = ResolveDateRange(dateFilter, fromDate, toDate, today);

            var rows = await _purchaseReportServices.GetPurchaseByItemsSummaryAsync(null, null, from, to);

            var vm = new ItemSalesReportViewModel
                {
                DateFilter = dateFilter ?? "All",
                FromDate = from,
                ToDate = to,
                ShowColumns = showColumns ?? "Default",
                SortBy = sortBy ?? "Item Name",
                ItemRows = rows
                };

            return View("PurchaseByItemsSummary", vm); // view name: ItemSummary.cshtml (below)
            }
        [HttpGet]
        public async Task<IActionResult> LoadRows(int level, string key, DateOnly fromDate, DateOnly toDate)
            {
            var rows = await _purchaseReportServices.GetPurchaseByItemsSummaryAsync(level, key, fromDate, toDate);
            return Json(rows);
            }

        private static (DateOnly from, DateOnly to) ResolveDateRange(string? filter, DateOnly? fromDate, DateOnly? toDate, DateOnly today)
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

        public async Task<IActionResult> PurchaseByItemsSummaryDetails(int Id, string? dateFilter, DateOnly? dateFrom, DateOnly? dateTo)
            {
            var model = await _purchaseReportServices
                  .GetPurchaseByItemDetailsAsync(Id, dateFilter, dateFrom, dateTo);

            return View(model);
            }
        }
    }
