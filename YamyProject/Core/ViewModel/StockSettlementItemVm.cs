namespace YamyProject.Core.ViewModel
{
    public class StockSettlementItemVm
    {
        public int? Id { get; set; }
        public int ItemId { get; set; }
       
        public decimal CostPrice { get; set; }
        public decimal qty { get; set; }
        public string ItemCode { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public decimal OnHand { get; set; }
        public decimal Price { get; set; }
        public decimal NewOnHand { get; set; }
        public decimal QtyDiff => NewOnHand - OnHand;
        public decimal MinusAmount => QtyDiff < 0 ? Math.Abs(QtyDiff) * Price : 0;
        public decimal PlusAmount => QtyDiff > 0 ? QtyDiff * Price : 0;

    }
}
