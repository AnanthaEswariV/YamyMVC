using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblProjectMaterialRequest
{
    public int Id { get; set; }

    public int TenderId { get; set; }

    public int PlanningId { get; set; }

    public DateOnly? RequestedDate { get; set; }

    public DateOnly? IssuedDate { get; set; }

    public DateOnly? ReceivedDate { get; set; }

    public int ItemId { get; set; }

    public string Unit { get; set; } = null!;

    public decimal RequestedQty { get; set; }

    public decimal IssuedQty { get; set; }

    public decimal ReceivedQty { get; set; }
}
