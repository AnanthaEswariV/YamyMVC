namespace YamyProject.Core.ViewModel
    {
    public class PurchaseRowViewModel
        {
        public int SN { get; set; }         // tbl_sales.id

        public int Id { get; set; }
        public string JvNo { get; set; } = "";
        public DateOnly Date { get; set; }
        public string InvoiceNo { get; set; } = "";
        public string VendorCode { get; set; } = ""; 
        public string VendorName { get; set; } = "";
        public string PaymentMethod { get; set; } = ""; 
        public decimal Total { get; set; }
        public decimal Vat { get; set; }
        public decimal Net { get; set; }
        public string ItemCode { get; set; } = "";
        public string ItemName { get; set; } = "";
        public decimal? Qty { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? ItemVat { get; set; }
        public decimal? ItemTotal { get; set; }
        }
    }
