namespace YamyProject.Core.ViewModel
    {
    public class ReceiveVoucherViewModel
        {
        // General Info
        public int? Id { get; set; }
        public String Type { get; set; }
        public bool IsSubcontractor { get; set; }
        public string? VoucherNo { get; set; }
        public DateOnly Date { get; set; }
        public decimal Amount { get; set; }
        public string? AmountInWords { get; set; }

        public int? PaymentType { get; set; }
        public IEnumerable<SelectListItem> PaymentTypes { get; set; } = new[]
        {
            new SelectListItem("Vendor", "1"),
            new SelectListItem("Employee", "2")
        };
       
        public int? PaymentMethod { get; set; }
        public IEnumerable<SelectListItem> PaymentMethods { get; set; } = new[]
        {
            new SelectListItem("Cash", "1"),
            new SelectListItem("Cheque", "2"),
            new SelectListItem("Transfer", "3")
        };
        public bool PrintAfterSave { get; set; }
        public int? CreditAccountId { get; set; }
        public int? CreditCostCenterId { get; set; }
        public int? DebitAccountId { get; set; }
        public int? DebitCostCenterId { get; set; }
        public int? vendorId { get; set; }
        public int? DebitEmployeeId { get; set; }

        public IEnumerable<TblCoaLevel4> Accounts { get; set; } = default!;
        public IEnumerable<TblVendor> vendors { get; set; } = default!;
        public IEnumerable<TblCheque> Cheques { get; set; } = default!;
        public IEnumerable<TblBankCard> BankCard { get; set; } = default!;
        public IEnumerable<TblEmployee> Employees { get; set; } = default!;
        public IEnumerable<TblCostCenter> CostCenters { get; set; } = default!;
        public IEnumerable<TblBank> DebitAccounts { get; set; } = Enumerable.Empty<TblBank>();
        public List<RvItemViewModel> Items { get; set; } = new();

        public PaySide Credit { get; set; } = new();
        public PaySide Debit { get; set; } = new();
    }
}