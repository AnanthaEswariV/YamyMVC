namespace YamyProject.Core.ViewModel
{
    public class CreateUpdateSettlementVm
    {
        public int? Id { get; set; }
        [Required]
        public string Code { get; set; } = "";
        public DateOnly? Date { get; set; }
        public int? WarehouseId { get; set; }

        public IEnumerable<TblWarehouse> warehouse { get; set; }
        public IEnumerable<WarehouseViewModel> WarehousesVm { get; set; }


        public IEnumerable<CreateUpdateSettlementItemVm> Items { get; set; } = new List<CreateUpdateSettlementItemVm>();

    }
}
