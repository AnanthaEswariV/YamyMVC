namespace YamyProject.Controllers.Reports.CustomerAndReceivable
    {
    public class CustomerAndReceivableController (ICustomerSummaryService customerSummary) : Controller
        {
        public ICustomerSummaryService _customerSummary = customerSummary;
      
        public IActionResult CustomerSummary()
            {
           
            var customerSummary = _customerSummary.GetCustomerBalancesAsync();

            return View(new CustomerSummaryViewModel
                {
                CustomerSummaries = customerSummary.Result
                });
            }
        public IActionResult CustomerSummaryDetails(int Id)
            {
            var customerDetails = _customerSummary.GetCustomerStatementAsync(Id);
            return View(new CustomerSummaryBalanceDetailViewModel
                {
                Transactions = customerDetails.Result
                });
            }
        public async Task<IActionResult> CustomerAgingSummary(DateOnly? dateFrom, DateOnly? dateTo ,bool AllDate)
            {
            if (AllDate)
                {
                dateFrom = null;
                dateTo = null;
                }
            var CustomerAgingSummary =await _customerSummary.GetCustomerAgingAsync(dateFrom, dateTo);
            return View(CustomerAgingSummary);
            }
        public async Task<IActionResult> CustomerAgingDetails(int id,DateOnly? dateFrom,DateOnly? dateTo)
            {
            var CustomerAgingDetails = await _customerSummary.GetCustomerSalesAsync(id, dateFrom, dateTo);

            return View(CustomerAgingDetails);
            }
        public async Task<IActionResult> CustomerBalanceSummary(DateOnly? dateFrom,DateOnly? dateTo)
            {
            var CustomerBalanceSummary = await _customerSummary.GetCustomerBalancesSummryAsync();
            return View(CustomerBalanceSummary);
            }
        public async Task<IActionResult> CustomerBalanceDetails(int Id)
            {
            var  CustomerBalanceDetails= await _customerSummary.GetCustomerDetailsStatementAsync(Id);
            return View(CustomerBalanceDetails);
            }
        }
    }