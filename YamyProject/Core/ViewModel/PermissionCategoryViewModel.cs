namespace YamyProject.Core.ViewModel
    {
    public class PermissionCategoryViewModel
        {
        public string Key { get; set; } = default!;      // e.g. "Accountant"
        public string DisplayName { get; set; } = default!; // e.g. "Accountant Access"
        public bool SelectAll { get; set; }
        public IList<PermissionItemViewModel> Items { get; set; } = new List<PermissionItemViewModel>();

        }
    }
