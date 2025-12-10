namespace YamyProject.Core.ViewModel
    {
    public class JournalVoucherMasterCustomerDetailsViewModel
        {
        public int Id { get; set; }
        public DateOnly Date {  get; set; }
        public string Name { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string Partner {  get; set; }
        public string Description { get; set; }

        }
    }
