using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblDebitNote
{
    public int Id { get; set; }
    [ForeignKey(nameof(Id))]
    public virtual TblTransaction? Transaction { get; set; }

    public DateOnly? Date { get; set; }

    public int? CreditAccount { get; set; }

    public int DebitAccount { get; set; }

    public string Type { get; set; } = null!;

    public string InvoiceId { get; set; } = null!;

    public decimal? Amount { get; set; }

    public decimal? Vat { get; set; }

    public decimal? Total { get; set; }

    public string? Description { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int CreatedBy { get; set; }

    public int ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public int State { get; set; }

    public int ProjectId { get; set; }
}
