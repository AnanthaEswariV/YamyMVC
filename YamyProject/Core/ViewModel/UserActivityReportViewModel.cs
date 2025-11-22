namespace YamyProject.Core.ViewModel
    {
    public class UserActivityReportViewModel
        {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool AllDates { get; set; }
        public IEnumerable<TblSecUser> Users { get; set; } = Enumerable.Empty<TblSecUser>();
        public IEnumerable<UserActivityRowViewModel> Rows { get; set; } = Enumerable.Empty<UserActivityRowViewModel>();


        }
    }