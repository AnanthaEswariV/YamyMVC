namespace YamyProject.Core.ViewModel
{
    public class SalesCenterViewModel
    {

        //column used in the table to count the row number
        //Na
        public int SN { get; set; }         // tbl_sales.id
        public DateOnly Date { get; set; }  // tbl_sales.date
        public int Id { get; set; }         // tbl_sales.id
        public int TranferStatus { get; set; }         // tbl_sales.id
        public string InvoiceId { get; set; } = null!; // tbl_sales.invoice_id as 'INV NO'
        public string PaymentMethod { get; set; } = null!; // tbl_sales.payment_method
        public decimal Total { get; set; }  // tbl_sales.total
        public decimal Vat { get; set; }    // tbl_sales.vat
        public decimal Net { get; set; }    // tbl_sales.net

        // From TblCustomer
        public string CustomerName { get; set; } = null!; // CONCAT(tbl_customer.code, ' - ', tbl_customer.name)

        // From TblTransaction (optional, only in second script)
        public string? JvNo { get; set; } // CONCAT('000', MAX(tbl_transaction.transaction_id))

        // From TblItem & TblSalesDetail (only in first script)
        public string? ItemName { get; set; }   // CONCAT(ti.code, ' - ', ti.name)
        public decimal? Qty { get; set; }       // ts.qty
        public decimal? Price { get; set; }     // ts.price
        public decimal? ItemVat { get; set; }   // ts.vat
        public decimal? ItemTotal { get; set; } // ts.total

    }
}