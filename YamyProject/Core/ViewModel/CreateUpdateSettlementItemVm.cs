namespace YamyProject.Core.ViewModel
{
    public class CreateUpdateSettlementItemVm
    {
        public int? Id { get; set; }
        public int ItemId { get; set; }
        public decimal OnHand { get; set; }
        public decimal Price { get; set; }
        public decimal NewOnHand { get; set; }
        public decimal MinusAmount { get; set; }
        public decimal PlusAmount { get; set; }
    }
}
