namespace YamyProject.Core.ViewModel
    {
    public class IncomeByCustomerReportViewModel
        {
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public bool AllDates { get; set; } = true;
        public string CompanyName { get; set; } 
        public IList<IncomeByCustomerRowViewModel> Rows { get; set; } = new List<IncomeByCustomerRowViewModel>();

        }
    }