namespace YamyProject.Core.ViewModel
    {
    public class UserAccessViewModel
        {
        public IEnumerable<UserListItemViewModel> Users { get; set; } = Enumerable.Empty<UserListItemViewModel>();
        public int? SelectedUserId { get; set; }
        public string StatusFilter { get; set; } = "Active"; // All / Active / Inactive
        public string? SearchTerm { get; set; }

        public UserInfoViewModel UserInfo { get; set; } = new();
        public IList<PermissionCategoryViewModel> Categories { get; set; } = new List<PermissionCategoryViewModel>();

        }
    }
