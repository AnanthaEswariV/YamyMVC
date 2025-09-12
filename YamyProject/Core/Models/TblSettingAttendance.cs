using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblSettingAttendance
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public string? Day { get; set; }

    public TimeOnly? Timein { get; set; }

    public TimeOnly? Timeout { get; set; }

    public int? State { get; set; }
}
