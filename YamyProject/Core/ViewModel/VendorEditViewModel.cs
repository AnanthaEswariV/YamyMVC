namespace YamyProject.Core.ViewModel
{
    public class VendorEditViewModel
    {
        
        public int Id { get; set; }

        [Display(Name = "Code")]
        public string? Code { get; set; } // formatted string

        [Required, StringLength(250)]
        public string? Name { get; set; }

        [Display(Name = "Category")]
        public int? CatId { get; set; }

        [Display(Name = "Account")]
        public int? AccountId { get; set; }

        [Display(Name = "TRN")]
        [StringLength(15, MinimumLength = 3)]
        public string? Trn { get; set; }

        [Display(Name = "Main Phone")]
        public string? MainPhone { get; set; }

        [Display(Name = "Work Phone")]
        public string? WorkPhone { get; set; }

        [Display(Name = "Mobile")]
        public string? Mobile { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? Ccemail { get; set; }
        public string? Website { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? BuildingName { get; set; }

        [Display(Name = "Opening Date")]
        public DateTime? Date { get; set; } // view-friendly DateTime

        public decimal OpeningDebit { get; set; }
        public decimal OpeningCredit { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; }

        // For dropdowns in the view
        public IEnumerable<SelectListItem>? CategorySelectList { get; set; }
        public IEnumerable<SelectListItem>? AccountSelectList { get; set; }
        public IEnumerable<SelectListItem>? ProjectSelectList { get; set; }
    }
}
