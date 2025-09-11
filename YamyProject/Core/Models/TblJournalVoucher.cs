using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblJournalVoucher
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public string? Code { get; set; }

    public decimal? Debit { get; set; }

    public decimal? Credit { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public int? State { get; set; }

    public int ProjectId { get; set; }
}
