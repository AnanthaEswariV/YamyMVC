using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblSale
{
    public int Id { get; set; }

    public DateOnly Date { get; set; }

    public int CustomerId { get; set; }

    public string InvoiceId { get; set; } = null!;

    public int WarehouseId { get; set; }

    public string PoNum { get; set; } = null!;

    public string BillTo { get; set; } = null!;

    public string City { get; set; } = null!;

    public string SalesMan { get; set; } = null!;

    public DateOnly? ShipDate { get; set; }

    public string? ShipVia { get; set; }

    public string ShipTo { get; set; } = null!;

    public string PaymentMethod { get; set; } = null!;

    public int AccountCashId { get; set; }

    public string PaymentTerms { get; set; } = null!;

    public DateOnly PaymentDate { get; set; }

    public decimal Total { get; set; }

    public decimal Vat { get; set; }

    public decimal Net { get; set; }

    public decimal Pay { get; set; }

    public decimal Change { get; set; }

    public int CreatedBy { get; set; }

    public DateOnly CreatedDate { get; set; }

    public int? ModifiedBy { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public int State { get; set; }

    public decimal Discount { get; set; }

    public decimal CostCenterId { get; set; }

    public int ProjectId { get; set; }

    public ICollection<TblSalesDetail> TblSalesDetails { get; set; }

}
