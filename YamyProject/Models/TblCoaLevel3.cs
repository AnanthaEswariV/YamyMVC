using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblCoaLevel3
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Code { get; set; }

    public int MainId { get; set; }
}
