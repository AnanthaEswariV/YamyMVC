using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblSalaryAdjustment
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? AdjustmentType { get; set; }

    public string? Description { get; set; }

    public decimal? Amount { get; set; }

    public DateOnly? Date { get; set; }

    public int RefId { get; set; }
}
