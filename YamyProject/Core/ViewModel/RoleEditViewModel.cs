namespace YamyProject.Core.ViewModel
    {
    public class RoleEditViewModel
        {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        }
    }
