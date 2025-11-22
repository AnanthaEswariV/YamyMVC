namespace YamyProject.Core.ViewModel
    {
    public class UserListItemViewModel
        {
        public int Id { get; set; }
        public string Code { get; set; } = default!;
        public string Name { get; set; } = default!;
        public int IsActive { get; set; }
   
        }
    }
