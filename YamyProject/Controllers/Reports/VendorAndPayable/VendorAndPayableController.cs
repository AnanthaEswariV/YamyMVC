namespace YamyProject.Controllers.Reports.VendorAndPayable
    {
    public class VendorAndPayableController(ICustomerSummaryService customerSummary) : Controller
        {
        //IVendorSummaryService
        public ICustomerSummaryService _customerSummary = customerSummary;
     
        public IActionResult VendorSummary()
            {
            var customerSummary = _customerSummary.GetCustomerBalancesAsync();

            return View(new CustomerSummaryViewModel
                {
                CustomerSummaries = customerSummary.Result
                });
            }
        public IActionResult VendorSummaryDetails(int Id)
            {
            var customerDetails = _customerSummary.GetCustomerStatementAsync(Id);
            return View(new CustomerSummaryBalanceDetailViewModel
                {
                Transactions = customerDetails.Result
                });
            }
        public async Task<IActionResult> VendorAgingSummary(DateOnly? dateFrom, DateOnly? dateTo, bool AllDate)
            {
            if (AllDate)
                {
                dateFrom = null;
                dateTo = null;
                }
            var CustomerAgingSummary = await _customerSummary.GetCustomerAgingAsync(dateFrom, dateTo);
            return View(CustomerAgingSummary);
            }
        public async Task<IActionResult> VendorAgingDetails(int id, DateOnly? dateFrom, DateOnly? dateTo)
            {
            var CustomerAgingDetails = await _customerSummary.GetCustomerSalesAsync(id, dateFrom, dateTo);

            return View(CustomerAgingDetails);
            }
        public async Task<IActionResult> VendorBalanceSummary(DateOnly? dateFrom, DateOnly? dateTo)
            {
            var CustomerBalanceSummary = await _customerSummary.GetCustomerBalancesSummryAsync();
            return View(CustomerBalanceSummary);
            }
        public async Task<IActionResult> VendorBalanceDetails(int Id)
            {
            var CustomerBalanceDetails = await _customerSummary.GetCustomerDetailsStatementAsync(Id);
            return View(CustomerBalanceDetails);
            }

        }
    }
