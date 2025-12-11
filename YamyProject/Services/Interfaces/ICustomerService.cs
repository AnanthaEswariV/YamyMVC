namespace YamyProject.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<TblCustomer>> GetAllAsync(string state);
        Task<IEnumerable<VendorIndexViewModel>> GetAllcustomersAsync(string state);
        Task<TblCustomer?> GetByIdAsync(int id);
        Task<IEnumerable<VendorTxnViewModel>> GetTransactionsAsync(int customerId);
        Task DeleteCustomerAsync(int id);


    }
}
