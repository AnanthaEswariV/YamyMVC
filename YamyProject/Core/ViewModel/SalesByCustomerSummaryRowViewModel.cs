namespace YamyProject.Core.ViewModel
    {
    public class SalesByCustomerSummaryRowViewModel
        {
        public int Sn { get; set; }     
        public int CustomerId { get; set; }

        public string Name { get; set; } = "";
        public decimal NetSales { get; set; }
        }
    }
