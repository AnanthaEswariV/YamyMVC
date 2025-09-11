using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblContractor
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int? CountryId { get; set; }
}
