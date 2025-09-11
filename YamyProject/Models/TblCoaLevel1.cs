using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblCoaLevel1
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? Code { get; set; }

    public string CategoryCode { get; set; } = null!;
}
