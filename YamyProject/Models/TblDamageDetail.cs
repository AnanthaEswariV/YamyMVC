using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblDamageDetail
{
    public int Id { get; set; }

    public int DamageId { get; set; }

    public int ItemId { get; set; }

    public decimal Qty { get; set; }

    public decimal CostPrice { get; set; }

    public decimal Total { get; set; }
}
