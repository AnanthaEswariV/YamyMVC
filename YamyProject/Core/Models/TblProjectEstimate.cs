using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblProjectEstimate
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public int ProjectId { get; set; }

    public int FundAccountId { get; set; }

    public decimal MaterialCost { get; set; }

    public decimal LaborCost { get; set; }

    public decimal EquipmentCost { get; set; }

    public decimal OverheadCost { get; set; }

    public decimal TotalEstimate { get; set; }

    public string Description { get; set; } = null!;

    public int State { get; set; }
}
