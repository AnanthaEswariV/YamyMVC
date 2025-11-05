namespace YamyProject.Services.Implementations
    {
  
    public class ReceiptVoucherService(YamyDbContext context) : IReceiptVoucherService
        {
        private DateOnly Starting = default;
        private DateOnly Ending = default;
        private readonly YamyDbContext _context= context;
     
        public async Task<ReceiptVoucherCenterViewModel> QuerySalesAsync(DateOnly from = default, DateOnly to = default, bool Date = true, CancellationToken ct = default)
            {
            if (Date == true)
                {
               
                    
                    if (from == default)
                        {
                        from = DateOnly.FromDateTime(DateTime.Today);
                        }
                    if (to == default)
                        {
                        to = DateOnly.FromDateTime(DateTime.Today);
                        }
                    Starting = from;
                    Ending = to;
                    }
            //var transactions = await _context.TblTransactions.ToListAsync();

            var query = _context.TblReceiptVouchers
              .Where(s => s.State == 0)
              .Include(s => s.Transaction)
              .Include(s => s.CreditAccount)
              .Include(s => s.DebitAccount)
              .Include(s => s.ReceiptVoucher)
              .Include(s => s.Customer)
              .OrderBy(s => s.Date)
              .AsQueryable();

            // Apply Customer filter if provided
          
            // Apply Starting date filter if provided
            if (Starting != default)
                query = query.Where(s => s.Date >= Starting);

            // Apply Ending date filter if provided
            if (Ending != default)
                query = query.Where(s => s.Date <= Ending);

          
     //       const string Collation = "utf8mb4_0900_ai_ci"; // or your column’s exact collation

            var sales = await query
                .OrderBy(s=>s.Date)
                .Select(s => new
        {
            s.ReceiptVoucher.Id,
            s.ReceiptVoucher.Date,
            ReceiptCode = s.Code,
            JVNO = s.ReceiptVoucher.Id.ToString("D4"),
            s.Amount,
            DebitAccount  = s.DebitAccount.Name,
            CreditAccount = s.CreditAccount.Name
        })
    .ToListAsync();

            int sn = 0;
            var list = sales.Select(r => new ReceiptVouchersViewModel
                {
                SN = ++sn,
                Id = r.Id,
                Date = r.Date , // if r.Date is nullable DateTime
                JVNO = r.Id.ToString("D4"),
                ReceiptCode = r.ReceiptCode,
                Amount = r.Amount,
                DebitAccount = r.DebitAccount,
                CreditAccount = r.CreditAccount,
                ReceiptVoucherDetail = Enumerable.Empty<RvItemViewModel>() // map real items if/when needed
                }).ToList();

            return new ReceiptVoucherCenterViewModel
                {
                FromDate = from,
                ToDate = to,
                All = Date,
                ReceiptVouchers = list
                };
} 
            
        public async Task<string> GenerateNextReceiptCode() 
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue =await _context.TblReceiptVouchers
               .Select(s => s.Code.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;

            return $"RV-{int.Parse(prefix) + 1:D4}";
            }
        public async Task<string> GenerateNextReceiptId() 
            {
            string newCode = "0"; // Prefix for Credit Note
            var lastCodeValue =await _context.TblReceiptVouchers
               .Select(s => s.Code.Substring(3))
               .MaxAsync();
            newCode = lastCodeValue ?? newCode;
            return $"{int.Parse(newCode) + 1:D4}";
            }

        }
    }