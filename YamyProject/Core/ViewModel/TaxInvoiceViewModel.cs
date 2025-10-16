namespace YamyProject.Core.ViewModel
{
    public class TaxInvoiceViewModel
    {
        public int Id { get; set; }         

        public int? CustomerId { get; set; }
        public int? CustomerCode { get; set; }
        public IEnumerable<TblCustomer> Customers { get; set; } 
        public IEnumerable<TblTax> Vat { get; set; } 
        public IEnumerable<TblCostCenter> CostCenter { get; set; } 
      //  public IEnumerable<TblCostCenter> CostPrice { get; set; } 
       
        public int? AccountsId { get; set; }

        public IEnumerable<TblCoaLevel4> Accounts { get; set; } 

        public String PayTo { get; set; }
        public String InvoiceType { get; set; }
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
        public string NextCode { get; set; } = null!;
        public string PONO { get; set; } = null!; 
        public string PreforamInvoiceNO { get; set; } = null!; 
        public string QuotationNO { get; set; } = null!; 

        public string PaymentMethod { get; set; } = null!; 
        public string PaymentTermDays { get; set; } = null!; 
        public string Notes { get; set; } = null!; 

        public decimal TotalBeforeVat { get; set; } 
        public decimal TotalVat { get; set; }
        public decimal TotaDiscount { get; set; }
        public decimal TotaAmount { get; set; }

        public List<SalesRowDataViewModel> Items { get; set; } = new List<SalesRowDataViewModel>();


    }
}
