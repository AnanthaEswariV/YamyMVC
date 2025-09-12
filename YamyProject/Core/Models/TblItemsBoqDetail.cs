using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblItemsBoqDetail
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int WarehouseId { get; set; }

    public string Type { get; set; } = null!;

    public int? CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public int UnitId { get; set; }

    public string? Barcode { get; set; }

    public decimal? CostPrice { get; set; }

    public int? CogsAccountId { get; set; }

    public int? VendorId { get; set; }

    public decimal? SalesPrice { get; set; }

    public int? IncomeAccountId { get; set; }

    public int? AssetAccountId { get; set; }

    public decimal? MinAmount { get; set; }

    public decimal? MaxAmount { get; set; }

    public decimal? OnHand { get; set; }

    public string? Method { get; set; }

    public decimal? TotalValue { get; set; }

    public DateOnly? Date { get; set; }

    public string? Img { get; set; }

    public bool? Active { get; set; }

    public int? State { get; set; }

    public int CreatedBy { get; set; }

    public DateTime? CreatedDate { get; set; }

    public int? RefId { get; set; }
}
