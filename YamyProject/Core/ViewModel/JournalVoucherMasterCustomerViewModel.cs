namespace YamyProject.Core.ViewModel
    {
    public class JournalVoucherMasterCustomerViewModel
        {
        public int Id { get; set; }
        public DateOnly? Date {  get; set; }
        public string JournalCode { get; set; }
        public string JVNo { get; set; }

        public decimal? DebitAmount {  get; set; }
        public decimal? CreditAmount { get; set; }
        }
    }
