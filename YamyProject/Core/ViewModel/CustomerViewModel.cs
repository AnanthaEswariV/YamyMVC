namespace YamyProject.Core.ViewModel
{
    public class CustomerViewModel
    {
        public TblCustomer Customer { get; set; }
        public IEnumerable<TblCustomerCategory> Categories { get; set; }
        public IEnumerable<TblCountry> Countries { get; set; }
        public IEnumerable<TblCity> Cities { get; set; }
        public IEnumerable<TblCoaLevel4> Accounts { get; set; }
        public IEnumerable<TblTransaction> Transactions { get; set; }
    }

}
