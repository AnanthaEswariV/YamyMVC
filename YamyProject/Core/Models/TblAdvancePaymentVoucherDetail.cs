namespace YamyProject.Core.Models;
public partial class TblAdvancePaymentVoucherDetail
{
    public int Id { get; set; }
    public int PaymentId { get; set; }
    [ForeignKey(nameof(PaymentId))]
    public virtual TblAdvancePaymentVoucher? AdvancePaymentVoucher { get; set; }
    public string? Name { get; set; }
    public int? BankId { get; set; }
    public string? BankName { get; set; }
    public string? CheckName { get; set; }
    public int? CheckNo { get; set; }
    public DateOnly? CheckDate { get; set; }
    public string? BankAccountName { get; set; }
    public int? BookNo { get; set; }
    public DateOnly? TransDate { get; set; }
    public string? TransName { get; set; }
    public string? TransRef { get; set; }
    public string? Description { get; set; }
    public decimal? Amount { get; set; }
    public int ProjectId { get; set; }
}