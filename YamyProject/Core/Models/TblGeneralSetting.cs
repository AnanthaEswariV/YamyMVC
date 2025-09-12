using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblGeneralSetting
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Value { get; set; }

    public string Description { get; set; } = null!;

    public int? Status { get; set; }
}
