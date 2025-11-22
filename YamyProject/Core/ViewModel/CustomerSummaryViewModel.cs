namespace YamyProject.Core.ViewModel
    {
    public class CustomerSummaryViewModel
        {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool AllDates { get; set; }

        public List<CustomerSummaryRowViewModel> CustomerSummaries { get; set; } = new();
        }
    }
