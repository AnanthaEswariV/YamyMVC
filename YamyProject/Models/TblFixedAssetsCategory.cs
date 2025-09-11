using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblFixedAssetsCategory
{
    public int Id { get; set; }

    public string? CategoryName { get; set; }

    public int AssetsAccountId { get; set; }

    public int DepreciationAccountId { get; set; }

    public int ExpenceAccountId { get; set; }
}
