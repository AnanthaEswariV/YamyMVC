using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblProjectSite
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int? LocationId { get; set; }

    public string? PlotNumber { get; set; }

    public string? Address { get; set; }
}
