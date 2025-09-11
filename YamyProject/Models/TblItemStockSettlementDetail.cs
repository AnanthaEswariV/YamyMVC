using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblItemStockSettlementDetail
{
    public int Id { get; set; }

    public int? SettleId { get; set; }

    public int? ItemId { get; set; }

    public decimal? OnHand { get; set; }

    public decimal? Price { get; set; }

    public decimal? NewOnHand { get; set; }

    public decimal? Qty { get; set; }

    public decimal? Minusamount { get; set; }

    public decimal? Plusamount { get; set; }
}
