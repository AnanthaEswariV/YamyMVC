namespace YamyProject.Core.ViewModel
    {
    public class PaymentVoucherRowViewModel
        {
        public int Id { get; set; }
        public int partnerId { get; set; }
        public IEnumerable<SelectListItem> Name { get; set; }

        public int BankId { get; set; }
        public IEnumerable<SelectListItem> BankName { get; set; }
        public string? CheckName { get; set; }
        public int? CheckNo { get; set; }
        public DateOnly? CheckDate { get; set; }
        public string? BankAccount { get; set; }
        public int? BookNo { get; set; }

        public DateOnly? TransDate { get; set; }
        public string? TransName { get; set; }
        public string? TransRef { get; set; }

        public string? Description { get; set; }
        public decimal Amount { get; set; }
        }
    }
