namespace YamyProject.Core.ViewModel
    {
    public class ItemInvoiceRowViewModel
        {
        public DateOnly Date { get; set; }
        public string Num { get; set; } = "";
        public string Memo { get; set; } = "";
        public string Customer { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        }
    }
