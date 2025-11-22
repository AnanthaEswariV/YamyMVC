namespace YamyProject.Core.ViewModel
    {
    public class CustomerSalesRowViewModel
        {
        public int Id { get; set; }              // transaction id
        public string Type { get; set; } = "";
        public DateOnly? Date { get; set; }
        public string Num { get; set; } = "";    // transaction_id (or any number you want)
        public string VoucherNo { get; set; } = "";
        public string Name { get; set; } = "";
        public string Terms { get; set; } = "30 Days";
        public DateOnly DueDate { get; set; }
        public int Aging { get; set; }           // days between today and Date
        public decimal OpeningBalance { get; set; }
        }
    }
