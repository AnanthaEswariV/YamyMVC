using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblProjectRole
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;
}
