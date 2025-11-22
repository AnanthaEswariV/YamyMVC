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
        public async Task<IActionResult> SalesByItemsSummary()
            {
            return View();
            }

        }
    }
