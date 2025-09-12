using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblProjectWorkDoneDetail
{
    public int Id { get; set; }

    public int RefId { get; set; }

    public int ItemId { get; set; }

    public int MainId { get; set; }

    public string? Code { get; set; }

    public decimal? QtyTotal { get; set; }

    public string? Unit { get; set; }

    public decimal? QtyUsed { get; set; }

    public DateTime? CreatedAt { get; set; }
}
