namespace YamyProject.Core.ViewModel
{
    public class ItemStockSettlementItemVm
    {
        public int Id { get; set; }              // detail id
        public int ItemId { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public decimal? OnHand { get; set; }
        public decimal? Price { get; set; }
        public decimal? NewOnHand { get; set; }
        public decimal? MinusAmount { get; set; }
        public decimal? PlusAmount { get; set; }
    }
}
