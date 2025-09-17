namespace YamyProject.Core.ViewModel
{

    public class SalesItemViewModel
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal NetPrice { get; set; }
        public string Vat { get; set; }
        public decimal Amount { get; set; }
        public string CostCenter { get; set; }
    }

}
