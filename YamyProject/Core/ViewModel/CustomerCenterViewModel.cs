namespace YamyProject.Core.ViewModel
{
    public class CustomerCenterViewModel
    {
        public IEnumerable<VendorIndexViewModel> Customers { get; set; } 
        public IEnumerable<TblCustomer> Customer { get; set; } = new List<TblCustomer>();
        public TblCustomer? SelectedCustomer { get; set; }
        public IEnumerable<TblTransaction> Transaction { get; set; } = new List<TblTransaction>();
        public IEnumerable<VendorTxnViewModel> Transactions { get; set; } 

        public string State { get; set; } = "Active";
        public string? SearchText { get; set; }
    }
}
