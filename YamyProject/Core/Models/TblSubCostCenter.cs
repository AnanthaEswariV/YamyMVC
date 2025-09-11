using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblSubCostCenter
{
    public int Id { get; set; }

    public int? Code { get; set; }

    public string? Name { get; set; }

    public int? MainId { get; set; }

    public int ProjectId { get; set; }
}
