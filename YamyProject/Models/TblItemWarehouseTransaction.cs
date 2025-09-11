using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblItemWarehouseTransaction
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public int? WarehouseFrom { get; set; }

    public int? WarehouseTo { get; set; }

    public int? ItemId { get; set; }

    public decimal? Qty { get; set; }

    public string? Description { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int ProjectId { get; set; }
}
