using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblAttendanceSalary
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public string? EmpCode { get; set; }

    public int? AbsenceDays { get; set; }

    public decimal? TotalAbsence { get; set; }

    public decimal? DelayMinutes { get; set; }

    public decimal? TotalDelay { get; set; }

    public decimal? TotalLoan { get; set; }

    public decimal? NetSalary { get; set; }

    public decimal? Pay { get; set; }

    public decimal? Change { get; set; }

    public int? SsNo { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }
}
