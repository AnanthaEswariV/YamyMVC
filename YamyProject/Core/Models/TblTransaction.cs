namespace YamyProject.Core.Models;

public partial class TblTransaction
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public int? AccountId { get; set; }

    [ForeignKey("AccountId")]
    public virtual TblCity? Citys { get; set; }
    public decimal? Debit { get; set; }

    public decimal? Credit { get; set; }


    public int? TransactionId { get; set; }
    [ForeignKey("TransactionId")]
    public virtual TblSale? Sale { get; set; }
    [ForeignKey("TransactionId")]
    public virtual TblItemStockSettlement? lItemStockSettlement { get; set; }

    public int? HumId { get; set; }

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
