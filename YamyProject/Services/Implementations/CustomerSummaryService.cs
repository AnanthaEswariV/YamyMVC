namespace YamyProject.Services.Implementations
    {
    public class CustomerSummaryService(YamyDbContext context): ICustomerSummaryService
        {
        public YamyDbContext _context = context;

        public async Task<List<CustomerSummaryRowViewModel>> GetCustomerBalancesAsync(/*DateOnly? dateFrom, DateOnly? dateTo*/)
            {
            // Allowed transaction types (your IN (...) list)
            var allowedTypes = new[]
            {
        "Customer Receipt",
        "Sales Invoice",
        "Customer Opening Balance",
        "Check Cancel (Customer)",
        "SalesReturn Invoice",
        "Credit Note",
        "PDC Receivable"
    };

            var query =
      _context.TblTransactions
          .Where(t => t.Customer != null)                    // <== navigation
          .Where(t => t.Customer.State == 0)                 // v.state = 0
          .Where(t => allowedTypes.Contains(t.Type))         // t.type IN (...)
         // .Where(t => !dateFrom.HasValue || t.Date >= dateFrom.Value)
        //  .Where(t => !dateTo.HasValue || t.Date <= dateTo.Value)
          .GroupBy(t => new
              {
              t.Customer.Id,
              t.Customer.Code,
              t.Customer.Name
              })
          .Select(g => new
              {
              g.Key.Id,
              Name = g.Key.Code + " - " + g.Key.Name,
              Debit = g.Sum(x => (decimal?)x.Debit) ?? 0m,
              Credit = g.Sum(x => (decimal?)x.Credit) ?? 0m,
              MinDate = g.Min(x => x.Date),

              Balance = g.Sum(x =>
                  x.Type == "Customer Receipt"
                      ? -(((x.Debit ?? 0m) == 0m ? (x.Credit ?? 0m) : (x.Debit ?? 0m)))

                  : x.Type == "Sales Invoice Cash"
                      ? 0m

                  : x.Type == "Customer Opening Balance" && (x.Credit ?? 0m) > 0m
                      ? -(x.Credit ?? 0m)

                  : x.Type == "Check Cancel (Customer)"
                      ? (x.Debit ?? 0m)

                  : x.Type == "PDC Receivable"
                      ? (((x.Debit ?? 0m) == 0m ? (x.Credit ?? 0m) : (x.Debit ?? 0m)))

                  : x.Type == "SalesReturn Invoice"
                      ? -(((x.Debit ?? 0m) == 0m ? (x.Credit ?? 0m) : (x.Debit ?? 0m)))

                  : x.Type == "Credit Note"
                      ? -(((x.Debit ?? 0m) == 0m ? (x.Credit ?? 0m) : (x.Debit ?? 0m)))

                  : (x.Type.StartsWith("Customer") || x.Type.StartsWith("Sales"))
                      ? (((x.Debit ?? 0m) == 0m ? (x.Credit ?? 0m) : (x.Debit ?? 0m)))

                  : 0m)
              });

            // Apply ORDER BY MIN(t.date) for ROW_NUMBER()
            var list = await query
                .OrderBy(x => x.MinDate)
                .ToListAsync();

            // Add SN and DATE (CAST(NOW() AS DATE))
            var today = DateTime.Today;
            var result = list
                .Select((x, index) => new CustomerSummaryRowViewModel
                    {
                    Sn = index + 1,     // ROW_NUMBER()
                    Id = x.Id,
                   // Date = today,         // CAST(NOW() AS DATE)
                    Name = x.Name,
                    Debit = x.Debit,
                    Credit = x.Credit,
                    Balance = x.Balance
                    })
                .ToList();

            return result;
            }
        public async Task<List<CustomerSummaryBalanceDetailRowViewModel>> GetCustomerStatementAsync(int humId/*,DateOnly? dateFrom, DateOnly? dateTo*/)
            {
            var allowedTypes = new[]
             {
                  "Customer Receipt",
                  "Sales Invoice",
                  "Sales Invoice Cash",
                  "Customer Opening Balance",
                  "Check Cancel (Customer)",
                  "SalesReturn Invoice",
                  "Credit Note",
                  "PDC Receivable"
              };
     
            var baseRows = await _context.TblTransactions
      .Include(t => t.Account)
      .Where(t => t.HumId == humId && allowedTypes.Contains(t.Type))
      .OrderBy(t => t.Date)
      .ThenBy(t => t.Id)
      .Select(t => new CustomerSummaryBalanceDetailRowViewModel
          {
          Type = t.Type ?? "",

          // If t.Date is DateTime?
          // Date = t.Date.HasValue 
          //     ? DateOnly.FromDateTime(t.Date.Value) 
          //     : default,

          // If t.Date is DateOnly?:
          // Date = t.Date ?? default,

          // If t.Date is non-nullable DateOnly:
          Date =(DateOnly) t.Date,   // simplest

          // If TransactionId is int?:
          TransactionId = t.TransactionId ?? 0,

          Num = t.VoucherNo ?? "SI-000",
          Account = t.Account != null ? t.Account.Name : "",

          // If Debit/Credit are decimal?:
          Amount = (t.Debit ?? 0m) - (t.Credit ?? 0m),

          // We'll fill Balance later
          Balance = 0m
          })
      .ToListAsync();

            // Now compute running balance in C#
            decimal runningBalance = 0m;
            foreach (var r in baseRows)
                {
                if (r.Type != "Sales Invoice Cash")
                    {
                    runningBalance += r.Amount; // safe now (non-nullable)
                    }

                r.Balance = runningBalance;
                }

            return baseRows;
            }
        public async Task<CustomerAgingSummaryViewModel> GetCustomerAgingAsync(DateOnly? dateFrom = null,DateOnly? dateTo = null)
            {
            var allowedTypes = new[]
            {
        "Customer Receipt",
        "Sales Invoice",
        "Check Cancel (Customer)",
        "SalesReturn Invoice",  
        "Credit Note",
        "PDC Receivable"
    };

            // 1) Base query (DB side)
                var txQuery = _context.TblTransactions
                .Include(t => t.Customer)
                .Where(t =>
                    t.State == 0 &&
                    t.HumId != null && t.HumId == t.Customer.Id &&
                    allowedTypes.Contains(t.Type));

            // 2) Optional date filter (same as your old dateFilter logic)
            //if (dateFrom.HasValue)
            //    txQuery = txQuery.Where(t => t.Date >= dateFrom.Value);

            //if (dateTo.HasValue)
            //    txQuery = txQuery.Where(t => t.Date <= dateTo.Value);

            // 3) Bring minimal data to memory (we’ll calculate age buckets in C#)
            var txList = await txQuery
                 .Where(t=> t.HumId==t.Customer.Id  && t.HumId!=0)
                .Select(t => new
                    {
                    CustomerId = t.Customer.Id,   // int?
                    CustomerCode = t.Customer.Code,
                    CustomerName = t.Customer.Name  ,
                    t.Date,    // DateOnly or DateOnly?
                    Debit = t.Debit ?? 0m,
                    Credit = t.Credit ?? 0m
                    })
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);

            // 4) Group by customer and calculate buckets
            var rows = new List<CustomerAgingRowViewModel>();
            int sn = 1;

            foreach (var grp in txList
               // .Where(x => x.CustomerId.HasValue)
                         .GroupBy(x => new { x.CustomerId, x.CustomerCode, x.CustomerName })
                         .OrderBy(g => g.Key.CustomerId))
                {
                // NOTE: non-nullable buckets (match your ViewModel)
                decimal current = 0m,
                        d1_30 = 0m,
                        d31_60 = 0m,
                        d61_90 = 0m,
                        d91Plus = 0m;

                foreach (var t in grp)
                    {
                    var txDate = t.Date ?? today;    // if null, treat as today

                    // Age in days = today - transaction date
                    var ageDays = (today.ToDateTime(TimeOnly.MinValue) -
                                   txDate.ToDateTime(TimeOnly.MinValue)).Days;

                    // Handle nullable debit/credit
                    var amount = (t.Debit ) - (t.Credit );

                    if (ageDays == 0)
                        current += amount;
                    else if (ageDays >= 1 && ageDays <= 30)
                        d1_30 += amount;
                    else if (ageDays >= 31 && ageDays <= 60)
                        d31_60 += amount;
                    else if (ageDays >= 61 && ageDays <= 90)
                        d61_90 += amount;
                    else if (ageDays > 90)
                        d91Plus += amount;
                    }

                var total = current + d1_30 + d31_60 + d61_90 + d91Plus;

                // Equivalent to HAVING SUM(t.debit - t.credit) > 0
                if (total <= 0)
                    continue;

                rows.Add(new CustomerAgingRowViewModel
                    {
                    Sn = sn++,
                    CustomerId = (int)grp.Key.CustomerId,
                    CustomerName = $"{grp.Key.CustomerCode} {grp.Key.CustomerName}",
                    Current = current,
                    Days1To30 = d1_30,
                    Days31To60 = d31_60,
                    Days61To90 = d61_90,
                    Days91Plus = d91Plus
                    });
                }

            // 5) Wrap in summary (for TOTAL row)
            var summary = new CustomerAgingSummaryViewModel
                {
                Rows = rows
                };

            return summary;
            }
        public async Task<CustomerSalesListViewModel> GetCustomerSalesAsync(int customerId,DateOnly? dateFrom,DateOnly? dateTo)
            {
            var allowedTypes = new[]
            {
            "Customer Receipt",
            "Sales Invoice",
            "Check Cancel (Customer)",
            "SalesReturn Invoice",
            "Credit Note",
            "PDC Receivable"
        };

            // 1) Base query
            var txQuery = _context.TblTransactions
                .AsNoTracking()
                .Include(t => t.Customer)
                .Where(t =>
                    t.HumId == customerId &&
                    t.State == 0 &&
                    allowedTypes.Contains(t.Type));

            // 2) Optional date filter (like your dateFilter string)
            //if (dateFrom.HasValue)
            //    {
            //    var from = dateFrom.Value.ToDateTime(TimeOnly.MinValue);
            //    txQuery = txQuery.Where(t => t.Date >= from);
            //    }

            //if (dateTo.HasValue)
            //    {
            //    var to = dateTo.Value.ToDateTime(TimeOnly.MaxValue);
            //    txQuery = txQuery.Where(t => t.Date <= to);
            //    }

            // 3) Bring minimal data to memory
            var txList = await txQuery
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Id) // or TransactionId
                .Select(t => new
                    {
                    t.Id,
                    t.Type,
                    t.Date,
                    t.TransactionId,
                    t.VoucherNo,
                    CustomerName = t.Customer.Name,
                    Balance = (t.Debit ?? 0m) - (t.Credit ?? 0m)
                    })
                .ToListAsync();

            var today = DateTime.Today;

            // 4) Map to ViewModel + compute Aging & totals in C#
            var rows = txList
                .Select(x => new CustomerSalesRowViewModel
                    {
                    Id = x.Id,
                    Type = x.Type,
                    Date = x.Date,
                    Num = x.TransactionId?.ToString() ?? "",
                    VoucherNo = x.VoucherNo ?? "",
                    Name = x.CustomerName ?? "",
                    Terms = "30 Days",
                    DueDate = x.Date.Value.AddDays(30),
                    Aging = (int)(today.Date - x.Date.Value.ToDateTime(TimeOnly.MinValue)).TotalDays,   // same as DATEDIFF
                    OpeningBalance = x.Balance                           // same as t.debit - t.credit
                    })
                .ToList();

            var vm = new CustomerSalesListViewModel
                {
              //  CustomerId = customerId,
                CustomerName = rows.FirstOrDefault()?.Name ?? "",
                Rows = rows,
                DateFrom = dateFrom,
                DateTo = dateTo
                };

            return vm;
            }
        public async Task<CustomerBalanceSummaryViewModel> GetCustomerBalancesSummryAsync()
            {
            // 1) Get first transaction date (like your firstDateStr)
           //Date Filtter

            // 2) Allowed types (same as your WHERE t.type IN (...))
            var allowedTypes = new[]
            {
            "Customer Receipt",
            "Sales Invoice",
            "Customer Opening Balance",
            "Check Cancel (Customer)",
            "SalesReturn Invoice",
            "Credit Note",
            "PDC Receivable"
        };

            // 3) Base query
            var query = _context.TblTransactions
                .Include(t => t.Customer)
                .Where(t =>
                    t.State == 0 &&
                    t.HumId != null && t.HumId != 0 &&
                    allowedTypes.Contains(t.Type));

            // 4) Group by customer and compute balance = SUM(debit - credit)
            var rows = await query
                .GroupBy(t => new
                    {
                    t.HumId,
                    t.Customer!.Code,
                    t.Customer!.Name
                    })
                .Select(g => new CustomerBalanceRowViewModel
                    {
                    Id = g.Key.HumId!.Value,
                    Account = g.Key.Code + " - " + g.Key.Name,
                    Amount = g.Sum(x => (x.Debit ?? 0m) - (x.Credit ?? 0m))
                    })
        //        .OrderBy(r => r.Account)
                .ToListAsync();

            return new CustomerBalanceSummaryViewModel
                {
                //  AmountHeader = header,
                Rows = rows
                };
            }

        public async Task<CustomerBalanceDetailViewModel> GetCustomerDetailsStatementAsync(int humId,DateOnly? dateFrom = null,DateOnly? dateTo = null)
            {
            var allowedTypes = new[]
            {
        "Customer Receipt",
        "Sales Invoice",
        "Sales Invoice Cash",
        "Customer Opening Balance",
        "Check Cancel (Customer)",
        "SalesReturn Invoice",
        "Credit Note",
        "PDC Receivable"
    };

            // Base query = FROM tbl_transaction t
            var query = _context.TblTransactions
                .Include(t => t.Account)   // tbl_coa_level_4
                .Where(t =>
                    t.HumId == humId &&
                    allowedTypes.Contains(t.Type));

            // {dateFilter}
            if (dateFrom.HasValue)
                query = query.Where(t => t.Date >= dateFrom.Value);

            if (dateTo.HasValue)
                query = query.Where(t => t.Date <= dateTo.Value);

            // ORDER BY t.date, t.id
            var rows = await query
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Id)
                .Select(t => new CustomerBalanceDetailsRowViewModel
                    {
                     Date = t.Date, 
                    TransactionId = t.TransactionId.ToString(),
                    Num = t.VoucherNo,            
                    Type = t.Type,
                    Account = t.Account.Name,
                    Amount = (t.Debit ?? 0m) - (t.Credit ?? 0m)

                    })
                .ToListAsync();

         
            decimal runningBalance = 0m;

            foreach (var row in rows)
                {
                if (!string.Equals(row.Type, "Sales Invoice Cash", StringComparison.OrdinalIgnoreCase))
                    {
                    runningBalance += row.Amount;
                    }

                row.Balance = runningBalance;
                }

            return new CustomerBalanceDetailViewModel
                {
                Rows=rows
                };
            }

        }

    }
