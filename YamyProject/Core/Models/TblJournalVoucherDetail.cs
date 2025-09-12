using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblJournalVoucherDetail
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public int InvId { get; set; }

    public string Description { get; set; } = null!;

    public string Partner { get; set; } = null!;

    public int AccountId { get; set; }

    public int ProjectId { get; set; }
}
