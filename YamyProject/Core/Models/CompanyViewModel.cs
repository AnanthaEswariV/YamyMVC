
using System.Collections.Generic;

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
        public int UnitId { get; set; }
        public string Barcode { get; set; }
        public decimal CostPrice { get; set; }
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
        public string? Code { get; set; }
        public List<PettyCashDetail>? Details { get; set; }
    }

    public class PettyCashDetail
    {
        public DateTime EntryDate { get; set; }
        public string? HumId { get; set; }
        public string? HumName { get; set; }
        public string? RefId { get; set; }
        public string? CostCenterId { get; set; }
        public string? Category { get; set; }
        public string? ProjectId { get; set; }
        public string? Description { get; set; }
        public decimal? Amount { get; set; }
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

    public class ProjectRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? CountryId { get; set; }
        public int? CityId { get; set; }
        public bool IsProjectOption { get; set; }
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


    public class ProjectPlanningRequest
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int ProjectId { get; set; }
        public int SiteId { get; set; }
        public int TenderId { get; set; }
        public int TenderNameId { get; set; }
        public int AccountId { get; set; }
        public int CashAccountId { get; set; }
        public decimal EstimatedBudget { get; set; }
        public string Status { get; set; }
        public string ProjectType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class MaterialRequest
    {
        public int Id { get; set; }
        public int TenderId { get; set; }
        public int PlanningId { get; set; }
        public DateTime RequestedDate { get; set; }
        public List<MaterialItem> Items { get; set; }
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
    public class AccountNode
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Balance { get; set; }
        public int CurrLvl { get; set; }
        public string State { get; set; } = "e";
        public string LoadState { get; set; } = "u";
        public List<AccountNode> Children { get; set; } = new List<AccountNode>();
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
        public int Id { get; set; } // 0 for new users
        public string Username { get; set; }
        public string Password { get; set; } // for new users
        public string ConfirmPassword { get; set; } // for new users
        public int? EmployeeId { get; set; } // optional, -1 if not selected
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
        public int Id { get; set; } = 0; // 0 = new category, >0 = update
        public string Name { get; set; }
    }


}
