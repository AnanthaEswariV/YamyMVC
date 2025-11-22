namespace YamyProject.Core.ViewModel
    {
    public class CashFlowStatementViewModel
        {
        public string? CompanyName { get; set; }
        public string? DateFilterMode { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public string? ShowColumns { get; set; }
        public string? SortBy { get; set; }

        public List<CashFlowRowViewModel> Rows { get; set; } = new();

        }
    }