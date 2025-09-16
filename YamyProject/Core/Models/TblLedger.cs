using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblLedger
{

    public int LedgerId { get; set; }

    public int Code { get; set; }

    public string? Name { get; set; }

    public decimal? Balance { get; set; }

    public string? Phone { get; set; }

    public string? Mobile { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public string? Country { get; set; }

    public string? City { get; set; }

    public string? Region { get; set; }

    public int Active { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int? State { get; set; }

    public string EntityType { get; set; } = null!;
}
