namespace YamyProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;
        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

   
        public IActionResult DataTable()
        {
            return View("DataTable");
        }


        #region Dashboard

        public async Task<IActionResult> Index(int? year)
        {
            var dbName = HttpContext.Session.GetString("DatabaseName");
            if (string.IsNullOrEmpty(dbName))
                return RedirectToAction("Login");

            var connStrBuilder = new MySqlConnectionStringBuilder(
                _config.GetConnectionString("DefaultConnection"))
            { Database = dbName };

            using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            // ── Year bounds ──────────────────────────────────────────────────────
            int selectedYear = year.HasValue && year.Value > 2000 && year.Value <= DateTime.Now.Year
                ? year.Value
                : DateTime.Now.Year;

            var yearStart = new DateTime(selectedYear, 1, 1);
            var yearEnd = new DateTime(selectedYear, 12, 31);

            var vm = new AccountingDashboardViewModel { SelectedYear = selectedYear };

            // ── 1. Total Income ──────────────────────────────────────────────────
            const string incomeQ = @"
        SELECT IFNULL(SUM(t.credit) - SUM(t.debit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        INNER JOIN tbl_coa_level_2 l2 ON l3.main_id   = l2.id
        INNER JOIN tbl_coa_level_1 l1 ON l2.main_id   = l1.id
        WHERE l1.category_code = 'INCOME'
          AND t.date BETWEEN @ys AND @ye
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(incomeQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);
                var r = await cmd.ExecuteScalarAsync();
                vm.TotalIncome = r != DBNull.Value ? Convert.ToDecimal(r) : 0;
            }

            // ── 2. Total Expenses ────────────────────────────────────────────────
            const string expenseQ = @"
        SELECT IFNULL(SUM(t.debit) - SUM(t.credit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        INNER JOIN tbl_coa_level_2 l2 ON l3.main_id   = l2.id
        INNER JOIN tbl_coa_level_1 l1 ON l2.main_id   = l1.id
        WHERE l1.category_code IN ('EXPENSE','COST')
          AND t.date BETWEEN @ys AND @ye
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(expenseQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);
                var r = await cmd.ExecuteScalarAsync();
                vm.TotalExpenses = r != DBNull.Value ? Convert.ToDecimal(r) : 0;
            }

            vm.CurrentBalance = vm.TotalIncome - vm.TotalExpenses;

            // ── 3. Petty Cash ────────────────────────────────────────────────────
            const string pettyCashQ = @"
        SELECT IFNULL(SUM(t.debit) - SUM(t.credit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        WHERE l3.name = 'Petty Cash'
          AND t.date BETWEEN @ys AND @ye
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(pettyCashQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);
                var r = await cmd.ExecuteScalarAsync();
                vm.PettyCash = r != DBNull.Value ? Convert.ToDecimal(r) : 0;
            }

            vm.CashInHand = vm.PettyCash;

            // ── 4. Bank Balance ──────────────────────────────────────────────────
            const string bankQ = @"
        SELECT IFNULL(SUM(t.debit) - SUM(t.credit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        WHERE l3.name = 'Banks'
          AND t.date BETWEEN @ys AND @ye
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(bankQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);
                var r = await cmd.ExecuteScalarAsync();
                vm.BankBalance = r != DBNull.Value ? Convert.ToDecimal(r) : 0;
            }

            // ── 5. Credit / Suppliers ────────────────────────────────────────────
            const string creditQ = @"
        SELECT IFNULL(SUM(t.credit) - SUM(t.debit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        WHERE l3.name = 'Suppliers'
          AND t.date BETWEEN @ys AND @ye
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(creditQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);
                var r = await cmd.ExecuteScalarAsync();
                vm.CreditBalance = -(r != DBNull.Value ? Convert.ToDecimal(r) : 0);
            }

            // ── 6. Receivables ───────────────────────────────────────────────────
            const string receivableQ = @"
        SELECT IFNULL(SUM(t.debit) - SUM(t.credit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        WHERE l3.name = 'Accounts Receivable'
          AND t.date BETWEEN @ys AND @ye
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(receivableQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);
                var r = await cmd.ExecuteScalarAsync();
                vm.Receivables = r != DBNull.Value ? Convert.ToDecimal(r) : 0;
            }

            // ── 7. Monthly Chart (all 12 months of selected year) ────────────────
            const string chartIncomeQ = @"
        SELECT MONTH(t.date) mo, IFNULL(SUM(t.credit) - SUM(t.debit), 0) AS total
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        INNER JOIN tbl_coa_level_2 l2 ON l3.main_id   = l2.id
        INNER JOIN tbl_coa_level_1 l1 ON l2.main_id   = l1.id
        WHERE l1.category_code = 'INCOME'
          AND t.date BETWEEN @ys AND @ye
          AND (t.state = 0 OR t.state IS NULL)
        GROUP BY mo";

            const string chartExpenseQ = @"
        SELECT MONTH(t.date) mo, IFNULL(SUM(t.debit) - SUM(t.credit), 0) AS total
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        INNER JOIN tbl_coa_level_2 l2 ON l3.main_id   = l2.id
        INNER JOIN tbl_coa_level_1 l1 ON l2.main_id   = l1.id
        WHERE l1.category_code IN ('EXPENSE','COST')
          AND t.date BETWEEN @ys AND @ye
          AND (t.state = 0 OR t.state IS NULL)
        GROUP BY mo";

            var incomeMap = new Dictionary<int, decimal>();
            var expenseMap = new Dictionary<int, decimal>();

            using (var cmd = new MySqlCommand(chartIncomeQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    incomeMap[rdr.GetInt32("mo")] = rdr.GetDecimal("total");
            }

            using (var cmd = new MySqlCommand(chartExpenseQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    expenseMap[rdr.GetInt32("mo")] = rdr.GetDecimal("total");
            }

            // Limit chart to months up to today if selected year == current year
            int maxMonth = selectedYear == DateTime.Now.Year ? DateTime.Now.Month : 12;

            for (int m = 1; m <= maxMonth; m++)
            {
                vm.MonthlyData.Add(new MonthlyChartItem
                {
                    Month = new DateTime(selectedYear, m, 1).ToString("MMM", new CultureInfo("en-US")),
                    Income = incomeMap.TryGetValue(m, out var inc) ? inc : 0,
                    Expense = expenseMap.TryGetValue(m, out var exp) ? exp : 0
                });
            }

            // ── 8. Recent Transactions ───────────────────────────────────────────
            const string recentQ = @"
        (SELECT date, IFNULL(description,'') AS description,
                code AS voucherCode, 'Income' AS txType, amount
         FROM tbl_receipt_voucher
         WHERE (state = 0 OR state IS NULL)
           AND date BETWEEN @ys AND @ye
         ORDER BY date DESC LIMIT 10)
        UNION ALL
        (SELECT date, IFNULL(description,'') AS description,
                code AS voucherCode, 'Expense' AS txType, amount
         FROM tbl_payment_voucher
         WHERE (state = 0 OR state IS NULL)
           AND date BETWEEN @ys AND @ye
         ORDER BY date DESC LIMIT 10)
        ORDER BY date DESC
        LIMIT 10";

            using (var cmd = new MySqlCommand(recentQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                {
                    vm.RecentTransactions.Add(new DashboardTransaction
                    {
                        Date = rdr.GetDateTime("date"),
                        Description = rdr["description"].ToString()!,
                        VoucherCode = rdr["voucherCode"].ToString()!,
                        TxType = rdr["txType"].ToString()!,
                        Amount = rdr.GetDecimal("amount")
                    });
                }
            }

            return View(vm);
        }

        #endregion
    }
}
