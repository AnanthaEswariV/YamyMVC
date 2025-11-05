namespace YamyProject.Core.ViewModel
    {
    public class MasterCreditNoteListViewModel
        {

        public int? ID { get; set; }
        public int? InvNo { get; set; }
        public int? invId { get; set; }//invoice Id
        public DateOnly? InvoiceDate { get; set; }
        public string? InvoiceType { get; set; }
        public string? RefId { get; set; }
        public string? InvoiceId { get; set; }
        public decimal Total { get; set; }
        public decimal vat { get; set; }
        public decimal Amount { get; set; }
        public decimal Balance { get; set; }
        public decimal Remaining { get; set; }
        public string? ProjectId { get; set; }

        }
    }
