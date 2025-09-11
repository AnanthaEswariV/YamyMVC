using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblTool
{
    public int Id { get; set; }

    public string? ToolName { get; set; }

    public int? IsSelected { get; set; }
}
