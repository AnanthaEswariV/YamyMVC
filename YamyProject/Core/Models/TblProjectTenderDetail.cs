using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblProjectTenderDetail
{
    public int Id { get; set; }

    public string Sr { get; set; } = null!;

    public int TenderId { get; set; }

    public string ItemId { get; set; } = null!;

    public decimal Qty { get; set; }

    public string UnitId { get; set; } = null!;

    public decimal Rate { get; set; }

    public decimal Amount { get; set; }

    public decimal MarginPercentage { get; set; }

    public decimal MarginAmount { get; set; }

    public decimal Total { get; set; }

    public decimal Progress { get; set; }

    public int? Assigned { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public decimal Length { get; set; }

    public decimal Width { get; set; }

    public string Thickness { get; set; } = null!;

    public string Note { get; set; } = null!;
}
