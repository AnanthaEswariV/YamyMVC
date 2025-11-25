namespace YamyProject.Core.ViewModel
    {
    public class ItemSalesReportViewModel
        {
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public List<ItemSalesRowViewModel> Rows { get; set; } = new();
        //
        public string? DateFilter { get; set; } = "All";
        public string? ShowColumns { get; set; } = "Default";
        public string? SortBy { get; set; } = "Item Name";
        public List<ItemSalesReportRowViewModel> ItemRows { get; set; } = new();
        }
    }
