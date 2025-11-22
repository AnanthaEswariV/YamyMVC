namespace YamyProject.Controllers.Reports.Company
    {
    public class IncomeSummaryController(IIncomeSummaryServices income,IListServices listServices) : Controller
        {
        private readonly IIncomeSummaryServices _income = income;
        private readonly IListServices _listServices = listServices;
        public IActionResult Index()
            {
            return View();
            }
        public async Task<IActionResult> IncomeByCustomerSummary()
            {
            var IncomeSummary =await _income.GetIncomeByCustomerSummary();
            return View("IncomeByCustomerSummary",new IncomeSummaryViewModel
                {
                Customer= IncomeSummary.ToList()
                });
            }     
        public async Task<IActionResult> IncomeByVendorSummary()
            {
            var IncomeSummary = await _income.BuildIncomeByVendorSummary();
            return View("IncomeByVendorsSummary", new IncomeByVendorSummaryViewModel
                {
                Rows = IncomeSummary.ToList()
                });
            }

        public async Task<IActionResult> IncomeByCustomerDetail(int Id)
            {
            var IncomeDetails = await _income.GetIncomeByCustomerDetail(Id);

            return View("IncomeByCustomerDetail", new IncomeByCustomerReportViewModel
                {
                Rows = IncomeDetails.ToList()
                });
            }
        public async Task<IActionResult> IncomeByVendorDetail(int Id)
            {
            var IncomeDetails = await _income.GetIncomeByVendorDetail(Id);

            return View("IncomeByVendorDetail", new IncomeByCustomerReportViewModel
                {
                Rows = IncomeDetails.ToList()
                });
            }
        public async Task<IActionResult> ProfitandLoss()
            {
            var ProfitandLoss = await _income.GetEquityWithFinalBalanceAsync();
            return View(ProfitandLoss);
            }
        public async Task<IActionResult> CashFlowStatement()
            {
            var CashFlowStatement = await _income.GetCashFlowStatementAsync();
            return View(new CashFlowStatementViewModel
                {
                Rows = CashFlowStatement.ToList()
                });
            }
        public async Task<IActionResult> IncomeExpenseStatement(DateOnly? startDate, DateOnly? endDate)
            {
            var from = startDate;//?? DateOnly.Today.AddMonths(-1).Date;
            var to = endDate;//?? DateOnly.Today.Date;

            var IncomeExpenseStatement = await _income.GetIncomeExpenseStatementAsync();
            return View(IncomeExpenseStatement);
            }
        public async Task<IActionResult> UserActivity()
            { 
            var User = await _listServices.GetUsersAsync();
            var UserList = User.Select(c => new TblSecUser
                {
                Id = c.Id,
                UserName = c.UserName
                }).ToList();
            var UserActivity = await _income.GetUserActivityAsync();
            return View(new UserActivityReportViewModel
                {
                Users = UserList,
                Rows = UserActivity.ToList()
                });
            
           // return View();
            }
        }
    }