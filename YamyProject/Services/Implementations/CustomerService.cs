namespace YamyProject.Services
{
 
    public class CustomerService : ICustomerService
    {
        private readonly YamyDbContext _context;

        public CustomerService(YamyDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TblCustomer>> GetAllAsync(string state)
        {
            var query = _context.TblCustomers
                .Include(c => c.CatIdNavigation) // assuming navigation property
                .AsQueryable();

            if (state == "Active")
                query = query.Where(c => c.Active == 0);
            else if (state == "Inactive")
                query = query.Where(c => c.Active != 0);

            return await query.ToListAsync();
        }

        public async Task<TblCustomer?> GetByIdAsync(int id)
        {
            return await _context.TblCustomers
                .Include(c => c.CatIdNavigation)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<TblTransaction>> GetTransactionsAsync(int customerId)
        {
            return await _context.TblTransactions
                .Where(t => t.HumId == customerId && t.State == 0)
                .OrderBy(t => t.Date)
                .ToListAsync();
        }

        public async Task DeleteCustomerAsync(int id)
        {
            var customer = await _context.TblCustomers.FindAsync(id);
            if (customer == null) return;

            var hasTransactions = await _context.TblTransactions.AnyAsync(t => t.HumId == id);
            if (hasTransactions)
                throw new InvalidOperationException("Customer has transactions. Cannot delete.");

            _context.TblCustomers.Remove(customer);
            await _context.SaveChangesAsync();
        }
    }
}
