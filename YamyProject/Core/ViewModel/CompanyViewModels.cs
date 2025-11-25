namespace YamyProject.Core.ViewModel
{
    public class CompanyViewModels
    {

        public int Id { get; set; }

        public string? Name { get; set; }

        public string? Descriptions { get; set; }

        public string? Phone1 { get; set; }

        public string? Phone2 { get; set; }
        public string? Mobile { get; set; }

        public string? Gmail { get; set; }

        public string? MobileNumber { get; set; }

        public string? Website { get; set; }

        public string? Address { get; set; }
        public string? Address2 { get; set; }

        public string? TrnNo { get; set; }
        public string? logoComp { get; set; }
        public string? stampComp { get; set; }

        public int CountryId { get; set; }
        [ForeignKey(nameof(CountryId))]
        public virtual TblCountry Country { get; set; }
        public bool IsMainCompany {get;set;}
        public byte[]? LogoFile { get; set; }
        public byte[]? StampFile { get; set; }
        public string? TaxRegistrationNo { get; set; }
        public DateOnly? TrnIssueDate { get; set; }
        public List<VatQuarterViewModel> Quarter { get; set; } = new List<VatQuarterViewModel>();

        public string? CorporateTaxNo { get; set; }
        public DateTime? CorporateIssueDate { get; set; }

        public YearlyPeriodViewModel CorporateYearly { get; set; }
            = new YearlyPeriodViewModel();


    
        }
    }
