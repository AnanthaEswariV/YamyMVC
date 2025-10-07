namespace YamyProject.Core.ViewModel
{
    public class CustomerCenterViewModel
    {
        public IEnumerable<TblCustomer> Customers { get; set; } = new List<TblCustomer>();
        public TblCustomer? SelectedCustomer { get; set; }
        public IEnumerable<TblTransaction> Transactions { get; set; } = new List<TblTransaction>();

        public string State { get; set; } = "Active";
        public string? SearchText { get; set; }
    }
}
