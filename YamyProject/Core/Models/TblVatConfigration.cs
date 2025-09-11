using System;
using System.Collections.Generic;

namespace YamyProject.Core.Models;

public partial class TblVatConfigration
{
    public int Id { get; set; }

    public string? RegistrationNo { get; set; }

    public DateOnly? TrnissueDate { get; set; }

    public DateOnly? QuarterOneStartDate { get; set; }

    public DateOnly? QuarterOneEndDate { get; set; }

    public DateOnly? QuarterOneDueDate { get; set; }

    public DateOnly? QuarterTwoStartDate { get; set; }

    public DateOnly? QuarterTwoEndDate { get; set; }

    public DateOnly? QuarterTwoDueDate { get; set; }

    public DateOnly? QuarterThreeStartDate { get; set; }

    public DateOnly? QuarterThreeEndDate { get; set; }

    public DateOnly? QuarterThreeDueDate { get; set; }

    public DateOnly? QuarterFourStartDate { get; set; }

    public DateOnly? QuarterFourEndDate { get; set; }

    public DateOnly? QuarterFourDueDate { get; set; }
}
