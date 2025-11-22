namespace YamyProject.Core.ViewModel
    {
    public class ItemSalesReportViewModel
        {
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public List<ItemSalesRowViewModel> Rows { get; set; } = new();
        }
    }
