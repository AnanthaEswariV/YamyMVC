namespace YamyProject.Core.ViewModel
    {
    public class SettingsMasterViewmodel
        {
        public IEnumerable<SelectListItem> Accounts { get; set; } = [];
        public IEnumerable<SelectListItem> Taxes { get; set; } = [];
        public IEnumerable<SelectListItem> Users { get; set; } = [];
        public UserAccessViewModel UserAccess { get; set; }
        public GeneralConfigViewModel GeneralConfig { get; set; }
        // Which primary/sub tab to show (purely UI state)
        public string ActivePrimary { get; set; } = "default";
        public string ActiveSub { get; set; } = "common";

        // -------- Default Account Config (sub-tabs) ----------
        public DefaultAccountViewModel DefaultAccount { get; set; } = new();
        public DefaultEmplowyeeViewModel Attendance { get; set; } = new();
     
        }
    }