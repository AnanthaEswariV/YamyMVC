using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblPettyCashRequest
{
    public int Id { get; set; }

    public DateOnly? RequestDate { get; set; }

    public string? RequestRef { get; set; }

    public string? PettyCashName { get; set; }

    public decimal? Amount { get; set; }

    public string? Description { get; set; }

    public int? DebitAccountId { get; set; }

    public int? CreditAccountId { get; set; }

    public DateOnly? ApprovedDate { get; set; }

    public string? State { get; set; }

    public decimal? Pay { get; set; }

    public decimal? Change { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }
}
