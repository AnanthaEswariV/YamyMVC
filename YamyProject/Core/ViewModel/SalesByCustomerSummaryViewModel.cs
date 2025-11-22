namespace YamyProject.Core.ViewModel
    {
    public class SalesByCustomerSummaryViewModel
        {
        public string? DateFilter { get; set; }
        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        public string? InvoiceType { get; set; }
        public string? SortBy { get; set; }

        public List<SalesByCustomerSummaryRowViewModel> Rows { get; set; } = new();
        public decimal TotalNetSales => Rows.Sum(r => r.NetSales);
        }
    }