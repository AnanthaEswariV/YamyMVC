namespace YamyProject.Core.Models;
public partial class TblDebitNoteDetail
{
    public int Id { get; set; }

    public int RefId { get; set; }

    public int InvoiceId { get; set; }

    public string InvNo { get; set; } = null!;

    public DateOnly? InvoiceDate { get; set; }

    public string InvoiceType { get; set; } = null!;

    public decimal Amount { get; set; }
    public bool Selected { get; set; }

    public decimal Vat { get; set; }

    public decimal Total { get; set; }

    public decimal Balance { get; set; }

    public decimal Remaining { get; set; }

    public int ProjectId { get; set; }
}
