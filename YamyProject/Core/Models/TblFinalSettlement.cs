using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblFinalSettlement
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public DateOnly? Date { get; set; }

    public int? EmpId { get; set; }

    public DateOnly? DateCommencement { get; set; }

    public DateOnly? DateLastWork { get; set; }

    public decimal? TotalSalary { get; set; }

    public decimal? OtherAdditions { get; set; }

    public decimal? TotalAdditions { get; set; }

    public decimal? Payments { get; set; }

    public decimal? OtherDeductions { get; set; }

    public decimal? TotalDeductions { get; set; }

    public decimal? NetAccruals { get; set; }
}
