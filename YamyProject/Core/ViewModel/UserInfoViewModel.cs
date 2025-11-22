namespace YamyProject.Core.ViewModel
    {
    public class UserInfoViewModel
        {
        public int? UserCode { get; set; } = 0;
        public string UserName { get; set; } = "-";
        public string Name { get; set; } = "-";
        public string Status { get; set; } = "-"; // Active / Inactive
        }
    }
