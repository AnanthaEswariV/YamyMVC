namespace YamyProject.Services.Implementations
    {
    public class PaymentVoucherService(YamyDbContext context, IHttpContextAccessor httpContextAccessor, IGlobalService GlobalService) : IPaymentVoucherService
        {
        private DateOnly Starting = default;
        private DateOnly Ending = default;
        private readonly YamyDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IGlobalService _GlobalService = GlobalService;

        public async Task<ReceiveVoucherCenterViewModel> QueryPurchaseAsync(DateOnly from = default, DateOnly to = default, bool Date = true, CancellationToken ct = default)
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

            var query = _context.TblPaymentVouchers
              .Where(s => s.State == 0)
              .Include(s => s.Transaction)
              .Include(s => s.CreditAccount)
              .Include(s => s.DebitAccount)
              .Include(s => s.PaymentVoucher)
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
                .OrderBy(s => s.Date)
                .Select(s => new
                    {
                    s.PaymentVoucher.Id,
                    s.PaymentVoucher.Date,
                    ReceiptCode = s.Code,
                    JVNO = s.PaymentVoucher.Id.ToString("D4"),
                    s.Amount,
                    DebitAccount = s.DebitAccount.Name,
                    CreditAccount = s.CreditAccount.Name
                    })
    .ToListAsync();

            int sn = 0;
            var list = sales.Select(r => new ReceiveVouchersViewModel
                {
                SN = ++sn,
                Id = r.Id,
                Date = r.Date, // if r.Date is nullable DateTime
                JVNO = r.Id.ToString("D4"),
                ReceiptCode = r.ReceiptCode,
                Amount = r.Amount,
                DebitAccount = r.DebitAccount,
                CreditAccount = r.CreditAccount,
                ReceiptVoucherDetail = Enumerable.Empty<RvItemViewModel>() // map real items if/when needed
                }).ToList();

            return new ReceiveVoucherCenterViewModel
                {
                FromDate = from,
                ToDate = to,
                All = Date,
                ReceiptVouchers = list
                };
            }
        public async Task<string> GenerateNextPaymentCode()
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue = await _context.TblPaymentVouchers
               .Select(s => s.Code.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;

            return $"PV-{int.Parse(prefix) + 1:D4}";
            }
        public async Task<string> GenerateNextPaymentId()
            {
            string newCode = "0"; // Prefix for Credit Note
            var lastCodeValue = await _context.TblPaymentVouchers
               .Select(s => s.Code.Substring(3))
               .MaxAsync();
            newCode = lastCodeValue ?? newCode;
            return $"{int.Parse(newCode) + 1:D4}";
            }
        // Create Payment Voucher
        public async Task CreatePvAsync(ReceiveVoucherViewModel Model)
            {
            if (Model is null) throw new ArgumentNullException(nameof(Model));
            if (Model.Items is null || Model.Items.Count == 0)
                throw new InvalidOperationException("Invoice must contain at least one item.");

            // 1) Generate invoice no (async)
            var invoiceNo =  await GenerateNextPaymentCode();

            // 2) Voucher + Transaction
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {

                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {// 3) Payment Voucher
                    var New_PaymentVoucher = new TblPaymentVoucher
                        {
                        Code = invoiceNo,
                        Date = Model.Date,
                        Type = Model.PaymentTypes.FirstOrDefault(i => i.Value == Model.PaymentType?.ToString())?.Text,
                        Method = Model.PaymentMethods.FirstOrDefault(i => i.Value == Model.PaymentMethod?.ToString())?.Text,
                        Amount = Model.Amount,
                        IsSubcontractor= Model.IsSubcontractor,
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
                        CreatedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0 , // TODO: Current User
                        CreatedDate = DateOnly.FromDateTime(DateTime.Now),
                        //  VendorId = Model.vendorId,
                        //  DebitEmployeeId = Model.DebitEmployeeId,
                        State = 0
                        };
                    _context.TblPaymentVouchers.Add(New_PaymentVoucher);
                    await _context.SaveChangesAsync(); // need sale.Id
                    var PaymentVoucherId = New_PaymentVoucher.Id;
                    if (PaymentVoucherId <= 0)
                        throw new InvalidOperationException("Failed to create Payment Voucher.");
                    // 4) Cheque
                    if (Model.PaymentMethods.FirstOrDefault(i => i.Value == Model.PaymentMethod?.ToString())?.Text == "Cheque")
                        {
                        var New_Cheque = new TblCheckDetail
                            {
                            Date = DateOnly.FromDateTime(DateTime.Now),
                            CheckId = Model.Debit.BookNo,  //Add From The model (  )
                            CheckNo = Model.Debit.BookNo,
                            CheckDate = Model.Debit.ChequeDate,
                            CheckType = "Payment",
                            PvcNo = PaymentVoucherId,
                            CheckName = Model.Debit.ChequeName,
                            Amount = Model.Amount,
                            State = "New",
                            //CreatedBy = 1, // TODO: Current User
                            //CreatedDate = DateOnly.FromDateTime(DateTime.Now)
                            };
                        _context.TblCheckDetails.Add(New_Cheque);
                        await _context.SaveChangesAsync();
                        }
                    //5) Payment Voucher Details + Update Employ + Update Vendor(Purchase) + Entry journals 
                    await AddPaymentVoucherDetailsAsync(Model, PaymentVoucherId, invoiceNo);
                    //7) Add User Log
                    await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Insert Payment Voucher", "Payment Voucher", PaymentVoucherId, "Insered Payment Voucher: " + invoiceNo);
                    //8)Debit Costcenter Entry
                    await InsertCostCenterTransaction(Model.Date, Model.Amount, 0, PaymentVoucherId, "Payment", "Payment Debit Entry", (int)Model.Debit.CostCenterId);
                    //9)Credit Costcenter Entry
                    await InsertCostCenterTransaction(Model.Date, 0, Model.Amount, PaymentVoucherId, "Payment", "Payment Credit Entry", (int)Model.Credit.CostCenterId);
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
        private async Task AddPaymentVoucherDetailsAsync(ReceiveVoucherViewModel Model, int PaymentVoucherId, string invoiceNo)
            {

            var details = new List<TblPaymentVoucherDetail>(Model.Items.Count);
            var Type = "";


            foreach (var i in Model.Items.Where(i => i != null))
                {
                details.Add(new TblPaymentVoucherDetail
                    {
                    PaymentId = PaymentVoucherId,
                    Date = i.Date,
                    HumId = i.humId,
                    InvCode = i.InvoiceNo,
                    InvId = i.invId,
                    Payment = i.Payment,
                    Description = i.Description,
                    VoucherType = i.VoucherType
                    }
                 );
                _context.TblPaymentVoucherDetails.AddRange(details);

                if (Model.PaymentTypes.FirstOrDefault(i => i.Value == Model.PaymentType?.ToString())?.Text == "Employee")
                    {
                    if (i.VoucherType == "Salary")
                        {
                        var affected = await _context.TblAttendanceSalaries
                            .Where(t => t.Id == i.invId)
                            .ExecuteUpdateAsync(updates => updates
                                .SetProperty(t => t.Pay, i.Payment)
                                .SetProperty(t => t.Change, i.Amount - i.Payment)
                           );
                        Type = "Employee Salary Payment";
                        }
                    else if (i.VoucherType == "Employee Loan Payment")
                        {

                        var affected = await _context.TblLoans
                            .Where(t => t.EmployeeId == i.invId.ToString() && t.LoanDate == i.Date)
                            .ExecuteUpdateAsync(updates => updates
                                .SetProperty(t => t.Pay, i.Payment)
                                .SetProperty(t => t.Change, i.Amount - i.Payment));
                        Type = "Employee Loan Payment";
                        }
                    await InsertJournals(Type, Model.DebitEmployeeId.ToString(), i.VoucherType, i.Payment.ToString(), Model.Date, (int)Model.Debit.AccountId, PaymentVoucherId, invoiceNo);
                    }
                else if (Model.PaymentTypes.FirstOrDefault(i => i.Value == Model.PaymentType?.ToString())?.Text != "Employee")
                    {
                    decimal? net = await _context.TblPurchases
                     .AsNoTracking()
                     .Where(p => p.Id == i.invId)
                     .Select(p => (decimal?)p.Net)
                     .FirstOrDefaultAsync() ?? 0m;
                    decimal totalPaid = await _context.TblPaymentVoucherDetails
                      .AsNoTracking()
                      .Where(d => d.InvId == i.invId)
                      .SumAsync(d => (decimal?)d.Payment) ?? 0m;

                    var affected = await _context.TblPurchases
                              .Where(t => t.Id == i.invId)
                              .ExecuteUpdateAsync(updates => updates
                                  .SetProperty(t => t.Pay, totalPaid)
                                  .SetProperty(t => t.Change, net - totalPaid )
                             );
                    if (Model.IsSubcontractor)
                        Type = "Subcontractor Payment";
                    else
                        Type = "Vendor Payment";
                    await InsertJournals(Type, Model.vendorId.ToString(), i.VoucherType, i.Payment.ToString(), Model.Date, (int)Model.Debit.AccountId, PaymentVoucherId, invoiceNo);
                    }
                if (Model.Items == null || Model.Items.Count == 0)
                    {
                    throw new InvalidOperationException("No items to add to Payment Voucher Details.");
                    }
                }

            await _context.SaveChangesAsync();

            }
        // Insert Journals
        public async Task InsertJournals(string Type, string humId, string voucherType, string amount, DateOnly Date, int AccountId, int TransactionId, string Code)
            {
            //The Debit Entry
            await AddTransactionEntry(
                Date,
                AccountId,
                decimal.Parse(amount),
                0,
                TransactionId,
                int.Parse(humId),
                Type,
                "PAYMENT",
                "Payment Voucher NO" + Code,
                _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0,//Add The User ID
                DateOnly.FromDateTime(DateTime.Now),
                VoucherNo: Code
                );
            //The Credit Entry
            await AddTransactionEntry(
                Date,
                AccountId,
                0,
                decimal.Parse(amount),
                TransactionId,
                0,
                Type,
                "PAYMENT",
                "Payment Voucher NO" + Code,
                _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0,//Add The User ID
                DateOnly.FromDateTime(DateTime.Now),
                VoucherNo: Code
                );
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
        //Cost Center Transaction
        public async Task InsertCostCenterTransaction(DateOnly Date, decimal Debit, decimal Credit, int RefId, string Type, string Description, int CostCenterId)
            {
            var CostCenter = new TblCostCenterTransaction
                {
                Date = Date,
                Debit = Debit,
                Credit = Credit,
                RefId = RefId,
                Type = Type,
                Description = Description,
                CostCenterId = CostCenterId
                };
            _context.TblCostCenterTransactions.Add(CostCenter);
            await _context.SaveChangesAsync();
            }

        // Update Payment Voucher
        public async Task updatePvAsync(ReceiveVoucherViewModel Model)
            {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    //1) Update Payment Voucher
                    var PaymentVoucher = _context.TblPaymentVouchers.Find(Model.Id);
                    if (PaymentVoucher == null) throw new KeyNotFoundException($"Sales Invoice with ID {Model.Id} not found.");
                    PaymentVoucher.Date = Model.Date;
                    PaymentVoucher.Code = Model.VoucherNo;
                    PaymentVoucher.Type = Model.PaymentTypes.FirstOrDefault(i => i.Value == Model.PaymentType?.ToString())?.Text;
                    PaymentVoucher.Method = Model.PaymentMethods.FirstOrDefault(i => i.Value == Model.PaymentMethod?.ToString())?.Text;
                    PaymentVoucher.Amount= Model.Amount;
                    PaymentVoucher.DebitAccountId = Model.DebitAccountId;
                    PaymentVoucher.DebitCostCenterId = Model.DebitCostCenterId;
                    PaymentVoucher.Description = Model.Debit.Note;
                    PaymentVoucher.CreditAccountId = Model.CreditAccountId;
                    PaymentVoucher.CreditCostCenterId = Model.CreditCostCenterId;
                    PaymentVoucher.BankId = Model.Debit.BankId;
                    PaymentVoucher.BankAccountId = Model.Debit.BankAccountId;
                    PaymentVoucher.BookNo = Model.Debit.BookNo;
                    PaymentVoucher.CheckName = Model.Debit.ChequeName;
                    PaymentVoucher.CheckNo = Model.Debit.ChequeNo;
                    PaymentVoucher.CheckDate = Model.Debit.ChequeDate;
                    PaymentVoucher.TransDate = Model.Debit.TransFerDate;
                    PaymentVoucher.TransName = Model.Debit.TRNSNAme;
                    PaymentVoucher.TransRef= Model.Debit.TRNSRef;
                    PaymentVoucher.ModifiedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0; // TODO: Current User
                    PaymentVoucher.ModifiedDate = DateOnly.FromDateTime(DateTime.Now);
                    await _context.SaveChangesAsync();

                    //2) Delete Old Transactions and Payment Voucher Details
                    var Transactions = await _context.TblTransactions
                  .Where(d => d.TransactionId == Model.Id && d.Type == "PAYMENT")
                  .ToListAsync();                   
                    var PaymentVoucherDetails = await _context.TblPaymentVoucherDetails
                   .Where(d => d.PaymentId ==Model.Id)
                   .ToListAsync();
                    _context.TblPaymentVoucherDetails.RemoveRange(PaymentVoucherDetails);
                   _context.TblTransactions.RemoveRange(Transactions);
                    await _context.SaveChangesAsync();
                    //3) Cheque
                    if (Model.PaymentMethods.FirstOrDefault(i => i.Value == Model.PaymentMethod?.ToString())?.Text== "Cheque")
                        {
                        var exists = await _context.TblCheckDetails.AsNoTracking().AnyAsync(c =>
                                 c.PvcNo == Model.Id &&
                                 c.CheckType == "Payment" &&
                                 c.CheckId == Model.Credit.BookNo);

                        if (exists)
                            {
                            var affected = await _context.TblCheckDetails
                                       .Where(c => c.PvcNo == Model.Id && c.CheckType == "Payment" && c.CheckId == Model.Credit.BookNo)
                                       .ExecuteUpdateAsync(u => u
                                           .SetProperty(c => c.CheckNo, Model.Credit.BookNo)
                                           .SetProperty(c => c.CheckDate, Model.Credit.ChequeDate)
                                           .SetProperty(c => c.CheckName, Model.Credit.ChequeName)
                                           .SetProperty(c => c.Amount, Model.Amount)
                                           .SetProperty(c => c.PvcNo, Model.Id)
                                           .SetProperty(c => c.CheckType, "Payment")
                                           .SetProperty(c => c.CheckId, Model.Credit.BookNo)
                                           
                                       );
                            }
                        else
                            {
                            var New_Cheque = new TblCheckDetail
                                {
                                Date= DateOnly.FromDateTime(DateTime.Now),
                                CheckId = Model.Debit.BookNo,  //Add From The model (  )
                                CheckNo = Model.Debit.BookNo,
                                CheckDate = Model.Debit.ChequeDate,
                                CheckType = "Payment",
                                PvcNo = Model.Id,
                                CheckName = Model.Debit.ChequeName,
                                Amount = Model.Amount,
                                State = "New",
                                //CreatedBy = 1, // TODO: Current User
                                //CreatedDate = DateOnly.FromDateTime(DateTime.Now)
                                };
                            _context.TblCheckDetails.Add(New_Cheque);
                            await _context.SaveChangesAsync();

                            }
                            }
                    //4) Transfer
                    else if (Model.PaymentMethods.FirstOrDefault(i => i.Value == Model.PaymentMethod?.ToString())?.Text== "Transfer")
                        { }
                    //5) Payment Voucher Details + Update Employ + Update Vendor(Purchase) + Entry journals
                    await AddPaymentVoucherDetailsAsync(Model, (int)Model.Id, Model.VoucherNo);

                    var PaymentCostCenter = await _context.TblCostCenterTransactions
                 .Where(d => d.RefId == Model.Id&& d.Type== "Payment")
                 .ToListAsync();
                    _context.TblCostCenterTransactions.RemoveRange(PaymentCostCenter);


                    //6)Debit Costcenter Entry
                    await InsertCostCenterTransaction(Model.Date, Model.Amount, 0, (int)Model.Id, "Payment", "Payment Debit Entry", (int)Model.Debit.CostCenterId);
                    //7)Credit Costcenter Entry
                    await InsertCostCenterTransaction(Model.Date, 0, Model.Amount, (int)Model.Id, "Payment", "Payment Credit Entry", (int)Model.Credit.CostCenterId);
                    //8) Add User Log
                    await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Update Payment Voucher", "Payment Voucher", (int)Model.Id, "Updated Payment Voucher: " + Model.VoucherNo);
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

        public async Task<ReceiveVoucherViewModel> GetEditAsync(int id)
            {
            var PaymentVoucher = await _context.TblPaymentVouchers
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Include(s => s.Transaction)
                .Include(s => s.PaymentVoucher)
                .Include(s => s.Customer)
                .Select(s => new ReceiveVoucherViewModel
                    {
                    Id = s.Id,
                    VoucherNo = s.Code,
                    Date = (DateOnly)s.Date,
                    Amount = (decimal)s.Amount,
                    PaymentType = s.Type == null ? null : (s.Type == "Employee" ? 2 : s.Type == "Vendor" ? 1 : (int?)null),
                    DebitAccountId = s.DebitAccountId,
                    DebitCostCenterId = s.DebitCostCenterId,
                    CreditAccountId = s.CreditAccountId,
                    CreditCostCenterId = s.CreditCostCenterId,
                    PaymentMethod = s.Method == null ? null : (s.Method == "Cash" ? 1 : s.Method == "Cheque" ? 2 : s.Method == "Transfer" ? 3 : (int?)null),
                    IsSubcontractor = s.IsSubcontractor,
                    Debit = new PaySide
                        {
                        Note = s.Description
                        },
                    Credit = new PaySide
                        {
                        transferDate = s.TransDate,
                        TRNSNAme = s.TransName,
                        TRNSRef = s.TransRef,
                        BankAccountId = (int)s.BankAccountId!,
                        BankId = (int)s.BankId!,
                        BookNo = (int)s.BookNo!,
                        ChequeName = s.CheckName!,
                        IsCheque = s.Method == "Cheque",
                        ChequeNo = s.CheckNo,
                        ChequeDate = s.CheckDate
                        },
                    Items = s.PaymentVoucher == null
                              ? new List<RvItemViewModel>()
                              : new List<RvItemViewModel> {
                                  new RvItemViewModel {
                                      SN = 1,
                                      humId = s.PaymentVoucher.HumId,
                                      invId = s.PaymentVoucher.InvId,
                                      Date = (DateOnly)s.PaymentVoucher.Date!,
                                      InvoiceNo = s.Code,
                                      Amount = (decimal)s.PaymentVoucher.Total!,
                                      Payment = (decimal)s.PaymentVoucher.Payment!,
                                      Description = s.PaymentVoucher.Description,
                                      VoucherType =s.PaymentVoucher.VoucherType
                                      } }}
                                  )
                .FirstOrDefaultAsync();
            return PaymentVoucher!;
            }
        
        }
    }