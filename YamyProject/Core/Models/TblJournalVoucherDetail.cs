namespace YamyProject.Core.Models;

public partial class TblJournalVoucherDetail
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public int InvId { get; set; }
    [ForeignKey(nameof(InvId))]
    public virtual TblJournalVoucher JournalVoucher { get; set; } = null!; // nav

    public string Description { get; set; } = null!;

    public string Partner { get; set; } = null!;

    public int AccountId { get; set; }
    [ForeignKey(nameof(AccountId))]
    public virtual TblCoaLevel4? Account { get; set; }

    public int ProjectId { get; set; }
}
