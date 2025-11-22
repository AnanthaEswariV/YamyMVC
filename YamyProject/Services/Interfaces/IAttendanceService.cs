namespace YamyProject.Services.Interfaces
    {
    public interface IAttendanceService
        {
        Task<SaveResultViewModel> SaveAttendanceAsync(DefaultEmplowyeeViewModel request);
        }
    }
