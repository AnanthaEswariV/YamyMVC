namespace YamyProject.Core.ViewModel
    {
    public class CustomerSalesDetailRowViewModel
        {
        public int Id { get; set; }              
        public string Type { get; set; } = "";     
        public DateOnly Date { get; set; }          
        public string Num { get; set; } = "";      
        public string CustomerName { get; set; } = "";
        public string Item { get; set; } = "";      
        public decimal? Qty { get; set; }
        public decimal? Price { get; set; }
        public decimal? Amount { get; set; }
        }
    }
