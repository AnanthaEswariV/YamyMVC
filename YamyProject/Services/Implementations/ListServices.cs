using Microsoft.EntityFrameworkCore;

namespace YamyProject.Services.Implementations
{
    public class ListServices:IListServices
    {

        private readonly YamyDbContext _context;

        public ListServices(YamyDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<TblCustomerCategory>> GetCustomerCategorysAsync()
        {
            return await _context.TblCustomerCategories.ToListAsync();
        }
        public async Task<IEnumerable<TblVendorCategory>> GetVenderCategorysAsync()
        {
            return await _context.TblVendorCategories.ToListAsync();
        }
        public async Task<IEnumerable<TblWarehouse>> GetWarehousesAsync()
        {
            return await _context.TblWarehouses.ToListAsync();
        }

        public async Task<IEnumerable<TblCity>> GetCitysAsync()
        {
            return await _context.TblCities.ToListAsync();
        }
        public async Task<IEnumerable<TblCountry>> GetCountriesAsync()
        {
            return await _context.TblCountries.ToListAsync();
        }

        public async Task<IEnumerable<TblCoaLevel4>> GetAccountsAsync()
        {
            return await _context.TblCoaLevel4s.ToListAsync();
        }
        public async Task<IEnumerable<TblVendor>> GetVendorsAsync()
        {
            return await _context.TblVendors.ToListAsync();
        }
        public async Task<IEnumerable<TblCustomer>> GetCustomersAsync()
        {
            return await _context.TblCustomers.ToListAsync();
        }
    }
}
