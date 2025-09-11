using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblItemTransaction
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public string? Type { get; set; }

    public string? Reference { get; set; }

    public int? ItemId { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? QtyIn { get; set; }

    public decimal? SalesPrice { get; set; }

    public decimal? QtyOut { get; set; }

    public decimal? QtyInc { get; set; }

    public string? Description { get; set; }

    public int WarehouseId { get; set; }

    public int ProjectId { get; set; }
}
