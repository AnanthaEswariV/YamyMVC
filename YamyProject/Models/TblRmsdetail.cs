using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblRmsdetail
{
    public int DetailId { get; set; }

    public int? MainId { get; set; }

    public int? ProId { get; set; }

    public decimal? Qty { get; set; }

    public decimal? Price { get; set; }

    public decimal? Amount { get; set; }
}
