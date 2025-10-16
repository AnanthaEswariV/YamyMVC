namespace YamyProject.Core.Models;
public partial class TblCoaConfig
{
    public int Id { get; set; }

    public int? AccountId { get; set; }

    public string Category { get; set; } = null!;
}
