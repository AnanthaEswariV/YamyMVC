using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblPurchaseDetail
{
    public int Id { get; set; }

    public int? PurchaseId { get; set; }

    public int? ItemId { get; set; }

    public decimal? Qty { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? Price { get; set; }

    public decimal? Vatp { get; set; }

    public int? Vat { get; set; }

    public decimal? Total { get; set; }

    public decimal? Discount { get; set; }

    public int? CostCenterId { get; set; }

    public int ProjectId { get; set; }
}
