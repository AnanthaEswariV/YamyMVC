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
        public int SourceWarehouseId { get; set; }   // from warehouse
        public int TargetWarehouseId { get; set; }   // to warehouse
        public int ItemId { get; set; }              // which item
        public decimal Qty { get; set; }             // transfer quantity
        public int UserId { get; set; }              // logged-in user
    }

    public class ItemRequest
    {
        public int Id { get; set; }
        public string Code { get; set; }      // Auto-generated on insert
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
        public int   AssetAccountId { get; set; }
        public int? VendorId { get; set; }
        public decimal MinAmount { get; set; }
        public decimal MaxAmount { get; set; }
        public decimal OnHand { get; set; }
        public string Method { get; set; } = "avg";
        public decimal TotalValue { get; set; }
        public DateTime Date { get; set; }
        public bool Active { get; set; }
        public string? ItemType { get; set; }  // e.g. Service, Inventory Assembly, etc.

        public List<UnitRequest> Units { get; set; } = new();
        public List<AssemblyRequest> Assemblies { get; set; } = new();
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
        public List<PettyCashDetailRequest>? Details { get; set; }
    }

    public class PettyCashDetailRequest
    {
        public DateTime EntryDate { get; set; }
        public string? RefId { get; set; }
        public string? HumId { get; set; }
        public string? HumName { get; set; }
        public string? Description { get; set; }
        public decimal? Amount { get; set; }
        public string? Category { get; set; } 
        public string? CostCenterId { get; set; }
        public string? Note { get; set; }
    }




    public class ChequeActionRequest
    {
        public int CheckDetailId { get; set; }
        public string Action { get; set; }  // "Pass", "Return", "Hold", "Cancel"
        public DateTime SelectedDate { get; set; }
        public bool IsPayable { get; set; } // true=Payable, false=Receivable
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



}
