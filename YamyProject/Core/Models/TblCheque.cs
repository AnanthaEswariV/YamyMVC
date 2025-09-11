using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblCheque
{
    public int Id { get; set; }

    public int? BankCardId { get; set; }

    public int? ChqBookNo { get; set; }

    public int? ChqBookQty { get; set; }

    public string? LeavesStartFrom { get; set; }

    public string? LeavesEndIn { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }
}
