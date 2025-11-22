namespace YamyProject.Core.Models;

public partial class TblUserPermission
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int SubMenuId { get; set; }
    [ForeignKey(nameof(SubMenuId))]
    public virtual TblSubMenu? SubMenu { get; set; }


    public bool CanView { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }
}
