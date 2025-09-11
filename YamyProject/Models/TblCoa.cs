using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblCoa
{
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public string AccountCode { get; set; } = null!;

    public string AccountName { get; set; } = null!;

    public int? ParentId { get; set; }

    public string AccountType { get; set; } = null!;

    public int Level { get; set; }

    public string? AccountCategory { get; set; }

    public bool? IsGroup { get; set; }

    public virtual ICollection<TblCoa> InverseParent { get; set; } = new List<TblCoa>();

    public virtual TblCoa? Parent { get; set; }
}
