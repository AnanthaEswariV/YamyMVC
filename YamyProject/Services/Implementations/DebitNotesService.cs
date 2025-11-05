using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace YamyProject.Services.Implementations
    {
    public class DebitNotesService(YamyDbContext context, IListServices listServices) : IDebitNotesService
        {
        private DateOnly Starting = default;
        private DateOnly Ending = default;    
        private readonly YamyDbContext _context = context;

        public async Task<MasterDebitNoteViewModel> QueryDebitNoteAsync( DateOnly from = default, DateOnly to = default, bool Date = true, CancellationToken ct = default)
            {        
            if (Date == true)
                {
                  Starting = from;
                 Ending = to;
                }
            var query = _context.TblDebitNotes
           .Where(s => s.State == 0)
           .Include(s => s.Transaction)
            .OrderBy(s => s.Date)
           .AsQueryable();
          
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
        
            return new MasterDebitNoteViewModel
                {
                FromDate = from,
                ToDate = to,
                MasterDebit = result,
                };
            }
        public async Task<string> GenerateNextDebitNoteCode()
            {
            var prefix = "0000"; // Prefix for Credit Note
            var lastCodeValue = await _context.TblDebitNotes
               .Select(s => s.InvoiceId.Substring(3))
               .MaxAsync();
            prefix = lastCodeValue ?? prefix;
            return $"DN-{int.Parse(prefix) + 1:D4}";
            }
        public async Task CreateDebitNoteAsync(DebitNoteViewModel Model)
        {
            // 1) Generate invoice no (async)
                var invoiceNo = string.IsNullOrWhiteSpace(Model.InvoiceNo)
                    ? await GenerateNextDebitNoteCode()
                    : Model.InvoiceNo.Trim();
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {

                    await using var tx = await _context.Database.BeginTransactionAsync();
                        try
                            {

                            var debitNote = new TblDebitNote
                                {
                                Date = Model.Date,
                                CreditAccount = Model.VendorId,
                                DebitAccount = (int)Model.DebitAccountId,
                                Type= "Vendor",
                                InvoiceId = invoiceNo,
                                Amount = Model.Amount,
                                Vat = Model.Vat,
                                Total = Model.TotalAmount,
                                Description = Model.Description,
                                CreatedBy = 1,
                                CreatedDate = DateOnly.FromDateTime(DateTime.Today),
                                State = 0,
                                };
                            _context.TblDebitNotes.Add(debitNote);
                            await _context.SaveChangesAsync();
                        var debitnoteId = debitNote.Id;
                            
                        await insertInvItems(Model, debitnoteId);// level4VendorId,                 level4VatId,                                    level4PurchaseReturn,
                        await AddTransaction(Model.Date, (int)Model.DebitAccountId, (int)Model.DebitAccountId,(int) Model.DebitAccountId, 
                            Model.TotalAmount, Model.Amount, debitnoteId, (int)Model.VendorId, invoiceNo, Model.Vat, Model.Amount);

                        //Add Loge Here

                        await tx.CommitAsync();

                            }
                        catch
                            {
                            await tx.RollbackAsync();
                            throw;
                            }
                });
        }
        //Insert Debit Note Details
        public async Task insertInvItems(DebitNoteViewModel Model, int InvId)
            {
            foreach (var i in Model.DebitItems.Where(i => i != null))
                {
                if(i.Selected)
                    {
                    var detail = new TblDebitNoteDetail
                        {
                        RefId= InvId,
                        InvNo = i.InvoiceNo,
                        InvoiceId = i.InvoiceId,
                        InvoiceDate = i.InvDate,
                        InvoiceType = i.InvoiceType,
                        Selected=i.Selected,
                        Total = i.Total,
                        Vat = i.Vat,
                        Amount = i.Amount,
                        Balance = i.Balance,
                        Remaining = i.Remaining,                      
                        };
                    _context.TblDebitNoteDetails.Add(detail);
                    await _context.SaveChangesAsync();
                    var change= await _context.TblPurchases
                         .Where(p => p.Id == i.InvoiceId)
                         .SumAsync(p => (decimal?)p.Change) ?? 0m;

                    var affected = await _context.TblPurchases
                      .Where(t => t.Id == i.InvoiceId)
                      .ExecuteUpdateAsync(updates => updates
                          .SetProperty(t => t.Pay, change)
                          .SetProperty(t => t.Change, i.Remaining));
                    //Add Loge
                    }
                }
            }
        //Add Transaction
        public async Task AddTransaction(DateOnly Date,int level4VendorId, int level4VatId, int level4PurchaseReturn, decimal TotalAmount , decimal Amount,int TransactionId,int humId,string Code, decimal Vat, decimal amount)
            {
            var VoucherName = "Debit Note";
            var Type = "Debit Note";
            //credit
            await AddTransactionEntry(
             Date,
             level4VendorId,
             0,
             TotalAmount,
             TransactionId,
             humId,
             Type,
             VoucherName,
             "Debit Note NO" + Code,
             1,//Add The User ID
             DateOnly.FromDateTime(DateTime.Now),
             VoucherNo: Code
              );
            //debit(Vat + Amount)
            //Vat
            await AddTransactionEntry(
               Date,
               level4VatId,
               Vat,
               0,
               TransactionId,
               0,
               Type,
               VoucherName,
               "Vat Input For  Voucher NO" + Code,
               1,//Add The User ID
               DateOnly.FromDateTime(DateTime.Now),
               VoucherNo: Code
               );
            //Amount
            await AddTransactionEntry(
               Date,
               level4PurchaseReturn,
               amount,
               0,
               TransactionId,
               0,
               Type,
               VoucherName,
               "Revenue For  Voucher NO" + Code,
               1,//Add The User ID
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
        public async Task UpdateDebitNoteAsync(DebitNoteViewModel Model)
            {
            // Implementation for updating a debit note
            var invoiceNo = string.IsNullOrWhiteSpace(Model.InvoiceNo)
                  ? await GenerateNextDebitNoteCode()
                  : Model.InvoiceNo.Trim();
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    //Update Debit Note
                    var DebitNote = _context.TblDebitNotes.Find(Model.Id);
                    if(DebitNote == null) throw new Exception("Debit Note not found");

                    DebitNote.Date = Model.Date;
                    DebitNote.CreditAccount = Model.VendorId;
                    DebitNote.DebitAccount = (int)Model.DebitAccountId;
                    DebitNote.InvoiceId = invoiceNo;
                    DebitNote.Amount = Model.Amount;
                    DebitNote.Vat = Model.Vat;
                    DebitNote.Total = Model.TotalAmount;
                    DebitNote.Description = Model.Description;
                    DebitNote.ModifiedBy = 1;
                    DebitNote.ModifiedDate = DateOnly.FromDateTime(DateTime.Today);
                    _context.TblDebitNotes.Update(DebitNote);
                    await _context.SaveChangesAsync();
                    //Delete Old Debit Note Details  
                    var DebitNot = await _context.TblDebitNoteDetails
                     .Where(d => d.RefId == Model.Id)
                     .ToListAsync();
                     _context.TblDebitNoteDetails.RemoveRange(DebitNot);
                    //Delete Old Debit Note from Transactions
                    var Transactions = await _context.TblTransactions
                     .Where(d => d.TransactionId == Model.Id && d.Type == "Debit Note")
                     .ToListAsync();
                     _context.TblTransactions.RemoveRange(Transactions);
                    //Insert New Debit Note Details
                    await insertInvItems(Model, Model.Id);// level4VendorId,                 level4VatId,                                    level4PurchaseReturn,
                    //insert New Debit Note Transactions
                    await AddTransaction(Model.Date, (int)Model.DebitAccountId, (int)Model.DebitAccountId, (int)Model.DebitAccountId,
                        Model.TotalAmount, Model.Amount, Model.Id, (int)Model.VendorId, invoiceNo, Model.Vat, Model.Amount);
                    //Save All Changes
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
    }   
}