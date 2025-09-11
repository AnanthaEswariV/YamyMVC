using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblAuditLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? ActionType { get; set; }

    public string? ModuleName { get; set; }

    public int? RecordId { get; set; }

    public string? Details { get; set; }

    public DateTime? ActionTime { get; set; }

    public string? IpAddress { get; set; }

    public string? MachineName { get; set; }
}
