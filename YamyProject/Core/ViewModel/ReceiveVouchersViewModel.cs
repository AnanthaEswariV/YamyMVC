namespace YamyProject.Core.ViewModel
    {
    public class ReceiveVouchersViewModel
        {
        public int SN { get; set; }
        public int Id { get; set; }

        public DateOnly? Date { get; set; }
        public string? JVNO { get; set; }
        public string? ReceiptCode { get; set; }
        public decimal? Amount { get; set; }

        public string? DebitAccount { get; set; }
        public string? CreditAccount { get; set; }

        public IEnumerable<RvItemViewModel> ReceiptVoucherDetail { get; set; }
        }
    }
