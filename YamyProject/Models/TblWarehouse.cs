using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblWarehouse
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public int? EmpId { get; set; }

    public string? City { get; set; }

    public string? BuildingName { get; set; }

    public int? AccountId { get; set; }

    public int? State { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }
}
