namespace YamyProject.Services.Implementations
    {
  
    public class ReceiptVoucherService(YamyDbContext context, IHttpContextAccessor httpContextAccessor, IGlobalService GlobalService) : IReceiptVoucherService
        {
        private DateOnly Starting = default;
        private DateOnly Ending = default;
        private readonly YamyDbContext _context= context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IGlobalService _GlobalService = GlobalService;

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
       
        public async Task<ReceiptVoucherViewModel> GetReceiptVoucherByIdAsync(int id)
            {
            var PaymentVoucher = await _context.TblReceiptVouchers
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Include(s => s.Transaction)
                .Include(s => s.ReceiptVoucher)
                .Include(s => s.Customer)
                .Select(s => new ReceiptVoucherViewModel
                    {
                    Id = s.Id,
                    VoucherNo = s.Code,
                    Date = (DateOnly)s.Date,
                    Amount = (decimal)s.Amount,
                    PaymentType = s.Type == null ? null : (s.Type == "Customer" ? 1  : (int?)null),
                    DebitAccountId = s.DebitAccountId,
                    DebitCostCenterId = s.DebitCostCenterId,
                    CreditAccountId = s.CreditAccountId,
                    CreditCostCenterId = s.CreditCostCenterId,
                    PaymentMethod = s.Method == null ? null : (s.Method == "Cash" ? 1 : s.Method == "Cheque" ? 2 : s.Method == "Transfer" ? 3 : (int?)null),
                    Debit = new PartySide
                        {
                        Note = s.Description
                        },
                    Credit = new PartySide
                        {
                        transferDate = s.TransDate,
                        TRNSNAme = s.TransName,
                        TRNSRef = s.TransRef,
                     //   BankAccountId = (int)s.BankAccountId!,
                       // BankId = (int)s.BankId!,
                       // BookNo = (int)s.BookNo!,
                        ChequeName = s.CheckName!,
                        IsCheque = s.Method == "Cheque",
                        ChequeNo = s.CheckNo,
                        ChequeDate = s.CheckDate
                        },
                    Items = s.ReceiptVoucher == null
                              ? new List<RvItemViewModel>()
                              : new List<RvItemViewModel> {
                                  new RvItemViewModel {
                                      SN = 1,
                                      humId = s.ReceiptVoucher.HumId,
                                      invId = s.ReceiptVoucher.InvId,
                                      Date = (DateOnly)s.ReceiptVoucher.Date!,
                                      InvoiceNo = s.Code,
                                      Amount = (decimal)s.ReceiptVoucher.Total!,
                                      Payment = (decimal)s.ReceiptVoucher.Payment!,
                                      Description = s.ReceiptVoucher.Description,
                                      Pay =false
                                      } }
                    })
                .FirstOrDefaultAsync();
            return PaymentVoucher!;
            }

        public async Task UpdatePvAsync(ReceiptVoucherViewModel model)
            {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
           {
               await using var tx = await _context.Database.BeginTransactionAsync();

               try
                   {
                   var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                   var rv = await _context.TblReceiptVouchers
                                  .FirstOrDefaultAsync(r => r.Id == model.Id);


                   // 2) Map basic fields (similar to CreatePvAsync)
                   var paymentTypeText = model.PaymentTypes
                       .FirstOrDefault(i => i.Value == model.PaymentType?.ToString())?.Text;

                   var methodText = model.PaymentMethods
                       .FirstOrDefault(i => i.Value == model.PaymentMethod?.ToString())?.Text;

                   rv.Date = model.Date;
                   // keep existing code or use model.Code if you post it:
                   // rv.Code           = model.Code;
                   rv.Type = paymentTypeText;
                   rv.Method = methodText;
                   rv.Amount = model.Amount;
                   rv.DebitAccountId = model.DebitAccountId;
                   rv.DebitCostCenterId = model.DebitCostCenterId;
                   rv.CreditAccountId = model.CreditAccountId;
                   rv.CreditCostCenterId = model.CreditCostCenterId;
                   rv.Description = model.Debit?.Note;

                   // if you have these columns in TblReceiptVoucher:
                   rv.ModifiedBy = userId;
                   rv.ModifiedDate = DateOnly.FromDateTime(DateTime.Now);

                   // 3) Payment-method specific fields
                   if (string.Equals(methodText, "Cheque", StringComparison.OrdinalIgnoreCase))
                       {
                       var chequeId = await GetChequeBookIdByCompanyOrFirstAsync();
                       var OlderCheck = await _context.TblCheckDetails
                           .FirstOrDefaultAsync(c =>
                               c.PvcNo == model.Id &&
                               c.CheckType == "Receipt" &&
                               c.CheckId == chequeId);
                       if (OlderCheck.Id > 0)
                           {
                           OlderCheck.Date = model.Date;
                           OlderCheck.CheckNo = int.Parse(model.Debit.ChequeNo);
                           OlderCheck.CheckDate = model.Debit.ChequeDate;
                           OlderCheck.CheckName = model.Debit.ChequeName;
                           OlderCheck.Amount = model.Amount;
                           OlderCheck.PvcNo = model.Id;
                           OlderCheck.CheckType = "Receipt";
                           OlderCheck.CheckId = chequeId;
                           }
                       else
                           {
                           var checkDetailId = new TblCheckDetail
                               {
                               Date = model.Date,
                               CheckId = chequeId,
                               CheckNo = int.Parse(model.Debit.ChequeNo),
                               CheckDate = model.Debit.ChequeDate,
                               CheckType = "Receipt",
                               PvcNo = rv.Id,
                               CheckName = model.Debit.ChequeName,
                               Amount = model.Amount,
                               State = "New"
                               };
                           _context.TblCheckDetails.Add(checkDetailId);
                           }

                       }
                   await insertINV(model, (int)model.Id, model.Code);
                   await DeleteCostCenterTransactionEntryAsync(model.Id, "Receipt");
                   await InsertCostCenterTransactionAsync(model.Date, model.Amount, 0, (int)model.Id, "Receipt", "Receipt Debit Entry", (int)model.DebitCostCenterId);
                   await InsertCostCenterTransactionAsync(model.Date, 0, model.Amount, (int)model.Id, "Receipt", "Receipt Credit Entry", (int)model.CreditCostCenterId);

                   _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Updated Receipt Voucher", "Receipt Voucher", (int)model.Id, "Updated Receipt Voucher: " + model.Code);

                   }
               catch { }

           });
         }

        public async Task CreatePvAsync(ReceiptVoucherViewModel Model)
            {
            var strategy = _context.Database.CreateExecutionStrategy();
            var Code = await GenerateNextReceiptCode();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try { 
                    var receiptvoucher= new TblReceiptVoucher
                        {
                        Date = Model.Date,
                        Code =  Code,
                        Type = Model.PaymentTypes.FirstOrDefault(i => i.Value == Model.PaymentType?.ToString())?.Text,
                        Method = Model.PaymentMethods.FirstOrDefault(i => i.Value == Model.PaymentMethod?.ToString())?.Text,
                        Amount = Model.Amount,
                        DebitAccountId = Model.DebitAccountId,
                        DebitCostCenterId = Model.DebitCostCenterId,
                        Description = Model.Debit.Note,
                        CreditAccountId = Model.CreditAccountId,
                        CreditCostCenterId = Model.CreditCostCenterId,
                        BankId = Model.Debit.BankId,
                        BankAccountId = Model.Debit.BankAccountId,
                        BookNo = Model.Debit.BookNo,
                        CheckName = Model.Debit.ChequeName,
                        CheckNo = Model.Debit.ChequeNo,
                        CheckDate = Model.Debit.ChequeDate,
                        TransDate = Model.Debit.TransFerDate,
                        TransName = Model.Debit.TRNSNAme,
                        TransRef = Model.Debit.TRNSRef,
                        CreatedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, // TODO: Current User
                        CreatedDate = DateOnly.FromDateTime(DateTime.Now),
                        State = 0
                        };
                    _context.TblReceiptVouchers.Add(receiptvoucher);
                    await _context.SaveChangesAsync();
                    if (Model.PaymentMethods.FirstOrDefault(i => i.Value == Model.PaymentMethod?.ToString())?.Text== "Cheque")
                        {
                        var chequeId = await GetChequeBookIdByCompanyOrFirstAsync();
                        var checkDetailId = new TblCheckDetail
                            { 
                            Date=Model.Date,
                            CheckId= chequeId,
                            CheckNo=int.Parse(Model.Debit.ChequeNo),
                            CheckDate=Model.Debit.ChequeDate,
                            CheckType= "Receipt",
                            PvcNo= receiptvoucher.Id,
                            CheckName= Model.Debit.ChequeName,
                            Amount=Model.Amount,
                            State= "New"
                            };                        
                        }
                   await insertINV(Model, receiptvoucher.Id, Code);
                  //  await DeleteCostCenterTransactionEntryAsync(receiptvoucher.Id, "Receipt");
                   await InsertCostCenterTransactionAsync(Model.Date, Model.Amount, 0, receiptvoucher.Id, "Receipt", "Receipt Debit Entry",(int)Model.DebitCostCenterId);
                   await InsertCostCenterTransactionAsync(Model.Date,0, Model.Amount, receiptvoucher.Id, "Receipt", "Receipt Credit Entry", (int)Model.CreditCostCenterId);

                    _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Create Receipt Voucher", "Receipt Voucher", receiptvoucher.Id, "Created Receipt Voucher: " + Code);
                    }
                catch
                    { }
            });
            }
        public async Task InsertCostCenterTransactionAsync(DateOnly date,decimal debit,decimal credit,int refId,string type,string description,int cost_center_id)
            {
            var entity = new TblCostCenterTransaction
                {
                Date = date,
                Type = type,
                RefId = refId,
                Debit = debit,
                Credit = credit,
                Description = description,
                CostCenterId = cost_center_id
                };

            _context.TblCostCenterTransactions.Add(entity);
            await _context.SaveChangesAsync();
            }
        public async Task DeleteCostCenterTransactionEntryAsync(int? refId, string type)
            {
            await _context.TblCostCenterTransactions
                .Where(t => t.Type == type && t.RefId == refId)
                .ExecuteDeleteAsync();   // EF Core 7+ bulk delete
            }
        //public async Task updatePvAsync(ReceiptVouchersViewModel Model)
        //    {
        //    var strategy = _context.Database.CreateExecutionStrategy();

        //    await strategy.ExecuteAsync(async () =>
        //    {
        //        await using var tx = await _context.Database.BeginTransactionAsync();
        //        try
        //            {
        //            //1) Update Payment Voucher
        //            var PaymentVoucher = _context.TblReceiptVouchers.Find(Model.Id);
        //            if (PaymentVoucher == null) throw new KeyNotFoundException($"Sales Invoice with ID {Model.Id} not found.");
        //            PaymentVoucher.Date = Model.Date;
        //            PaymentVoucher.Code = Model.JVNO;
        //            PaymentVoucher.Type = Model.PaymentTypes.FirstOrDefault(i => i.Value == Model.PaymentType?.ToString())?.Text;
        //            PaymentVoucher.Method = Model.PaymentMethods.FirstOrDefault(i => i.Value == Model.PaymentMethod?.ToString())?.Text;
        //            PaymentVoucher.Amount = Model.Amount;
        //            PaymentVoucher.DebitAccountId = Model.DebitAccountId;
        //            PaymentVoucher.DebitCostCenterId = Model.DebitCostCenterId;
        //            PaymentVoucher.Description = Model.Debit.Note;
        //            PaymentVoucher.CreditAccountId = Model.CreditAccountId;
        //            PaymentVoucher.CreditCostCenterId = Model.CreditCostCenterId;
        //            PaymentVoucher.BankId = Model.Debit.BankId;
        //            PaymentVoucher.BankAccountId = Model.Debit.BankAccountId;
        //            PaymentVoucher.BookNo = Model.Debit.BookNo;
        //            PaymentVoucher.CheckName = Model.Debit.ChequeName;
        //            PaymentVoucher.CheckNo = Model.Debit.ChequeNo;
        //            PaymentVoucher.CheckDate = Model.Debit.ChequeDate;
        //            PaymentVoucher.TransDate = Model.Debit.TransFerDate;
        //            PaymentVoucher.TransName = Model.Debit.TRNSNAme;
        //            PaymentVoucher.TransRef = Model.Debit.TRNSRef;
        //            PaymentVoucher.ModifiedBy = 1; // TODO: Current User
        //            PaymentVoucher.ModifiedDate = DateOnly.FromDateTime(DateTime.Now);
        //            await _context.SaveChangesAsync();

        //            //2) Delete Old Transactions and Payment Voucher Details
        //            var Transactions = await _context.TblTransactions
        //          .Where(d => d.TransactionId == Model.Id && d.Type == "PAYMENT")
        //          .ToListAsync();
        //            var PaymentVoucherDetails = await _context.TblPaymentVoucherDetails
        //           .Where(d => d.PaymentId == Model.Id)
        //           .ToListAsync();
        //            _context.TblPaymentVoucherDetails.RemoveRange(PaymentVoucherDetails);
        //            _context.TblTransactions.RemoveRange(Transactions);
        //            await _context.SaveChangesAsync();
        //            //3) Cheque
        //            if (Model.PaymentMethods.FirstOrDefault(i => i.Value == Model.PaymentMethod?.ToString())?.Text == "Cheque")
        //                {
        //                var exists = await _context.TblCheckDetails.AsNoTracking().AnyAsync(c =>
        //                         c.PvcNo == Model.Id &&
        //                         c.CheckType == "Payment" &&
        //                         c.CheckId == Model.Credit.BookNo);

        //                if (exists)
        //                    {
        //                    var affected = await _context.TblCheckDetails
        //                               .Where(c => c.PvcNo == Model.Id && c.CheckType == "Payment" && c.CheckId == Model.Credit.BookNo)
        //                               .ExecuteUpdateAsync(u => u
        //                                   .SetProperty(c => c.CheckNo, Model.Credit.BookNo)
        //                                   .SetProperty(c => c.CheckDate, Model.Credit.ChequeDate)
        //                                   .SetProperty(c => c.CheckName, Model.Credit.ChequeName)
        //                                   .SetProperty(c => c.Amount, Model.Amount)
        //                               );
        //                    }
        //                else
        //                    {
        //                    var New_Cheque = new TblCheckDetail
        //                        {
        //                        PvcNo = Model.Id,
        //                        CheckId = Model.Debit.BookNo,  //Add From The model (  )
        //                        CheckNo = Model.Debit.BookNo,
        //                        CheckDate = Model.Debit.ChequeDate,
        //                        CheckType = "Payment",
        //                        CheckName = Model.Debit.ChequeName,
        //                        Amount = Model.Amount,
        //                        State = "New",
        //                        //CreatedBy = 1, // TODO: Current User
        //                        //CreatedDate = DateOnly.FromDateTime(DateTime.Now)
        //                        };
        //                    _context.TblCheckDetails.Add(New_Cheque);
        //                    await _context.SaveChangesAsync();

        //                    }
        //                }
        //            //4) Transfer
        //            else if (Model.PaymentMethods.FirstOrDefault(i => i.Value == Model.PaymentMethod?.ToString())?.Text == "Transfer")
        //                { }
        //            //5) Payment Voucher Details + Update Employ + Update Vendor(Purchase) + Entry journals
        //            await AddPaymentVoucherDetailsAsync(Model, (int)Model.Id, Model.VoucherNo);

        //            //6)Debit Costcenter Entry
        //            await InsertCostCenterTransaction(Model.Date, Model.Amount, 0, (int)Model.Id, "Payment", "Payment Debit Entry", (int)Model.Debit.CostCenterId);
        //            //7)Credit Costcenter Entry
        //            await InsertCostCenterTransaction(Model.Date, 0, Model.Amount, (int)Model.Id, "Payment", "Payment Credit Entry", (int)Model.Credit.CostCenterId);
        //            //8) Add User Log
        //            //Add User Log
        //            await _context.SaveChangesAsync();
        //            await tx.CommitAsync();
        //            }
        //        catch
        //            {
        //            await tx.RollbackAsync();
        //            throw;
        //            }
        //    });
        //    }
        public async Task insertINV( ReceiptVoucherViewModel Model,int pvId,string code)
            {
            var voucherType = "Customer";

            foreach (var item in Model.Items)
                {
                var receiptvoucherdetail = new TblReceiptVoucherDetail
                    {
                    HumId = item.humId,
                    InvId = item.invId,
                    Date = item.Date,
                    InvCode = item.InvoiceNo,
                    Total = item.Amount,
                    Payment = item.Payment,
                    Description = item.Description,
                    PaymentId = pvId
                    };
                _context.TblReceiptVoucherDetails.Add(receiptvoucherdetail);
                if (item.invId>0)
                    {
                    var netResult= await _context.TblSales
                        .Where(s => s.Id == item.invId)
                        .Select (s => s.Net).FirstOrDefaultAsync();
                            var result=await _context.TblReceiptVoucherDetails
                        .Where(s=> s.InvId == item.invId)
                        .SumAsync(s=> s.Payment);
                    var change= netResult - result;
                    var salesInvoice = await _context.TblSales
                        .Where(s => s.Id == item.invId)
                        .ExecuteUpdateAsync(updates => updates
                        .SetProperty(s => s.Pay, result)
                        .SetProperty(s => s.Change, change));
                    _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Update Sales Invoice", "Sales Invoice", (int)item.invId, "Updated Sales Invoice: " + item.InvoiceNo);
                    }
               await insertJournals(Model.Date, (int)Model.DebitAccountId,(int)Model.CreditAccountId, pvId,(int)Model.CustomerId, code,item.Payment,item.Description);
                await _context.SaveChangesAsync();
                }
            }
        public async Task<int> GetChequeBookIdByCompanyOrFirstAsync()
            {
            var companyName = await _context.TblCompanies
               .OrderBy(c => c.Id)
               .Select(c => c.Name)
               .FirstOrDefaultAsync();

            //var chequeQuery= await _context.TblCheques
            //    .OrderBy(c => c.Id)
            //    //.Select(c => c.Id)
            //    .Where(c=>c.BankCard.AccountName.Contains(companyName))
            //    .FirstOrDefaultAsync();

           

            // base query: cheque + bank card
            var chequeQuery =
                from c in _context.TblCheques
                join bc in _context.TblBankCards on c.BankCardId equals bc.Id
                select new { c.Id, bc.AccountName };

            if (!string.IsNullOrWhiteSpace(companyName))
                {
                var chequeForCompanyId = await chequeQuery
                    .Where(x => x.AccountName.Contains(companyName))
                    .OrderBy(x => x.Id)
                    .Select(x => (int?)x.Id)
                    .FirstOrDefaultAsync();

                if (chequeForCompanyId.HasValue)
                    return chequeForCompanyId.Value;
                }

            var firstChequeId = await chequeQuery
                .OrderBy(x => x.Id)
                .Select(x => (int?)x.Id)
                .FirstOrDefaultAsync();

            return firstChequeId ?? -1;
            }
        public async Task insertJournals(DateOnly Date,int DebitAccount,int CreditAccount,int ID,int CustomerId,string Code,  decimal amount, string description)
            {
            await addTransactionEntry(Date, DebitAccount,amount,0, ID, 0, "Customer" + " Receipt", "RECEIPT", "Receipt Voucher NO. " + Code,
                _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.Now), Code);
            await addTransactionEntry(Date, CreditAccount,0,amount, ID, CustomerId, "Customer" + " Receipt", "RECEIPT", "Receipt Voucher NO. " + Code,
                _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.Now), Code);

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
        }
    }