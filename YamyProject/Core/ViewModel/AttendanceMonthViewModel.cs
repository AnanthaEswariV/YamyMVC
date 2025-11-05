namespace YamyProject.Core.ViewModel
    {
    public class AttendanceMonthViewModel
        {
        [Range(2000, 2050)]
        public int Year { get; set; }

        [Range(1, 12)]
        public int Month { get; set; }

        [Required, RegularExpression(@"^\d{1,2}:\d{2}$")]
        public string DefaultTimeIn { get; set; } = "08:00";

        [Required, RegularExpression(@"^\d{1,2}:\d{2}$")]
        public string DefaultTimeOut { get; set; } = "17:00";

        // Off-day names: "Friday", "Saturday", etc.
        public List<string> OffDays { get; set; } = new();

        // Generated/Loaded rows
        public List<AttendanceSectionViewModel> Rows { get; set; } = new();

        // For dropdowns
        public IEnumerable<int> YearOptions { get; set; } = Enumerable.Range(2000, 51);
        public IEnumerable<(int Value, string Name)> MonthOptions { get; set; } =
            Enumerable.Range(1, 12).Select(m => (m, new DateTime(2000, m, 1).ToString("MMMM")));
        }
    }
