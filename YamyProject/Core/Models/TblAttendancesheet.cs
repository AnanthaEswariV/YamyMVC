using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblAttendancesheet
{
    public int Id { get; set; }

    public int AttendanceSalaryId { get; set; }

    public int Code { get; set; }

    public DateOnly WorkDate { get; set; }

    public TimeOnly? TimeIn { get; set; }

    public TimeOnly? TimeOut { get; set; }

    public string? DayOfWeek { get; set; }

    public string? Status { get; set; }

    public string? Reference { get; set; }

    public string? RefCode { get; set; }
}
