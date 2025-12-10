namespace YamyProject.Services.Interfaces
{
    public interface IEditeCustomerService
    {
        Task<CustomerViewModel> GetCustomerFormDataAsync(int? id);
        Task SaveCustomerAsync(CustomerViewModel customer);
        Task<CustomerViewModel> GetCreateCustomerFormAsync();
        string GenerateNextCustomerCode();
    }
}
