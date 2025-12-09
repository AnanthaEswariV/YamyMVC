namespace YamyProject.Services.Implementations
    {
    public class GeneralJournalVouchersService(YamyDbContext context, IHttpContextAccessor httpContextAccessor, IGlobalService GlobalService) : IGeneralJournalVouchersService
        {
        private readonly YamyDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IGlobalService _GlobalService = GlobalService;

        public async Task<JournalVoucherMasterViewModel> GetCustmerData(DateOnly From = default, DateOnly To = default, bool All = true)
            {
            var query = _context.TblJournalVouchers
            .Where(s => s.State == 0)
            .OrderBy(s => s.Date)
            .AsQueryable();

            if (!All)
                {
                if (From != default)
                    query = query.Where(s => s.Date >= From);
                if (To != default)
                    query = query.Where(s => s.Date <= To);
                }

            var rows = await query
                .OrderBy(s => s.Date)
                .Select(s => new JournalVoucherMasterCustomerViewModel
                    {
                    Id = s.Id,
                    Date = s.Date,
                    JournalCode = s.Code,
                    JVNo = (s.Transaction != null && s.Transaction.TType == "JOURNAL"
                                    ? (s.Transaction.TransactionId ?? 0): 0).ToString().PadLeft(4, '0'),
                    //(s.Transaction == null && (s.Transaction.TType == null || s.Transaction.TType != "JOURNAL")
                    //        ? 0 : (s.Transaction.TransactionId ?? 0)
                    //            ).ToString().PadLeft(4, '0'),
                    DebitAmount = s.Debit ?? 0m,
                    CreditAmount = s.Credit ?? 0m
                    })
                .AsNoTracking()
                .ToListAsync();
            var journalVouchers = rows
       .GroupBy(x => x.Id)
       .Select(g => g.First())
       .OrderBy(x => x.Date)
       .ToList();
            var result = new JournalVoucherMasterViewModel
                {
                Customer = journalVouchers
                };
            return result;
            }
        public async Task<IEnumerable<JournalVoucherMasterCustomerDetailsViewModel>> GetJVDetails(int id)
            {
            var journalVouchersDetails = await _context.TblJournalVoucherDetails
                .AsNoTracking()
                .Where(j => j.Id == id)
                .Select(j => new JournalVoucherMasterCustomerDetailsViewModel
                    {
                    Date = j.Date,
                    Name = j.Account.Name,
                    DebitAmount = j.Debit,
                    CreditAmount = j.Credit,
                    Partner = j.Partner,
                    Description = j.Description
                    }).ToListAsync();
            return journalVouchersDetails;
            }
        public async Task<string> GenerateNextReceiptCode()
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue = await _context.TblJournalVouchers
               .Select(s => s.Code.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;
            return $"JV-{int.Parse(prefix) + 1:D4}";
            }
        public async Task<int> GenerateNextReceiptId()
            {
            var prefix = 1;
            var lastIdValue = await _context.TblJournalVouchers
              .Select(s => s.Id)
              .MaxAsync();
            prefix = (lastIdValue !=0)? lastIdValue+1: prefix;
            return prefix;
            }
        public async Task CreateJournalVoucher(JournalVoucherViewModel Model)
            {

            var invoiceNo =  await GenerateNextReceiptCode();
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    var journalVoucher = new TblJournalVoucher
                        {
                        Code = invoiceNo,
                        Date = Model.Date,
                        Debit = Model.DrAmount,
                        Credit = Model.CrAmount,
                        CreatedBy = -_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0,
                        CreatedDate = DateOnly.FromDateTime(DateTime.Now),
                        State = 0
                        };
                    _context.TblJournalVouchers.Add(journalVoucher);
                    await _context.SaveChangesAsync();
                    var journalId= journalVoucher.Id;

                    foreach (var Customer in Model.Customer)
                        {
                        var journalVoucherDetail = new TblJournalVoucherDetail
                            {
                            Date = Model.Date,
                            Debit = Customer.DebitAmount,
                            Credit = Customer.CreditAmount,
                            InvId= journalId,
                            Description = Customer.Description,
                            Partner = Customer.Partner,
                            AccountId = Customer.Id
                            };
                        _context.TblJournalVoucherDetails.Add(journalVoucherDetail);
                        await _context.SaveChangesAsync();
                        await AddTransactionEntry(Model.Date, Customer.Id, Customer.DebitAmount, Customer.CreditAmount,
                            journalId, 0, "JOURNAL VOUCHER", "JOURNAL", "Journal Voucher NO. "+ invoiceNo, -_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0,
                            DateOnly.FromDateTime(DateTime.Now), invoiceNo);
                        }
                    await _GlobalService.LogAudit(-_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Create Journal Voucher", "Journal Voucher", journalId, "Created Journal Voucher: " + invoiceNo);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    }
                catch
                    {
                    await tx.RollbackAsync();
                    throw;
                    }
            });
            
            }
        public async Task UpdateJournalVoucher(JournalVoucherViewModel Model)
            {
            var invoiceNo = string.IsNullOrWhiteSpace(Model.JournalCode)
             ? await GenerateNextReceiptCode()
             : Model.JournalCode.Trim();
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                {
                    var JournalVoucher = _context.TblJournalVouchers.Find(Model.JournalId);
                    if (JournalVoucher == null) throw new Exception("Journal Vouchers not found");

                    JournalVoucher.Date= Model.Date;
                    JournalVoucher.Debit= Model.DrAmount;
                    JournalVoucher.Credit= Model.CrAmount;
                    JournalVoucher.ModifiedBy = -_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                    JournalVoucher.ModifiedDate = DateOnly.FromDateTime(DateTime.Now);
                    _context.TblJournalVouchers.Update(JournalVoucher);
                    await _context.SaveChangesAsync();

                    var JorunalDetails = await _context.TblJournalVoucherDetails
                   .Where(d => d.InvId == Model.JournalId)
                   .ToListAsync();
                    _context.TblJournalVoucherDetails.RemoveRange(JorunalDetails);

                   var Transaction = await _context.TblTransactions
                   .Where(d => d.TransactionId == Model.JournalId && d.TType== "JOURNAL")
                   .ToListAsync();
                    _context.TblTransactions.RemoveRange(Transaction);

                    foreach (var Customer in Model.Customer)
                        {
                        var journalVoucherDetail = new TblJournalVoucherDetail
                            {
                            InvId = Model.JournalId,
                            Date = Model.Date,
                            AccountId = Customer.Id,
                            Debit = Customer.DebitAmount,
                            Credit = Customer.CreditAmount,
                            Description = Customer.Description,
                            Partner = Customer.Partner
                            };
                        _context.TblJournalVoucherDetails.Add(journalVoucherDetail);
                        await _context.SaveChangesAsync();
                        await AddTransactionEntry(Model.Date, Customer.Id, Customer.DebitAmount, Customer.CreditAmount,
                            Model.JournalId, 0, "JOURNAL VOUCHER", "JOURNAL", "Journal Voucher NO. " + invoiceNo, -_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0,
                            DateOnly.FromDateTime(DateTime.Now), JournalVoucher.Code);
                        }
                    await _GlobalService.LogAudit(-_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Update Journal Voucher", "Journal Voucher", Model.JournalId, "Updated Journal Voucher: " + invoiceNo);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch
                {
                    await tx.RollbackAsync();
                    throw;
                }
            }); 
    }
        //Add Transaction Entry
        public async Task AddTransactionEntry(DateOnly date, int? accountId, decimal debit, decimal credit, int transactionId, int humId, string type, string voucher_name,
                    string description, int createdBy, DateOnly createdDate, string VoucherNo)
            {
            var trx = new TblTransaction
                {
                Date = date,
                AccountId = accountId,
                Debit = debit,
                Credit = credit,
                TransactionId = transactionId,
                HumId = humId,
                TType = voucher_name,
                Type = type,
                Description = description,
                CreatedBy = createdBy,
                VoucherNo = voucher_name,
                CreatedDate = createdDate,
                State = 0
                };
            _context.TblTransactions.Add(trx);
            await _context.SaveChangesAsync();
            }

        public async Task<JournalVoucherViewModel> GEtJournalVoucher(int Id)
            {
            var JournalVoucher =await _context.TblJournalVouchers
                .Include(j=>j.JournalDetils)
                        .FirstOrDefaultAsync(j=>j.Id== Id);
            return new JournalVoucherViewModel
                {
                Date = JournalVoucher.Date ?? DateOnly.FromDateTime(DateTime.Now),
                DrAmount = JournalVoucher.Debit ?? 0m,
                CrAmount = JournalVoucher.Credit ?? 0m,
                JournalCode = JournalVoucher.Code,
                JournalId = JournalVoucher.Id,
                Customer = JournalVoucher.JournalDetils.Select(j => new JournalVoucherCustomerViewModel
                    {
                    Id = j.AccountId,
                    AccountCode = j.Account.Code,
                    DebitAmount = j.Debit,
                    CreditAmount = j.Credit,
                    Description = j.Description,
                    Partner = j.Partner
                    }).ToList()
                };
            }
        public class PartnerLookupDto
            {
            public int Id { get; set; }
            public string Name { get; set; }  // "CODE - NAME"
            }

        //public async Task<List<PartnerLookupDto>> GetPartnersByAccountNameAsync(string accountName)
        //    {
        //    // Get the account id once (same in all three sub-queries)
        //    var accountId = await _context.TblCoaLevel4s
        //        .Where(a => a.Name == accountName)
        //        .Select(a => a.Id)
        //        .FirstOrDefaultAsync();

        //    if (accountId == 0)
        //        return new List<PartnerLookupDto>();

        //    // vendors
        //    var vendors = _context.TblVendors
        //        .AsNoTracking()
        //        .Where(v => v.AccountId == accountId)
        //        .Select(v => new PartnerLookupDto
        //            {
        //            Id = v.Id,
        //            Name = v.Code + " - " + v.Name
        //            });

        //    // customers
        //    var customers = _context.TblCustomers
        //        .AsNoTracking()
        //        .Where(c => c.AccountId == accountId)
        //        .Select(c => new PartnerLookupDto
        //            {
        //            Id = c.Id,
        //            Name = c.Code + " - " + c.Name
        //            });

        //    // employees
        //    var employees = _context.TblEmployees
        //       .AsNoTracking()
        //        .Where(e => e.AccountId == accountId)
        //        .Select(e => new PartnerLookupDto
        //            {
        //            Id = e.Id,
        //            Name = e.Code + " - " + e.Name
        //            });

        //    // UNION ALL  ->  Concat in LINQ
        //    //var result = await vendors
        //    //    .Concat(customers)
        //    //    .Concat(employees)
        //    //    .ToListAsync();
        //    List<PartnerLookupDto> result = new(vendors.Count + customers.Count + employees.Count);
        //    result.AddRange(vendors);
        //    result.AddRange(customers);
        //    result.AddRange(employees);

        //   // return result;
        //    return result;
        //    }
        public async Task<List<PartnerLookupDto>> GetPartnersByAccountNameAsync(string accountName)
            {
            // 1) Guard clause
            if (string.IsNullOrWhiteSpace(accountName))
                return new List<PartnerLookupDto>();

            accountName = accountName.Trim();

            // 2) Resolve account id by NAME (or CODE if needed)
            var accountId = await _context.TblCoaLevel4s
                .AsNoTracking()
                .Where(a => a.Name == accountName)
                .Select(a => (int?)a.Id)
                .FirstOrDefaultAsync();

            // If not found, return empty
            if (accountId == null)
                return new List<PartnerLookupDto>();

            // 3) Load each partner type separately (no Concat in SQL)
            var vendors = await _context.TblVendors
                .AsNoTracking()
                .Where(v => v.AccountId == accountId.Value)
                .Select(v => new PartnerLookupDto
                    {
                    Id = v.Id,
                    Name = v.Code + " - " + v.Name
                    })
                .ToListAsync();

            var customers = await _context.TblCustomers
                .AsNoTracking()
                .Where(c => c.AccountId == accountId.Value)
                .Select(c => new PartnerLookupDto
                    {
                    Id = c.Id,
                    Name = c.Code + " - " + c.Name
                    })
                .ToListAsync();

            var employees = await _context.TblEmployees
                .AsNoTracking()
                .Where(e => e.AccountId == accountId.Value)
                .Select(e => new PartnerLookupDto
                    {
                    Id = e.Id,
                    Name = e.Code + " - " + e.Name
                    })
                .ToListAsync();

            // 4) Combine in memory (equivalent to UNION ALL)
            var result = new List<PartnerLookupDto>(vendors.Count + customers.Count + employees.Count);
            result.AddRange(vendors);
            result.AddRange(customers);
            result.AddRange(employees);

            return result;
            }

        }
    }