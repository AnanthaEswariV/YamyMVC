namespace YamyProject.Core.ViewModel
{
    public class ItemStockSettlementDetailsVm
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public DateTime? Date { get; set; }
        public int? WarehouseId { get; set; }
        public decimal? TotalPlus { get; set; }
        public decimal? TotalMinus { get; set; }

        [ForeignKey("TblItemStockSettlement")] // points to navigation property
        [Column("id")] // database column name (or actual FK column)
        public int ItemStockSettlementId { get; set; }
        public List<ItemStockSettlementItemVm> Items { get; set; } = new();
    }
}
