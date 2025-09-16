using System;
using YamyProject.Services.Interfaces;

namespace YamyProject.Services.Implementations
{


    public class CustomerEditeService : IEditeCustomerService
    {
        private readonly YamyDbContext _context;
        public CustomerEditeService(YamyDbContext context) { _context = context; }

        public async Task<CustomerViewModel> GetCustomerFormDataAsync(int? id)
        {
            var vm = new CustomerViewModel
            {
                Categories = await _context.TblCustomerCategories.ToListAsync(),
                Countries = await _context.TblCountries.ToListAsync(),
                Accounts = await _context.TblCoaLevel4s.ToListAsync(),
                Cities = await _context.TblCities.ToListAsync()
            };

            if (id.HasValue)
            {
                vm.Customer = await _context.TblCustomers
                    .Include(c => c.Country)
                 //   .Include(c => c.Countrys)
                    .FirstOrDefaultAsync(c => c.Id == id.Value);

                //vm.Cities = await _context.TblCities
                //    .Where(x => x.CountryId.Equals( vm.Customer.Country)).ToListAsync();

                vm.Transactions = await _context.TblTransactions
                    .Where(t => t.HumId == id.Value).ToListAsync();
            }
            else
            {
                vm.Customer = new TblCustomer();
                vm.Cities = new List<TblCity>();
                vm.Transactions = new List<TblTransaction>();
            }
            return vm;
        }

        public async Task SaveCustomerAsync(TblCustomer customer)
        {
            if (customer.Id == 0)
                _context.TblCustomers.Add(customer);
            else
                _context.TblCustomers.Update(customer);

            await _context.SaveChangesAsync();
        }

    }

}
