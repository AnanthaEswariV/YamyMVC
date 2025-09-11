using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblTax
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int? Value { get; set; }

    public string? Description { get; set; }

    public int? State { get; set; }
}
