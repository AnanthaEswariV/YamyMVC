using System.ComponentModel.DataAnnotations;

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
        public string City { get; set; }
        public string BuildingName { get; set; }
        public int? EmpId { get; set; }
        public int? AccountId { get; set; }
        public int CreadedBy { get; set;}
    }
    public class EmployeeRequest
    {
        public int Id { get; set; }

        public int Code { get; set; }

        public string? Name { get; set; }

        public int? CityId { get; set; }

        public string? Address { get; set; }

        public string? Phone { get; set; }

        public string? SocialStatus { get; set; }

        public string? SocialInsuranceNumber { get; set; }

        public string? Email { get; set; }

        public string? EmergencyName { get; set; }

        public string? EmergencyAddress { get; set; }

        public string? EmergencyPhone { get; set; }

        public string? Relation { get; set; }

        public decimal? BasicSalary { get; set; }

        public decimal? HousingAllowance { get; set; }

        public decimal? TransportationAllowance { get; set; }

        public decimal? Other { get; set; }

        public int? BankId { get; set; }

        public string? IbanNumber { get; set; }

        public string? BankAccountNumber { get; set; }

        public string? EmiratesIdfileNumber { get; set; }

        public string? EmiratesIdissuingAuthority { get; set; }

        public string? PassportNumber { get; set; }

        public string? CountryOfIssue { get; set; }

        public string? WorkContractNumber { get; set; }

        public string? WorkContractType { get; set; }

        public int? PositionId { get; set; }

        public int? DepartmentId { get; set; }

        public int? WorkDays { get; set; }

        public int? Workinghours { get; set; }


        public string? ResidencyFileNumber { get; set; }

        public string? ResidencyIssuingAuthority { get; set; }

        public int? AccountId { get; set; }

        public int? AccruedSalariesId { get; set; }

        public int? EmployeeRecivableId { get; set; }

        public int? AcroalLeaveSalaryId { get; set; }

        public int? GratuitId { get; set; }

        public int? PettyCashId { get; set; }

        public int Active { get; set; }

        public int State { get; set; }

        public string? SRole { get; set; }

        public DateTime? BirthDay { get; set; }
        public DateTime? PassportIssueDate { get; set; }
        public DateTime? PassportExpiryDate { get; set; }
        public DateTime? ContractIssueDate { get; set; }
        public DateTime? ContractExpiryDate { get; set; }
        public DateTime? ResidencyIssueDate { get; set; }
        public DateTime? ResidencyExpiryDate { get; set; }
        public DateTime? EmiratesIdissueDate { get; set; }
        public DateTime? EmiratesIdexpiryDate { get; set; }

        public int ProjectId { get; set; }
    }

}
