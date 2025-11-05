namespace YamyProject.Core.ViewModel
    {
    public class PartySide
        {
        public string? AccountCode { get; set; }
        public int? AccountId { get; set; }
        public int? CostCenterId { get; set; }
        public string? CustomerCode { get; set; }
        public int? CustomerId { get; set; }
        public string? Note { get; set; }

        // Transfer section (credit side)
        public DateOnly? TransFerDate { get; set; }
        public string? TRNSNAme { get; set; }
        public string? TRNSRef { get; set; }
        // Cheque section (debit side)
        public string? ChequeName { get; set; }
        public bool IsCheque { get; set; }
        public string? ChequeNo { get; set; }
        public DateOnly? ChequeDate { get; set; }
        public DateOnly? transferDate { get; set; }
        }
    }
