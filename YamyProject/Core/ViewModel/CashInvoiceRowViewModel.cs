namespace YamyProject.Core.ViewModel
    {
    public class CashInvoiceRowViewModel
        {
        public int? VendorId { get; set; }       
        public string VoucherNo { get; set; } = string.Empty;
        public DateOnly? Date { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string VoucherType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
               // Optional: UI can ignore and use index of loop instead
        public int? Sn { get; set; }
        }
    }
