namespace YamyProject.Core.ViewModel
    {
    public class DebitNoteItemViewModel
        {
        public int? SaleId { get; set; }
        public DateOnly? InvDate { get; set; }
        public string? InvoiceNo { get; set; }
        public int InvoiceId { get; set; }
        public string InvoiceType { get; set; }

        public decimal Vat { get; set; }

        public decimal Total { get; set; }
        public decimal Balance { get; set; }
        public bool Selected { get; set; }
        public decimal Amount { get; set; }
        public decimal Remaining { get; set; }
        }
    }
