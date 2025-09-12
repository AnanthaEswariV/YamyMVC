using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblRmsmain
{
    public int MainId { get; set; }

    public DateOnly? ADate { get; set; }

    public string? Time { get; set; }

    public string? TableName { get; set; }

    public string? WaiterName { get; set; }

    public string? Status { get; set; }

    public string? OrderType { get; set; }

    public decimal? Total { get; set; }

    public decimal? Received { get; set; }

    public decimal? Changetot { get; set; }

    public int? DriverId { get; set; }

    public string? CusTname { get; set; }

    public string? CustPhone { get; set; }

    public int? TdOrderNo { get; set; }

    public string? UserSale { get; set; }

    public string? PaidSt { get; set; }
}
