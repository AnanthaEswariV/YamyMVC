namespace YamyProject.Core.ViewModel
    {
    public class CustomerAgingSummaryViewModel
        {
        public bool AllDate { get; set; }=true;
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public string? CompanyName { get; set; }
        public List<CustomerAgingRowViewModel> Rows { get; set; } = new();

        public decimal? TotalCurrent => Rows.Sum(r => r.Current);
        public decimal? Total1To30 => Rows.Sum(r => r.Days1To30);
        public decimal? Total31To60 => Rows.Sum(r => r.Days31To60);
        public decimal? Total61To90 => Rows.Sum(r => r.Days61To90);
        public decimal? Total91Plus => Rows.Sum(r => r.Days91Plus);
        public decimal? GrandTotal => Rows.Sum(r => r.Total);
        }
    }
