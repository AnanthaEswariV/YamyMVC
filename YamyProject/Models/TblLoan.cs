using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblLoan
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public DateOnly? LoanDate { get; set; }

    public string? EmployeeId { get; set; }

    public string? EmployeeName { get; set; }

    public decimal? RequestAmount { get; set; }

    public int? Installments { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public DateOnly? LoanDates { get; set; }

    public string? Months { get; set; }

    public string? Description { get; set; }

    public decimal? Amount { get; set; }

    public int? DebitAccountId { get; set; }

    public int? CreditAccountId { get; set; }

    public decimal? Pay { get; set; }

    public decimal? Change { get; set; }
}
