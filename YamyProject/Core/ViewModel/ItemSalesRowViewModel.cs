namespace YamyProject.Core.ViewModel
    {
    public class ItemSalesRowViewModel
        {
        public string ItemType { get; set; }
        public string Category { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public decimal Amount { get; set; }
        public decimal PercentOfSales { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal Cogs { get; set; }
        public decimal AvgCogs { get; set; }
        public decimal GrossMargin { get; set; }
        public decimal MarginPercent { get; set; }
        }
    }
