namespace YamyProject.Core.ViewModel
    {
    public class ReceiveVoucherCenterViewModel
        {
         public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public bool? All { get; set; }
        public IEnumerable<ReceiveVouchersViewModel> ReceiptVouchers { get; set; }

        }
    }
