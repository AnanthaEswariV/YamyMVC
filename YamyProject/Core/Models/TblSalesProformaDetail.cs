using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblSalesProformaDetail
{
    public int Id { get; set; }

    public int? SalesId { get; set; }

    public int? ItemId { get; set; }

    public decimal? Qty { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? Price { get; set; }

    public decimal? Discount { get; set; }

    public decimal? Vatp { get; set; }

    public int? Vat { get; set; }

    public int? CostCenterId { get; set; }

    public decimal? Total { get; set; }

    public int ProjectId { get; set; }
}
