using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblItemsWarehouse
{
    public int Id { get; set; }

    public int? WarehouseId { get; set; }

    public int? ItemId { get; set; }

    public decimal? Qty { get; set; }
}
