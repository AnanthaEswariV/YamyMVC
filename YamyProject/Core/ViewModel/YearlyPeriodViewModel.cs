namespace YamyProject.Core.ViewModel
    {
    public class YearlyPeriodViewModel
        {
        [Display(Name = "Start Date")]
        public DateOnly? StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateOnly? EndDate { get; set; }

        [Display(Name = "Due Date")]
        public DateOnly? DueDate { get; set; }
        }
    }
