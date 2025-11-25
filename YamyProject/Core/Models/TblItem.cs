namespace YamyProject.Core.Models;
public partial class TblItem
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Type { get; set; } = null!;

    public int WarehouseId { get; set; }

    public string Name { get; set; } = null!;

    public int UnitId { get; set; }

    public string Barcode { get; set; } = null!;

    public decimal CostPrice { get; set; }

    public int CogsAccountId { get; set; }

    public int VendorId { get; set; }

    public decimal SalesPrice { get; set; }

    public int TaxCodeId { get; set; }

    public int IncomeAccountId { get; set; }

    public int AssetAccountId { get; set; }

    public decimal MinAmount { get; set; }

    public decimal MaxAmount { get; set; }

    public decimal OnHand { get; set; }

    public decimal TotalValue { get; set; }

    public DateOnly Date { get; set; }

    public byte[]? Img { get; set; }

    public byte[]? ItemImg { get; set; }

    public int Active { get; set; }

    public string Method { get; set; } = null!;

    public int State { get; set; }

    public int CreatedBy { get; set; }

    public DateOnly CreatedDate { get; set; }

    public int DeletedBy { get; set; }

    public int? CategoryId { get; set; }
    [ForeignKey(nameof(CategoryId))]
    public virtual TblItemCategory? Category { get; set; }

    public int? PosItem { get; set; }

    public string ItemType { get; set; } = null!;
}
