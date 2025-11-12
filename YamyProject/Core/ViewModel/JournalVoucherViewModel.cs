namespace YamyProject.Core.ViewModel
    {
    public class JournalVoucherViewModel
        {
        public int JournalId { get; set; }
        public string JournalCode { get; set; }
        public DateOnly Date {  get; set; }
        public decimal CrAmount { get; set; }
        public decimal DrAmount { get; set; }
        public IEnumerable<TblCoaLevel4> Accounts { get; set; }

        public List<JournalVoucherCustomerViewModel> Customer { get; set; }

        }
    }
