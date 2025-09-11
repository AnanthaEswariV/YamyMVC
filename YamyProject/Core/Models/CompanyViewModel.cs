namespace YamyProject.Core.Models
{

    public class ItemCatoryViewModel
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }
    public class ChangePasswordRequest
    {
        public string? UserName { get; set; }
        public int UserId { get; set; }
        public bool RequireOldPassword { get; set; } = true;
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}
