using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblDeletedRecord
{
    public int Id { get; set; }

    public string? TableName { get; set; }

    public string? RecordData { get; set; }

    public int? DeletedBy { get; set; }

    public DateTime? DeletedAt { get; set; }
}
