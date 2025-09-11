using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblProjectResource
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public DateOnly? Date { get; set; }

    public string Name { get; set; } = null!;

    public int Role { get; set; }

    public string Phone { get; set; } = null!;

    public string Type { get; set; } = null!;

    public decimal PriceUnit { get; set; }

    public decimal UnitTime { get; set; }

    public decimal MaxUnitTime { get; set; }

    public int? EmployeeId { get; set; }
}
