namespace YamyProject.Core.ViewModel
    {
    public class CustomerBalanceSummaryViewModel
        {
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public bool AllDates { get; set; } = true;

        public string CompanyName { get; set; } = "ARC MASTER";
        public DateTime GeneratedOn { get; set; } = DateTime.Now;

        // e.g. "Jan 01, 01"
        public string ColumnHeaderText { get; set; } = "Jan 01, 01";

        public List<CustomerBalanceRowViewModel> Rows { get; set; } = new();

        public decimal TotalBalance => Rows.Sum(r => r.Amount);
        }
    }
