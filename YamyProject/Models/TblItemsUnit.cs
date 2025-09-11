using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblItemsUnit
{
    public int Id { get; set; }

    public int ItemId { get; set; }

    public int UnitId { get; set; }

    public int Factor { get; set; }
}
