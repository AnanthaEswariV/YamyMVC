using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblPettyCashSubmitionDetail
{
    public int Id { get; set; }

    public int PettyId { get; set; }

    public DateOnly? Date { get; set; }

    public int? AccountId { get; set; }

    public int? Category { get; set; }

    public int? CostCenterId { get; set; }

    public decimal? Amount { get; set; }

    public decimal? Vat { get; set; }

    public decimal? Total { get; set; }

    public string? Note { get; set; }

    public string? State { get; set; }
}
