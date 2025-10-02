namespace YamyProject.Core.ViewModel
{
    public class ItemStockSettlementListVm
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; } = null!;
        public string ItemName { get; set; } = null;
        public string Quantity { get; set; } = null;
        public string CostPrice { get; set; } = null;
        public string NewOnHand { get; set; } = null;
        public DateTime? Date { get; set; }
        public int? WarehouseId { get; set; }
        public decimal? TotalPlus { get; set; }
        public decimal? TotalMinus { get; set; }

        // When the UI is in "Default" mode your SQL concatenated a JV NO:
        public string? JvNo { get; set; }

        // Items count (useful when not default mode)
        public int ItemsCount { get; set; }

    }
}
