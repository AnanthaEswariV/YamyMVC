namespace YamyProject.Core.Models;

public partial class TblCostCenterTransaction
{
    public int Id { get; set; }

    public string Type { get; set; } = null!;

    public DateOnly? Date { get; set; }

    public int? RefId { get; set; }

    public decimal? Debit { get; set; }

    public decimal? Credit { get; set; }

    public string? Description { get; set; }

    public int? CostCenterId { get; set; }

    public int ProjectId { get; set; }
}
