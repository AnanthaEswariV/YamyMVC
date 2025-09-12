using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblCorporateTaxConfigration
{
    public int Id { get; set; }

    public string CorporateTaxNo { get; set; } = null!;

    public DateOnly? TrnIssueDate { get; set; }

    public DateOnly? CorporatetaxStartDate { get; set; }

    public DateOnly? CorporatetaxEndDate { get; set; }

    public DateOnly? CorporatetaxDueDate { get; set; }
}
