using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblCreditNoteDetail
{
    public int Id { get; set; }

    public int RefId { get; set; }

    public int InvoiceId { get; set; }

    public string InvNo { get; set; } = null!;

    public DateOnly? InvoiceDate { get; set; }

    public string InvoiceType { get; set; } = null!;

    public decimal Total { get; set; }

    public decimal Vat { get; set; }

    public decimal Amount { get; set; }

    public decimal Balance { get; set; }

    public decimal Remaining { get; set; }

    public int ProjectId { get; set; }
}
