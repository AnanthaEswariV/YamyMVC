namespace YamyProject.Core.ViewModel
{
    public class ItemStockSettlementDetailViewModel
    {
        public int SN { get; set; }
        public int? SettlementId { get; set; }
        public DateTime? Date { get; set; }
        public string? InvNo { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public decimal? Qty { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? NewOnHand { get; set; }
        public decimal? MinusAmount { get; set; }
        public decimal? PlusAmount { get; set; }
    }
}
