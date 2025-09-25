using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblItemStockSettlementDetail
{
    public int Id { get; set; }

    [Column("id")]
    public int? SettleId { get; set; }

    [ForeignKey(nameof(SettleId))]
    public virtual TblItemStockSettlement Settlement { get; set; }


    public int? ItemId { get; set; }
    [ForeignKey(nameof(ItemId))]

    public TblItem Item { get; set; }


    public decimal? OnHand { get; set; }

    public decimal? Price { get; set; }

    public decimal? NewOnHand { get; set; }

    public decimal? Qty { get; set; }

    public decimal? Minusamount { get; set; }
    public int MinusAmount { get; internal set; }
    public decimal? Plusamount { get; set; }
    public int PlusAmount { get; internal set; }
}
