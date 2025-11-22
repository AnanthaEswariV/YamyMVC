namespace YamyProject.Core.ViewModel
    {
    public class IncomeByVendorSummaryViewModel
        {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool IsAll { get; set; }

        public List<IncomeByVendorSummaryRowViewModel> Rows { get; set; }
            = new();
        }
    }
