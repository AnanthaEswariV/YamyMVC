namespace YamyProject.Core.Models
{
    public class CompanyViewModel
    {
        public int Id { get; set; }

        public string CompanyName { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; }
        public string Descriptions { get; set; }
        public string Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public string? DatabaseName { get; set; }
        public string Gmail { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string? MobileNumber { get; set; }
        public string Address { get; set; }
    }
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
    public class CostCenterRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsMain { get; set; }
        public bool IsSub { get; set; }
        public int ProjectId { get; set; }
        public int MainId { get; set; } 
        public int UserId { get; set; } 
    }
    public class WarehouseRequest
    {
        public int Id { get; set; } 
        public string Name { get; set; }
        public int? EmpId { get; set; }
        public string City { get; set; }
        public string BuildingName { get; set; }
        public int? AccountId { get; set; }
        public int CreatedBy { get; set; }
    }

}
