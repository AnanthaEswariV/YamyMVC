using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblUserPermission
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int SubMenuId { get; set; }

    public bool CanView { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }
}
