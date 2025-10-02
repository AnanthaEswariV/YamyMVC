namespace YamyProject.Core.ViewModel
{
    public class CreateUpdateSettlementViewmodel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public int? WarehouseId { get; set; }
        

        public List<StockSettlementItemVm> Items { get; set; } = new List<StockSettlementItemVm>();
        public List<TblItem> Item { get; set; } = new List<TblItem>();
        // Dropdowns
        public List<TblWarehouse> WarehousesVm { get; set; } = new List<TblWarehouse>();

        public decimal TotalMinus => Items.Sum(x => x.MinusAmount);
        public decimal TotalPlus => Items.Sum(x => x.PlusAmount);
    }
}
