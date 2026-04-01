
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

    public class UnitViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class InvoiceViewModel
    {
        public int Id { get; set; }
        public int SN { get; set; }
        public string Date { get; set; }
        public string RefId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int ItemId { get; set; }
        public string Account { get; set; }
        public decimal CostPrice { get; set; }
        public string Description { get; set; }
    }


    public class CoaNode
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Credit { get; set; }
        public decimal Debit { get; set; }
        public DateTime? Date { get; set; }
        public int? CostCenter { get; set; }
        public List<CoaNode> Children { get; set; } = new List<CoaNode>();
    }
    public class TaxViewModel
    {
        public int Id { get; set; }           // for edit/delete
        public string Name { get; set; }      // Tax Name
        public decimal Value { get; set; }    // Tax Rate (%)
        public string Description { get; set; } // Optional
        public int State { get; set; }        // 0 = active, 1 = deleted (for recycle)
    }
    public class RestoreTaxViewModel
    {
        public int Id { get; set; }
    }
    public class CompanyModels
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string? DatabaseName { get; set; }
        public string CompanyName { get; set; }
        public string Name { get; set; }
        public string? Code { get; set; }
        public string? Descriptions { get; set; }
        public string Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public string Gmail { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string? MobileNumber { get; set; }
        public string? Website { get; set; }
        public string Address { get; set; }
        public string? TrnNo { get; set; }
        public byte[]? LogoComp { get; set; }
        public int? DefaultCompany { get; set; }
        public string? CustomerCode { get; set; }
        public byte[]? StampComp { get; set; }
    }
    public class UserResponse
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public List<UserPermissionDto> Permissions { get; set; }
    }
    public class UserPermissionDto
    {
        public int SubMenuId { get; set; }
        public string SubMenuName { get; set; }
        public int MainMenuId { get; set; }
        public string MainMenuName { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
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
        public int? ProjectId { get; set; }
        public int? MainId { get; set; } 
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

        public int? CountryOfIssue { get; set; }

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
    public class DepartmentRequest
    {
        public int Id { get; set; } 
        public string Name { get; set; }
    }

    public class PositionRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int DepartmentId { get; set; }
    }
    public class TransferRequest
    {
        public int SourceWarehouseId { get; set; }   
        public int TargetWarehouseId { get; set; }   
        public int ItemId { get; set; }             
        public decimal Qty { get; set; }
        public int UserId { get; set; }         
    }

    //public class ItemRequest
    //{
    //    public int Id { get; set; }
    //    public string Code { get; set; }    
    //    public string Name { get; set; }
    //    public int WarehouseId { get; set; }
    //    public string Type { get; set; }
    //    public int CategoryId { get; set; }
    //    public int UnitId { get; set; }
    //    public string Barcode { get; set; }
    //    public decimal CostPrice { get; set; }
    //    public decimal SalesPrice { get; set; }
    //    public int CogsAccountId { get; set; }
    //    public int IncomeAccountId { get; set; }
    //    public int   AssetAccountId { get; set; }
    //    public int? VendorId { get; set; }
    //    public decimal MinAmount { get; set; }
    //    public decimal MaxAmount { get; set; }
    //    public decimal OnHand { get; set; }
    //    public string Method { get; set; } = "avg";
    //    public decimal TotalValue { get; set; }
    //    public DateTime Date { get; set; }
    //    public bool Active { get; set; }
    //    public string? ItemType { get; set; }  // e.g. Service, Inventory Assembly, etc.

    //    public List<UnitRequest> Units { get; set; } = new();
    //    public List<AssemblyRequest> Assemblies { get; set; } = new();
    //}

    //public class UnitRequest
    //{
    //    public int? UnitId { get; set; }
    //    public decimal? Factor { get; set; }
    //}

    //public class AssemblyRequest
    //{
    //    public int? ItemId { get; set; }
    //    public decimal? Qty { get; set; }
    //}


    public class ItemRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int WarehouseId { get; set; }
        public string Type { get; set; }
        public int CategoryId { get; set; }
        public int? UnitId { get; set; }
        public string Barcode { get; set; }
        public decimal CostPrice { get; set; } = 0;
        public decimal SalesPrice { get; set; }
        public int CogsAccountId { get; set; }
        public int IncomeAccountId { get; set; }
        public int AssetAccountId { get; set; }
        public int? VendorId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal OnHand { get; set; }
        public string Method { get; set; } = "avg";
        public decimal TotalValue { get; set; }
        public DateTime Date { get; set; }
        public bool Active { get; set; }
        public string? ItemType { get; set; }
        public int CreatedBy { get; set; }
        public List<UnitRequest> Units { get; set; } = new();
        public List<AssemblyRequest> Assemblies { get; set; } = new();
        public int? CostCenter { get; set; }
    }

    public class UnitRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsMain { get; set; }
        public bool IsSub { get; set; }
        public int? MainId { get; set; }
        public int? UnitId { get; set; }   
        public decimal? Factor { get; set; }
    
    }
    public class AssemblyRequest
    {
        public int? ItemId { get; set; }
        public decimal? Qty { get; set; }
    }
    public class FixedAssetsCategoryRequest
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public int AssetsAccountId { get; set; }
        public int DepreciationAccountId { get; set; }
        public int ExpenceAccountId { get; set; }
    }

    public class FixedAssetRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public int CategoryId { get; set; }
        public string Model { get; set; }
        public string Supplier { get; set; }
        public string Status { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DepreciationLife { get; set; }
        public decimal PurchasePrice { get; set; }
        public int DebitAccountId { get; set; }
        public int CreditAccountId { get; set; }
        public int ExpenceAccountId { get; set; }
        public bool CreateJournal { get; set; }
        public int CreatedBy { get; set; }
        public int ManufactureId { get; set; }
        public int? CostCenter { get; set; }
    }



    public class BankRequest
    {
        public int Id { get; set; }
        public string AbbName { get; set; }
        public string EntId { get; set; }
        public string RouteNum { get; set; }
        public string BankName { get; set; }
        public int? CountryId { get; set; }
    }

    public class BankCardRequest
    {
        public int Id { get; set; } 
        public int BankId { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; }
        public string AccountNo { get; set; }
        public string Swift { get; set; }
        public string IBANNo { get; set; }
        public string BranchName { get; set; }
        public string Emirates { get; set; }
        public string Currency { get; set; }
        public string AccountManager { get; set; }
        public string AccountSign { get; set; }
        public string AccountMob { get; set; }
        public int AccountId { get; set; }
        public bool CompanyAc { get; set; }
    }
    public class ChequeRequest
    {
        public int Id { get; set; }
        public int BankCardId { get; set; }
        public int ChqBookNo { get; set; }
        public int ChqBookQty { get; set; }
        public int LeavesStartFrom { get; set; }
        public int LeavesEndIn { get; set; }
    }
    public class PettyCashVoucherRequest
    {
        public int Id { get; set; } 
        public DateTime VoucherDate { get; set; }
        public int CashAccountId { get; set; }
        public int EmployeeId { get; set; }
        public string? Notes { get; set; }
        public decimal Total { get; set; }
        public decimal Tax { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Code { get; set; }
        public int? VendorId { get; set; }   
        public List<PettyCashDetail>? Details { get; set; }
    }
    public class UpdatePettyCashStatusRequest
    {
        public int RequestId { get; set; }
        public string NewStatus { get; set; } = "";   // "Approved" | "Declined"
    }

    public class PettyCashDetail
    {
        public DateTime? EntryDate { get; set; }
        public string? HumId { get; set; }
        public string? HumName { get; set; }
        public string? RefId { get; set; }
        public string? CostCenterId { get; set; }
        public string? Category { get; set; }
        public string? ProjectId { get; set; }
        public string? VendorId { get; set; }
        public string? Description { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Tax { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Note { get; set; }
    }


    public class PettyCashDetailDto
    {
        public int SN { get; set; }
        public int Id { get; set; }
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public string CostCenterId { get; set; } = "";
        public string HumId { get; set; } = "";
        public string Note { get; set; } = "";
        public int AccountId { get; set; }
        public string AccountName { get; set; } = "";
    }

    public class ChequeActionRequest
    {
        public int CheckDetailId { get; set; }
        public string Action { get; set; }  
        public DateTime SelectedDate { get; set; }
        public bool IsPayable { get; set; } 
    }
    public class PettyCashCategoryRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
    }



    public class PettyCashCardRequest
    {
        public int Id { get; set; }
        public string Code { get; set; } 
        public int EmployeeId { get; set; }   
        public string Mobile { get; set; }
        public string WhatsappNo { get; set; } 
        public string Email { get; set; } 
        public int AccountId { get; set; }    
    }
    
    public class CoaLevel4Request
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int Level3Id { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public DateTime? Date { get; set; }
        public int? CostCenter { get; set; }
    }

    public class Level1AccountRequest
    {
        public int Id { get; set; }           
        public string Name { get; set; } = "";
        public string CategoryCode { get; set; } = "";
    }

    public class Level2AccountRequest
    {
        public int Id { get; set; }          
        public string Name { get; set; }     
        public int Level1Id { get; set; }    
    }
    public class Level3AccountRequest
    {
        public int Id { get; set; } = 0;
        public int Level2Id { get; set; }
        public string Name { get; set; }
    }
    public class PrepaidExpenseCategoryRequest
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
    }
    public class PrepaidExpenseRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }
        public int DebitAccountId { get; set; }
        public int CreditAccountId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public decimal Total { get; set; }
        public int? CostCenter { get; set; }
    }

    //public class ProjectRequest
    //{
    //    public int Id { get; set; }
    //    public string Code { get; set; }
    //    public string Name { get; set; }
    //    public string Category { get; set; }
    //    public string Description { get; set; }
    //    public DateTime StartDate { get; set; }
    //    public DateTime EndDate { get; set; }
    //    public int? CountryId { get; set; }
    //    public int? CityId { get; set; }
    //    public bool IsProjectOption { get; set; }
    //}


    public class ProjectRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameArabic { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }

        public string Emirate { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? ExtendDelay { get; set; }
        public int? ExecutionPeriodMonths { get; set; }

        public string Customer { get; set; }
        public string Contractor { get; set; }
        public string Consultant { get; set; }

        public string Location { get; set; }
        public string Details { get; set; }

        public decimal ContractValue { get; set; }
        public decimal AdditionalValue { get; set; }
        public decimal DeductionValue { get; set; }
        public decimal TotalValue { get; set; }

        public decimal BilledToDate { get; set; }
        public decimal Expenses { get; set; }
        public decimal Balance { get; set; }

        public int? CountryId { get; set; }
        public int? CityId { get; set; }

        public DateTime? ContractingDate { get; set; }
        public DateTime? BuildingLicensingDate { get; set; }

        public string ContactPerson { get; set; }
        public string ContactPersonNumber { get; set; }

        public string Area { get; set; }
        public string PlotNumber { get; set; }
        public string Block { get; set; }

        public string Country { get; set; }
        public string City { get; set; }

        public decimal TotalValues { get; set; }
        public decimal BilledToDates { get; set; }
        public decimal Balances { get; set; }

    }

    public class ProjectResponse
    {
        public int SN { get; set; }
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameArabic { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Emirate { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string ExtendDelay { get; set; }
        public int? ExecutionPeriodMonths { get; set; }
        public string Customer { get; set; }
        public string CustomerName { get; set; }
        public string Contractor { get; set; }
        public string Consultant { get; set; }
        public string Location { get; set; }
        public string Details { get; set; }
        public decimal ContractValue { get; set; }
        public decimal AdditionalValue { get; set; }
        public decimal DeductionValue { get; set; }
        public decimal TotalValue { get; set; }
        public decimal BilledToDate { get; set; }
        public decimal Expenses { get; set; }
        public decimal Balance { get; set; }

        public string ContractingDate { get; set; }
        public string BuildingLicensingDate { get; set; }
        public string ContactPerson { get; set; }
        public string ContactPersonNumber { get; set; }
        public string Area { get; set; }
        public string PlotNumber { get; set; }
        public string Block { get; set; }
        public decimal TotalValues { get; set; }
        public decimal BilledToDates { get; set; }
        public decimal Balances { get; set; }
        public string Country { get; set; }
        public string City { get; set; }

        public List<ProjectAccountItem> Accounts { get; set; } = new();
        public List<ProjectAttachmentItem> Attachments { get; set; }
    }

    public class ProjectAccountRequest
    {
        public int ProjectId { get; set; }
        public List<ProjectAccountItem> Accounts { get; set; }
    }
    public class ProjectAttachmentRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Description { get; set; }
        public IFormFile File { get; set; }
    }

    public class ProjectAttachmentItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Description { get; set; }
    }


    public class ProjectSubcontractorRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int CodeId { get; set; }
        public int SubcontractorId { get; set; }
        public string ContractNo { get; set; }
        public decimal ContractValue { get; set; }
        public DateTime? ContractDate { get; set; }
        public string ContractPeriod { get; set; }
        public string Works { get; set; }
        public string WorksEn { get; set; }
    }

    public class ProjectSubcontractorResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<ProjectSubcontractorItem> Subcontractors { get; set; }
    }

    public class ProjectSubcontractorItem
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int CodeId { get; set; }
        public int SubcontractorId { get; set; }
        public string ContractNo { get; set; }
        public decimal ContractValue { get; set; }
        public string ContractDate { get; set; }
        public string ContractPeriod { get; set; }
        public string Works { get; set; }
        public string WorksEn { get; set; }
    }

    public class ProjectEngineerRequest
    {
        public int Id { get; set; }              
        public int ProjectId { get; set; }
        public string EngineerCode { get; set; }
        public string EngineerName { get; set; }
        public string Phone { get; set; }
        public DateTime? ContractDate { get; set; }
    }


    public class ProjectAccountItem
    {
        public string Name { get; set; }
        public int AccountId { get; set; }
        public string Code { get; set; }
        public bool CheckboxId { get; set; }
    }
    // TenderRequest DTO
    public class TenderRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class SiteRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public int? LocationId { get; set; }
        public string PlotNumber { get; set; }
        public string Address { get; set; }
    }
    public class ProjectTenderRequest
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int ProjectId { get; set; }         
        public int TenderNameId { get; set; }       
        public int AccountId { get; set; }
        public int WarehouseId { get; set; }        
        public DateTime SubmissionDate { get; set; }
        public decimal? Fees { get; set; }
        public string? Description { get; set; }
        public decimal? Amount { get; set; }
        public List<ProjectTenderItem> Items { get; set; } = new();
        public string AssemblyJson { get; set; }
    }
    public class WbsRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int ParentWbsId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int IsActive { get; set; } = 1;
    }

    public class ZoneRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int SiteId { get; set; }
        public int ParentZoneId { get; set; }
        public string Name { get; set; }
        public string ZoneType { get; set; }
        public int IsActive { get; set; }
    }
    public class ProjectTenderItem
    {
        public string? Sr { get; set; }
        public string? Description { get; set; }
        public string? Unit { get; set; }
        public decimal? Qty { get; set; }
        public decimal? Rate { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Length { get; set; }
        public decimal? Width { get; set; }
        public decimal? Thick { get; set; }
        public string? Note { get; set; }
        public decimal? MarginAmount { get; set; }
        public decimal? MarginPercentage { get; set; }
    }
    public class AssemblySaveRequest
    {
        public string RefId { get; set; }
        public int AssetAccountId { get; set; }
        public int COGSAccountId { get; set; }
        public int IncomeAccountId { get; set; }
        public int VendorAccountId { get; set; }
        public List<AssemblyItemModel> Items { get; set; }
    }
    public class GenerateEstimateRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int TenderNameId { get; set; }
        public int WarehouseId { get; set; }
        public int AccountId { get; set; }
        public DateTime Date { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string? Description { get; set; }
        public string? Fees { get; set; }
        public string TotalAmount { get; set; }
        public string? TenderName { get; set; }
        public int AssetAccountId { get; set; }
        public int COGSAccountId { get; set; }
        public int IncomeAccountId { get; set; }
        public string? AssemblyJson { get; set; }
        public List<EstimateItemRequest> Items { get; set; }
    }

    public class EstimateItemRequest
    {
        public string? Sr { get; set; }
        public string? Name { get; set; }
        public string? Unit { get; set; }
        public string? Qty { get; set; }
        public string? Rate { get; set; }
        public string? Amount { get; set; }
        public string? Length { get; set; }
        public string? Width { get; set; }
        public string? Thick { get; set; }
        public string? Note { get; set; }
        public string? MarginAmount { get; set; }
        public string? MarginPercentage { get; set; }
        public List<AssemblyItemRequest>? AssemblyItems { get; set; }
    }

    public class AssemblyItemRequest
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Cost { get; set; }
        public decimal Qty { get; set; }
        public decimal Total { get; set; }
        public int AssetAccountId { get; set; }
        public int COGSAccountId { get; set; }
        public int IncomeAccountId { get; set; }
        public int VendorAccountId { get; set; }
    }

    public class AssemblyItemModel
    {
        public int ItemId { get; set; }
        public string RefId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Qty { get; set; }
        public decimal Cost { get; set; }
        public decimal Total { get; set; }
    }

    //public class ProjectPlanningRequest
    //{
    //    public int Id { get; set; }
    //    public DateTime Date { get; set; }
    //    public int ProjectId { get; set; }
    //    public int SiteId { get; set; }
    //    public int TenderId { get; set; }
    //    public int TenderNameId { get; set; }
    //    public int AccountId { get; set; }
    //    public int CashAccountId { get; set; }
    //    public decimal EstimatedBudget { get; set; }
    //    public string Status { get; set; }
    //    public string ProjectType { get; set; }
    //    public DateTime StartDate { get; set; }
    //    public DateTime EndDate { get; set; }
    //}

    public class ProjectPlanningRequest
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int ProjectId { get; set; }
        public int SiteId { get; set; }
        public string SiteName { get; set; } 
        public int TenderId { get; set; }
        public int TenderNameId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal EstimatedBudget { get; set; }
        public int SubcontractorId { get; set; }
        public decimal RetentionPercent { get; set; }
        public string Status { get; set; }
    }

    public class MaterialRequest
    {
        public int Id { get; set; }
        public int TenderId { get; set; }
        public int PlanningId { get; set; }
        public DateTime RequestedDate { get; set; }
        public List<MaterialItem> Items { get; set; }
    }

    public class DailyWorkRow
    {
        public int BoqItemId { get; set; }
        public decimal BoqQty { get; set; }
        public decimal PrevDoneQty { get; set; }
        public decimal TodayQty { get; set; }
        public decimal CumQty { get; set; }
        public decimal Progress { get; set; }
        public string Remarks { get; set; }
    }

    public class SaveDailyWorkRequest
    {
        public int ProjectId { get; set; }
        public int SiteId { get; set; }
        public int TenderId { get; set; }
        public DateTime WorkDate { get; set; }
        public List<DailyWorkRow> DailyWorks { get; set; }
    }

    public class MilestoneRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int SiteId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string MilestoneType { get; set; }
        public string PlannedDate { get; set; }
        public string ActualDate { get; set; }
        public decimal CompletionPct { get; set; }
        public string Responsible { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }
    }

    public class MaterialItem
    {
        public int? Id { get; set; }    
        public int ItemId { get; set; }
        public decimal RequestedQty { get; set; }
        public string Unit { get; set; }
    }
    public class IssueMaterialModel
    {
        public int TenderId { get; set; }
        public int PlanningId { get; set; }
        public DateTime IssueDate { get; set; }
        public List<IssueMaterialItem> Items { get; set; }
    }

    public class IssueMaterialItem
    {
        public int ItemId { get; set; }
        public decimal IssuedQty { get; set; }
        public string Unit { get; set; }
    }

    public class ReceiveMaterialModel
    {
        public int TenderId { get; set; }
        public int PlanningId { get; set; }
        public DateTime ReceiveDate { get; set; }
        public List<ReceiveMaterialItem> Items { get; set; }
    }

    public class ReceiveMaterialItem
    {
        public int ItemId { get; set; }
        public decimal ReceivedQty { get; set; }
        public string Unit { get; set; }
    }
    public class ProjectRoleRequest
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string Name { get; set; }
    }

    public class ProjectResourceRequest
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string? Phone { get; set; }
        public string? Type { get; set; }
        public decimal? PriceUnit { get; set; }
        public decimal? UnitTime { get; set; }
        public decimal? MaxUnitTime { get; set; }
        public int? EmployeeId { get; set; }
    }

    public class AssignResourcesRequest
    {
        public int PlanningId { get; set; }
        public List<int> ResourceIds { get; set; } = new List<int>();
    }
    public class ProjectActivityRequest
    {
        public int Id { get; set; }
        public int PlanningId { get; set; }
        public int TenderId { get; set; }
        public int ItemId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? Progress { get; set; }
        public List<int>? AssignedResources { get; set; } = new();
    }

    public class ProjectWorkDoneRequest
    {
        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public int ProjectId { get; set; }
        public int SiteId { get; set; }
        public int TenderId { get; set; }
        public int PlanningId { get; set; }
        public int AccountId { get; set; }
        public int WarehouseId { get; set; }
        public List<ProjectItem> Items { get; set; }
    }
    public class LookaheadRequest
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int SiteId { get; set; }
        public string PeriodStart { get; set; }
        public string PeriodEnd { get; set; }
        public int WeekNumber { get; set; }
        public string PreparedBy { get; set; }
        public string ApprovedBy { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }

    public class LookaheadItemRequest
    {
        public int Id { get; set; }
        public int LookaheadId { get; set; }
        public int ActivityId { get; set; }
        public int ZoneId { get; set; }
        public decimal PlannedQty { get; set; }
        public string PlannedStart { get; set; }
        public string PlannedEnd { get; set; }
        public int RequiredLabor { get; set; }
        public string RequiredEquip { get; set; }
        public string Notes { get; set; }
    }

    public class ProjectItem
    {
        public int ItemId { get; set; }
        public int MainItemId { get; set; }
        public string Code { get; set; }
        public decimal QtyTotal { get; set; }
        public decimal QtyUsed { get; set; }
        public string Unit { get; set; }
    }


    public class ProjectManagementRequest
    {
        public int Id { get; set; } = 0;
        public int ProjectPlanningId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public DateTime? Date { get; set; }
        public double Budget { get; set; }
        public double ActualCost { get; set; }
        public double RemainingBudget { get; set; }
        public double Progress { get; set; }
    }
    //public class AccountNode
    //{
    //    public int Id { get; set; }
    //    public string Name { get; set; }
    //    public decimal Balance { get; set; }
    //    public int CurrLvl { get; set; }
    //    public string State { get; set; } = "e";
    //    public string LoadState { get; set; } = "u";
    //    public List<AccountNode> Children { get; set; } = new List<AccountNode>();
    //}

    public class AccountNode
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string CategoryCode { get; set; } = "";
        public int CurrLvl { get; set; }

        // Standard / detail / by Class
        public decimal Balance { get; set; }

        // Prev Year Comparison
        public decimal Balance1 { get; set; }
        public decimal Balance2 { get; set; }
        public decimal Change { get; set; }
        public decimal ChangePercent { get; set; }
        public string State { get; set; } = "e";
        public string LoadState { get; set; } = "u";
        // Detail transactions (only for level-4 nodes when type == "detail")
        public List<TransactionDetail>? Transactions { get; set; }

        public List<AccountNode> Children { get; set; } = new();
    }

    public class TransactionDetail
    {
        public int Id { get; set; }
        public string? Type { get; set; }
        public string? Date { get; set; }
        public string? Num { get; set; }
        public string? Memo { get; set; }
        public string? Split { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }


    public class AccountReport
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<TransactionRow> Transactions { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal Balance { get; set; }
    }

    public class TransactionRow
    {
        public string Num { get; set; }
        public string Type { get; set; }
        public string Date { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
    }

    public class InvoiceDto
    {
        public int SN { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Barcode { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalesPrice { get; set; }
        public decimal OpeningQty { get; set; }
        public decimal Purchase { get; set; }
        public decimal Sales { get; set; }
        public decimal PurchaseReturn { get; set; }
        public decimal SalesReturn { get; set; }
        public decimal Damage { get; set; }
        public decimal BalanceQty { get; set; }
    }

    public class ItemMovingDto
    {
        public DateTime Date { get; set; }
        public string InvNo { get; set; }
        public string CustomerName { get; set; }
        public string ItemName { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SalesPrice { get; set; }
        public decimal Qty { get; set; }
        public decimal CostAmount { get; set; }
        public decimal SalesAmount { get; set; }
        public decimal Profit { get; set; }
    }
    // DTO class
    public class WarehouseItemDto
    {
        public int SN { get; set; }
        public int Id { get; set; }
        public string WarehouseName { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
    }

    public class InvoiceViewModels
    {
        public int SN { get; set; }
        public int Id { get; set; }
        public string Date { get; set; }
        public string Type { get; set; }
        public string RefId { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public int ItemId { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public decimal QtyIn { get; set; }
        public decimal QtyOut { get; set; }
        public string Description { get; set; }
    }
    public class JournalVoucherRequest
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string VType { get; set; }
        public string CurrentVoucherName { get; set; }
        public string CurrentVoucherId { get; set; }
        public string UserId { get; set; }
        public List<JournalEntryItem> JournalEntries { get; set; }
    }

    public class JournalEntryItem
    {
        public int? AccountId { get; set; }
        public string AccountName { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public string Description { get; set; }
        public string HumId { get; set; }
        public string TId { get; set; }
    }




    public class CityRequest
    {
        public int Id { get; set; } = 0; 
        public string Name { get; set; }
        public int CountryId { get; set; }
    }

    public class CountryRequest
    {
        public int Id { get; set; }       
        public string Name { get; set; } 
    }

    public class Login
    {
       // [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; }

        //[Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }
    }
    public class AttendanceSaveRequest
    {
        public string EmpName { get; set; }
        public int CreatedBy { get; set; }
        public bool IsAllDataSucceed { get; set; }
        public List<AttendanceRow> AttendanceRows { get; set; }
    }


    public class AttendanceRow
    {
        public string EmpId { get; set; }
        public string EmpName { get; set; }
        public DateTime WorkDate { get; set; }
        public string TimeIn { get; set; }   
        public string TimeOut { get; set; } 
        public string DayOfWeek { get; set; } 
        public string Status { get; set; }  


    }


    public class LoanScheduleItemDto
    {
        public DateTime Date { get; set; }
        public string Month { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    public class LoanRequestDto
    {
        public string EmployeeName { get; set; }
        public int EmployeeCode { get; set; }
        public DateTime RequestDate { get; set; }
        public decimal RequestAmount { get; set; }
        public int Installments { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DebitAccountId { get; set; }
        public int CreditAccountId { get; set; }

        public List<LoanScheduleItemDto> Schedule { get; set; }
    }


    public class FinalSettlementSaveRequest
    {
        public string EmployeeName { get; set; }
        public int EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public DateTime DateCommencement { get; set; }
        public DateTime DateLastWork { get; set; }
        public decimal TotalSalary { get; set; }
        public decimal TotalAdditions { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetAccruals { get; set; }
    }

    public class PermissionRequest
    {
        public int SubMenuId { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class BulkPermissionRequest
    {
        public List<PermissionRequest> Permissions { get; set; }
    }


    public class RoleRequest
    {
        public int Id { get; set; } 
        public string Name { get; set; }
    }

    public class UserRequest
    {
        public int Id { get; set; } 
        public string Username { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; } 
        public int? EmployeeId { get; set; } 
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int RoleId { get; set; }
        public bool IsActive { get; set; }
    }
    public class MenuDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<SubMenuDto> SubMenus { get; set; }
    }

    public class SubMenuDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class CustomerRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int? CategoryId { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public DateTime? OpeningBalanceDate { get; set; }

        public string MainPhone { get; set; }
        public string WorkPhone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string CCEmail { get; set; }
        public string Website { get; set; }

        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        public string Region { get; set; }
        public string BuildingName { get; set; }
        public int? AccountId { get; set; }
        public string TRN { get; set; }
        public string FacilityName { get; set; }

        // ✅ FIX
        public bool Active { get; set; }

        // ✅ FIX: use int list
        public List<int> ProjectSites { get; set; } = new();
    }

    public class CategoryRequest
    {
        public string Name { get; set; }
    }


    public class SubContractRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int? CategoryId { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public DateTime? OpeningBalanceDate { get; set; }

        public string MainPhone { get; set; }
        public string WorkPhone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string CCEmail { get; set; }
        public string Website { get; set; }

        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        public string Region { get; set; }
        public string BuildingName { get; set; }
        public int? AccountId { get; set; }
        public string TRN { get; set; }
        public string FacilityName { get; set; }

        // ✅ FIX
        public bool Active { get; set; }

        // ✅ FIX: use int list
        public List<int> ProjectSites { get; set; } = new();
    }

    public class VendorRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int? CategoryId { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public DateTime? OpeningBalanceDate { get; set; }

        public string MainPhone { get; set; }
        public string WorkPhone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string CCEmail { get; set; }
        public string Website { get; set; }

        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        public string Region { get; set; }
        public string BuildingName { get; set; }
        public int? AccountId { get; set; }
        public string TRN { get; set; }
        public string FacilityName { get; set; }

        // ✅ FIX
        public bool Active { get; set; }

        // ✅ FIX: use int list
        public List<int> ProjectSites { get; set; } = new();

    }

    // Request model
    public class VendorCategoryRequest
    {
        public int Id { get; set; } = 0; 
        public string Name { get; set; }
    }

    public class SaleDto
    {
        public int SN { get; set; }
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string InvoiceNo { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Total { get; set; }
        public decimal Vat { get; set; }
        public decimal Net { get; set; }
        public string JvNo { get; set; }
        public int Warehouse_Id { get; set; }
        public string PO_Num { get; set; }
        public string Bill_To { get; set; }
        public string City { get; set; }
        public string Sales_Man { get; set; }
        public DateTime Ship_Date { get; set; }
        public string Ship_Via { get; set; }
        public string Ship_To { get; set; }
        public int Account_Cash_Id { get; set; }
        public string Payment_Terms { get; set; }
        public DateTime Payment_Date { get; set; }
        public decimal Pay { get; set; }
        


        public List<SaleItemDto> Items { get; set; } = new();
    }

    public class SaleItemDto
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal ItemVat { get; set; }
        public decimal ItemTotal { get; set; }
        public decimal Discount { get; set; }
        public int Sales_Id { get; set; }
        public decimal Cost_Price { get; set; }
        public decimal Sales_Price { get; set; }
        public decimal VatP { get; set; }
        public int Cost_Center_Id { get; set; }
    }

    public class SalesInvoiceRequest
    {
        public int Id { get; set; }  // 0 = New, >0 = Update
        public DateTime InvoiceDate { get; set; }
        public int CustomerId { get; set; }
        public string InvoiceCode { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string? PoNo { get; set; }
        public string? BillTo { get; set; }
        public string? City { get; set; }
        public string? SalesMan { get; set; }
        public DateTime ShipDate { get; set; }
        public string? ShipVia { get; set; }
        public string? ShipTo { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public int AccountCashId { get; set; }
        public int? PaymentCreditAccountId { get; set; }
        public string? PaymentTerms { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal TotalBeforeVat { get; set; }
        public decimal Vat { get; set; }
        public decimal NetTotal { get; set; }
        public int? SalesRevenueAccountId { get; set; } 
        public int? VatAccountId { get; set; }        
        public decimal? InventoryCost { get; set; }   
        public int? CogsAccountId { get; set; }



        // Ensure Items is never null
        public List<SalesItemRequest> Items { get; set; } = new List<SalesItemRequest>();
    }

    public class SaleReportDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string InvoiceNo { get; set; }
        public string CustomerName { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Total { get; set; }
        public decimal Vat { get; set; }
        public decimal Net { get; set; }
        public string AccountName { get; set; }
        public string City { get; set; }
        public string SalesMan { get; set; }
        public DateTime? ShipDate { get; set; }
    }
    public class SaleItemReportDto
    {
        public int ItemId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal? Discount { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public string CostCenterName { get; set; }
        public string UnitName { get; set; }
    }
    public class CompanyReportDto
    {
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string TRN { get; set; }

        public byte[] Logo { get; set; }
        public byte[] QrCode { get; set; }
    }



    public class SalesItemRequest
    {
        public int ItemId { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal CostPrice { get; set; }
        public decimal? Discount { get; set; } = 0;
        public decimal Vat { get; set; }
        public decimal VatP { get; set; }
        public decimal Total { get; set; }
        public int? CostCenterId { get; set; }

        public string Type { get; set; }
        public string Method { get; set; }
    }

    public class DefaultAccountSettingDto
    {
        public string Category { get; set; }
        public int AccountId { get; set; }
    }


    public class CompanyRequest
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Gmail { get; set; }
        public string MobileNumber { get; set; }
        public string Website { get; set; }
        public string TrnNo { get; set; }
        public int CountryId { get; set; }
        public string LogoComp { get; set; }
        public string StampComp { get; set; }
        public bool IsDefault { get; set; }
    }
    public class VatConfigRequest
    {
        public int Id { get; set; }  // VAT config ID
        public string RegistrationNo { get; set; }
        public DateTime TRNIssueDate { get; set; }
        public DateTime QuarterOneStartDate { get; set; }
        public DateTime QuarterOneEndDate { get; set; }
        public DateTime QuarterOneDueDate { get; set; }
        public DateTime QuarterTwoStartDate { get; set; }
        public DateTime QuarterTwoEndDate { get; set; }
        public DateTime QuarterTwoDueDate { get; set; }
        public DateTime QuarterThreeStartDate { get; set; }
        public DateTime QuarterThreeEndDate { get; set; }
        public DateTime QuarterThreeDueDate { get; set; }
        public DateTime QuarterFourStartDate { get; set; }
        public DateTime QuarterFourEndDate { get; set; }
        public DateTime QuarterFourDueDate { get; set; }

        public int CorporateTaxId { get; set; }   
        public string CorporateTaxNo { get; set; }
        public DateTime TRNIssueDateCorp { get; set; } 
        public DateTime CorporateTaxStartDate { get; set; }
        public DateTime CorporateTaxEndDate { get; set; }
        public DateTime CorporateTaxDueDate { get; set; }
    }

    public class CompanyMasterResponse
    {
        public CompanyDto Company { get; set; }
        public VatConfigDto VatConfig { get; set; }
        public CorporateTaxDto CorporateTax { get; set; }
    }
    public class CompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Gmail { get; set; }
        public string MobileNumber { get; set; }
        public string Website { get; set; }
        public string TrnNo { get; set; }
        public string Address { get; set; }
        public int CountryId { get; set; }
        public bool IsDefault { get; set; }
        public string Logo { get; set; }
        public string Stamp { get; set; }
    }
    public class VatConfigDto
    {
        public int Id { get; set; }
        public string RegistrationNo { get; set; }
        public DateTime TRNIssueDate { get; set; }

        public DateTime QuarterOneStartDate { get; set; }
        public DateTime QuarterOneEndDate { get; set; }
        public DateTime QuarterOneDueDate { get; set; }

        public DateTime QuarterTwoStartDate { get; set; }
        public DateTime QuarterTwoEndDate { get; set; }
        public DateTime QuarterTwoDueDate { get; set; }

        public DateTime QuarterThreeStartDate { get; set; }
        public DateTime QuarterThreeEndDate { get; set; }
        public DateTime QuarterThreeDueDate { get; set; }

        public DateTime QuarterFourStartDate { get; set; }
        public DateTime QuarterFourEndDate { get; set; }
        public DateTime QuarterFourDueDate { get; set; }
    }

    public class CorporateTaxDto
    {
        public string CorporateTaxNo { get; set; }
        public DateTime TrnIssueDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class PaymentVoucherRequest
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } 
        public string PaymentType { get; set; }
        public bool IsSubContractor { get; set; }
        public string Method { get; set; }
        public decimal Amount { get; set; }

        // Debit
        public int DebitAccountId { get; set; }
        public int? DebitCostCenterId { get; set; }
        public int? VendorId { get; set; }

        // Credit
        public int CreditAccountId { get; set; }
        public int? CreditCostCenterId { get; set; }

        public string Description { get; set; }

        // Cheque
        public int? BankId { get; set; }
        public int? BankAccountId { get; set; }
        public int? BookNo { get; set; }
        public string CheckName { get; set; }
        public string CheckNo { get; set; }
        public DateTime? CheckDate { get; set; }  

        // Transfer
        public DateTime? TransDate { get; set; }  
        public string TransName { get; set; }
        public string TransRef { get; set; }

        public List<EmployeeInfo> Employees { get; set; } = new List<EmployeeInfo>();
        public List<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }

    public class EmployeeInfo
    {
        public int EmployeeId { get; set; }
    }

    public class InvoiceDetail
    {
        public int? InvId { get; set; }
        public string InvCode { get; set; }
        public DateTime InvDate { get; set; }  
        public decimal Total { get; set; }
        public decimal Pay { get; set; }
        public string? Payment { get; set; }
        public string Description { get; set; }
        public int? VendorId { get; set; }
        public string VoucherType { get; set; }
    }
    public class CustomerInvoiceDto
    {
        public int SN { get; set; }
        public int CustomerId { get; set; }
        public int? InvoiceId { get; set; }
        public string Date { get; set; }
        public string InvoiceNo { get; set; }
        public decimal Amount { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
    public class ReceiptVoucherRequest
    {
        public int Id { get; set; }
        public DateTime? Date { get; set; }

        public string PaymentType { get; set; }
        public string Method { get; set; }

        public int CreditAccountId { get; set; }
        public int CreditCostCenterId { get; set; }

        public int DebitAccountId { get; set; }
        public int DebitCostCenterId { get; set; }

        public int? CustomerId { get; set; }
        public decimal Amount { get; set; }

        // Cheque
        public int? BankId { get; set; }
        public int? BankAccountId { get; set; }
        public int? BookNo { get; set; }
        public string BankCode { get; set; }
        public string CheckName { get; set; }
        public string CheckNo { get; set; }
        public DateTime? CheckDate { get; set; }

        // Transfer
        public DateTime? TransDate { get; set; }
        public string TransName { get; set; }
        public string TransRef { get; set; }

        public List<InvoiceDetails> InvoiceDetails { get; set; } = new();
    }

    public class InvoiceDetails
    {
        public int InvId { get; set; }
        public int HumId { get; set; }
        public string InvCode { get; set; }
        public DateTime? InvDate { get; set; }
        public decimal Total { get; set; }
        public decimal Pay { get; set; }
        public string Description { get; set; }
    }

    public class AdvancePaymentVoucherRequest
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string PaymentType { get; set; }
        public string Method { get; set; }
        public decimal Amount { get; set; }

        public int DebitAccountId { get; set; }
        public int CreditAccountId { get; set; }
        public int DebitCostCenterId { get; set; }
        public int CreditCostCenterId { get; set; }

        public string Description { get; set; }
        public List<AdvancePaymentDetailRequest> Details { get; set; }
    }

    public class AdvancePaymentDetailRequest
    {
        public string PartnerId { get; set; }
        public string BankName { get; set; }
        public string CheckName { get; set; }
        public string CheckNo { get; set; }
        public DateTime? CheckDate { get; set; }
        public string BankAccountName { get; set; }
        public int? BookNo { get; set; }
        public DateTime? TransDate { get; set; }
        public string TransName { get; set; }
        public string TransRef { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    public class JournalVoucherRequests
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public decimal DebitTotal { get; set; }
        public decimal CreditTotal { get; set; }
        public List<JournalVoucherDetailRequest> Details { get; set; }
    }

    public class JournalVoucherDetailRequest
    {
        public int AccountId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
        public string Partner { get; set; }
    }
    public class SalesQuotationDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string InvoiceCode { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Total { get; set; }
        public decimal Vat { get; set; }
        public decimal Net { get; set; }
        public int TranferStatus { get; set; }
        public int WarehouseId { get; set; }
        public string PONumber { get; set; }
        public string BillTo { get; set; }
        public string City { get; set; }
        public string SalesMan { get; set; }
        public DateTime ShipDate { get; set; }
        public string ShipVia { get; set; }
        public string ShipTo { get; set; }
        public int AccountCashId { get; set; }
        public string PaymentTerms { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Description { get; set; }
        public List<SalesQuotationItemDto> Items { get; set; } = new();
    }

    public class SalesQuotationItemDto
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public decimal Price { get; set; }
        public decimal Sales_Price { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public int CostCenterId { get; set; }
        public decimal VatP { get; set; }   
    }

    public class QuotationRequest
    {
        public int Id { get; set; } 
        public DateTime Date { get; set; } 
        public int CustomerId { get; set; } 
        public string InvoiceCode { get; set; } 
        public int WarehouseId { get; set; }
        public string PONumber { get; set; } 
        public string BillTo { get; set; } 
        public string City { get; set; } 
        public string SalesMan { get; set; } 
        public DateTime ShipDate { get; set; } 
        public string ShipVia { get; set; }
        public string ShipTo { get; set; } 
        public string PaymentMethod { get; set; } 
        public int AccountCashId { get; set; } 
        public string PaymentTerms { get; set; } 
        public DateTime PaymentDate { get; set; } 
        public decimal TotalBefore { get; set; } 
        public decimal Vat { get; set; } 
        public decimal NetTotal { get; set; } 
        public string Description { get; set; }
        public List<QuotationItemRequest> Items { get; set; } = new List<QuotationItemRequest>();
    }

    public class QuotationItemRequest
    {
        public int ItemId { get; set; }
        public decimal Quantity { get; set; } 
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; } 
        public decimal VatPercentage { get; set; }
        public decimal Vat { get; set; } 
        public decimal Total { get; set; } 
        public int CostCenterId { get; set; }
    }

    public class SalesReturnRequest
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int CustomerId { get; set; }
        public string InvoiceCode { get; set; }
        public int WarehouseId { get; set; }
        public string PONumber { get; set; }
        public string BillTo { get; set; }
        public string City { get; set; }
        public string SalesMan { get; set; }
        public DateTime ShipDate { get; set; }
        public string ShipVia { get; set; }
        public string ShipTo { get; set; }
        public string PaymentMethod { get; set; }
        public int AccountCashId { get; set; }
        public string PaymentTerms { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal TotalBefore { get; set; }
        public decimal Vat { get; set; }
        public decimal NetTotal { get; set; }
        public string Description { get; set; }
        public List<SalesReturnItemRequest> Items { get; set; }

        public int PaymentCreditAccountId { get; set; }
        public int level4VatId { get; set; }    
        public int level4SalesReturnId { get; set; }    
    }

    public class SalesReturnItemRequest
    {
        public int ItemId { get; set; }
        public decimal Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public decimal VatPercentage { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public int CostCenterId { get; set; }
    }

    public class PurchaseDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string InvoiceNo { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Total { get; set; }
        public decimal Vat { get; set; }
        public decimal Net { get; set; }
        public int? WarehouseId { get; set; }
        public string PO_Num { get; set; }
        public string BillTo { get; set; }
        public string PurchaseType { get; set; }
        public int? FixedAssetCategoryId { get; set; }
        public string City { get; set; }
        public DateTime? Ship_Date { get; set; }
        public string Ship_Via { get; set; }
        public string Ship_To { get; set; }
        public int? Account_Cash_Id { get; set; }
        public string Payment_Terms { get; set; }
        public DateTime? Payment_Date { get; set; }
        public string Description { get; set; }
        public decimal Pay { get; set; }
        public string SalesMan { get; set; }
        public int? ProjectId { get; set; }
        public List<PurchaseItemDto> Items { get; set; } = new();
    }

    public class PurchaseItemDto
    {
        public int ItemId { get; set; }               
        public string ItemCode { get; set; }          
        public string ItemName { get; set; }          
        public decimal Qty { get; set; }              
        public decimal CostPrice { get; set; }        
        public decimal Price { get; set; }        
        public decimal Vat { get; set; }              
        public decimal VatP { get; set; }              
        public decimal Total { get; set; }            
        public int? Cost_Center_Id { get; set; }      
    }


    public class PurchaseInvoiceRequest
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int VendorId { get; set; }
        public string InvoiceCode { get; set; }
        public int WarehouseId { get; set; }
        public string PONumber { get; set; }
        public string BillTo { get; set; }
        public string City { get; set; }
        public string SalesMan { get; set; }
        public DateTime ShipDate { get; set; }
        public string ShipVia { get; set; }
        public string ShipTo { get; set; }
        public string PaymentMethod { get; set; }
        public int AccountCashId { get; set; }
        public string PaymentTerms { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal TotalBefore { get; set; }
        public decimal Vat { get; set; }
        public decimal NetTotal { get; set; }
        public string PurchaseType { get; set; }
        public int FixedAssetCategoryId { get; set; }
        public string Description { get; set; }
        public List<PurchaseItemRequest> Items { get; set; }
    }

    public class PurchaseItemRequest
    {
        public int ItemId { get; set; }
        public decimal Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal VatPercentage { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public int CostCenterId { get; set; }
        public string Type { get; set; }
    }

    public class DefaultAccountIds
    {
        public int PaymentCreditMethodId { get; set; }
        public int VatId { get; set; }
        public int PurchaseInvoiceId { get; set; }
        public int InventoryId { get; set; }
    }
    public class PurchaseOrderRequest
    {
        public int Id { get; set; } = 0; 
        public DateTime Date { get; set; }
        public int VendorId { get; set; }
        public string InvoiceCode { get; set; }
        public int WarehouseId { get; set; }
        public string PONumber { get; set; }
        public string BillTo { get; set; }
        public string City { get; set; }
        public string SalesMan { get; set; }
        public DateTime ShipDate { get; set; }
        public string ShipVia { get; set; }
        public string ShipTo { get; set; }
        public string PaymentMethod { get; set; }
        public int AccountCashId { get; set; }
        public string PaymentTerms { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal TotalBefore { get; set; }
        public decimal Vat { get; set; }
        public decimal NetTotal { get; set; }
        public decimal Pay { get; set; }
        public decimal Change { get; set; }
        public string Description { get; set; }
        public int ProjectId { get; set; }
        public List<PurchaseOrderItemRequest> Items { get; set; }
        public bool Print { get; set; }
    }

    public class PurchaseOrderItemRequest
    {
        public int ItemId { get; set; }
        public decimal Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal VatPercentage { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public int CostCenterId { get; set; }
        public string Type { get; set; }
    }
    public class PurchaseReturnRequest
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int VendorId { get; set; }
        public string InvoiceCode { get; set; }
        public int WarehouseId { get; set; }
        public string PONumber { get; set; }
        public string BillTo { get; set; }
        public string City { get; set; }
        public string SalesMan { get; set; }
        public DateTime ShipDate { get; set; }
        public string ShipVia { get; set; }
        public string ShipTo { get; set; }
        public string PaymentMethod { get; set; }
        public int AccountCashId { get; set; }
        public string PaymentTerms { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal TotalBefore { get; set; }
        public decimal Vat { get; set; }
        public decimal NetTotal { get; set; }
        public string Description { get; set; }
        public List<PurchaseReturnItemRequest> Items { get; set; }
    }

    public class PurchaseReturnItemRequest
    {
        public int ItemId { get; set; }
        public decimal Qty { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public decimal VatPercentage { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public int CostCenterId { get; set; }
    }
    public class PurchaseReturnDto
    {
        public int SN { get; set; }
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string JVNo { get; set; }
        public string InvoiceNo { get; set; }
        public string VendorName { get; set; }
        public string PaymentMethod { get; set; }
        public decimal Total { get; set; }
        public decimal Vat { get; set; }
        public decimal Net { get; set; }

        public int? Warehouse_Id { get; set; }
        public string PO_Num { get; set; }
        public string Bill_To { get; set; }
        public string Sales_Man { get; set; }
        public string City { get; set; }
        public int? ProjectId { get; set; }
        public DateTime? Ship_Date { get; set; }
        public string Ship_Via { get; set; }
        public int VendorId { get; set; }
        public string Ship_To { get; set; }
        public int? Account_Cash_Id { get; set; }
        public string Payment_Terms { get; set; }
        public DateTime? Payment_Date { get; set; }
        public string Description { get; set; }
        public decimal Pay { get; set; }

        public List<PurchaseReturnItemDto> Items { get; set; }
    }

    public class PurchaseReturnItemDto
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal Qty { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public decimal Vat { get; set; }
        public decimal VatP { get; set; }
        public decimal Total { get; set; }
        public int? Cost_Center_Id { get; set; }
    }
    public class CreditNoteDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string InvoiceNo { get; set; }
        public string JvNo { get; set; }
        public decimal Amount { get; set; }
        public decimal Vat { get; set; }
        public decimal TotalAmount { get; set; }     
        public string Description { get; set; }     
        public int CreditAccount { get; set; }
        public int DebitAccount { get; set; }

        public List<CreditNoteItemDto> Items { get; set; } = new();
    }

    public class CreditNoteItemDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNo { get; set; }        
        public DateTime InvoiceDate { get; set; }
        public string InvoiceType { get; set; }
        public decimal Amount { get; set; }
        public decimal Total { get; set; }          
        public decimal Remaining { get; set; }      
        public decimal Vat { get; set; }
    }

    public class CreditNoteItemRequest
    {
        public int InvoiceId { get; set; }           
        public string InvoiceNo { get; set; }        
        public DateTime InvoiceDate { get; set; }    
        public decimal Total { get; set; }           
        public decimal Amount { get; set; }          
        public decimal Vat { get; set; }             
        public decimal Balance { get; set; }         
        public decimal Remaining { get; set; }       
        public bool Selected { get; set; }           
    }
    public class CreditNoteRequest
    {
        public int Id { get; set; }                   
        public DateTime Date { get; set; }            
        public string InvoiceCode { get; set; }       
        public int CustomerId { get; set; }           
        public string CustomerCode { get; set; }      
        public int AccountCashId { get; set; }        
        public int Level4CustomerId { get; set; }     
        public int Level4VatId { get; set; }          
        public int Level4SalesReturn { get; set; }    
        public decimal Amount { get; set; }           
        public decimal Vat { get; set; }              
        public decimal TotalAmount { get; set; }      
        public string Description { get; set; }       
        public List<CreditNoteItemRequest> Items { get; set; } = new List<CreditNoteItemRequest>();
    }

    public class DebitNoteDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string InvoiceNo { get; set; }
        public string JvNo { get; set; }
        public decimal Amount { get; set; }
        public decimal Vat { get; set; }
        public int CreditAccount { get; set; }
        public int DebitAccount { get; set; }
        public List<DebitNoteItemDto> Items { get; set; }
    }

    public class DebitNoteItemDto
    {
        public int InvoiceId { get; set; }
        public string InvoiceNo { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string InvoiceType { get; set; }
        public decimal Amount { get; set; }
        public decimal Total { get; set; }
        public decimal Remaining { get; set; }
        public decimal Vat { get; set; }
    }
    public class DebitNoteRequest
    {
        public int Id { get; set; } = 0;                     
        public DateTime Date { get; set; }                 
        public int VendorId { get; set; }                 
        public int AccountCashId { get; set; }             
        public string InvoiceCode { get; set; } = string.Empty; 
        public decimal Amount { get; set; } = 0;            
        public decimal Vat { get; set; } = 0;               
        public decimal TotalAmount { get; set; } = 0;       
        public string Description { get; set; } = string.Empty;

        // ✅ Level 4 Account IDs (for transactions)
        public int Level4VendorId { get; set; } = 0;
        public int Level4VatId { get; set; } = 0;
        public int Level4PurchaseReturn { get; set; } = 0;
        public int Level4COGS { get; set; } = 0;
        public int Level4Inventory { get; set; } = 0;

        // ✅ Debit Note Items
        public List<DebitNoteItem> Items { get; set; } = new List<DebitNoteItem>();
    }

    public class DebitNoteItem
    {
        public int InvoiceId { get; set; } = 0;       
        public string InvoiceNo { get; set; } = ""; 
        public DateTime InvoiceDate { get; set; }    
        public string InvoiceType { get; set; } = "PURCHASE"; 
        public decimal Total { get; set; } = 0;     
        public decimal Vat { get; set; } = 0;       
        public decimal Amount { get; set; } = 0;     
        public decimal Balance { get; set; } = 0;    
        public decimal Remaining { get; set; } = 0; 
        public bool Selected { get; set; } = true;   
    }
    public class StockSettlementRequest
    {
        public int Id { get; set; } // 0 = insert
        public DateTime Date { get; set; }
        public int WarehouseId { get; set; }
        public decimal TotalPlus { get; set; }
        public decimal TotalMinus { get; set; }
        public List<StockSettlementItemDto> Items { get; set; }
    }

    public class StockSettlementItemDto
    {
        public int ItemId { get; set; }
        public decimal OnHand { get; set; }
        public decimal Price { get; set; }
        public decimal NewOnHand { get; set; }
        public decimal QtyDiff { get; set; }
        public decimal MinusAmount { get; set; }
        public decimal PlusAmount { get; set; }
        public string Method { get; set; } // fifo | lifo | avg
    }

    public class PurchaseReportDto
    {
        // 🔹 Purchase Info
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string InvoiceNo { get; set; }

        public string BillTo { get; set; }
        public string City { get; set; }
        public string SalesMan { get; set; }

        public DateTime? ShipDate { get; set; }
        public string ShipVia { get; set; }
        public string ShipTo { get; set; }

        public string PoNumber { get; set; }

        public string PaymentMethod { get; set; }
        public string PaymentTerms { get; set; }
        public DateTime? PaymentDate { get; set; }

        // 🔹 Amounts
        public decimal Total { get; set; }
        public decimal Vat { get; set; }
        public decimal Net { get; set; }
        public decimal Pay { get; set; }
        public decimal Change { get; set; }

        // 🔹 Account
        public string AccountName { get; set; }

        // 🔹 Vendor Info
        public string VendorName { get; set; }
        public string VendorPhone { get; set; }
        public string VendorEmail { get; set; }
        public string VendorMobile { get; set; }
        public string VendorTRN { get; set; }
    }
    public class PurchaseItemReportDto
    {
        public int ItemId { get; set; }

        public string Code { get; set; }
        public string Name { get; set; }

        public decimal Qty { get; set; }
        public decimal CostPrice { get; set; }
        public decimal Price { get; set; }
        public decimal SubTotal { get; set; }       
        public decimal Discount { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }

        public string CostCenterName { get; set; }
        public string UnitName { get; set; }
    }

}
