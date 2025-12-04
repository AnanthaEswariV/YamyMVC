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


        }
    }