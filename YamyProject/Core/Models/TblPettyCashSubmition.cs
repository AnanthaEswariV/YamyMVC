using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblPettyCashSubmition
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public decimal? Amount { get; set; }

    public decimal? TotalBeforeVat { get; set; }

    public decimal? TotalVat { get; set; }

    public decimal? NetAmount { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }
}
