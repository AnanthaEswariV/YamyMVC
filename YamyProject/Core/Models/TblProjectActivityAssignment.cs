using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblProjectActivityAssignment
{
    public int Id { get; set; }

    public int ActivityId { get; set; }

    public int ResourceId { get; set; }
}
