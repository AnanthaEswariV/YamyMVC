namespace YamyProject.Core.ViewModel
    {
    public class ReceiptVoucherViewModel
        {
        // General Info
        public int? Id { get; set; }
        public string Code { get; set; }
        public string? VoucherNo { get; set; }
        public DateOnly Date { get; set; }
        public decimal Amount { get; set; }
        public string? AmountInWords { get; set; }
        public int? PaymentType { get; set; }
      //  public SelectList PaymentTypes { get; set; } = default!; // Customer, Supplier, ...
        public IEnumerable<SelectListItem> PaymentTypes { get; set; } = new[]
        {
            new SelectListItem("Customer", "1"),
            new SelectListItem("General", "2")
        };
        public int? PaymentMethod { get; set; }
     //   public SelectList PaymentMethods { get; set; } = default!; // Cash, Cheque, Transfer
     
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
        public int? CustomerId { get; set; }
        public decimal vat { get; set; }

        public IEnumerable<TblCoaLevel4> Accounts { get; set; } = default!;
        public IEnumerable<TblCustomer> Customers { get; set; } = default!;
        public IEnumerable<TblCostCenter> CostCenters { get; set; } = default!;


        public PartySide Credit { get; set; } = new();
        public PartySide Debit { get; set; } = new();

        public List<RvItemViewModel> Items { get; set; } = new();
        public IEnumerable<TblBank> DebitAccounts { get; set; } = Enumerable.Empty<TblBank>();

        }
    }
