namespace YamyProject.Core.ViewModel
    {
    public class CustomerBalanceDetailsRowViewModel
        {
        public string Type { get; set; } = "";
        public string TransactionId { get; set; } = "";
        public DateOnly? Date { get; set; }
        public string Num { get; set; } = "";
        public string Account { get; set; } = "";
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        }
    }
