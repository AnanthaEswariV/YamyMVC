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

            var connBuilder = new MySqlConnectionStringBuilder(
                _config.GetConnectionString("DefaultConnection"))
            {
                Database = dbName
            };

            using var conn = new MySqlConnection(connBuilder.ConnectionString);
            await conn.OpenAsync();

            int selectedYear = year.HasValue &&
                               year.Value > 2000 &&
                               year.Value <= DateTime.Now.Year
                ? year.Value
                : DateTime.Now.Year;

            DateTime yearStart = new(selectedYear, 1, 1);
            DateTime yearEnd = new(selectedYear, 12, 31);

            var vm = new AccountingDashboardViewModel
            {
                SelectedYear = selectedYear
            };

            // ==========================
            // TOTAL INCOME
            // ==========================
            const string incomeQ = @"
    SELECT IFNULL(SUM(t.credit) - SUM(t.debit),0)
    FROM tbl_transaction t
    INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
    INNER JOIN tbl_coa_level_3 l3 ON l4.main_id = l3.id
    INNER JOIN tbl_coa_level_2 l2 ON l3.main_id = l2.id
    INNER JOIN tbl_coa_level_1 l1 ON l2.main_id = l1.id
    WHERE l1.category_code IN ('ASSET','INCOME')
      AND t.date BETWEEN @ys AND @ye
      AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(incomeQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);

                var result = await cmd.ExecuteScalarAsync();
                vm.TotalIncome = result != DBNull.Value
                    ? Convert.ToDecimal(result)
                    : 0;
            }

            // ==========================
            // TOTAL EXPENSE
            // ==========================
            const string expenseQ = @"
    SELECT IFNULL(SUM(t.debit) - SUM(t.credit),0)
    FROM tbl_transaction t
    INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
    INNER JOIN tbl_coa_level_3 l3 ON l4.main_id = l3.id
    INNER JOIN tbl_coa_level_2 l2 ON l3.main_id = l2.id
    INNER JOIN tbl_coa_level_1 l1 ON l2.main_id = l1.id
    WHERE l1.category_code IN ('EXPENSE','COST')
      AND t.date BETWEEN @ys AND @ye
      AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(expenseQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);

                var result = await cmd.ExecuteScalarAsync();
                vm.TotalExpenses = result != DBNull.Value
                    ? Convert.ToDecimal(result)
                    : 0;
            }

            // ==========================
            // NET BALANCE
            // ==========================
            vm.CurrentBalance =
                vm.TotalIncome - vm.TotalExpenses;

            // ==========================
            // PETTY CASH
            // ==========================
            const string pettyCashQ = @"
    SELECT IFNULL(SUM(t.debit) - SUM(t.credit),0)
    FROM tbl_transaction t
    INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
    INNER JOIN tbl_coa_level_3 l3 ON l4.main_id = l3.id
    WHERE l3.name = 'Petty Cash'
      AND t.date BETWEEN @ys AND @ye
      AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(pettyCashQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);

                var result = await cmd.ExecuteScalarAsync();
                vm.PettyCash = result != DBNull.Value
                    ? Convert.ToDecimal(result)
                    : 0;
            }

            vm.CashInHand = vm.PettyCash;

            // ==========================
            // BANK BALANCE
            // ==========================
            const string bankQ = @"
    SELECT IFNULL(SUM(t.debit) - SUM(t.credit),0)
    FROM tbl_transaction t
    INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
    INNER JOIN tbl_coa_level_3 l3 ON l4.main_id = l3.id
    WHERE l3.name = 'Banks'
      AND t.date BETWEEN @ys AND @ye
      AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(bankQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);

                var result = await cmd.ExecuteScalarAsync();
                vm.BankBalance = result != DBNull.Value
                    ? Convert.ToDecimal(result)
                    : 0;
            }

            // ==========================
            // CREDIT BALANCE
            // ==========================
            const string creditQ = @"
    SELECT IFNULL(SUM(t.credit) - SUM(t.debit),0)
    FROM tbl_transaction t
    INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
    INNER JOIN tbl_coa_level_3 l3 ON l4.main_id = l3.id
    WHERE l3.name = 'Suppliers'
      AND t.date BETWEEN @ys AND @ye
      AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(creditQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);

                var result = await cmd.ExecuteScalarAsync();

                vm.CreditBalance = result != DBNull.Value
                    ? Convert.ToDecimal(result)
                    : 0;
            }

            // ==========================
            // RECEIVABLES
            // ==========================
            const string receivableQ = @"
    SELECT IFNULL(SUM(t.debit) - SUM(t.credit),0)
    FROM tbl_transaction t
    INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
    INNER JOIN tbl_coa_level_3 l3 ON l4.main_id = l3.id
    WHERE l3.name = 'Accounts Receivable'
      AND t.date BETWEEN @ys AND @ye
      AND (t.state = 0 OR t.state IS NULL)";

            using (var cmd = new MySqlCommand(receivableQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);

                var result = await cmd.ExecuteScalarAsync();

                vm.Receivables = result != DBNull.Value
                    ? Convert.ToDecimal(result)
                    : 0;
            }

            // ==========================
            // MONTHLY CHART
            // ==========================
            const string chartQ = @"
    SELECT
        MONTH(t.date) AS MonthNo,
        l1.category_code,
        SUM(t.debit) AS Debit,
        SUM(t.credit) AS Credit
    FROM tbl_transaction t
    INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
    INNER JOIN tbl_coa_level_3 l3 ON l4.main_id = l3.id
    INNER JOIN tbl_coa_level_2 l2 ON l3.main_id = l2.id
    INNER JOIN tbl_coa_level_1 l1 ON l2.main_id = l1.id
    WHERE t.date BETWEEN @ys AND @ye
      AND (t.state = 0 OR t.state IS NULL)
      AND l1.category_code IN ('INCOME','EXPENSE','COST')
    GROUP BY MONTH(t.date), l1.category_code";

            var incomeMap = new Dictionary<int, decimal>();
            var expenseMap = new Dictionary<int, decimal>();

            using (var cmd = new MySqlCommand(chartQ, conn))
            {
                cmd.Parameters.AddWithValue("@ys", yearStart);
                cmd.Parameters.AddWithValue("@ye", yearEnd);

                using var rdr = await cmd.ExecuteReaderAsync();

                while (await rdr.ReadAsync())
                {
                    int month = rdr.GetInt32("MonthNo");
                    string category = rdr["category_code"].ToString()!;

                    decimal debit = rdr.IsDBNull("Debit")
                        ? 0
                        : Convert.ToDecimal(rdr["Debit"]);

                    decimal credit = rdr.IsDBNull("Credit")
                        ? 0
                        : Convert.ToDecimal(rdr["Credit"]);

                    if (category == "INCOME")
                    {
                        incomeMap[month] =
                            incomeMap.GetValueOrDefault(month) +
                            (credit - debit);
                    }
                    else
                    {
                        expenseMap[month] =
                            expenseMap.GetValueOrDefault(month) +
                            (debit - credit);
                    }
                }
            }

            int maxMonth = selectedYear == DateTime.Now.Year
                ? DateTime.Now.Month
                : 12;

            for (int m = 1; m <= maxMonth; m++)
            {
                vm.MonthlyData.Add(new MonthlyChartItem
                {
                    Month = new DateTime(selectedYear, m, 1)
                        .ToString("MMM", new CultureInfo("en-US")),

                    Income = incomeMap.GetValueOrDefault(m),
                    Expense = expenseMap.GetValueOrDefault(m)
                });
            }

            // ==========================
            // RECENT TRANSACTIONS
            // ==========================
            const string recentQ = @"
    (
        SELECT
            date,
            IFNULL(description,'') AS description,
            code AS voucherCode,
            'Income' AS txType,
            amount
        FROM tbl_receipt_voucher
        WHERE (state = 0 OR state IS NULL)
          AND date BETWEEN @ys AND @ye
        ORDER BY date DESC
        LIMIT 10
    )
    UNION ALL
    (
        SELECT
            date,
            IFNULL(description,'') AS description,
            code AS voucherCode,
            'Expense' AS txType,
            amount
        FROM tbl_payment_voucher
        WHERE (state = 0 OR state IS NULL)
          AND date BETWEEN @ys AND @ye
        ORDER BY date DESC
        LIMIT 10
    )
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
                        Description = rdr["description"].ToString() ?? "",
                        VoucherCode = rdr["voucherCode"].ToString() ?? "",
                        TxType = rdr["txType"].ToString() ?? "",
                        Amount = Convert.ToDecimal(rdr["amount"])
                    });
                }
            }

            return View(vm);
        }

        #endregion
    }
}
