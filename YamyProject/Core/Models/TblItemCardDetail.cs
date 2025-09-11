using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblItemCardDetail
{
    public int Id { get; set; }

    public int ItemId { get; set; }

    public DateOnly? Date { get; set; }

    public int WharehouseId { get; set; }

    public string InvNo { get; set; } = null!;

    public int TransNo { get; set; }

    public string TransType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal Price { get; set; }

    public decimal QtyIn { get; set; }

    public decimal QtyOut { get; set; }

    public decimal QtyBalance { get; set; }

    public decimal Debit { get; set; }

    public decimal Credit { get; set; }

    public decimal Balance { get; set; }

    public decimal FifoQty { get; set; }

    public decimal FifoCost { get; set; }
}
