namespace YamyProject.Core.ViewModel
    {
    public class ReceiptVoucherCenterViewModel
        {
         public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public bool? All { get; set; }
        public IEnumerable<ReceiptVouchersViewModel> ReceiptVouchers { get; set; }

        }
    }
