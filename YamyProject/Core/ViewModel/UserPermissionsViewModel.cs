namespace YamyProject.Core.ViewModel
    {
    public class UserPermissionsViewModel
        {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int? subMenuId { get; set; }
        public int? subMenuName { get; set; }
        public int? mainMenuId { get; set; }
        public int? mainMenuName { get; set; }
        public int? canView { get; set; }
        public int? canEdit { get; set; }
        public int? canDelete { get; set; }

        }
    }
