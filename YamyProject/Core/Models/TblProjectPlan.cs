using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblProjectPlan
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public int ProjectId { get; set; }

    public string Location { get; set; } = null!;

    public int Site { get; set; }

    public string? PlotNumber { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = null!;

    public string ProjectType { get; set; } = null!;

    public decimal EstimatedBudget { get; set; }

    public int FundAccountId { get; set; }

    public string? Description { get; set; }

    public string? FundPeriod { get; set; }

    public string? AssignedTeam { get; set; }

    public float? Progress { get; set; }

    public int? TenderId { get; set; }

    public int? TenderNameId { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public int? State { get; set; }
}
