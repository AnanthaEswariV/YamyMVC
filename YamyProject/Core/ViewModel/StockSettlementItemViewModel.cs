namespace YamyProject.Core.ViewModel
{
    public class StockSettlementItemViewModel
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public decimal Quantity { get; set; }
        public decimal NewQty { get; set; }
    }
}
