namespace YamyProject.Core.ViewModel
{
    public class CompanyViewModel
    {
        public int Id { get; set; }

        public string CompanyName { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Descriptions { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string DatabaseName { get; set; }
        public string Gmail { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string MobileNumber { get; set; }
        public string Address { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime? LastUpdatedOn { get; set; }

    }
}
