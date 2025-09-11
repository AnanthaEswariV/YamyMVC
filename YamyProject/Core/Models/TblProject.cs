using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblProject
{
    public int Id { get; set; }

    public string? Code { get; set; }

    public string Name { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string Description { get; set; } = null!;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int CountryId { get; set; }

    public int CityId { get; set; }

    public string? Status { get; set; }
}
