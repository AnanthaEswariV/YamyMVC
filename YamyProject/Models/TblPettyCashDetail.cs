using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblPettyCashDetail
{
    public int Id { get; set; }

    public int PettyCashId { get; set; }

    public DateOnly EntryDate { get; set; }

    public string? RefId { get; set; }

    public int HumId { get; set; }

    public string? Category { get; set; }

    public int CostCenterId { get; set; }

    public string? Description { get; set; }

    public decimal Amount { get; set; }

    public int ProjectId { get; set; }

    public string? Note { get; set; }
}
