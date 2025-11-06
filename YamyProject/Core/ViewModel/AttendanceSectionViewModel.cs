namespace YamyProject.Core.ViewModel
    {
    public class AttendanceSectionViewModel
        {
        public int DayNumber { get; set; }
        public DateOnly Date { get; set; }
        public string DayName { get; set; } = "";
        public string TimeIn { get; set; } = "";
        public string TimeOut { get; set; } = "";
        public int State { get; set; } // 0 off, 1 work
        }
    }
