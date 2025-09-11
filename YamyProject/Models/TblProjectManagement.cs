using System;
using System.Collections.Generic;

namespace YamyProject.Models;

public partial class TblProjectManagement
{
    public int Id { get; set; }

    public DateOnly? Date { get; set; }

    public int ProjectPlanningId { get; set; }

    public int ProjectId { get; set; }

    public decimal Budget { get; set; }

    public decimal ActualCost { get; set; }

    public decimal RemainingBudget { get; set; }

    public DateOnly? ModifiedDate { get; set; }

    public int CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int ModifiedBy { get; set; }

    public int State { get; set; }
}
