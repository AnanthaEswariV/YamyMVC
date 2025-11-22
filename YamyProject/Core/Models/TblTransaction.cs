namespace YamyProject.Core.Models;
public partial class TblTransaction
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public int? AccountId { get; set; }

    [ForeignKey("AccountId")]
    public virtual TblCity? Citys { get; set; }

    [ForeignKey("AccountId")]
    public virtual TblCoaLevel4? Account { get; set; }
    public decimal? Debit { get; set; }

    public decimal? Credit { get; set; }

    public int? TransactionId { get; set; }
    [ForeignKey("TransactionId")]
    public virtual TblSale? Sale { get; set; }
    [ForeignKey("TransactionId")]
    public virtual TblSalesReturn? SalesReturn { get; set; }
    [ForeignKey("TransactionId")]
    public virtual TblItemStockSettlement? LitemStockSettlement { get; set; }
    [ForeignKey("TransactionId")]
    public virtual TblPurchase? Purchase { get; set; }
 
    [ForeignKey("TransactionId")]
    public virtual TblPurchaseReturn? PurchaseReturn { get; set; }
    [ForeignKey("TransactionId")]
    public virtual TblAdvancePaymentVoucher? AdvancePaymentVoucher { get; set; }
    [ForeignKey("TransactionId")]
    public virtual TblJournalVoucher? JournalVoucher { get; set; }

    public int? HumId { get; set; }
    [ForeignKey("HumId")]
    public virtual TblCustomer? Customer { get; set; }
    [ForeignKey("HumId")]
    public virtual TblVendor? Vendors { get; set; }

    public string? Type { get; set; }

    public string? TType { get; set; }

    public string? Description { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public int? State { get; set; }

    public string? VoucherNo { get; set; }

    public int ProjectId { get; set; }
}
