namespace YamyProject.Core.ViewModel
    {
    public class MasterCreditViewModel
        {        
        public int? SN { get; set; }
        public int? Id { get; set; }
        public DateOnly? Date { get; set; }
        public string? JvNo { get; set; }
        public string InvNo { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Vat { get; set; }
        public decimal? Total { get; set; }
        public int? CreditAccount { get; set; }
        public int? DebitAccount { get; set; }
        }
    }
