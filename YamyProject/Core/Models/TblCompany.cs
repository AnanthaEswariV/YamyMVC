using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblCompany
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Descriptions { get; set; }

    public string? Phone1 { get; set; }

    public string? Phone2 { get; set; }

    public string? Gmail { get; set; }

    public string? MobileNumber { get; set; }

    public string? Website { get; set; }

    public string? Address { get; set; }

    public string? TrnNo { get; set; }

    public int CountryId { get; set; }

    public byte[]? LogoComp { get; set; }
}
