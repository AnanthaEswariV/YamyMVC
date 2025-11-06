namespace YamyProject.Core.ViewModel
   {
    public class SalesReturnViewModel
        {
        public int Id { get; set; }
        public int SalesRefId { get; set; }
        public int? CustomerId { get; set; }
        public int? CustomerCode { get; set; }
        public IEnumerable<TblCustomer> Customers { get; set; }
        public IEnumerable<SelectListItem> Vat { get; set; } = Enumerable.Empty<SelectListItem>();
        public IEnumerable<TblCostCenter> CostCenter { get; set; }
        public int? AccountsId { get; set; }
        public IEnumerable<TblCoaLevel4> Accounts { get; set; }
        public String PayTo { get; set; }
        public String Emirate { get; set; }
        public int? WarehousesId { get; set; }
        public IEnumerable<TblWarehouse> Warehouses { get; set; }
        public String PaymentTerms { get; set; }
        public String CustomerName { get; set; } = string.Empty;
        public DateOnly DueTo { get; set; }
        public String Ship { get; set; }
        public DateOnly ShipDate { get; set; }
        public String Val { get; set; }
        public string SalesMane { get; set; } = null!;
        public DateOnly Date { get; set; }
        public string Invoce { get; set; } = null!;
        public string InvoiceId { get; set; } = null!;
        public string PONO { get; set; } = null!;
        public string NextCode { get; set; } = null!;
        public string Description { get; set; } = null!;
        public decimal TotalBeforeVat { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotaDiscount { get; set; }
        public decimal TotaAmount { get; set; }
        public List<SalesReturnRowDataViewModel> Items { get; set; } = new List<SalesReturnRowDataViewModel>();
        }
    }
