using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblPrintConfig
{
    public int? TableBorder { get; set; }

    public int? CompanyName { get; set; }

    public int? Portrait { get; set; }

    public int? Landscape { get; set; }

    public int? Orientation { get; set; }
}
