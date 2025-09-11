using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblLeaveSalary
{
    public int Id { get; set; }

    public string? Date { get; set; }

    public int? Code { get; set; }

    public string? Name { get; set; }

    public string? Reference { get; set; }

    public string? Description { get; set; }

    public decimal? Debit { get; set; }

    public decimal? LeaveDays { get; set; }

    public decimal? Credit { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }
}
