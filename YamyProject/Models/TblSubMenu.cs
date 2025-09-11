using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblSubMenu
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? MId { get; set; }

    public virtual TblMainMenu? MIdNavigation { get; set; }
}
