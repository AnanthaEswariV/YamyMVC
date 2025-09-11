using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblSalary
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public string? EmployeeName { get; set; }

    public DateOnly? Date { get; set; }

    public string? Month { get; set; }

    public int? Year { get; set; }

    public decimal? Salary { get; set; }

    public decimal? Pay { get; set; }

    public decimal? Change { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }
}
