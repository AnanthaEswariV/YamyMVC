namespace YamyProject.Core.ViewModel
    {
    public class CustomerBalanceDetailViewModel
        {
        public string CompanyName { get; set; } = "";
        public string ReportTitle { get; set; } = "";
        public string ReportSubTitle { get; set; } = "";
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public bool AllDates { get; set; } = true;

        public List<CustomerBalanceDetailsRowViewModel> Rows { get; set; } = new();

        public decimal TotalBalance => Rows.Sum(r => r.Balance);
        }
    }
