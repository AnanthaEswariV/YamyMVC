using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblPettyCash
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public DateOnly VoucherDate { get; set; }

    public int CashAccountId { get; set; }

    public int EmployeeId { get; set; }

    public decimal? Total { get; set; }

    public string? Notes { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int ProjectId { get; set; }

    public int Status { get; set; }
}
