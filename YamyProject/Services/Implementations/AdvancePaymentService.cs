namespace YamyProject.Services.Implementations
    {
    public class AdvancePaymentService(YamyDbContext context, IHttpContextAccessor httpContextAccessor, IGlobalService GlobalService) : IAdvancePaymentService
        {
        private readonly YamyDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IGlobalService _GlobalService = GlobalService;

        public async Task<AdvancePaymentVoucherViewModel> GetAdvancePayments()
            {
            var rows = await _context.TblAdvancePaymentVouchers
                .Where(ap => ap.State == 0)
                .OrderBy(ap => ap.Date)
                .Select(ap => new AdvancePaymentVoucherRowViewModel
                    {
                    Id = ap.Id,
                    Date = ap.Date,
                    PaymentCode= ap.PvCode,
                    JvNo = $"{ap.Id :D4}" ,
                    Amount = (decimal)ap.Amount,                // use (ap.Amount ?? 0m) if nullable
                    DebitAccount = ap.DebitAccount.Name,
                    CreditAccount = ap.CreditAccount.Name,
                    Type = ap.Type
                    })
                .ToListAsync();
            return new AdvancePaymentVoucherViewModel {
                SelectedType = "Vendor",
                From = DateTime.Today.AddDays(-30),
                To = DateTime.Today,
                Types = new() { "Vendor", "Customer"},
                Rows = rows };
            }
        public async Task<string> GenerateNextAdvancePaymentCode()
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue = await _context.TblAdvancePaymentVouchers
               .Select(s => s.PvCode.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;
            return $"AP-{int.Parse(prefix) + 1:D4}";           
            }
        public async Task CreateAdvancePaymentAsync(AdvancePaymentViewModel model)
            {
            var CODE = GenerateNextAdvancePaymentCode();
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {

                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    if (model.VoucherNo != CODE.ToString())
                        { model.VoucherNo = CODE.ToString(); }
                    var advancePayment = new TblAdvancePaymentVoucher
                        {
                        PvCode = model.VoucherNo,
                        Date = model.Date,
                        Type = model.PaymentTypes.FirstOrDefault(x => x.Value == model.PaymentType.ToString())?.Text,
                        Method = model.PaymentMethods.FirstOrDefault(x => x.Value == model.PaymentMethod.ToString())?.Text,
                        Amount = model.Amount,
                        DebitAccountId = model.DebitAccountId,
                        DebitCostCenterId = model.DebitCostCenterId,
                        Description = model.Note,
                        CreditAccountId = model.CreditAccountId,
                        CreditCostCenterId = model.CreditCostCenterId,
                        CreatedBy = -_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, // Replace with actual user ID
                        CreatedDate = DateOnly.FromDateTime(DateTime.Now),
                        State = 0 // Assuming 0 means 'New' or 'Pending'
                        };
                    _context.TblAdvancePaymentVouchers.Add(advancePayment);
                    await _context.SaveChangesAsync();
                    var voucherId = advancePayment.Id;

                    if (model.PaymentMethods.FirstOrDefault(i => i.Value == model.PaymentMethod?.ToString())?.Text == "Cheque")
                        {
                        foreach (var Row in model.Rows)
                            {
                            //  var chequeId = await GetChequeBookIdByCompanyOrFirstAsync();
                            var checkDetailId = new TblCheckDetail
                                {
                                Date = model.Date,
                                CheckId = 0,
                                CheckNo = Row.CheckNo,
                                CheckDate = Row.CheckDate,
                                CheckType = "Advance Payment",                               
                                PvcNo = voucherId,
                                CheckName = Row.CheckName,
                                Amount = Row.Amount,
                                State = "New"
                                };
                            }
                        }


                    await insertInvItems(model, voucherId);
                    await InsertCostCenterTransaction(model.Date,model.Amount,0, voucherId, "AdvancePayment","Advance Payment Debit Entry",(int)model.DebitCostCenterId);
                    await InsertCostCenterTransaction(model.Date, 0,model.Amount, voucherId, "AdvancePayment", "Advance Payment Credit Entry", (int)model.CreditCostCenterId);

                    await _GlobalService.LogAudit(-_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Insert Advance Payment Voucher", "Advance Payment Voucher", voucherId, "Inserted Advance Payment Voucher: " + model.VoucherNo);

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
        public async Task insertInvItems(AdvancePaymentViewModel model, int voucherId)
            {
            var type = "";
            if( model.PaymentMethods.FirstOrDefault(x => x.Value == model.PaymentMethod.ToString())?.Text== "Cash")
                {
                type = "Vendor Advance Payment";
                }
            else if (model.PaymentMethods.FirstOrDefault(x => x.Value == model.PaymentMethod.ToString())?.Text == "Check") 
                {
                type = "Customer Advance Payment";
                }
            foreach (var i in model.Rows.Where(i => i != null))
                {
                var AdvancePaymentVoucherDetail = new TblAdvancePaymentVoucherDetail
                    {
                    PaymentId= voucherId,
                    Name = i.partnerId.ToString(),
                    Description = i.Description,
                    Amount = i.Amount

                    };
                if (model.PaymentMethods.FirstOrDefault(x => x.Value == model.PaymentMethod.ToString())?.Text == "Check")
                    {
                    AdvancePaymentVoucherDetail.BankName = i.BankId.ToString();
                    AdvancePaymentVoucherDetail.CheckName = i.CheckName;
                    AdvancePaymentVoucherDetail.CheckNo = i.CheckNo;
                    AdvancePaymentVoucherDetail.CheckDate = i.CheckDate;
                    AdvancePaymentVoucherDetail.BankAccountName = i.BankAccount;
                    AdvancePaymentVoucherDetail.BookNo = i.BookNo;
                    }
                else if (model.PaymentMethods.FirstOrDefault(x => x.Value == model.PaymentMethod.ToString())?.Text == "Transfer") 
                    {
                    AdvancePaymentVoucherDetail.TransDate = i.TransDate;
                    AdvancePaymentVoucherDetail.TransName = i.TransName;
                    AdvancePaymentVoucherDetail.TransRef = i.TransRef;
                        }
                _context.TblAdvancePaymentVoucherDetails.Add(AdvancePaymentVoucherDetail);
                await _context.SaveChangesAsync();

                
              //  if (model.PaymentMethods.FirstOrDefault(x => x.Value == model.PaymentMethod.ToString())?.Text == "Check") // Cheque
              //          {
              //      var CheckDetails = await _context.TblCheckDetails
              //.Where(d => d.PvcNo == voucherId && d.CheckType== "Payment" && d.CheckId==i.BookNo)
              //        .ToListAsync();

              //      var cheque = new TblCheckDetail
              //              {
              //              Date = DateOnly.FromDateTime(DateTime.Now),
              //              CheckId = i.BookNo,
              //              CheckNo = i.CheckNo,
              //              CheckDate = i.CheckDate,
              //              CheckType = "Advance Payment",
              //              PvcNo = voucherId,
              //              CheckName = i.CheckName,
              //              Amount = i.Amount,
              //              State = "New",
              //              };
              //          _context.TblCheckDetails.Add(cheque);
              //          await _context.SaveChangesAsync();
              //          }
                await AddTransactionEntry(model.Date, model.DebitAccountId, i.Amount, 0m, voucherId, i.partnerId , type,
                    "Advance PAYMENT", "ADVANCE Payment Voucher NO. " + model.VoucherNo, -_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.Now),model.VoucherNo);
                await AddTransactionEntry(model.Date, model.CreditAccountId,  0m, i.Amount, voucherId, 0 , type,
                    "Advance PAYMENT", "ADVANCE Payment Voucher NO. " + model.VoucherNo, -_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, DateOnly.FromDateTime(DateTime.Now),model.VoucherNo);


                }
            }
        public async Task AddTransactionEntry(DateOnly Date, int? accountId, decimal debit, decimal credit, int transactionId, int humId, string type, string voucher_name,string description, int createdBy, DateOnly createdDate,string VoucherNo)
            {
            var trx = new TblTransaction
                {
                Date = Date,
                AccountId = accountId,
                Debit = debit,
                Credit = credit,
                TransactionId = transactionId,
                HumId = humId,
                TType = voucher_name,
                Type = type,
                Description = description,
                CreatedBy = createdBy,
                VoucherNo = VoucherNo,
                CreatedDate = createdDate,
                State = 0
                };

            _context.TblTransactions.Add(trx);
            await _context.SaveChangesAsync();
            }
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


        public async Task<AdvancePaymentViewModel> GetEditAsync(int Id)
            {
            var advancePayment = await _context.TblAdvancePaymentVouchers
                //.Include(ap => ap.AdvancePaymentVoucherDetail)
                .Include(ap => ap.DebitAccount)
                .Include(ap => ap.CreditAccount)
                .FirstOrDefaultAsync(ap => ap.Id == Id);
            if (advancePayment == null) throw new NotImplementedException();
            var Model= await MapToViewModel(advancePayment);

            return Model;
            }

        private async Task<AdvancePaymentViewModel> MapToViewModel(TblAdvancePaymentVoucher s)
            {
            var details = s.AdvancePaymentVoucherDetail;

            return new AdvancePaymentViewModel
                {
                Id = s.Id,
                VoucherNo = s.PvCode,
                Date = s.Date ?? DateOnly.FromDateTime(DateTime.Now),
                Amount = s.Amount ?? 0m,
                Note = s.Description,
                PaymentType = s.Type == "Vendor" ? 1 : 2,
                PaymentMethod = s.Method == "Cash" ? 1 : s.Method == "Check" ? 2 : 3,
                DebitAccountId = s.DebitAccountId,
                DebitCostCenterId = s.DebitCostCenterId,
                CreditAccountId = s.CreditAccountId,
                CreditCostCenterId = s.CreditCostCenterId,
                Rows = details != null ? new List<PaymentVoucherRowViewModel>
                    {
                    new PaymentVoucherRowViewModel
                        {
                        Id = details.Id,
                        partnerId = int.Parse(details.Name),

                         BankId = details.BankName != null ? int.Parse(details.BankName) : 0,
                         CheckName = details.CheckName,
                         CheckNo = details.CheckNo,
                         CheckDate = details.CheckDate,
                         BankAccount = details.BankAccountName,
                         BookNo = details.BookNo,

                         TransDate = details.TransDate,
                         TransName = details.TransName,
                         TransRef = details.TransRef,


                         Description = details.Description,
                         Amount = details.Amount ?? 0m,
                        }
                    } : new List<PaymentVoucherRowViewModel>().ToList()

                }; 
            }
        public async Task UpdateAdvancePaymentAsync(AdvancePaymentViewModel Model)
            {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    var advancePayment = _context.TblAdvancePaymentVouchers.Find(Model.Id);
                    if (advancePayment == null) throw new NotImplementedException();

                    advancePayment.Id=(int) Model.Id;
                    advancePayment.Date= Model.Date;
                    advancePayment.PvCode= Model.VoucherNo;
                    advancePayment.Type= Model.PaymentTypes.FirstOrDefault(x => x.Value == Model.PaymentType.ToString())?.Text;
                    advancePayment.Method= Model.PaymentMethods.FirstOrDefault(x => x.Value == Model.PaymentMethod.ToString())?.Text;
                    advancePayment.Amount= Model.Amount;
                    advancePayment.DebitAccountId= Model.DebitAccountId;
                    advancePayment.DebitCostCenterId= Model.DebitCostCenterId;
                    advancePayment.Description= Model.Note;
                    advancePayment.CreditAccountId= Model.CreditAccountId;
                    advancePayment.CreditCostCenterId= Model.CreditCostCenterId;
                    advancePayment.ModifiedBy= 1; // Replace with actual user ID
                    advancePayment.ModifiedDate= DateOnly.FromDateTime(DateTime.Now);
                    _context.TblAdvancePaymentVouchers.Update(advancePayment);
                    await _context.SaveChangesAsync();
                    //---
                    // await RemoveAsync((int)Model.Id);
                    await DeleteCostCenterTransactionEntry((int)Model.Id);
                    //---
                    await insertInvItems(Model, (int)Model.Id);
                    await InsertCostCenterTransaction(Model.Date, Model.Amount, 0, (int)Model.Id, "AdvancePayment", "Advance Payment Debit Entry", (int)Model.DebitCostCenterId);
                    await InsertCostCenterTransaction(Model.Date, 0, Model.Amount, (int)Model.Id, "AdvancePayment", "Advance Payment Credit Entry", (int)Model.CreditCostCenterId);
                    //Add log
                    await _GlobalService.LogAudit(-_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Update Advance Payment Voucher", "Advance Payment Voucher", (int)Model.Id, "Updated Advance Payment Voucher: " +Model.VoucherNo);
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
        public async Task DeleteCostCenterTransactionEntry(int id)
            {
            var CostCenterTransactions = await _context.TblCostCenterTransactions
                    .Where(d => d.RefId == id && d.Type== "AdvancePayment")
                    .ToListAsync();
            _context.TblCostCenterTransactions.RemoveRange(CostCenterTransactions);
            await _context.SaveChangesAsync();
            }
        //public async Task RemoveAsync(int id)
        //    {
        //    //remove advance payment voucher Details
        //    var advancePaymentDetails = await _context.TblAdvancePaymentVoucherDetails
        //        .Where(d => d.PaymentId == id)
        //                .ToListAsync();
        //    if (advancePaymentDetails == null) throw new NotImplementedException();
        //    _context.TblAdvancePaymentVoucherDetails.RemoveRange(advancePaymentDetails);
        //    await _context.SaveChangesAsync();
        //    //remove Transaction Entries
        //    var Transactions = await _context.TblTransactions
        //             .Where(d => d.TransactionId == id)
        //             .ToListAsync();
        //    _context.TblTransactions.RemoveRange(Transactions);
        //    await _context.SaveChangesAsync();
        //    //Remove Cost Center Transactions
        //    var CostCenterTransactions = await _context.TblCostCenterTransactions
        //             .Where(d => d.RefId == id && d.Type == "AdvancePayment")
        //             .ToListAsync();
        //    _context.TblCostCenterTransactions.RemoveRange(CostCenterTransactions);
        //    await _context.SaveChangesAsync();
        //    }

        }
}