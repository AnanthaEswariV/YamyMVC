namespace YamyProject.Services.Implementations
    {
    public class CreditNoteService(YamyDbContext context, IListServices listServices, IHttpContextAccessor httpContextAccessor, IGlobalService GlobalService) : ICreditNoteService
        {
        private readonly DateOnly Starting = default;
        private readonly DateOnly Ending = default;
        private string Customer = null;
        private readonly IListServices _ListServices = listServices;
        private readonly YamyDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IGlobalService _GlobalService = GlobalService;

        public async Task<MasterCreditNoteViewModel> QueryCreditNoteAsync(string selectCustomer = null,bool Custmer = true,DateOnly? from = default,DateOnly? to = default,bool Date = true,CancellationToken ct = default)
            {
            var query = _context.TblCreditNotes
                .Where(s => s.State == 0)
                .ToListAsync();

            // Customer filter
            //if (Custmer && !string.IsNullOrWhiteSpace(selectCustomer))
            //    {
            //    if (int.TryParse(selectCustomer, out var customerId))
            //        {
            //        query = query.Where(s =>
            //            s.CreditAccount == customerId ||
            //            s.CreditCustomer.Name == selectCustomer);
            //        }
            //    else
            //        {
            //        query = query.Where(s => s.CreditCustomer.Name == selectCustomer);
            //        }
            //    }

            //// Date filters
            //if (Date && from.HasValue)
            //    {
            //    query = query.Where(s => s.Date >= from.Value);
            //    }

            //if (Date && to.HasValue)
            //    {
            //    query = query.Where(s => s.Date <= to.Value);
            //    }
             var sales = await query;
              ////  .OrderBy(s => s.Date)
              //  .Select(s => new
              //      {
              //      s.Id,
              //      s.Date,
              //      InvNo = s.InvoiceId,
              //      TransactionId = (int?)(s.Transaction != null ? s.Transaction.TransactionId : (int?)null),
              //      s.Amount,
              //      s.Vat,
              //      s.Total,
              //      s.CreditAccount,
              //      s.DebitAccount
              //      })
              //  .ToListAsync();

            // If you really need JV with "000" prefix:
            var result = new List<MasterCreditViewModel>();
            int sn = 1;

            foreach (var s in sales)
                {
                result.Add(new MasterCreditViewModel
                    {
                    SN = sn,
                    JvNo = "000" + s.Id,// (s.Transaction.TransactionId ?? 0),
                    Id = s.Id,
                    Date = s.Date,
                    InvNo = s.InvoiceId,
                    Amount = s.Amount,
                    Vat = s.Vat,
                    Total = s.Total,
                    CreditAccount = s.CreditAccount,
                    DebitAccount = s.DebitAccount
                    });

                sn++;
                }

            var customers = await _ListServices.GetCustomersRetrunAsync();
            var customerSelectList = customers
                .Select(c => new TblCustomer
                    {
                    Id = c.Id,
                    Code = c.Code,
                    Name = c.Name
                    })
                .ToList();

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
            var lastCodeValue = await _context.TblCreditNotes
               .Select(s => s.InvoiceId.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;
            return $"CN-{int.Parse(prefix) + 1:D4}";
            }

        public async Task insertInvoice(CreditNoteViewModel model)
            {
            var strategy = _context.Database.CreateExecutionStrategy();
            var Code = await GenerateNextCreditNoteCode();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    var receiptvoucher = new TblCreditNote
                        {
                        Date = model.Date,
                        CreditAccount = model.CustomerId,
                        DebitAccount = model.DebitAccountId.Value,
                        Type = "Customer",
                        InvoiceId = Code,
                        Amount = model.Amount,
                        Vat = model.Vat,
                        Total = model.TotalAmount,
                        Description = model.Description,
                        CreatedBy = 1,
                        CreatedDate = DateOnly.FromDateTime(DateTime.Now),
                        State = 0
                        };
                    await _context.TblCreditNotes.AddAsync(receiptvoucher);
                    await _context.SaveChangesAsync();

                    await insertINV(model, receiptvoucher.Id, Code);
                    await addTransaction(model, receiptvoucher.Id, Code);
                    await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Add Credit Note", "Credit Note", receiptvoucher.Id, "Added Credit Note: " + Code);

                    }
                catch { }
            });
            }
        public async Task insertINV(CreditNoteViewModel Model, int RefId, string code)
            {
            var type = "SALES";
            foreach (var item in Model.Items)
                {
                var receiptvoucherDetails = new TblCreditNoteDetail
                    {
                    RefId = RefId,
                    InvNo = item.InvoiceNo,
                    InvoiceId = (int)item.SaleId,
                    InvoiceDate = item.InvDate,
                    InvoiceType = type,
                    Total = item.Total,
                    Vat = item.vat,
                    Amount = item.Amount,
                    Balance = item.Balance,
                    Remaining = item.Remaining
                    };
                _context.TblCreditNoteDetails.Add(receiptvoucherDetails);
                var result = await _context.TblSales
                       .Where(s => s.Id == item.SaleId)
                       .SumAsync(s => s.Change);
                var salesInvoice = await _context.TblSales
                      .Where(s => s.Id == item.SaleId)
                      .ExecuteUpdateAsync(updates => updates
                      .SetProperty(s => s.Pay, result)
                      .SetProperty(s => s.Change, item.Remaining));
                await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Add Credit Note Item", "Credit Note", RefId,
                    $"Added Credit Note Item: Invoice No. {item.InvoiceNo}, Invoice ID: {(int)item.SaleId}, Total: {item.Total}, VAT: {item.vat}, Amount: {item.Amount}");
                }
            }
        public async Task addTransaction(CreditNoteViewModel Model, int invId, string Code)
            {
            var level4CustomerId = await _ListServices.DefaultAccountsSet("Customer");
            var level4SalesReturn = await _ListServices.DefaultAccountsSet("SalesReturn");
            var level4VatId = await _ListServices.DefaultAccountsSet("Vat Output");
            await addTransactionEntry(Model.Date, level4CustomerId, 0, Model.Amount, invId, (int)Model.CustomerId, "Credit Note", "Credit Note", "Credit Note NO. " + Code,
          _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.UtcNow), Code);
            //vat
            //if(Model.)
            await addTransactionEntry(Model.Date, level4VatId, Model.Vat, 0, invId, 0, "Credit Note", "Credit Note", "Vat Output  Note NO. " + Code,
          _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.UtcNow), Code);
            //sales return
            await addTransactionEntry(Model.Date, level4SalesReturn, Model.Amount, 0, invId, 0, "Credit Note", "Credit Note", "Revenue Note NO. " + Code,
          _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.UtcNow), Code);
            }

        public async Task addTransactionEntry(DateOnly date, int? accountId, decimal debit, decimal credit, int transactionId, int humId, string type, string voucher_name,
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

        public async Task updateInvoice(CreditNoteViewModel model)
            {
            var updateCreditNote = await _context.TblCreditNotes
                .Where(s => s.Id == model.Id)
                .FirstOrDefaultAsync();
            if (updateCreditNote != null)
                {
                updateCreditNote.Date = model.Date;
                updateCreditNote.CreditAccount = model.CustomerId;
                updateCreditNote.DebitAccount = model.DebitAccountId.Value;
                updateCreditNote.InvoiceId = model.InvoiceNo;
                updateCreditNote.Amount = model.Amount;
                updateCreditNote.Vat = model.Vat;
                updateCreditNote.Total = model.TotalAmount;
                updateCreditNote.Description = model.Description;
                updateCreditNote.ModifiedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                updateCreditNote.ModifiedDate = DateOnly.FromDateTime(DateTime.Now);
                await _context.SaveChangesAsync();
                }
            await DeleteCostCenterTransactionEntryAsync(model.Id);
            await insertINV(model, model.Id, model.CustomerCode);
            await DeleteTransactionEntry(model.Id, "Credit Note");
            await addTransaction(model, model.Id,model.Code);
            await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Add Credit Note", "Credit Note", model.Id, "Added Credit Note: " + model.Code);


            }
        public async Task DeleteCostCenterTransactionEntryAsync(int? refId)
            {
            await _context.TblCreditNoteDetails
                .Where(t => t.RefId == refId)
                .ExecuteDeleteAsync();  
            }
        public async Task DeleteTransactionEntry(int? refId, string type)
            {
            await _context.TblTransactions
                .Where(t => t.TransactionId == refId && t.Type == type)
                .ExecuteDeleteAsync();  

            }


        }
    }