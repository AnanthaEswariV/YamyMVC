using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblPosition
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int? DepartmentId { get; set; }
}
