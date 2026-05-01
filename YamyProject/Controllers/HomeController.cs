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

        //public IActionResult Index()
        //    {
        //    int UserId = HttpContext.Session.GetInt32("UserId") ?? 0;
        //    return View();
        //}
        public IActionResult DataTable()
        {
            return View("DataTable");
        }

        // ?????????????????????????????????????????????????????????????????????????????
        // REPLACE the existing stub  "#region Dashboard … #endregion"  inside
        // AccountController.cs with this block.
        //
        // All queries are written against YOUR actual MySQL schema:
        //   tbl_transaction | tbl_coa_level_1–4 | tbl_receipt_voucher | tbl_payment_voucher
        // ?????????????????????????????????????????????????????????????????????????????

        #region Dashboard

        public async Task<IActionResult> Index()
        {
            var dbName = HttpContext.Session.GetString("DatabaseName");
            if (string.IsNullOrEmpty(dbName))
                return RedirectToAction("Login");

            var connStrBuilder = new MySqlConnectionStringBuilder(
                _config.GetConnectionString("DefaultConnection"))
            {
                Database = dbName
            };

            using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            var vm = new AccountingDashboardViewModel();

            var yearStart = new DateTime(DateTime.Now.Year, 1, 1);

            // ????????????????????????????????????????????????????????????????????
            // 1.  TOTAL INCOME (YTD)
            //     Sum credit – debit in tbl_transaction for INCOME category accounts
            // ????????????????????????????????????????????????????????????????????
            const string incomeQuery = @"
        SELECT IFNULL(SUM(t.credit) - SUM(t.debit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id  = l3.id
        INNER JOIN tbl_coa_level_2 l2 ON l3.main_id  = l2.id
        INNER JOIN tbl_coa_level_1 l1 ON l2.main_id  = l1.id
        WHERE l1.category_code = 'INCOME'
          AND t.date >= @yearStart
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(incomeQuery, conn))
            {
                cmd.Parameters.AddWithValue("@yearStart", yearStart);
                var r = await cmd.ExecuteScalarAsync();
                vm.TotalIncome = r != DBNull.Value ? Convert.ToDecimal(r) : 0;
            }

            // ????????????????????????????????????????????????????????????????????
            // 2.  TOTAL EXPENSES (YTD)
            //     Sum debit – credit for EXPENSE + COST category accounts
            // ????????????????????????????????????????????????????????????????????
            const string expenseQuery = @"
        SELECT IFNULL(SUM(t.debit) - SUM(t.credit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id  = l3.id
        INNER JOIN tbl_coa_level_2 l2 ON l3.main_id  = l2.id
        INNER JOIN tbl_coa_level_1 l1 ON l2.main_id  = l1.id
        WHERE l1.category_code IN ('EXPENSE','COST')
          AND t.date >= @yearStart
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(expenseQuery, conn))
            {
                cmd.Parameters.AddWithValue("@yearStart", yearStart);
                var r = await cmd.ExecuteScalarAsync();
                vm.TotalExpenses = r != DBNull.Value ? Convert.ToDecimal(r) : 0;
            }

            // ????????????????????????????????????????????????????????????????????
            // 3.  PETTY CASH (running balance from tbl_transaction)
            //     tbl_coa_level_3 row whose name = 'Petty Cash'
            // ????????????????????????????????????????????????????????????????????
            const string pettyCashQuery = @"
        SELECT IFNULL(SUM(t.debit) - SUM(t.credit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        WHERE l3.name = 'Petty Cash'
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(pettyCashQuery, conn))
            {
                var r = await cmd.ExecuteScalarAsync();
                vm.PettyCash = r != DBNull.Value ? Convert.ToDecimal(r) : 0;
            }

            // ????????????????????????????????????????????????????????????????????
            // 4.  ACCOUNT OVERVIEW
            // ????????????????????????????????????????????????????????????????????

            // Cash in Hand  ?  same as Petty Cash balance above
            vm.CashInHand = vm.PettyCash;

            // Bank Accounts  (debit – credit for all accounts under 'Banks')
            const string bankQuery = @"
        SELECT IFNULL(SUM(t.debit) - SUM(t.credit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        WHERE l3.name = 'Banks'
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(bankQuery, conn))
            {
                var r = await cmd.ExecuteScalarAsync();
                vm.BankBalance = r != DBNull.Value ? Convert.ToDecimal(r) : 0;
            }

            // Credit Accounts  ?  Suppliers (credit – debit  = what we owe)
            const string creditQuery = @"
        SELECT IFNULL(SUM(t.credit) - SUM(t.debit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        WHERE l3.name = 'Suppliers'
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(creditQuery, conn))
            {
                var r = await cmd.ExecuteScalarAsync();
                vm.CreditBalance = -(r != DBNull.Value ? Convert.ToDecimal(r) : 0); // negative = liability
            }

            // Receivables  (debit – credit  = what customers owe us)
            const string receivableQuery = @"
        SELECT IFNULL(SUM(t.debit) - SUM(t.credit), 0)
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        WHERE l3.name = 'Accounts Receivable'
          AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(receivableQuery, conn))
            {
                var r = await cmd.ExecuteScalarAsync();
                vm.Receivables = r != DBNull.Value ? Convert.ToDecimal(r) : 0;
            }

            // ????????????????????????????????????????????????????????????????????
            // 5.  MONTHLY CHART  (last 6 months – income vs expense per month)
            // ????????????????????????????????????????????????????????????????????
            var sixMonthsAgo = DateTime.Now.AddMonths(-5);
            var chartStart = new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1);

            const string chartIncomeQ = @"
        SELECT YEAR(t.date) yr, MONTH(t.date) mo,
               IFNULL(SUM(t.credit) - SUM(t.debit), 0) AS total
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        INNER JOIN tbl_coa_level_2 l2 ON l3.main_id   = l2.id
        INNER JOIN tbl_coa_level_1 l1 ON l2.main_id   = l1.id
        WHERE l1.category_code = 'INCOME'
          AND t.date >= @chartStart
          AND (t.state = 0 OR t.state IS NULL)
        GROUP BY yr, mo";

            const string chartExpenseQ = @"
        SELECT YEAR(t.date) yr, MONTH(t.date) mo,
               IFNULL(SUM(t.debit) - SUM(t.credit), 0) AS total
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
        INNER JOIN tbl_coa_level_3 l3 ON l4.main_id   = l3.id
        INNER JOIN tbl_coa_level_2 l2 ON l3.main_id   = l2.id
        INNER JOIN tbl_coa_level_1 l1 ON l2.main_id   = l1.id
        WHERE l1.category_code IN ('EXPENSE','COST')
          AND t.date >= @chartStart
          AND (t.state = 0 OR t.state IS NULL)
        GROUP BY yr, mo";

            // build a lookup: (year*100+month) ? income / expense
            var incomeMap = new Dictionary<int, decimal>();
            var expenseMap = new Dictionary<int, decimal>();

            using (var cmd = new MySqlCommand(chartIncomeQ, conn))
            {
                cmd.Parameters.AddWithValue("@chartStart", chartStart);
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    incomeMap[rdr.GetInt32("yr") * 100 + rdr.GetInt32("mo")] =
                        rdr.GetDecimal("total");
            }

            using (var cmd = new MySqlCommand(chartExpenseQ, conn))
            {
                cmd.Parameters.AddWithValue("@chartStart", chartStart);
                using var rdr = await cmd.ExecuteReaderAsync();
                while (await rdr.ReadAsync())
                    expenseMap[rdr.GetInt32("yr") * 100 + rdr.GetInt32("mo")] =
                        rdr.GetDecimal("total");
            }

            for (int i = 0; i < 6; i++)
            {
                var d = DateTime.Now.AddMonths(-5 + i);
                int key = d.Year * 100 + d.Month;
                vm.MonthlyData.Add(new MonthlyChartItem
                {
                    Month = d.ToString("MMM yy"),
                    Income = incomeMap.TryGetValue(key, out var inc) ? inc : 0,
                    Expense = expenseMap.TryGetValue(key, out var exp) ? exp : 0
                });
            }

            // ????????????????????????????????????????????????????????????????????
            // 6.  RECENT TRANSACTIONS  (last 10 – union of receipt & payment vouchers)
            // ????????????????????????????????????????????????????????????????????
            const string recentQuery = @"
        (SELECT date, IFNULL(description,'') AS description,
                code AS voucherCode, 'Income' AS txType, amount
         FROM tbl_receipt_voucher
         WHERE state = 0 OR state IS NULL
         ORDER BY date DESC LIMIT 10)
        UNION ALL
        (SELECT date, IFNULL(description,'') AS description,
                code AS voucherCode, 'Expense' AS txType, amount
         FROM tbl_payment_voucher
         WHERE state = 0 OR state IS NULL
         ORDER BY date DESC LIMIT 10)
        ORDER BY date DESC
        LIMIT 10";

            using (var cmd = new MySqlCommand(recentQuery, conn))
            using (var rdr = await cmd.ExecuteReaderAsync())
            {
                while (await rdr.ReadAsync())
                {
                    vm.RecentTransactions.Add(new DashboardTransaction
                    {
                        Date = rdr.GetDateTime("date"),
                        Description = rdr["description"].ToString(),
                        VoucherCode = rdr["voucherCode"].ToString(),
                        TxType = rdr["txType"].ToString(),
                        Amount = rdr.GetDecimal("amount")
                    });
                }
            }

            return View(vm);
        }

        #endregion
    }
}
