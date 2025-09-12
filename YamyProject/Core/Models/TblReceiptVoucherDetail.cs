using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblReceiptVoucherDetail
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public int? PaymentId { get; set; }

    public int? InvId { get; set; }

    public int? HumId { get; set; }

    public string? InvCode { get; set; }

    public decimal? Total { get; set; }

    public decimal? Payment { get; set; }

    public string? Description { get; set; }

    public int? CostCenterId { get; set; }

    public int ProjectId { get; set; }
}
