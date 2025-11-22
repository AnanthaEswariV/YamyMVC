namespace YamyProject.Core.ViewModel
    {
    public class CustomerSalesListViewModel
        {
        public List<CustomerSalesRowViewModel> Rows { get; set; } = new();

        // computed total like your TOTAL row
        public decimal TotalOpeningBalance => Rows.Sum(r => r.OpeningBalance);
        public string CustomerName { get; set; } = "";

        public DateOnly? DateFrom { get; set; }
        public DateOnly? DateTo { get; set; }
        }
    }
