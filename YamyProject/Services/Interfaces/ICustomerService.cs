namespace YamyProject.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<TblCustomer>> GetAllAsync(string state);
        Task<TblCustomer?> GetByIdAsync(int id);
        Task<IEnumerable<TblTransaction>> GetTransactionsAsync(int customerId);
        Task DeleteCustomerAsync(int id);


    }
}
