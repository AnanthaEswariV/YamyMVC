namespace YamyProject.Core.ViewModel
{
    public class VendorIndexViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty; // formatted code
        public string? Name { get; set; }
        public string? WorkPhone { get; set; }
        public string? MainPhone { get; set; }
        public string? Region { get; set; }
        public string? Email { get; set; }
        public string? TRN { get; set; }
        public string? CategoryName { get; set; }
        public string? AccountName { get; set; }
        public decimal Balance { get; set; }
        public decimal Amount { get; set; }
        public DateOnly? Date { get; set; } // converted from DateOnly?
        public decimal OpeningDebit { get; set; }
        public decimal OpeningCredit { get; set; }
        public bool Active { get; set; }
    }
}
