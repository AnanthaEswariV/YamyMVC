namespace YamyProject.Core.Models;

public partial class TblEmployee
{
    public int Id { get; set; }

    public int Code { get; set; }

    public string? Name { get; set; }

    public int? CityId { get; set; }

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public DateOnly? BirthDay { get; set; }

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

    public DateOnly? EmiratesIdissueDate { get; set; }

    public DateOnly? EmiratesIdexpiryDate { get; set; }

    public string? PassportNumber { get; set; }

    public string? CountryOfIssue { get; set; }

    public DateOnly? PassportIssueDate { get; set; }

    public DateOnly? PassportExpiryDate { get; set; }

    public string? WorkContractNumber { get; set; }

    public string? WorkContractType { get; set; }

    public int? PositionId { get; set; }

    public int? DepartmentId { get; set; }

    public int? WorkDays { get; set; }

    public int? Workinghours { get; set; }

    public DateOnly? ContractIssueDate { get; set; }

    public DateOnly? ContractExpiryDate { get; set; }

    public string? ResidencyFileNumber { get; set; }

    public string? ResidencyIssuingAuthority { get; set; }

    public DateOnly? ResidencyIssueDate { get; set; }

    public DateOnly? ResidencyExpiryDate { get; set; }

    public int? AccountId { get; set; }

    public int? AccruedSalariesId { get; set; }

    public int? EmployeeRecivableId { get; set; }

    public int? AcroalLeaveSalaryId { get; set; }

    public int? GratuitId { get; set; }

    public int? PettyCashId { get; set; }

    public int Active { get; set; }

    public int State { get; set; }

    public string? SRole { get; set; }

    public int ProjectId { get; set; }
}
