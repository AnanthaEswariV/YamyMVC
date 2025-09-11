using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblDamage
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public int WarehouseId { get; set; }

    public string ReferenceNo { get; set; } = null!;

    public int ReportedBy { get; set; }

    public string DamageReason { get; set; } = null!;

    public decimal Total { get; set; }

    public int AccountId { get; set; }

    public int CreatedBy { get; set; }

    public DateOnly CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public int? State { get; set; }
}
