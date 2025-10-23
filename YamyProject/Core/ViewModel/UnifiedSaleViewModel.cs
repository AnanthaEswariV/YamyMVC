namespace YamyProject.Core.ViewModel
    {
    public class UnifiedSaleViewModel
        {
        public int Id { get; set; }
        public int? CustomerId { get; set; }
        public int Code { get; set; }
        public String BillTo { get; set; } = string.Empty;
        public String PayTo { get; set; }
        public String InvoiceType { get; set; }
        public String City { get; set; }
        public int? WarehousesId { get; set; }
        public int? AccountCashId { get; set; }
        public String PaymentTerms { get; set; }
        public DateOnly PaymentDate { get; set; }
        public String ShipTo { get; set; }
        public DateOnly ShipDate { get; set; }
        public String ShipVia { get; set; }
        public string SalesMan { get; set; } = null!;
        public DateOnly Date { get; set; }
        public string InvoiceId { get; set; } = null!;
        public string NextCode { get; set; } = null!;
        public string PoNum { get; set; } = null!;
        public string PreforamInvoiceNO { get; set; } = null!;
        public string QuotationNO { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string PaymentTermDays { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal Total { get; set; }
        public decimal Vat { get; set; }
        public decimal Discount { get; set; }
        public decimal Net { get; set; }
        public List<SalesRowDataViewModel> TblSalesDetails { get; set; } = new List<SalesRowDataViewModel>();

        }
    }


