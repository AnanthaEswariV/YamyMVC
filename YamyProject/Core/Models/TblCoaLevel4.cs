namespace YamyProject.Core.Models;
public partial class TblCoaLevel4
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Code { get; set; }

    public int MainId { get; set; }
    [ForeignKey("MainId")]
    public virtual TblCoaLevel3? Account { get; set; }

    public decimal? Debit { get; set; }

    public decimal? Credit { get; set; }

    public DateTime? Date { get; set; }
}

