
namespace YamyProject.Core.Models;

public partial class TblPaymentVoucherDetail
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public int? PaymentId { get; set; }

    public int? HumId { get; set; }
    [ForeignKey(nameof(HumId))]
    public virtual TblEmployee Employee { get; set; }
    public int? InvId { get; set; }

    public string? InvCode { get; set; }

    public decimal? Total { get; set; }

    public decimal? Payment { get; set; }

    public string? Description { get; set; }

    public string? VoucherType { get; set; }

    public int? CostCenterId { get; set; }

    public int ProjectId { get; set; }
}
