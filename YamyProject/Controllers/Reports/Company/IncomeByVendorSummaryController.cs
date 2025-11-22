namespace YamyProject.Controllers.Reports.Company
    {
    public class IncomeByVendorSummaryController(IIncomeSummaryServices income) : Controller
        {
        private readonly IIncomeSummaryServices _income = income;
        public IActionResult Index()
            {
            return View();
            }

        }
    }