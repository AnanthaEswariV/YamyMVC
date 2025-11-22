namespace YamyProject.Core.ViewModel
    {
    public class PermissionItemViewModel
        {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        }
    }
