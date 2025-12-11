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
            var vendorsQuery = _context.TblCustomers
                              .AsNoTracking()
                              .Where(v => v.State == 0);
            var query = _context.TblCustomers
               .AsQueryable();
            return await query.ToListAsync();
            }
        public async Task<IEnumerable<VendorIndexViewModel>> GetAllcustomersAsync(string state)
            {
            var wanted = new[]
            {
              "Customer Receipt",
                                    "Sales Invoice",
                                    "Customer Opening Balance",
                                    "Customer Advance Payment",
                                    "Check Cancel (Customer)",
                                    "SalesReturn Invoice",
                                    "Credit Note",
                                    "PDC Receivable"
             };
            var vendorsQuery = _context.TblCustomers
                                  .AsNoTracking()
                                  .Where(v => v.State == 0);
            var items = await vendorsQuery
                .Select(v => new VendorIndexViewModel
                    {
                    Id = v.Id,
                    Name = (v.Name ?? ""),
                    Code = v.Code.ToString(),
                    WorkPhone = v.WorkPhone,
                    MainPhone = v.MainPhone,
                    CategoryName = _context.TblVendorCategories.Where(c => c.Id == v.CatId).Select(c => c.Name)
                         .FirstOrDefault(),
                    Region = v.Region,
                    Email = v.Email,
                    TRN = v.Trn,
                    Balance = v.Balance ?? 0m,
                    Date = v.Date,
                    OpeningDebit = _context.TblTransactions
                         .Where(t => t.State == 0 && t.HumId == v.Id && wanted.Contains(t.Type)).Sum(t => (decimal?)t.Debit) ?? 0m,
                    OpeningCredit = _context.TblTransactions.Where(t => t.State == 0 && t.HumId == v.Id && wanted.Contains(t.Type))
                          .Sum(t => (decimal?)t.Credit) ?? 0m,
                    Amount = _context.TblTransactions
                   .Where(t => t.State == 0 && t.HumId == v.Id
                            && wanted.Contains(t.Type)).Sum(t => (decimal?)(t.Credit - t.Debit)) ?? 0m,
                    Active = (v.Active == 0)
                    })
                .ToListAsync();

            return items;
        }

        public async Task<TblCustomer?> GetByIdAsync(int id)
        {
            return await _context.TblCustomers
                .Include(c => c.CatIdNavigation)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<IEnumerable<VendorTxnViewModel>> GetTransactionsAsync(int customerId)
        {
            var wantedTypes = new[]
        {
                 "Customer Receipt",
                 "Sales Invoice",
                 "Sales Invoice Cash",
                 "Customer Opening Balance",
                 "Customer Advance Payment",
                 "Check Cancel (Customer)",
                 "SalesReturn Invoice",
                 "Credit Note",
                 "PDC Receivable"
             };
            var Transaction=  await _context.TblTransactions
                .Where(t => t.HumId == customerId && t.State == 0 && wantedTypes.Contains(t.Type))
                .Select(t=>new VendorTxnViewModel
                    {
                    Id=t.Id,
                    invoiceId=t.ProjectId,
                    Date=t.Date,
                    VoucherNo = string.IsNullOrWhiteSpace(t.VoucherNo) ? "GV-00" + t.TransactionId:t.VoucherNo , //t.VoucherNo,
                    VoucherType =t.Type,
                    Description=t.Description,
                    Debit=(decimal)t.Debit,
                    Credit= (decimal)t.Credit,
                    Balance= (decimal)(t.Debit-t.Credit)
                    })
                .OrderBy(t => t.Date)
                .ToListAsync();
            return Transaction;
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
