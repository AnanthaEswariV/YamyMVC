namespace YamyProject.Core.ViewModel
{
    public class ItemStockSettlementIndexViewModel
    {
        public int SN { get; set; }
        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public string InvNo { get; set; } = string.Empty;
        public string? JvNo { get; set; }
    }
}
