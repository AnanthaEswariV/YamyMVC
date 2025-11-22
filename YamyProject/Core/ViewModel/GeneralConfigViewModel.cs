namespace YamyProject.Core.ViewModel
    {
    public class GeneralConfigViewModel
        {
    
        public List<GeneralConfigItemViewModel> GeneralConfig { get; set; }
        public int? DefaultTaxPercent { get; set; } 


        public List<SelectListItem> TaxOptions { get; set; } = new()
        {
            new SelectListItem { Value = "2", Text = "TAX-0%" },
            new SelectListItem { Value = "1", Text = "TAX-5%" }
        };
        }
    }
