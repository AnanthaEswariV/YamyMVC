using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblCostCenter
{
    public int Id { get; set; }

    public int? Code { get; set; }

    public string? Name { get; set; }

    public int ProjectId { get; set; }
}
