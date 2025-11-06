namespace YamyProject.Core.ViewModel
    {
    public class CreditNoteViewModel
        {
        public int Id { get; set; }

        [DataType(DataType.Date)]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public string? InvoiceNo { get; set; }
        public string? CustomerCode { get; set; }
        public int? CustomerId { get; set; }
        public IEnumerable<TblCustomer> Customers { get; set; }
        public int? DebitAccountId { get; set; }
        public IEnumerable<TblCoaLevel4> DebitAccounts { get; set; }
        public decimal Vat { get; set; } 
        public decimal Amount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Description { get; set; }
        public bool PrintCreditAfterSave { get; set; }
        public List<CreditNoteItemViewModel> Items { get; set; } = new();

        }
    }