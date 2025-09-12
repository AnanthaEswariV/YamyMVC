using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblManufacturerBatchdetail
{
    public int Id { get; set; }

    public string? BatchId { get; set; }

    public int? Itemid { get; set; }

    public decimal? Cost { get; set; }

    public decimal? Qty { get; set; }

    public decimal? Total { get; set; }

    public decimal? RequestQty { get; set; }

    public decimal? ReceiveQty { get; set; }
}
