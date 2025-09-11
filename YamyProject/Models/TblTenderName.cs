using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblTenderName
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Code { get; set; }
}
