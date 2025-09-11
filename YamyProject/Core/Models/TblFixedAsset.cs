using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblFixedAsset
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? Brand { get; set; }

    public int? CategoryId { get; set; }

    public string? Model { get; set; }

    public string? Supplier { get; set; }

    public string? Status { get; set; }

    public string? InvoiceNumber { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? DepreciationLife { get; set; }

    public decimal? PurchasePrice { get; set; }

    public int? DebitAccountId { get; set; }

    public int? CreditAccountId { get; set; }

    public int? ExpenceAccountId { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public int? State { get; set; }

    public int? Manufacture { get; set; }

    public string? ManufactureStatus { get; set; }
}
