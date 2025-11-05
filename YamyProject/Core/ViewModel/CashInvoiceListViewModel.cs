namespace YamyProject.Core.ViewModel
    {
    public class CashInvoiceListViewModel
        {
        public int VendorId { get; set; }
        public IList<CashInvoiceRowViewModel> Rows { get; set; } = new List<CashInvoiceRowViewModel>();

        [DisplayFormat(DataFormatString = "{0:N3}")]
        public decimal Total => Rows?.Sum(r => r.Amount) ?? 0m;
        }
    }
