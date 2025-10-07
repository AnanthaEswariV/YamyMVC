namespace YamyProject.Core.ViewModel
{
    public class CustomerViewModel
    {
        public TblCustomer Customer { get; set; } = new TblCustomer();
       

        public TblTransaction Transactions { get; set; } = new TblTransaction();
        public IEnumerable<SelectListItem> Categoriess { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Citys { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Account { get; set; } = new List<SelectListItem>();
        public IEnumerable<SelectListItem> Countriess { get; set; } = new List<SelectListItem>();

        [Display(Name = "Search")]
        public string? SearchText { get; set; }
        public bool IsActive
        {
            get => Customer.Active == 0; // or != 0 depending on your logic
            set => Customer.Active = value ? 0 : 1;
        }
    }

}
