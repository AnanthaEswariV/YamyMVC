namespace YamyProject.Services.Interfaces
{
    public interface IListServices
    {
        Task<IEnumerable<TblCustomerCategory>> GetCustomerCategorysAsync();
        Task<IEnumerable<TblVendorCategory>> GetVenderCategorysAsync();
        Task<IEnumerable<TblWarehouse>> GetWarehousesAsync();
        Task<IEnumerable<TblCountry>> GetCountriesAsync();
        Task<IEnumerable<TblCity>> GetCitysAsync();
        Task<IEnumerable<TblCoaLevel4>> GetAccountsAsync();
        Task<IEnumerable<TblVendor>> GetVendorsAsync();
        Task<IEnumerable<TblCustomer>> GetCustomersAsync();

    }
}
