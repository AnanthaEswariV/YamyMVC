namespace YamyProject.Core.ViewModel
    {
    public class UserEditViewModel
        {
        public int Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }

        [Required]
        //[Display(Name = "User Name")]
        public string UserName { get; set; } = string.Empty;

        [Required]
      //  [Display(Name = "Role")]
        public int? RoleId { get; set; }

       // [Display(Name = "Employee")]
        public string EmployeeId { get; set; }

        [DataType(DataType.Password)]
//        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
 //       [Display(Name = "Confirm Password")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; } = true;

        // dropdown data
        public List<SelectListItem> Roles { get; set; } = new();
        public List<SelectListItem> Employees { get; set; } = new();

        public bool IsNew { get; set; } = true;
        }
    }