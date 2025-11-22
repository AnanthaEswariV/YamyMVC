namespace YamyProject.Core.ViewModel
    {
    public class CustomerSummaryRowViewModel
        {
        public int Sn { get; set; }
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        }
    }
