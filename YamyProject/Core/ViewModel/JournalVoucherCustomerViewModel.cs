namespace YamyProject.Core.ViewModel
    {
    public class JournalVoucherCustomerViewModel
        {
        public int Id { get; set; }
        public int TransactionId { get; set; }
        public string Type { get; set; }
        public DateOnly Date { get; set; }

        public int AccountId { get; set; }
        public int AccountCode { get; set; }
        public string AccountName { get; set; }

        public decimal DebitAmount {  get; set; }
        public decimal CreditAmount { get; set; }

        public string Description { get; set; }
        public string Partner { get; set; }
        public int? HumId { get; set; }
        public string HumName { get; set; }
        }
    }
