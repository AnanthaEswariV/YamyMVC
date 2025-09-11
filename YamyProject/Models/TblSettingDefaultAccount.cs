using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblSettingDefaultAccount
{
    public int Id { get; set; }

    public string Type { get; set; } = null!;

    public int Level4Id { get; set; }
}
