namespace YamyProject.Core.ViewModel
    {
    public class IncomeByCustomerRowViewModel
        {
        public int Id { get; set; }
        public int? TransactionId { get; set; }
        public string Customer { get; set; }
        public string Type { get; set; }
        public DateOnly? Date { get; set; }
        public string Num { get; set; }
        public string Memo { get; set; }
        public string Account { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public decimal Balance { get; set; }
        }
    }
