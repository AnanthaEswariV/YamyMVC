using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblColor
{
    public int Id { get; set; }

    public string HeaderColor { get; set; } = null!;

    public string TextColor { get; set; } = null!;
}
