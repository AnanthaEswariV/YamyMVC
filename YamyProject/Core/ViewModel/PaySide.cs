namespace YamyProject.Core.ViewModel
    {
    public class PaySide
        {
        public string? AccountCode { get; set; }
        public int? AccountId { get; set; }
        public string? EmployeeCode { get; set; }
        public int? EmployeeId { get; set; }
        public int? CostCenterId { get; set; }
        public string? VendorCode { get; set; }
        public int? VendorId { get; set; }
        public string? Note { get; set; }
        // Transfer section (credit side)
        public DateOnly? TransFerDate { get; set; }
        public string? TRNSNAme { get; set; }
        public string? TRNSRef { get; set; }

        // Cheque section (debit side)
        public int BankId { get; set; }
        public int BankAccountId { get; set; }
        public int BookNo { get; set; }
        public int BookCode { get; set; }
        public string ChequeName { get; set; }
        public bool IsCheque { get; set; }
        public string? ChequeNo { get; set; }
        public DateOnly? ChequeDate { get; set; }
        public DateOnly? transferDate { get; set; }
        }
    }
