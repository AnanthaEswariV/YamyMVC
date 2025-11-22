namespace YamyProject.Services.Implementations
    {
    public class IncomeSummaryServices(YamyDbContext context): IIncomeSummaryServices
        {
        private readonly YamyDbContext _context = context;
        public async Task<IEnumerable<IncomeSummaryRowViewModel>> GetIncomeByCustomerSummary(/*DateOnly dateFrom, DateOnly dateTo,bool All=true*/)
            {
            //var DateFelter = "  allowedTypes.Contains(t.Type)  ";
            //if (!All)
            //    { DateFelter = " && (t.Date >= dateFrom) && (t.Date >= dateTo)"; }
            var allowedTypes = new[]
      {
            "Sales Invoice",
            "Sales Invoice Cash",
            "Customer Opening Balance",
            "Customer Receipt"
        };
            // DateTime? toExclusive = to?.Date.AddDays(1);

            //var rows = await _context.TblTransactions
            //                              .Where(t => allowedTypes.Contains(t.Type)
            //       )
            //.GroupBy(t => new { t.HumId, t.Customer!.Id, t.Customer!.Name })
            //.Select(g => new IncomeSummaryRowViewModel
            //    {
            //    Id = g.Key.Id,                        // Equivalent to MIN(h.id) for that group
            //    Name = g.Key.Name,
            //    Balance = (decimal)g.Sum(x => x.Debit - x.Credit)
            //    })
            //.OrderByDescending(x => x.Balance)
            //.ToListAsync();

            var rows = await _context.TblTransactions
    .Where(t => allowedTypes.Contains(t.Type) && t.HumId != null && t.HumId !=0) // if relation optional
    .GroupBy(t => new { Id = t.Customer!.Id, Name = t.Customer!.Name })
    .Select(g => new IncomeSummaryRowViewModel
        {
        Id = g.Key.Id,                 // non-null now
        Name = g.Key.Name,
        SN=+1,
        Balance = g.Sum(x => (x.Debit ?? 0m) - (x.Credit ?? 0m)) // no cast needed
        })
    .OrderByDescending(x => x.Balance)
    .ToListAsync();

            return rows;
            // Implementation logic to fetch and return income summary by customer
            throw new NotImplementedException();
            }
        public async Task<IEnumerable<IncomeByVendorSummaryRowViewModel>> BuildIncomeByVendorSummary(/*DateOnly? fromDate, DateOnly? toDate, bool isAll = true*/)
            {
            var allowedTypes = new[]
              {
                  "Purchase Invoice",
                  "Purchase Invoice Cash",
                  "Vendor Opening Balance",
                  "Vendor Payment"
                  
              };
            var Index = 0;
            var rows = await _context.TblTransactions
                 .Where(t => allowedTypes.Contains(t.Type) && t.HumId != null && t.HumId != 0) // if relation optional
                 .GroupBy(t => new { Id = t.Vendors!.Id, Name = t.Vendors!.Name })
                 .Select(g => new IncomeByVendorSummaryRowViewModel
                     {
                     Id = g.Key.Id,                 // non-null now
                     VendorName = g.Key.Name,
                     
                     Balance = g.Sum(x => (x.Debit ?? 0m) - (x.Credit ?? 0m)) // no cast needed
                     })
                 .OrderByDescending(x => x.Balance)
                 .ToListAsync();


            return rows;
            // Implementation logic to fetch and return income summary by customer
            throw new NotImplementedException();

            }

       public async Task<IEnumerable<IncomeByCustomerRowViewModel>> GetIncomeByCustomerDetail(int customerId)
            {        

            var allowedTypes = new[]
            {
                "Sales Invoice",
                "Sales Invoice Cash",
                "Customer Opening Balance",
                "Customer Receipt"
             };
            // base query
            var query = _context.TblTransactions
                .Where(t => allowedTypes.Contains(t.Type)
                            && t.HumId == customerId)  
                .Include(t => t.Customer)           
                .Include(t => t.Account);             

            //// {dateFilter}
            //if (fromDate.HasValue)
            //    query = query.Where(t => t.Date >= fromDate.Value);

            //if (toDate.HasValue)
            //    query = query.Where(t => t.Date <= toDate.Value);

            // ORDER BY h.name, t.date, t.id
            var result = await query
                .OrderBy(t => t.Customer.Name)
                .ThenBy(t => t.Date)
                .ThenBy(t => t.Id)
                .Select(t => new IncomeByCustomerRowViewModel
                    {
                    Id = t.Id,
                    TransactionId = t.TransactionId,
                    Customer = t.Customer.Name,
                    Type = t.Type,
                    Date = t.Date,
                    Num = t.VoucherNo,
                    Memo = t.Description,
                    Account = t.Account != null ? t.Account.Name : null,
                    Debit = t.Debit,
                    Credit = t.Credit,
                    Balance= (decimal)(t.Debit ?? 0m - (t.Credit ?? 0m))
                    })
                .ToListAsync();

            return result;

            throw new NotImplementedException();
        }
       public async Task<IEnumerable<IncomeByCustomerRowViewModel>> GetIncomeByVendorDetail(int customerId)
            {        

            var allowedTypes = new[]
            {
                  "Purchase Invoice",
                  "Purchase Invoice Cash",
                  "Vendor Opening Balance",
                  "Vendor Payment"
             };
            // base query
            var query = _context.TblTransactions
                .Where(t => allowedTypes.Contains(t.Type)
                            && t.HumId == customerId)  
                .Include(t => t.Vendors)           
                .Include(t => t.Account);             

            //// {dateFilter}
            //if (fromDate.HasValue)
            //    query = query.Where(t => t.Date >= fromDate.Value);

            //if (toDate.HasValue)
            //    query = query.Where(t => t.Date <= toDate.Value);

            // ORDER BY h.name, t.date, t.id
            var result = await query
                .OrderBy(t => t.Vendors.Name)
                .ThenBy(t => t.Date)
                .ThenBy(t => t.Id)
                .Select(t => new IncomeByCustomerRowViewModel
                    {
                    Id = t.Id,
                    TransactionId = t.TransactionId,
                    Customer = t.Vendors.Name,
                    Type = t.Type,
                    Date = t.Date,
                    Num = t.VoucherNo,
                    Memo = t.Description,
                    Account = t.Account != null ? t.Account.Name : null,
                    Debit = t.Debit,
                    Credit = t.Credit,
                    Balance= (decimal)(t.Debit ?? 0m - (t.Credit ?? 0m))
                    })
                .ToListAsync();

            return result;

            throw new NotImplementedException();
        }
       public async Task<List<EquityBalanceRowViewModel>> GetEquityWithFinalBalanceAsync()
            {
            // 0) Base query – include Level1 via Level4 → Level3 → Level2 → Level1
            var txWithL1 = _context.TblTransactions
                .Where(t =>
                    t.Account != null &&
                    t.Account.Account != null &&
                    t.Account.Account.Account != null &&
                    t.Account.Account.Account.Account != null)
                .Select(t => new
                    {
                    t.Debit,
                    t.Credit,
                    Level1Id = t.Account.Account.Account.Account.Id,    // tbl_coa_level_1.id
                    Level1Name = t.Account.Account.Account.Account.Name   // tbl_coa_level_1.name
                    });

            // 1) AccountBalances CTE => Income, Cost, General & Direct Expenses
            var plCategories = new[] { "Income", "Cost", "General & Direct Expenses" };

            var accountBalances = await txWithL1
                .Where(x => plCategories.Contains(x.Level1Name))
                .GroupBy(x => x.Level1Name)
                .Select(g => new
                    {
                    Category = g.Key,
                    Balance = g.Sum(x =>
                        (decimal?)((x.Debit ?? 0m) - (x.Credit ?? 0m))
                    ) ?? 0m
                    })
                .ToListAsync();

            var income = accountBalances.FirstOrDefault(x => x.Category == "Income")?.Balance ?? 0m;
            var cost = accountBalances.FirstOrDefault(x => x.Category == "Cost")?.Balance ?? 0m;
            var expenses = accountBalances.FirstOrDefault(x => x.Category == "General & Direct Expenses")?.Balance ?? 0m;

            // 2) FinalBalance CTE => Income - Cost - Expenses
            var finalBalance = income - cost - expenses;

            // 3) EquityBalances CTE – balances for Assets, Liabilities, Equity (Level 1)
            //    (If in your DB Level1 names are different, just change this array)
            var equityCategories = new[] { "Assets", "Liabilities", "Equity" };
            // If you really meant the WHERE from your SQL (`Income`, `Cost`, `General & Direct Expenses`)
            // then use those strings here instead.

            var equityBalancesRaw = await txWithL1
                .Where(x => equityCategories.Contains(x.Level1Name))
                .GroupBy(x => new { x.Level1Id, x.Level1Name })
                .Select(g => new
                    {
                    Id = g.Key.Level1Id,
                    Name = g.Key.Level1Name,
                    Balance = g.Sum(x =>
                        (decimal?)((x.Debit ?? 0m) - (x.Credit ?? 0m))
                    ) ?? 0m
                    })
                .OrderBy(x => x.Id)
                .ToListAsync();

            // 4) Final SELECT with CASE WHEN name = 'Equity' THEN balance + FinalBalance ELSE balance
            var result = equityBalancesRaw
                .Select(e => new EquityBalanceRowViewModel
                    {
                    Id = e.Id,
                    Name = e.Name,
                    Balance = e.Name == "Equity"
                              ? e.Balance + finalBalance
                              : e.Balance
                    })
                .ToList();

            // (Optional) If you ALSO want a separate row for the Final Balance like in the CTE comment:
            // result.Add(new EquityBalanceRowViewModel
            // {
            //     Id      = 0,
            //     Name    = "Final Balance (Income - Cost - Expenses)",
            //     Balance = finalBalance
            // });

            return result;
            }
          
        public async Task<List<CashFlowRowViewModel>> GetCashFlowStatementAsync(/*DateOnly startDate, DateOnly endDate*/)
            {
            // 1) Net Income part (the CTE + final expression)

            var plCategories = new[] { "INCOME", "COST", "EXPENSE" };

            // Transactions with Level1 (category_code)
            var txWithL1 = _context.TblTransactions
                .Include(t => t.Account)                     // TblCoaLevel4
                    .ThenInclude(l4 => l4.Account)          // TblCoaLevel3
                        .ThenInclude(l3 => l3.Account)      // TblCoaLevel2
                            .ThenInclude(l2 => l2.Account)  // TblCoaLevel1
                .Where(t =>
                    t.State == 0 &&
                   // t.Date >= startDate && t.Date <= endDate &&
                    t.Account != null &&
                    t.Account.Account != null &&
                    t.Account.Account.Account != null &&
                    t.Account.Account.Account.Account != null &&
                    plCategories.Contains(t.Account.Account.Account.Account.CategoryCode))
                .Select(t => new
                    {
                    t.Debit,
                    t.Credit,
                    Category = t.Account.Account.Account.Account.CategoryCode
                    });

            var accountBalances = await txWithL1
                .GroupBy(x => x.Category)
                .Select(g => new
                    {
                    Category = g.Key,
                    Balance = g.Sum(x => x.Credit - x.Debit)   // SUM(t.credit - t.debit)
                    })
                .ToListAsync();

            decimal income = accountBalances.FirstOrDefault(x => x.Category == "INCOME")?.Balance ?? 0m;
            decimal cost = accountBalances.FirstOrDefault(x => x.Category == "COST")?.Balance ?? 0m;
            decimal expense = accountBalances.FirstOrDefault(x => x.Category == "EXPENSE")?.Balance ?? 0m;
            decimal netIncome = income - cost - expense;
          
            // 2) The last SELECT: asset accounts with SUM(t.debit - t.credit)

            var assetAdjustments = await
                (from t in _context.TblTransactions
                 where
                     t.State == 0 &&
                    // t.Date >= startDate &&
                    // t.Date <= endDate &&
                     t.Account != null &&
                     t.Account.Account != null &&
                     t.Account.Account.Account != null &&
                     t.Account.Account.Account.Account != null &&
                     t.Account.Account.Account.Account.CategoryCode == "ASSET" // l1.category_code = 'ASSET'
                 group t by new
                     {
                     L4Id = t.Account.Id,
                     L4Name = t.Account.Name
                     }
                 into g
                 select new CashFlowRowViewModel
                     {
                     Name = "  " + g.Key.L4Name,                              // CONCAT('  ', l4.name)
                     Id = g.Key.L4Id,
                     Balance = g.Sum(x => (decimal?)(x.Debit - x.Credit)) ?? 0m, // SUM(t.debit - t.credit)
                     Mode = "n",
                     Level = "4",
                     Symbol = "                    "
                     })
                .ToListAsync();


            // 3) UNION all parts in the same order as the SQL

            var rows = new List<CashFlowRowViewModel>
    {
        new CashFlowRowViewModel
        {
            Name    = "OPERATING ACTIVITIES",
            Id      = null,          // '' in SQL
            Balance = null,          // NULL
            Mode    = "u",
            Level   = "1",
            Symbol  = " ►     "
        },
        new CashFlowRowViewModel
        {
            Name    = "Net Income",
            Id      = null,
            Balance = netIncome,     // result of the CTE calculation
            Mode    = "n",
            Level   = "2",
            Symbol  = "                    "
        },
        new CashFlowRowViewModel
        {
            Name    = "Adjustments to reconcile Net Income",
            Id      = null,
            Balance = null,
            Mode    = "u",
            Level   = "2",
            Symbol  = "   ►     "
        },
        new CashFlowRowViewModel
        {
            Name    = "to net cash provided by operations:",
            Id      = null,
            Balance = null,
            Mode    = "u",
            Level   = "2",
            Symbol  = "   ►     "
        }
    };

            rows.AddRange(assetAdjustments);



            rows.Add(new CashFlowRowViewModel
                {
                Mode = "n",
                Level = "2",
                Symbol = "         ",  // indentation
                Name = "Net cash provided by Operating Activities",
                Id = 0,
                Balance = netIncome
                });

            decimal totalAmount = netIncome; // totalAmount = totalBalance;
            decimal totalBalance = 0m;                   // totalBalance = 0;


            // Header row: 'INVESTING ACTIVITIES'
            rows.Add(new CashFlowRowViewModel
                {
                Mode = "u",
                Level = "1",
                Symbol = " ►     ",
                Name = "INVESTING ACTIVITIES",
                Id = null,
                Balance = null
                });

            // Detail rows: accounts under Level1 names 'Fixed Assets' or 'Investments'
            var investingDetails = await _context.TblTransactions
                .Include(t => t.Account)                       // L4
                    .ThenInclude(l4 => l4.Account)            // L3
                        .ThenInclude(l3 => l3.Account)        // L2
                            .ThenInclude(l2 => l2.Account)    // L1
                .Where(t =>
                    t.State == 0 &&
                 //   t.Date >= startDate &&
                  //  t.Date <= endDate &&
                    t.Account != null &&
                    t.Account.Account != null &&
                    t.Account.Account.Account != null &&
                    t.Account.Account.Account.Account != null &&
                    (
                        t.Account.Account.Account.Account.Name == "Fixed Assets" ||
                        t.Account.Account.Account.Account.Name == "Investments"
                    ))
                .GroupBy(t => new
                    {
                    L4Id = t.Account.Id,
                    L4Name = t.Account.Name
                    })
                .Select(g => new CashFlowRowViewModel
                    {
                    Mode = "n",
                    Level = "4",
                    Symbol = "                    ",
                    Name = "  " + g.Key.L4Name,                           // CONCAT('  ', l4.name)
                    Id = g.Key.L4Id,
                    Balance = g.Sum(x => (decimal?)(x.Debit - x.Credit)) ?? 0m
                    })
                .ToListAsync();

            rows.AddRange(investingDetails);

            // This is your: totalBalance += reader["balance"] ...
            totalBalance = investingDetails.Sum(x => x.Balance ?? 0m);


            // --------------------------------------------
            // 3) "Net cash provided by Investing Activities"
            // --------------------------------------------
            rows.Add(new CashFlowRowViewModel
                {
                Mode = "n",
                Level = "3",
                Symbol = "         ",
                Name = "Net cash provided by Investing Activities",
                Id = 0,
                Balance = totalBalance
                });

            totalAmount += totalBalance;  // totalAmount += totalBalance;


            // --------------------------------------------
            // 4) "Net cash increase for period"
            // --------------------------------------------
            rows.Add(new CashFlowRowViewModel
                {
                Mode = "n",
                Level = "2",
                Symbol = "    ",
                Name = "Net cash increase for period",
                Id = 0,
                Balance = totalAmount
                });

            // --------------------------------------------
            // 5) "Cash at end of period"
            // --------------------------------------------
            rows.Add(new CashFlowRowViewModel
                {
                Mode = "n",
                Level = "1",
                Symbol = "  ",
                Name = "Cash at end of period",
                Id = 0,
                Balance = totalAmount
                });

            return rows;
            }
        public async Task<List<IncomeExpenseRowViewModel>> GetIncomeExpenseStatementAsync( /*DateOnly startDate, DateOnly endDat*/)
            {
            // Base query with full account tree loaded
            var baseQuery = _context.TblTransactions
                .Include(t => t.Account)                 // Level 4
                    .ThenInclude(l4 => l4.Account)      // Level 3
                        .ThenInclude(l3 => l3.Account)  // Level 2
                            .ThenInclude(l2 => l2.Account) // Level 1
                .Where(t =>
                    t.State == 0 &&
                   // t.Date >= startDate &&
                    //t.Date <= endDate &&
                    t.Account != null &&
                    t.Account.Account != null &&
                    t.Account.Account.Account != null &&
                    t.Account.Account.Account.Account != null);

            var rows = new List<IncomeExpenseRowViewModel>();

            decimal totalRevenue = 0m;
            decimal totalCOGS = 0m;
            decimal totalExpenses = 0m;

            // 1. Ordinary Income/Expense Header
            rows.Add(new IncomeExpenseRowViewModel
                {
                RowType = "e",
                Mode = "u",
                Level = 1,
                Text = "Ordinary Income/Expense",
                Amount = null
                });

            // ==============================
            // 2. INCOME SECTION
            // ==============================
            rows.Add(new IncomeExpenseRowViewModel
                {
                RowType = "e",
                Mode = "u",
                Level = 2,
                Text = "Income",
                Amount = null
                });

            // Income grouped by Level 3 (code + name), sum(credit - debit)
            var incomeDetail = await baseQuery
                .Where(t => t.Account.Account.Account.Account.CategoryCode == "INCOME")
                .GroupBy(t => new
                    {
                    Code = t.Account.Account.Code,  // Level 3 code
                    Name = t.Account.Account.Name   // Level 3 name
                    })
                .Select(g => new
                    {
                    Name = g.Key.Code + " " + g.Key.Name,
                    Balance = g.Sum(x => x.Credit - x.Debit)
                    })
                .ToListAsync();

            foreach (var item in incomeDetail)
                {
                totalRevenue += (decimal)item.Balance;

                rows.Add(new IncomeExpenseRowViewModel
                    {
                    RowType = "e",
                    Mode = "n",
                    Level = 3,
                    Text = "      " + item.Name,
                    Amount = item.Balance
                    });
                }

            // Total Income
            rows.Add(new IncomeExpenseRowViewModel
                {
                RowType = "e",
                Mode = "u",
                Level = 2,
                Text = "Total Income",
                Amount = totalRevenue
                });

            // ==============================
            // 3. COGS SECTION
            // ==============================
            rows.Add(new IncomeExpenseRowViewModel
                {
                RowType = "e",
                Mode = "u",
                Level = 2,
                Text = "Cost of Goods Sold",
                Amount = null
                });

            // COST grouped by Level 3 (code + name), sum(debit - credit)
            var cogsDetail = await baseQuery
                .Where(t => t.Account.Account.Account.Account.CategoryCode == "COST")
                .GroupBy(t => new
                    {
                    Code = t.Account.Account.Code,
                    Name = t.Account.Account.Name
                    })
                .Select(g => new
                    {
                    Name = g.Key.Code + " " + g.Key.Name,
                    Balance = g.Sum(x => x.Debit - x.Credit)
                    })
                .ToListAsync();

            foreach (var item in cogsDetail)
                {
                totalCOGS += (decimal)item.Balance;

                rows.Add(new IncomeExpenseRowViewModel
                    {
                    RowType = "e",
                    Mode = "n",
                    Level = 3,
                    Text = "      " + item.Name,
                    Amount = item.Balance
                    });
                }

            // Total COGS
            rows.Add(new IncomeExpenseRowViewModel
                {
                RowType = "e",
                Mode = "u",
                Level = 2,
                Text = "Total COGS",
                Amount = totalCOGS
                });

            // ==============================
            // 4. GROSS PROFIT
            // ==============================
            var grossProfit = totalRevenue - totalCOGS;

            rows.Add(new IncomeExpenseRowViewModel
                {
                RowType = "e",
                Mode = "n",
                Level = 2,
                Text = "Gross Profit",
                Amount = grossProfit
                });

            // ==============================
            // 5. EXPENSE SECTION
            // ==============================
            rows.Add(new IncomeExpenseRowViewModel
                {
                RowType = "e",
                Mode = "u",
                Level = 2,
                Text = "Expense",
                Amount = null
                });

            // EXPENSE grouped by Level 4 (name), sum(debit - credit)
            var expenseDetail = await baseQuery
                .Where(t => t.Account.Account.Account.Account.CategoryCode == "EXPENSE")
                .GroupBy(t => new
                    {
                    Id = t.Account.Id,
                    Name = t.Account.Name
                    })
                .Select(g => new
                    {
                    Name = g.Key.Name,
                    Balance = g.Sum(x => x.Debit - x.Credit)
                    })
                .ToListAsync();

            foreach (var item in expenseDetail)
                {
                totalExpenses += (decimal)item.Balance;

                rows.Add(new IncomeExpenseRowViewModel
                    {
                    RowType = "e",
                    Mode = "n",
                    Level = 3,
                    Text = "      " + item.Name,
                    Amount = item.Balance
                    });
                }

            // Total Expense
            rows.Add(new IncomeExpenseRowViewModel
                {
                RowType = "e",
                Mode = "n",
                Level = 2,
                Text = "Total Expense",
                Amount = totalExpenses
                });

            // ==============================
            // 6. NET INCOME
            // ==============================
            var netIncome = grossProfit - totalExpenses;

            rows.Add(new IncomeExpenseRowViewModel
                {
                RowType = "e",
                Mode = "n",
                Level = 1,
                Text = "Net Ordinary Income",
                Amount = netIncome
                });

            rows.Add(new IncomeExpenseRowViewModel
                {
                RowType = "e",
                Mode = "n",
                Level = 1,
                Text = "Net Income",
                Amount = netIncome
                });

            return rows;
            }
        public async Task<List<UserActivityRowViewModel>> GetUserActivityAsync( /*DateOnly startDate, DateOnly endDat*/)
            {
            var UserActivities = await _context.TblAuditLogs
                .Include( ua => ua.User)
                //.Where(ua => ua.ActionTime >= startDate && ua.ActionTime <= endDate)
                .OrderByDescending(ua => ua.ActionTime)
                .Select(ua => new UserActivityRowViewModel
                    {
                    Id = ua.Id,
                    Username = ua.User.UserName,
                    ActionType = ua.ActionType,
                    ModuleName = ua.ModuleName,
                    RecordId =(int) ua.RecordId,
                    Details = ua.Details,
                    ActionTime = (DateTime)ua.ActionTime,
                    IpAddress = ua.IpAddress
                    })
                .ToListAsync();
            return UserActivities;
            }
        }
    }