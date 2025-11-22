namespace YamyProject.Core.ViewModel
    {
    public class CustomerSummaryBalanceDetailViewModel
        {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public bool ShowAll { get; set; } = true;

        public string CompanyName { get; set; } = "ARC MASTER CONTRACTING L.L.C.";
        public string ReportTitle { get; set; } = "Customer Balance Detail";
        public string ReportTitleVendor { get; set; } = "Vendor Balance Detail";
        public string SubTitle { get; set; } = "All Transaction";

        public List<CustomerSummaryBalanceDetailRowViewModel> Transactions { get; set; } = new();
        }
    }
