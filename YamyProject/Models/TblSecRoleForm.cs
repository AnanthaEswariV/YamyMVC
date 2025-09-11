using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblSecRoleForm
{
    public int Id { get; set; }

    public int? RoleId { get; set; }

    public int? FormId { get; set; }
}
