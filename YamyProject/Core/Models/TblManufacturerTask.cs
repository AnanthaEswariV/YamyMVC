using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblManufacturerTask
{
    public int Id { get; set; }

    public int? MachineId { get; set; }

    public int? BatchId { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? Status { get; set; }

    public int? UserId { get; set; }
}
