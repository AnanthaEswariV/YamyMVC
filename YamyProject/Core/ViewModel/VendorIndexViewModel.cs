namespace YamyProject.Core.ViewModel
{
    public class VendorIndexViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // formatted code
        public string? Name { get; set; }
        public string? CategoryName { get; set; }
        public string? AccountName { get; set; }
        public decimal Balance { get; set; }
        public DateTime? Date { get; set; } // converted from DateOnly?
        public decimal OpeningDebit { get; set; }
        public decimal OpeningCredit { get; set; }
        public bool Active { get; set; }
    }
}
