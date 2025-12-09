namespace YamyProject.Services.Interfaces
{
    public interface IListServices
    {
        //Customer
        Task<IEnumerable<TblCustomer>> GetCustomersAsync();
        Task<IEnumerable<TblCustomerCategory>> GetCustomerCategorysAsync();
        Task<IEnumerable<TblCustomer>> GetCustomersRetrunAsync();
        //Vendor
        Task<IEnumerable<TblVendor>> GetVendorsAsync();
        Task<IEnumerable<TblVendor>> GetVendorSubcontractorsAsync();
        Task<IEnumerable<TblVendorCategory>> GetVenderCategorysAsync();
        Task<IEnumerable<TblVendor>> GetVendorsRetrunAsync();
        Task<IEnumerable<TblEmployee>> GetEmployeeAsync();
         //other
        Task<IEnumerable<TblWarehouse>> GetWarehousesAsync();
        Task<IEnumerable<TblCountry>> GetCountriesAsync();
        Task<IEnumerable<TblCity>> GetCitysAsync();
        Task<int> GetCitysAsync(int Id);
        Task<IEnumerable<TblProject>> GetProjectAsync();
        Task<IEnumerable<TblCoaLevel4>> GetAccountsAsync();
        Task<IEnumerable<TblTax>> GetVatAsync();
        Task<IEnumerable<TblBank>> GetBanksAsync();
        Task<IEnumerable<TblSubCostCenter>> GetCostCenterAsync();
        Task<IEnumerable<TblFixedAssetsCategory>> GetFixedAssetAsync();
        Task<IEnumerable<TblBank>> BindCombosBindBankAccount();
        Task<IEnumerable<TblSecUser>> GetUsersAsync();
        Task AreDefaultAccountsSet(List<string> list);
        Task<int> DefaultAccountsSet(string Category);


        }
    }
