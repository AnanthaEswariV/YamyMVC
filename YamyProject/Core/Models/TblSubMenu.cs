namespace YamyProject.Core.Models;

public partial class TblSubMenu
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? MId { get; set; }
    [ForeignKey(nameof(MId))]

    public virtual TblMainMenu? MIdNavigation { get; set; }
}
