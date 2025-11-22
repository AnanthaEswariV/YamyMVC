namespace YamyProject.Services.Implementations
    {
    public class AttendanceService(YamyDbContext context): IAttendanceService
        {
        private readonly YamyDbContext _context = context;
        public async Task<SaveResultViewModel> SaveAttendanceAsync(DefaultEmplowyeeViewModel request)
            {         
            int year = request.SelectedYear;
            int month = request.SelectedMonth;

            // check if month already exists (like the COUNT(*) query)
            bool exists = await _context.TblSettingAttendances
                .AnyAsync(a => a.Date.Value.Year == year && a.Date.Value.Month == month);

            if (exists)
                {
                return new SaveResultViewModel
                    {
                    Success = false,
                    Message = $"Attendance data for {month}/{year} already exists in the database!"
                    };
                }

            var regex = new System.Text.RegularExpressions.Regex(@"^\d{1,2}:\d{2}$");

            foreach (var row in request.AttendanceRows)
                {
                if (string.IsNullOrWhiteSpace(row.Date.ToString()) || string.IsNullOrWhiteSpace(row.DayName.ToString()))
                    continue;

                if (!regex.IsMatch(row.TimeIn.ToString()) || !regex.IsMatch(row.TimeOut.ToString()))
                    {
                    return new SaveResultViewModel
                        {
                        Success = false,
                        Message = "Invalid IN or OUT time format. Please enter time as HH:mm (e.g. 08:00)."
                        };
                    }

                if (!DateOnly.TryParse(row.Date.ToString(), out var attendanceDate))
                    {
                    return new SaveResultViewModel
                        {
                        Success = false,
                        Message = $"Invalid date value: {row.Date}"
                        };
                    }

                var entity = new TblSettingAttendance
                    {
                    Date = attendanceDate,
                    Day = row.DayName,
                    Timein = TimeOnly.Parse(row.TimeIn.ToString()), // same as original: add seconds
                    Timeout = TimeOnly.Parse(row.TimeOut.ToString()),
                    State = row.State
                    };

                _context.TblSettingAttendances.Add(entity);
                }

            await _context.SaveChangesAsync();

            return new SaveResultViewModel
                {
                Success = true,
                Message = "Attendance data saved successfully!"
                };
            }   
        }
    }
