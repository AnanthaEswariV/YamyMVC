using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblProjectWorkDone
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public int PlanningId { get; set; }

    public int AccountId { get; set; }

    public int WarehouseId { get; set; }

    public int CreatedBy { get; set; }

    public DateOnly CreatedDate { get; set; }

    public sbyte? State { get; set; }
}
