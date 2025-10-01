namespace YamyProject.Core.ViewModel
{
    public class StockSettlementDetailViewModel
    {
        public int Id { get; set; }
        public int? SettleId { get; set; }
        public string ItemCode { get; set; }
        public string ItemType { get; set; }
        public string Method { get; set; }
        public decimal? OnHand { get; set; }
        [DataType(DataType.Currency)]
        public decimal? Price { get; set; }
        public decimal? NewOnHand { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Quantity must be a positive number")]
        public decimal? Qty { get; set; }
        [DataType(DataType.Currency)]
        public decimal? MinusAmount { get; set; }
        [DataType(DataType.Currency)]
        public decimal? PlusAmount { get; set; }
        public IEnumerable<SelectListItem>? Methods { get; set; } 
        public string? FormattedPrice => Price.HasValue ? $"{Price.Value:C2}" : "-";
    }
}
