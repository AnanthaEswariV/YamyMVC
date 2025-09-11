using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblProjectActivity
{
    public int Id { get; set; }

    public int PlanningId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public decimal Progress { get; set; }

    public int Status { get; set; }
}
