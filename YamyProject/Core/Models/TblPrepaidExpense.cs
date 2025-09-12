using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblPrepaidExpense
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public int? CategoryId { get; set; }

    public int? DebitAccountId { get; set; }

    public int? CreditAccountId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal? Amount { get; set; }

    public decimal? Fee { get; set; }

    public decimal? Total { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public int? State { get; set; }
}
