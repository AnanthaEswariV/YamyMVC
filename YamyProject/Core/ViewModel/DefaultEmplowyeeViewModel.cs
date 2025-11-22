using System.Globalization;

namespace YamyProject.Core.ViewModel
    {
    public class DefaultEmplowyeeViewModel
        {
        public int TimeIn { get; set; }
        public int TimeOut { get; set; }
        public string TimeInPeriod { get; set; } = "AM";
        public string TimeOutPeriod { get; set; } = "PM";
        public int SelectedDayOff { get; set; }

        public int SelectedMonth { get; set; } = DateTime.Now.Month;
        public int SelectedYear { get; set; } = DateTime.Now.Year;
        public IEnumerable<SelectListItem> Days { get; set; } = Enum.GetValues(typeof(DayOfWeek))
              .Cast<DayOfWeek>()
              .Select(d => new SelectListItem
                  {
                  Value = ((int)d).ToString(),          
                  Text = CultureInfo.CurrentCulture       
                                .DateTimeFormat
                                .GetDayName(d)
                  });
        public IEnumerable<SelectListItem> Months {  get; set; } = Enumerable.Range(1, 12)
            .Select(i => new SelectListItem
                {
                Value = i.ToString(),
                Text = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(i)
                });
        public IEnumerable<SelectListItem> Years { get; set; }
           = Enumerable.Range(1980, 3050 - 1980 + 1)   // 1980..2050
               .Select(y => new SelectListItem
                   {
                   Value = y.ToString(),
                   Text = y.ToString()
                   });
        public List<string> AllDaysOfWeek { get; set; } =
        Enum.GetNames(typeof(DayOfWeek)).ToList();
        public IEnumerable<SelectListItem> DaysOff {  get; set; } 
    

        public IEnumerable<DefaultMonthSetingViewModel> AttendanceRows { get; set; } = new List<DefaultMonthSetingViewModel>();

        }
    }