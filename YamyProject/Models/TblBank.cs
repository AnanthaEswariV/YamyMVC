using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblBank
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? AbbName { get; set; }

    public string? EntId { get; set; }

    public string? RouteNum { get; set; }

    public int? CountryId { get; set; }

    public int? State { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }
}
