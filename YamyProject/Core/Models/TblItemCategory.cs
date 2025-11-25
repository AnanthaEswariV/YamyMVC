namespace YamyProject.Core.Models;

public partial class TblItemCategory
{
    public int Id { get; set; }
    [ForeignKey(nameof(Id))]
    public virtual TblItem? Item { get; set; } 
    public string? Code { get; set; }

    public string? Name { get; set; }
}
