using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblCheckDetail
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public int? CheckId { get; set; }

    public int? CheckNo { get; set; }

    public DateOnly? CheckDate { get; set; }

    public string? CheckType { get; set; }

    public int? PvcNo { get; set; }

    public string? CheckName { get; set; }

    public decimal? Amount { get; set; }

    public DateOnly? PassDate { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public DateOnly? HoldDate { get; set; }

    public DateOnly? CancelDate { get; set; }

    public string? State { get; set; }
}
