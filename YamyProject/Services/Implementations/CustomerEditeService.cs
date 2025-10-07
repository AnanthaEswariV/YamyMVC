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
               };

           
         if (id.HasValue)
          {
                    vm.Customer = await _context.TblCustomers
                     .FirstOrDefaultAsync(c => c.Id == id.Value) ?? new TblCustomer();

                    vm.Transactions = await _context.TblTransactions
                      .FirstOrDefaultAsync(t => t.HumId == id.Value && t.Type == "Customer Opening Balance") ?? new TblTransaction();
            }
            
            return vm;
        }

        public async Task<CustomerViewModel> GetCreateCustomerFormAsync()
        {
            // Initialize a new customer view model for creation
            var vm = new CustomerViewModel
            {
                Customer = new TblCustomer(),
                Transactions =new TblTransaction() // empty for new customer
            };
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
    