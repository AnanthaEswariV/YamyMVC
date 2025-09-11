using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblAdvancePaymentVoucher
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public string? PvCode { get; set; }

    public string? Type { get; set; }

    public string? Method { get; set; }

    public decimal? Amount { get; set; }

    public int? DebitAccountId { get; set; }

    public int? DebitCostCenterId { get; set; }

    public string? Description { get; set; }

    public int? CreditAccountId { get; set; }

    public int? CreditCostCenterId { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public int? State { get; set; }

    public int ProjectId { get; set; }
}
