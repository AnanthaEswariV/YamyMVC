namespace YamyProject.Core.ViewModel
    {
    public class AdvancePaymentViewModel
        {
        public int? Id { get; set; }
        public string? VoucherNo { get; set; }
        public DateOnly Date { get; set; }
        public decimal Amount { get; set; }
        public string? AmountInWords { get; set; }

        public int? PaymentType { get; set; }
        public IEnumerable<SelectListItem> PaymentTypes { get; set; } = new[]
        {
            new SelectListItem("Vendor", "1"),
            new SelectListItem("Customer", "2")
        };
        public int? PaymentMethod { get; set; }
        public IEnumerable<SelectListItem> PaymentMethods { get; set; } = new[]
        {
            new SelectListItem("Cash", "1"),
            new SelectListItem("Check", "2"),
            new SelectListItem("Transfer", "3")
        };
        public bool PrintAfterSave { get; set; }
        public int? CreditAccountId { get; set; }
        public string? CreditAccountCode { get; set; }
        public int? CreditCostCenterId { get; set; }

        public int? DebitAccountId { get; set; }
        public string? DebitAccountCode { get; set; }
        public string? Note { get; set; }
        public int? DebitCostCenterId { get; set; }
        public IEnumerable<SelectListItem> vendors { get; set; } = default!;
        public IEnumerable<SelectListItem> customers { get; set; } = default!;
        public IEnumerable<TblCoaLevel4> Accounts { get; set; } = default!;
        public IEnumerable<TblCostCenter> CostCenters { get; set; } = default!;
        public List<PaymentVoucherRowViewModel> Rows { get; set; } = new();


        }
    }
