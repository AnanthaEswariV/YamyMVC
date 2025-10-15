namespace YamyProject.Core.ViewModel
{
    public class SalesCenterPageViewModel
    {
        public IEnumerable<SelectListItem> Customers { get; set; }
        public IEnumerable<SalesCenterViewModel> Sales { get; set; }

        public int? CustomerId { get; set; }
        public DateOnly DateFrom { get; set; }  // tbl_sales.date
        public DateOnly DateTo { get; set; }  // tbl_sales.date

    }
}
