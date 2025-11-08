namespace YamyProject.Core.ViewModel
    {
    public class AdvancePaymentVoucherRowViewModel
        {
        public int Id { get; set; }
        public DateOnly? Date { get; set; }
        public string PaymentCode { get; set; } = "";
        public string JvNo { get; set; } = "";
        public decimal Amount { get; set; }
        public string DebitAccount { get; set; } = "";
        public string CreditAccount { get; set; } = "";
        public string Type { get; set; } = "Vendor"; // e.g., Vendor | Customer | Employee, etc.

        // Optional: details shown in the bottom grid
        public List<AdvancePaymentVoucherDetailViewModel> Details { get; set; } = new();

        }
    }
