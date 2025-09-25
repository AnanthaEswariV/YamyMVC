namespace YamyProject.Core.ViewModel
{
    public class CreateUpdateSettlementVm
    {
        public int? Id { get; set; }
        [Required]
        public string Code { get; set; } = "";
        public DateTime Date { get; set; }
        public int? WarehouseId { get; set; }

        public List<CreateUpdateSettlementItemVm> Items { get; set; } = new();
    }
}
