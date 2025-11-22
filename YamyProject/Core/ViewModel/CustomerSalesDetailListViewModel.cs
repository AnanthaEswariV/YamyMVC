namespace YamyProject.Core.ViewModel
    {
    public class CustomerSalesDetailListViewModel
        {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = "";

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public List<CustomerSalesDetailRowViewModel> Rows { get; set; } = new();

        public decimal? TotalAmount => Rows.Sum(r => r.Amount);
        }
    }
