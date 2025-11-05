namespace YamyProject.Core.Models;
public partial class TblReceiptVoucher
{
    public int Id { get; set; }
    [ForeignKey(nameof(Id))]
    public virtual TblTransaction? Transaction { get; set; }
    [ForeignKey(nameof(Id))]
    public virtual TblReceiptVoucherDetail ReceiptVoucher { get; set; }
    public DateOnly? Date { get; set; }
    public string? Code { get; set; }
    public string? Type { get; set; }
    public string? Method { get; set; }
    public decimal? Amount { get; set; }
    public int? HumId { get; set; }
    [ForeignKey(nameof(HumId))]
    public virtual TblCustomer? Customer { get; set; }
    public int? CreditAccountId { get; set; }
    [ForeignKey(nameof(CreditAccountId))]
    public virtual TblCoaLevel4 CreditAccount { get; set; }
    public int? CreditCostCenterId { get; set; }
    [ForeignKey(nameof(CreditCostCenterId))]
    public virtual TblCostCenter? CreditCostCenter { get; set; }
    public string? Description { get; set; }
    public int? DebitAccountId { get; set; }
    [ForeignKey(nameof(DebitAccountId))]
    public virtual TblCoaLevel4? DebitAccount { get; set; }
    public int? DebitCostCenterId { get; set; }
    [ForeignKey(nameof(DebitCostCenterId))]
    public virtual TblCostCenter? DebitCostCenter { get; set; }
    public int? BankAccountId { get; set; }
    [ForeignKey(nameof(BankAccountId))]
    public virtual TblBank? BankAccountI { get; set; }
    public int? BookNo { get; set; }
    public int? BankId { get; set; }
    public int? BankAccount { get; set; }
    public string? BankCode { get; set; }
    public string? CheckName { get; set; }
    public string? CheckNo { get; set; }
    public DateOnly? CheckDate { get; set; }
    public DateOnly? TransDate { get; set; }
    public string? TransName { get; set; }
    public string? TransRef { get; set; }
    public int? CreatedBy { get; set; }
    public DateOnly? CreatedDate { get; set; }
    public int? State { get; set; }
    public int? ModifiedBy { get; set; }
    public DateOnly? ModifiedDate { get; set; }
    public int ProjectId { get; set; }
}