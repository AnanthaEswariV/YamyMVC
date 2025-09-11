using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblItemAssemblyBo
{
    public int Id { get; set; }

    public int AssemblyId { get; set; }

    public int ItemId { get; set; }

    public decimal Qty { get; set; }
}
