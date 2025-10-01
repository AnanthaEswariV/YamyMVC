namespace YamyProject.Core.ViewModel
{
    public class ItemViewModel
    {
        public int Id { get; set; }
        public string Method { get; set; } = null!;
        public string Type { get; set; } = null!;
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public decimal CostPrice { get; set; }
        public decimal Qty { get; set; }            // Calculated from item transactions
        public decimal? WarehouseQty { get; set; }  // From TblItemsWarehouse (optional)
    }
}
