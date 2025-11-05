namespace YamyProject.Services.Implementations
    {
    public class ListServices : IListServices
        {

        private readonly YamyDbContext _context;

        public ListServices(YamyDbContext context)
            {
            _context = context;
            }

        //Customer
        public async Task<IEnumerable<TblCoaLevel4>> GetAccountsAsync()
            {
            return await _context.TblCoaLevel4s.ToListAsync();
            }
        public async Task<IEnumerable<TblCustomer>> GetCustomersAsync()
            {
            return await _context.TblCustomers.ToListAsync();
            }
        public async Task<IEnumerable<TblCustomer>> GetCustomersRetrunAsync()
            {
            return await _context.TblCustomers.AsNoTracking()
                                              .Where(c => _context.TblSales.Any(s => s.CustomerId == c.Id))
                                              .ToListAsync();
            }
        public async Task<IEnumerable<TblCustomerCategory>> GetCustomerCategorysAsync()
            {
            return await _context.TblCustomerCategories.ToListAsync();
            }
        public async Task<IEnumerable<TblEmployee>> GetEmployeeAsync()
            {
            return await _context.TblEmployees.ToListAsync();
            }
        //Vendor
        public async Task<IEnumerable<TblVendor>> GetVendorsAsync()
            {
            return await _context.TblVendors.ToListAsync();
            }
        public async   Task<IEnumerable<TblVendor>> GetVendorSubcontractorsAsync()
            {
            return await _context.TblVendors.Where(t=>t.Type== "Subcontractor").ToListAsync();
            }
        public async Task<IEnumerable<TblVendorCategory>> GetVenderCategorysAsync()
            {
            return await _context.TblVendorCategories.ToListAsync();
            }
        public async Task<IEnumerable<TblVendor>> GetVendorsRetrunAsync()
            {
            return await _context.TblVendors.AsNoTracking()
                                              .Where(c => _context.TblPurchases.Any(s => s.VendorId == c.Id))
                                              .ToListAsync();
            }
        //other
        public async Task<IEnumerable<TblFixedAssetsCategory>> GetFixedAssetAsync()
            {

            return await _context.TblFixedAssetsCategories.ToListAsync();
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
        public async Task<IEnumerable<TblBank>> BindCombosBindBankAccount()
            {
            return (IEnumerable<TblBank>)await _context.TblBanks
                           .AsNoTracking()
                           .Where(c => c.State == 0 &&
                                       _context.TblBankCards.Any(s => s.BankId == c.Id && s.CompanyAc == 1))
                           .Select(c => new
                               {
                               name = c.Code + " - " + c.Name,
                               id = c.Id
                               })
                           .ToListAsync();
            }
        public async Task<IEnumerable<TblTax>> GetVatAsync()
            {
            return await _context.TblTaxes.ToListAsync();
            }
        public async Task<IEnumerable<TblSubCostCenter>> GetCostCenterAsync()
            {
            return await _context.TblSubCostCenters.ToListAsync();
            }
        public async Task AreDefaultAccountsSet(List<string> list)
            {
            var count = await _context.TblCoaConfigs
               .AsNoTracking()
               .Where(t => t.Category != null && list.Contains(t.Category))
                .CountAsync();
            //if count < 0
            //    return true;
            //else
            //    return false;
            }
        }
    }