namespace YamyProject.Services.Implementations
    {
    public class CreditNoteService(YamyDbContext context, IListServices listServices) : ICreditNoteService
        {
        private DateOnly Starting = default;
        private DateOnly Ending = default;
        private string Customer = null;
        private readonly IListServices _ListServices = listServices;

        private readonly YamyDbContext _context = context;

        public async Task<MasterCreditNoteViewModel> QueryCreditNoteAsync(string selectCustomer = null, bool Custmer = true, DateOnly? from = default, DateOnly? to = default, bool Date = true, CancellationToken ct = default)
            {
            if (Custmer == true)
                Customer = selectCustomer;
            if (Date == true)
                {
                //  Starting = from;
                // Ending = to;
                }
            var query = _context.TblCreditNotes
           .Where(s => s.State == 0)
           .Include(s => s.Transaction)
           .Include(s => s.CreditCustomer)
           .OrderBy(s => s.Date)
           .AsQueryable();
            // Apply Customer filter if provided
            if (!string.IsNullOrEmpty(Customer))
                query = query.Where(s => s.CreditAccount == int.Parse(Customer) || s.CreditCustomer.Name == Customer);

            // Apply Starting date filter if provided
            if (Starting != default)
                query = query.Where(s => s.Date >= Starting);

            // Apply Ending date filter if provided
            if (Ending != default)
                query = query.Where(s => s.Date <= Ending);


            const string Collation = "utf8mb4_0900_ai_ci"; // or your column’s exact collation
            var sales = await query
                    .OrderBy(s => s.Date)
               //     .Where(S=>S.State==0)
                    .Select(s => new
                        {
                        s.Id,
                        s.Date,
                        JVNO = s.Transaction.TransactionId,
                        TransactionId = (int?)(s.Transaction != null ? s.Transaction.TransactionId : (int?)null),
                        InvNo = s.InvoiceId,
                        s.Amount,
                        s.Vat,
                        s.Total,
                        s.CreditAccount,
                        s.DebitAccount,                        
                        })
                    .GroupBy(x => new
                            {
                            x.Id,
                            x.Date,
                            x.JVNO,
                            x.InvNo,
                            x.Amount,
                            x.Total,
                            x.Vat,
                            x.CreditAccount,
                            x.DebitAccount,
                            })
                        .Select(g => new
                            {
                            g.Key.Id,
                            g.Key.Date,
                            InvNo = g.Key.InvNo,
                            JvNo = "000" + (g.Max(x => x.TransactionId) ?? 0),   
                            g.Key.Amount,
                            g.Key.Total,
                            g.Key.Vat,
                            g.Key.CreditAccount,
                            g.Key.DebitAccount,
                          
                        })
        .ToListAsync();

            var result = new List<MasterCreditViewModel>();

            int sn = 1;
            foreach (var s in sales)
                {
                result.Add(new MasterCreditViewModel
                    {
                    SN = sn,

                    JvNo = s.JvNo,
                    Id = s.Id,
                    Date = s.Date, // DateTime or DateOnly — make sure VM matches
                    InvNo = s.InvNo,
                    Amount = s.Amount,
                    Vat = s.Vat,
                    Total = s.Total,
                    CreditAccount = s.CreditAccount,
                    DebitAccount = s.DebitAccount
                    });


                }
            var customers = await _ListServices.GetCustomersRetrunAsync();
            var customerSelectList = customers.Select(c => new TblCustomer
                {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name
                }).ToList();
          
            return new MasterCreditNoteViewModel
                {
                FromDate = from,
                ToDate = to,
                MasterCredit = result,
                  Customers = customerSelectList
                };
            }
        public async Task<string> GenerateNextCreditNoteCode() 
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue =await  _context.TblCreditNotes
               .Select(s => s.InvoiceId.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;
            return $"CN-{int.Parse(prefix) + 1:D4}";
            }


        }
    }