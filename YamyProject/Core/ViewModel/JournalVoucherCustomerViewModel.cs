namespace YamyProject.Core.ViewModel
    {
    public class JournalVoucherCustomerViewModel
        {
        public int Id { get; set; }
        public int AccountCode { get; set; }
        public string AccountId { get; set; }

        public decimal DebitAmount {  get; set; }
        public decimal CreditAmount { get; set; }
        public string Description { get; set; }
        public string Partner { get; set; }
        }
    }
