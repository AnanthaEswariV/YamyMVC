namespace YamyProject.Core.ViewModel
    {
    public class UserActivityRowViewModel
        {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public string ActionType { get; set; } = "";
        public string ModuleName { get; set; } = "";
        public int RecordId { get; set; }
        public string Details { get; set; } = "";
        public DateTime ActionTime { get; set; }
        public string IpAddress { get; set; } = "";
        }
    }
