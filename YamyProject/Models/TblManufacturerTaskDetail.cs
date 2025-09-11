using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblManufacturerTaskDetail
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public int DepartmentId { get; set; }

    public int EmployeeId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? Status { get; set; }

    public string? Remarks { get; set; }
}
