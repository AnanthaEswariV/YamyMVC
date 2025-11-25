namespace YamyProject.Core.ViewModel
    {
    public class ItemSalesReportRowViewModel
        {
        public string State { get; set; } = "e";   
        public string LoadState { get; set; } = "u"; 
        public int Level { get; set; }             

        public string Name { get; set; } = "";     
        public string Key { get; set; } = "";      

        public decimal? Qty { get; set; }
        public decimal? Amount { get; set; }
        public decimal? PercentOfSales { get; set; }
        public decimal? AvgPrice { get; set; }
        public decimal? Cogs { get; set; }
        public decimal? AvgCogs { get; set; }
        public decimal? GrossMargin { get; set; }
        public decimal? GrossMarginPercent { get; set; }
        }
    }
