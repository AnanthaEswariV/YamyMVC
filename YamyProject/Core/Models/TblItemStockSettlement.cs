namespace YamyProject.Core.Models;
public partial class TblItemStockSettlement
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public DateOnly? Date { get; set; }

    public int? WarehouseId { get; set; }

    public decimal? TotalPlus { get; set; }

    public decimal? TotalMinus { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public int? State { get; set; }

    public virtual ICollection<TblItemStockSettlementDetail> Details { get; set; } = new List<TblItemStockSettlementDetail>();

    public virtual ICollection<TblTransaction> Transactions { get; set; } = new List<TblTransaction>();

}
