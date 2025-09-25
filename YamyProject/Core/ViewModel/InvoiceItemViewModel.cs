namespace YamyProject.Core.ViewModel
{
    public class InvoiceItemViewModel
    {
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public decimal CostPrice { get; set; }
        public decimal NewOnHand { get; set; }
        public decimal MinusAmount { get; set; }
        public decimal PlusAmount { get; set; }
    }
}
