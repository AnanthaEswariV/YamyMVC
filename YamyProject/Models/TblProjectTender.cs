using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblProjectTender
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public int TenderNameId { get; set; }

    public int AccountId { get; set; }

    public int ProjectId { get; set; }

    public decimal Fees { get; set; }

    public DateOnly SubmissionDate { get; set; }

    public string Status { get; set; } = null!;

    public string TenderName { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int WarehouseId { get; set; }

    public int CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public decimal Amount { get; set; }

    public decimal BidAmount { get; set; }

    public int State { get; set; }

    public int ContractorId { get; set; }

    public int? EstimateStatus { get; set; }
}
