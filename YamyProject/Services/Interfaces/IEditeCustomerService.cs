namespace YamyProject.Services.Interfaces
{
    public interface IEditeCustomerService
    {
        Task<CustomerViewModel> GetCustomerFormDataAsync(int? id);
        Task SaveCustomerAsync(TblCustomer customer);
        Task<CustomerViewModel> GetCreateCustomerFormAsync();
        string GenerateNextCustomerCode();
    }
}
