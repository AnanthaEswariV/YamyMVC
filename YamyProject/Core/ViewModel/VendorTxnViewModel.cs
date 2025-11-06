namespace YamyProject.Core.ViewModel
    {
    public class VendorTxnViewModel
        {
        public int Sn { get; init; }
        public DateOnly? Date { get; init; }
        public string VoucherNo { get; init; } = "";
        public string VoucherType { get; init; } = "";
        public string? Description { get; init; }
        public decimal Debit { get; init; }
        public decimal Credit { get; init; }
        public decimal Balance { get; init; }
        }
    }
