namespace YamyProject.Core.ViewModel
    {
    public class DebitNoteViewModel
        {
        public int Id { get; set; }

        [DataType(DataType.Date)]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        public string? InvoiceNo { get; set; }
        public string? VendorCode { get; set; }
        public int? VendorId { get; set; }
        public IEnumerable<TblVendor> Vendors { get; set; }
        public bool IsSubcontractor { get; set; }

        public int? DebitAccountId { get; set; }
        public IEnumerable<TblCoaLevel4> DebitAccounts { get; set; }
        public decimal Vat { get; set; } 
        public decimal Amount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Description { get; set; }
        public bool PrintDebitAfterSave { get; set; }
        public List<DebitNoteItemViewModel> DebitItems { get; set; } = new();

        }
    }