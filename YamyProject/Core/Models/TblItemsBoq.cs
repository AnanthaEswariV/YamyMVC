using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblItemsBoq
{
    public int Id { get; set; }

    public string Sr { get; set; } = null!;

    public int RefId { get; set; }

    public string Type { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? UnitName { get; set; }

    public decimal? Qty { get; set; }

    public decimal? Price { get; set; }

    public decimal? Amount { get; set; }

    public decimal? Length { get; set; }

    public decimal? Width { get; set; }

    public string? Thickness { get; set; }

    public string Note { get; set; } = null!;
}
