namespace YamyProject.Core.Models;
public partial class TblCustomer
{
    public int Id { get; set; }
    
    public int Code { get; set; }

    public string? Name { get; set; }

    public int? CatId { get; set; }

     [ForeignKey(nameof(CatId))]
    public virtual TblCustomerCategory? CatIdNavigation { get; set; }

   public decimal? Balance { get; set; }

    public DateOnly? Date { get; set; }

    public string? MainPhone { get; set; }

    public string? WorkPhone { get; set; }

    public string? Mobile { get; set; }

    public string? Email { get; set; }

    public string? Ccemail { get; set; }

    public string? Website { get; set; }

    public string? Country { get; set; }
      
    public string? City { get; set; }

   
    public string? Region { get; set; }

    public string? BuildingName { get; set; }

    public int? AccountId { get; set; }
    [ForeignKey(nameof(AccountId))]
    public virtual TblCoaLevel4? Account { get; set; }

    public string? Trn { get; set; }

    public string? FaciltyName { get; set; }

    public int? Active { get; set; }

    public int? CreatedBy { get; set; }

    public DateOnly? CreatedDate { get; set; }

    public int? State { get; set; }

    public int ProjectId { get; set; }

    public string? ProjectSite { get; set; }
}
