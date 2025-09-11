using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblManufacturerBatch
{
    public int Id { get; set; }

    public string? Batchname { get; set; }

    public decimal? Costamount { get; set; }

    public decimal? Amount { get; set; }

    public decimal? Hours { get; set; }

    public string? Userinsert { get; set; }

    public DateOnly? Date { get; set; }

    public string? Description { get; set; }

    public int? FixedassetsId { get; set; }

    public string? FixedStatus { get; set; }

    public int? ProductId { get; set; }

    public int? WarehouseId { get; set; }

    public decimal? ProductQty { get; set; }
}
