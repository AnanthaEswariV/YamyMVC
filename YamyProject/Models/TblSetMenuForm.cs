using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblSetMenuForm
{
    public int Id { get; set; }

    public int? MenuId { get; set; }

    public string? FormName { get; set; }

    public string? FormText { get; set; }

    public string? Params { get; set; }

    public int? Seq { get; set; }
}
