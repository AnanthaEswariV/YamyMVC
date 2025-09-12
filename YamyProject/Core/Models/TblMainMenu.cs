using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblMainMenu
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<TblSubMenu> TblSubMenus { get; set; } = new List<TblSubMenu>();
}
