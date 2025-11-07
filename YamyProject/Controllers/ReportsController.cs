using Microsoft.AspNetCore.Mvc;
using YamyProject.Core.Models.DTOs;

namespace YamyProject.Controllers
{
    [Route("Reports/[action]")]
    public class ReportsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly MySqlConnection _connection;
        public ReportsController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _connection = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        }

        #region Account Balance Report  

        public IActionResult AccountBalanceReport()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetBalanceSheetReport(DateTime date)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Step 1: Load top-level accounts with balance
                string queryTop = @"
WITH AccountBalances AS (
    SELECT l1.id, l1.name, l1.category_code,
           COALESCE(SUM(t.debit) - SUM(t.credit),0) AS Balance
    FROM tbl_coa_level_1 l1
    LEFT JOIN tbl_coa_level_2 l2 ON l2.main_id = l1.id
    LEFT JOIN tbl_coa_level_3 l3 ON l3.main_id = l2.id
    LEFT JOIN tbl_coa_level_4 l4 ON l4.main_id = l3.id
    LEFT JOIN tbl_transaction t ON t.account_id = l4.id AND t.state=0 AND t.date <= @dated
    WHERE l1.category_code IN ('ASSET','LIABILITY','EQUITY')
    GROUP BY l1.id, l1.name, l1.category_code
),
FinalBalance AS (
    SELECT COALESCE((SELECT SUM(t.debit-t.credit) FROM tbl_coa_level_1 l1
                    LEFT JOIN tbl_coa_level_2 l2 ON l2.main_id=l1.id
                    LEFT JOIN tbl_coa_level_3 l3 ON l3.main_id=l2.id
                    LEFT JOIN tbl_coa_level_4 l4 ON l4.main_id=l3.id
                    LEFT JOIN tbl_transaction t ON t.account_id=l4.id AND t.state=0 AND t.date<=@dated
                    WHERE l1.category_code='INCOME'),0)
         - COALESCE((SELECT SUM(t.debit-t.credit) FROM tbl_coa_level_1 l1
                     LEFT JOIN tbl_coa_level_2 l2 ON l2.main_id=l1.id
                     LEFT JOIN tbl_coa_level_3 l3 ON l3.main_id=l2.id
                     LEFT JOIN tbl_coa_level_4 l4 ON l4.main_id=l3.id
                     LEFT JOIN tbl_transaction t ON t.account_id=l4.id AND t.state=0 AND t.date<=@dated
                     WHERE l1.category_code='COST'),0)
         - COALESCE((SELECT SUM(t.debit-t.credit) FROM tbl_coa_level_1 l1
                     LEFT JOIN tbl_coa_level_2 l2 ON l2.main_id=l1.id
                     LEFT JOIN tbl_coa_level_3 l3 ON l3.main_id=l2.id
                     LEFT JOIN tbl_coa_level_4 l4 ON l4.main_id=l3.id
                     LEFT JOIN tbl_transaction t ON t.account_id=l4.id AND t.state=0 AND t.date<=@dated
                     WHERE l1.category_code='EXPENSE'),0) AS Balance
)
SELECT ab.id, ab.name, ab.category_code, 
       CASE WHEN ab.category_code='EQUITY' THEN ab.Balance + (SELECT Balance FROM FinalBalance)
            ELSE ab.Balance END AS Balance
FROM AccountBalances ab
ORDER BY ab.id;
";

                var topAccounts = new List<AccountNode>();
                await using (var cmd = new MySqlCommand(queryTop, conn))
                {
                    cmd.Parameters.AddWithValue("@dated", date.Date);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        topAccounts.Add(new AccountNode
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader["name"].ToString(),
                            Balance = reader.GetDecimal("Balance"),
                            CurrLvl = 1
                        });
                    }
                }

                // Step 2: Recursively load children with balances in one query per level
                foreach (var account in topAccounts)
                {
                    await LoadChildAccountsOptimizedAsync(conn, account, date, 1);
                }

                return Ok(new { status = true, data = topAccounts });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task LoadChildAccountsOptimizedAsync(MySqlConnection conn, AccountNode parent, DateTime date, int currLvl)
        {
            if (currLvl >= 4) return;

            string queryChild = $@"
SELECT l.id,
       CONCAT(l.code,' - ',l.name) AS name,
       COALESCE(SUM(t.debit)-SUM(t.credit),0) AS Balance
FROM tbl_coa_level_{currLvl + 1} l
LEFT JOIN tbl_transaction t ON t.account_id=l.id AND t.state=0 AND t.date<=@dated
WHERE l.main_id=@parentId
GROUP BY l.id, l.code, l.name
ORDER BY l.code;
";

            var children = new List<AccountNode>();
            await using (var cmd = new MySqlCommand(queryChild, conn))
            {
                cmd.Parameters.AddWithValue("@dated", date.Date);
                cmd.Parameters.AddWithValue("@parentId", parent.Id);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    children.Add(new AccountNode
                    {
                        Id = reader.GetInt32("id"),
                        Name = reader["name"].ToString(),
                        Balance = reader.GetDecimal("Balance"),
                        CurrLvl = currLvl + 1,
                        State = (currLvl + 1 == 4) ? "n" : "e",
                        LoadState = "u"
                    });
                }
            }

            // Add children and recursively load their children
            foreach (var child in children)
            {
                parent.Children.Add(child);
                await LoadChildAccountsOptimizedAsync(conn, child, date, currLvl + 1);
            }
        }

        #endregion

        #region Employee Balance Summary

        public IActionResult EmployeeBalanceSummary()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeBalanceSummary(bool showAll = true, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // ✅ Build connection string dynamically
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Prepare base query
                var query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY v.id) AS SN,
                v.id,
                CONCAT(v.code, ' - ', v.name) AS Name,
                IFNULL(SUM(
                    CASE 
                        WHEN t.type IN ('Employee Salary', 'Loan Request') THEN t.debit
                        ELSE 0
                    END
                ), 0) AS Credit,
                IFNULL(SUM(
                    CASE 
                        WHEN t.type IN ('Employee Salary Payment', 'Employee Loan Payment') THEN t.debit
                        ELSE 0
                    END
                ), 0) AS Debit,
                IFNULL(SUM(
                    CASE 
                        WHEN t.type IN ('Employee Salary', 'Loan Request') THEN t.debit
                        WHEN t.type IN ('Employee Salary Payment', 'Employee Loan Payment') THEN -t.debit
                        ELSE 0
                    END
                ), 0) AS Balance
            FROM
                tbl_employee v
            INNER JOIN
                tbl_transaction t ON v.id = t.hum_id AND t.state = 0
            WHERE
                v.state = 0
        ";

                var parameters = new List<MySqlParameter>();

                // ✅ Apply date range filter only if showAll = false
                if (!showAll && startDate.HasValue && endDate.HasValue)
                {
                    query += " AND t.date >= @dateFrom AND t.date <= @dateTo";
                    parameters.Add(new MySqlParameter("@dateFrom", startDate.Value.Date));
                    parameters.Add(new MySqlParameter("@dateTo", endDate.Value.Date.AddDays(1).AddSeconds(-1)));
                }

                query += " GROUP BY v.id, v.code, v.name;";

                await using var cmd = new MySqlCommand(query, conn);
                if (parameters.Any())
                    cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();

                var employeeBalances = new List<object>();
                int sn = 1;
                decimal totalCredit = 0, totalDebit = 0, totalBalance = 0;

                while (await reader.ReadAsync())
                {
                    decimal credit = reader["Credit"] != DBNull.Value ? Convert.ToDecimal(reader["Credit"]) : 0;
                    decimal debit = reader["Debit"] != DBNull.Value ? Convert.ToDecimal(reader["Debit"]) : 0;
                    decimal balance = reader["Balance"] != DBNull.Value ? Convert.ToDecimal(reader["Balance"]) : 0;

                    employeeBalances.Add(new
                    {
                        SN = sn++,
                        Id = reader["id"],
                        Account = reader["Name"]?.ToString(),
                        Credit = credit.ToString("N2"),
                        Debit = debit.ToString("N2"),
                        Balance = balance.ToString("N2")
                    });

                    totalCredit += credit;
                    totalDebit += debit;
                    totalBalance += balance;
                }

                await reader.CloseAsync();
                await conn.CloseAsync();

                // ✅ Add TOTAL summary row
                employeeBalances.Add(new
                {
                    SN = "",
                    Id = "",
                    Account = "TOTAL",
                    Credit = totalCredit.ToString("N2"),
                    Debit = totalDebit.ToString("N2"),
                    Balance = totalBalance.ToString("N2")
                });

                return Ok(new { status = true, data = employeeBalances });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }



        #endregion

        #region Employee Balance Details


        public IActionResult EmployeeBalanceDetails()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeTransactionDetails(int id, bool showAll = true, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"
            SELECT 
                DATE_FORMAT(t.date, '%M %d %Y') AS `Date`,
                t.transaction_id,
                CASE 
                    WHEN t.type IN ('Employee Salary Payment', 'Employee Loan Payment', 'Employee Petty Cash Payment') THEN 
                        (SELECT code FROM tbl_payment_voucher WHERE id = t.transaction_id)
                    ELSE '' 
                END AS `Num`,
                t.type AS `Type`,
                coa.code AS `AccountCode`,
                coa.name AS `AccountName`,
                (t.debit - t.credit) AS `Amount`,
                SUM(t.debit - t.credit) OVER (
                    PARTITION BY t.hum_id 
                    ORDER BY t.date, t.id
                ) AS `Balance`
            FROM 
                tbl_transaction t
            JOIN 
                tbl_coa_level_4 coa ON t.account_id = coa.id
            WHERE 
                t.hum_id = @id
                AND t.type IN (
                    'Employee Salary Payment', 
                    'Employee Loan Payment',
                    'Employee Petty Cash Payment'
                )
                AND t.debit > 0
                AND t.state = 0
        ";

                var parameters = new List<MySqlParameter> { new("@id", id) };

                if (!showAll && startDate.HasValue && endDate.HasValue)
                {
                    query += " AND t.date >= @dateFrom AND t.date <= @dateTo";
                    parameters.Add(new MySqlParameter("@dateFrom", startDate.Value.Date));
                    parameters.Add(new MySqlParameter("@dateTo", endDate.Value.Date.AddDays(1).AddSeconds(-1)));
                }

                query += " ORDER BY t.date, t.id;";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();

                var transactionList = new List<object>();
                decimal totalAmount = 0, totalBalance = 0;

                while (await reader.ReadAsync())
                {
                    if (reader["Date"] == DBNull.Value) continue;

                    decimal amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0;
                    decimal balance = reader["Balance"] != DBNull.Value ? Convert.ToDecimal(reader["Balance"]) : 0;

                    transactionList.Add(new
                    {
                        Date = reader["Date"]?.ToString(),
                        TransactionId = reader["transaction_id"]?.ToString(),
                        Num = reader["Num"]?.ToString(),
                        Type = reader["Type"]?.ToString(),
                        Account = reader["AccountName"]?.ToString(),
                        Amount = amount.ToString("N2"),
                        Balance = balance.ToString("N2")
                    });

                    totalAmount += amount;
                    totalBalance = balance;
                }

                await reader.CloseAsync();

                transactionList.Add(new
                {
                    Date = "",
                    TransactionId = "",
                    Num = "",
                    Type = "",
                    Account = "TOTAL",
                    Amount = totalAmount.ToString("N2"),
                    Balance = totalBalance.ToString("N2")
                });

                await conn.CloseAsync();

                return Ok(new { status = true, data = transactionList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Trial Balance

        public IActionResult TrialBalance()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTrialBalanceReport(int reportType, DateTime startDate, DateTime endDate, bool includeAllDates = false)
        {
            try
            {
                // Get connection with selected database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string dateFilter = includeAllDates ? "" : " AND t.date >= @startDate AND t.date <= @endDate";
                string query = string.Empty;

                // Choose query by report type (similar to comboBox1.SelectedIndex)
                if (reportType == 0)
                {
                    query = $@"
                SELECT * FROM (
                    SELECT 
                        t4.id,
                        CONCAT(t4.code, ' - ', t4.name) AS `Account Name`,
                        SUM(CASE WHEN t.debit > t.credit THEN t.debit - t.credit ELSE 0 END) AS Debit,
                        SUM(CASE WHEN t.debit < t.credit THEN t.credit - t.debit ELSE 0 END) AS Credit,
                        SUM(CASE WHEN t.debit > t.credit THEN t.debit - t.credit ELSE 0 END) -
                        SUM(CASE WHEN t.debit < t.credit THEN t.credit - t.debit ELSE 0 END) AS Balance
                    FROM tbl_transaction t
                    INNER JOIN tbl_coa_level_4 t4 ON t.account_id = t4.id AND t.state = 0 {dateFilter}
                    GROUP BY t4.id, t4.code, t4.name
                ) AS result
                ORDER BY CASE WHEN result.id IS NULL THEN 1 ELSE 0 END, result.id;";
                }
                else if (reportType == 1)
                {
                    query = $@"
                SELECT * FROM (
                    SELECT 
                        t4.id,
                        CONCAT(t4.code, ' - ', t4.name) AS `Account Name`,
                        CASE WHEN SUM(t.debit - t.credit) >= 0 THEN SUM(t.debit - t.credit) ELSE 0 END AS Debit,
                        CASE WHEN SUM(t.debit - t.credit) < 0 THEN -SUM(t.debit - t.credit) ELSE 0 END AS Credit
                    FROM tbl_transaction t
                    INNER JOIN tbl_coa_level_4 t4 ON t.account_id = t4.id
                    WHERE t.state = 0 {dateFilter}
                    GROUP BY t4.id, t4.code, t4.name

                    UNION ALL

                    SELECT 
                        NULL AS id,
                        'TOTAL' AS `Account Name`,
                        SUM(CASE WHEN Balance >= 0 THEN Balance ELSE 0 END) AS Debit,
                        SUM(CASE WHEN Balance < 0 THEN -Balance ELSE 0 END) AS Credit
                    FROM (
                        SELECT 
                            t.account_id,
                            SUM(t.debit - t.credit) AS Balance
                        FROM tbl_transaction t
                        WHERE t.state = 0 {dateFilter}
                        GROUP BY t.account_id
                    ) AS balances
                ) AS result
                ORDER BY CASE WHEN result.id IS NULL THEN 1 ELSE 0 END, result.id;";
                }
                else if (reportType == 2)
                {
                    query = $@"
                SELECT * FROM (
                    SELECT 
                        t2.id,
                        CONCAT(t2.code, ' - ', t2.name) AS `Account Name`,
                        IFNULL((
                            SELECT SUM(tt.debit - tt.credit)
                            FROM tbl_transaction tt
                            WHERE tt.account_id = t2.id AND tt.state = 0 AND tt.date < @startDate
                        ), 0) AS Opening,
                        IFNULL(SUM(CASE WHEN t.date >= @startDate AND t.date <= @endDate THEN t.debit ELSE 0 END), 0) AS Debit,
                        IFNULL(SUM(CASE WHEN t.date >= @startDate AND t.date <= @endDate THEN t.credit ELSE 0 END), 0) AS Credit,
                        IFNULL((
                            SELECT SUM(tt.debit - tt.credit)
                            FROM tbl_transaction tt
                            WHERE tt.account_id = t2.id AND tt.state = 0 AND tt.date <= @endDate
                        ), 0) AS Balance
                    FROM tbl_transaction t
                    INNER JOIN tbl_coa_level_2 t2 ON t.account_id = t2.id AND t.state = 0
                    WHERE t.id > 0
                    GROUP BY t2.id, t2.code, t2.name

                    UNION ALL
                    SELECT 
                        NULL,
                        'TOTAL',
                        IFNULL((SELECT SUM(debit - credit) FROM tbl_transaction WHERE state = 0 AND date < @startDate), 0),
                        IFNULL((SELECT SUM(debit) FROM tbl_transaction WHERE state = 0 AND date >= @startDate AND date <= @endDate), 0),
                        IFNULL((SELECT SUM(credit) FROM tbl_transaction WHERE state = 0 AND date >= @startDate AND date <= @endDate), 0),
                        IFNULL((SELECT SUM(debit - credit) FROM tbl_transaction WHERE state = 0 AND date <= @endDate), 0)
                ) AS result
                ORDER BY CASE WHEN result.id IS NULL THEN 1 ELSE 0 END, result.id;";
                }
                else
                {
                    query = $@"
                SELECT * FROM (
                    SELECT 
                        t4.id,
                        CONCAT(t4.code, ' - ', t4.name) AS `Account Name`,
                        SUM(CASE WHEN t.debit > t.credit THEN t.debit - t.credit ELSE 0 END) AS Debit,
                        SUM(CASE WHEN t.debit < t.credit THEN t.credit - t.debit ELSE 0 END) AS Credit,
                        SUM(CASE WHEN t.debit > t.credit THEN t.debit - t.credit ELSE 0 END) -
                        SUM(CASE WHEN t.debit < t.credit THEN t.credit - t.debit ELSE 0 END) AS Balance
                    FROM tbl_transaction t
                    INNER JOIN tbl_coa_level_4 t4 ON t.account_id = t4.id AND t.state = 0 {dateFilter}
                    GROUP BY t4.id, t4.code, t4.name
                ) AS result
                ORDER BY CASE WHEN result.id IS NULL THEN 1 ELSE 0 END, result.id;";
                }

                // Execute the query
                var result = new List<Dictionary<string, object>>();
                await using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@startDate", startDate.Date);
                    cmd.Parameters.AddWithValue("@endDate", endDate.Date);

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        result.Add(row);
                    }
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        public IActionResult TransactionByAccount()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetMasterTransactionByAccount(int accountId, DateTime startDate, DateTime endDate, bool includeAllDates = false)
        {
            try
            {
                // Build connection string based on active database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Handle optional date filter
                string dateFilter = includeAllDates ? "" : " AND t.date >= @startDate AND t.date <= @endDate";

                string query = $@"
            SELECT 
                t.date,
                t.type,
                t.hum_id,
                t.description,
                t.transaction_id,
                t.debit,
                t.credit,
                SUM(t.debit - t.credit) OVER (PARTITION BY t.account_id ORDER BY t.date, t.id) AS Amount
            FROM tbl_transaction t
            WHERE t.account_id = @accountId
            {dateFilter}
            ORDER BY t.date, t.id;";

                var result = new List<Dictionary<string, object>>();

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@accountId", accountId);
                    cmd.Parameters.AddWithValue("@startDate", startDate.Date);
                    cmd.Parameters.AddWithValue("@endDate", endDate.Date);

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        }
                        result.Add(row);
                    }
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region General Ledger

        public IActionResult GeneralLedger()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesReport(
      string reportType,
      DateTime? startDate = null,
      DateTime? endDate = null)
        {
            try
            {
                // 1️⃣ Build connection string dynamically (session-based database name)
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 2️⃣ Get all relevant chart-of-account (COA) entries with transactions
                string sqlAccounts = @"
            SELECT id, CONCAT(code, ' - ', name) AS name
            FROM tbl_coa_level_4
            WHERE id IN (SELECT DISTINCT account_id FROM tbl_transaction);";

                var accounts = new List<AccountReport>();

                await using (var cmd = new MySqlCommand(sqlAccounts, conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        accounts.Add(new AccountReport
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader["name"].ToString(),
                            Transactions = new List<TransactionRow>(),
                            TotalDebit = 0,
                            TotalCredit = 0,
                            Balance = 0
                        });
                    }
                }

                // 3️⃣ Loop through each account and pull its transactions
                foreach (var acc in accounts)
                {
                    string sqlTrans = @"
                SELECT 
                    DATE_FORMAT(t.date, '%M %d %Y') AS date,
                    t.transaction_id AS Num,
                    t.type AS Type,
                    t.description,
                    t.debit,
                    t.credit
                FROM tbl_transaction t
                WHERE t.account_id = @id";

                    if (startDate.HasValue && endDate.HasValue)
                        sqlTrans += " AND t.date >= @startDate AND t.date <= @endDate";

                    sqlTrans += " ORDER BY t.date;";

                    await using var cmdTrans = new MySqlCommand(sqlTrans, conn);
                    cmdTrans.Parameters.AddWithValue("@id", acc.Id);
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        cmdTrans.Parameters.AddWithValue("@startDate", startDate.Value.Date);
                        cmdTrans.Parameters.AddWithValue("@endDate", endDate.Value.Date);
                    }

                    decimal runningDebit = 0, runningCredit = 0;

                    await using var readerTrans = await cmdTrans.ExecuteReaderAsync();
                    while (await readerTrans.ReadAsync())
                    {
                        decimal debit = readerTrans.GetDecimal("debit");
                        decimal credit = readerTrans.GetDecimal("credit");

                        runningDebit += debit;
                        runningCredit += credit;

                        acc.Transactions.Add(new TransactionRow
                        {
                            Num = readerTrans["Num"].ToString(),
                            Type = readerTrans["Type"].ToString(),
                            Date = readerTrans["date"].ToString(),
                            Description = readerTrans["description"].ToString(),
                            Debit = debit,
                            Credit = credit,
                            Balance = runningDebit - runningCredit
                        });
                    }

                    acc.TotalDebit = runningDebit;
                    acc.TotalCredit = runningCredit;
                    acc.Balance = runningDebit - runningCredit;
                }

                // 4️⃣ Adjust logic for "Simple" report type (skip per-account totals if needed)
                if (reportType?.Equals("Simple", StringComparison.OrdinalIgnoreCase) == true)
                {
                    // Flatten all transactions into one combined report
                    var flatList = new List<TransactionRow>();
                    foreach (var acc in accounts)
                    {
                        foreach (var t in acc.Transactions)
                        {
                            flatList.Add(new TransactionRow
                            {
                                Num = t.Num,
                                Type = t.Type,
                                Date = t.Date,
                                Description = $"{acc.Name} - {t.Description}",
                                Debit = t.Debit,
                                Credit = t.Credit,
                                Balance = t.Balance
                            });
                        }
                    }

                    return Ok(new
                    {
                        status = true,
                        data = flatList,
                        grandTotal = flatList.Sum(x => x.Balance)
                    });
                }

                // 5️⃣ Compute grand total for Default type
                decimal grandTotal = accounts.Sum(a => a.Balance);

                // 6️⃣ Return JSON result
                return Ok(new
                {
                    status = true,
                    data = accounts,
                    grandTotal
                });
            }
            catch (Exception ex)
            {
                // Return clean error info to client
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }


        #endregion

        #region General Ledger Details

        public IActionResult GeneralLedgerDetails()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetGeneralLedger(int id, DateTime? fromDate = null, DateTime? toDate = null, bool includeAllDates = false)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var parameters = new List<MySqlParameter>
        {
            new("@id", id)
        };

                string query = @"
            SELECT 
                DATE_FORMAT(t.date, '%M %d %Y') AS date,
                t.transaction_id AS Num,
                t.type AS Type,
                t.t_type,
                t.description,
                t.debit,
                t.credit
            FROM tbl_transaction t
            INNER JOIN tbl_coa_level_4 l4 ON t.account_id = l4.id
            WHERE t.account_id = @id
        ";

                // If date filters are applied
                if (!includeAllDates && fromDate.HasValue && toDate.HasValue)
                {
                    query += " AND t.date >= @fromDate AND t.date <= @toDate";
                    parameters.Add(new("@fromDate", fromDate.Value.Date));
                    parameters.Add(new("@toDate", toDate.Value.Date));
                }

                query += " ORDER BY t.date;";

                var transactions = new List<object>();
                decimal runningDebit = 0, runningCredit = 0;

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    await using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        decimal debit = reader["debit"] != DBNull.Value ? Convert.ToDecimal(reader["debit"]) : 0;
                        decimal credit = reader["credit"] != DBNull.Value ? Convert.ToDecimal(reader["credit"]) : 0;

                        runningDebit += debit;
                        runningCredit += credit;

                        var balance = runningDebit - runningCredit;

                        transactions.Add(new
                        {
                            Num = reader["Num"].ToString(),
                            Type = reader["Type"].ToString(),
                            Date = reader["date"].ToString(),
                            Description = reader["description"].ToString(),
                            Debit = debit.ToString("N2"),
                            Credit = credit.ToString("N2"),
                            Balance = balance.ToString("N2")
                        });
                    }
                }

                var totalRow = new
                {
                    Num = "",
                    Type = "",
                    Date = "",
                    Description = "TOTAL",
                    Debit = runningDebit.ToString("N2"),
                    Credit = runningCredit.ToString("N2"),
                    Balance = (runningDebit - runningCredit).ToString("N2")
                };
                transactions.Add(totalRow);


                return Ok(new { status = true, data = transactions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region VAT Report

        public IActionResult VATReport()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetVatReport(string type, DateTime? fromDate = null, DateTime? toDate = null, bool includeAllDates = false)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var parameters = new List<MySqlParameter>();

                string query = string.Empty;

                if (type == "Vat Input")
                {
                    if (includeAllDates)
                    {
                        query = @"
                    SELECT s.id, 'Purchase' AS Type, s.date AS Date, s.invoice_id AS `Inv No`, CONCAT(c.code,' - ',c.name) AS Name,
                           s.payment_method AS `Payment Method`, s.total AS `Amount Before Vat`, s.vat AS `Vat Amount`, s.net AS `Total Amount`
                    FROM tbl_purchase s
                    JOIN tbl_vendor c ON s.vendor_id = c.id
                    WHERE s.vat > 0
                    UNION ALL
                    SELECT s.id,'Sales Return', s.date, s.invoice_id, CONCAT(c.code,' - ',c.name),
                           s.payment_method, s.total, s.vat, s.net
                    FROM tbl_sales_return s
                    JOIN tbl_customer c ON s.customer_id = c.id
                    WHERE s.vat > 0
                    UNION ALL
                    SELECT s.id,'Debit Note', s.date, s.invoice_id, CONCAT(c.code,' - ',c.name),
                           '', s.amount, s.vat, s.total
                    FROM tbl_debit_note s
                    JOIN tbl_purchase p ON p.invoice_id = s.invoice_id
                    JOIN tbl_vendor c ON p.vendor_id = c.id
                    WHERE s.vat > 0
                    UNION ALL
                    SELECT s.id,'Petty Cash', s.date, s.code, CONCAT(c.code,' - ',c.name),
                           '', s.total_before_vat, s.total_vat, s.net_amount
                    FROM tbl_petty_cash_submition s
                    JOIN tbl_employee c ON s.name = c.id
                    WHERE s.total_vat > 0
                ";
                    }
                    else
                    {
                        query = @"
                    SELECT s.id, 'Purchase' AS Type, s.date AS Date, s.invoice_id AS `Inv No`, CONCAT(c.code,' - ',c.name) AS Name,
                           s.payment_method AS `Payment Method`, s.total AS `Amount Before Vat`, s.vat AS `Vat Amount`, s.net AS `Total Amount`
                    FROM tbl_purchase s
                    JOIN tbl_vendor c ON s.vendor_id = c.id
                    WHERE s.vat > 0 AND s.date BETWEEN @fromDate AND @toDate
                    UNION ALL
                    SELECT s.id,'Sales Return', s.date, s.invoice_id, CONCAT(c.code,' - ',c.name),
                           s.payment_method, s.total, s.vat, s.net
                    FROM tbl_sales_return s
                    JOIN tbl_customer c ON s.customer_id = c.id
                    WHERE s.vat > 0 AND s.date BETWEEN @fromDate AND @toDate
                    UNION ALL
                    SELECT s.id,'Debit Note', s.date, s.invoice_id, CONCAT(c.code,' - ',c.name),
                           '', s.amount, s.vat, s.total
                    FROM tbl_debit_note s
                    JOIN tbl_purchase p ON p.invoice_id = s.invoice_id
                    JOIN tbl_vendor c ON p.vendor_id = c.id
                    WHERE s.vat > 0 AND s.date BETWEEN @fromDate AND @toDate
                    UNION ALL
                    SELECT s.id,'Petty Cash', s.date, s.code, CONCAT(c.code,' - ',c.name),
                           '', s.total_before_vat, s.total_vat, s.net_amount
                    FROM tbl_petty_cash_submition s
                    JOIN tbl_employee c ON s.name = c.id
                    WHERE s.total_vat > 0 AND s.date BETWEEN @fromDate AND @toDate
                ";

                        parameters.Add(new MySqlParameter("@fromDate", fromDate?.Date));
                        parameters.Add(new MySqlParameter("@toDate", toDate?.Date));
                    }
                }
                else // Vat Output
                {
                    if (includeAllDates)
                    {
                        query = @"SELECT s.id,'Sales' type,s.date AS DATE, s.invoice_id AS 'Inv No', CONCAT(c.code,' - ',c.name) AS 'Name',
                                                    s.payment_method AS 'Payment Method',s.total AS 'Amount Before Vat', s.vat AS 'Vat Amount', s.net AS 'Total Amount'
                                                    FROM tbl_sales s, tbl_customer c WHERE s.customer_id = c.id AND s.vat>0
                                                    UNION ALL
                                                    SELECT s.id,'Purchase Return' type,s.date AS DATE, s.invoice_id AS 'Inv No', CONCAT(c.code,' - ',c.name) AS 'Name',
                                                    s.payment_method AS 'Payment Method',s.total AS 'Amount Before Vat', s.vat AS 'Vat Amount', s.net AS 'Total Amount'
                                                    FROM tbl_purchase_return s, tbl_vendor c WHERE s.vendor_id = c.id AND s.vat>0
                                                    UNION ALL
                                                    SELECT s.id,'Debit Note' TYPE,s.date AS DATE,s.invoice_id AS 'Inv No', CONCAT(c.code,' - ',c.name) AS 'Name',
                                                    '' AS 'Payment Method',s.amount AS 'Amount Before Vat', s.vat AS 'Vat Amount', s.total AS 'Total Amount' 
                                                    FROM tbl_credit_note s, tbl_customer c, tbl_sales p
                                                    WHERE p.invoice_id = s.invoice_id and p.customer_id = c.id AND s.vat>0  ";
                    }
                    else
                    {
                        query = @"SELECT s.id,'Sales' type,s.date AS DATE, s.invoice_id AS 'Inv No', CONCAT(c.code,' - ',c.name) AS 'Name',
                                                    s.payment_method AS 'Payment Method',s.total AS 'Amount Before Vat', s.vat AS 'Vat Amount', s.net AS 'Total Amount'
                                                    FROM tbl_sales s, tbl_customer c WHERE s.customer_id = c.id AND s.vat>0
                                                    UNION ALL
                                                    SELECT s.id,'Purchase Return' type,s.date AS DATE, s.invoice_id AS 'Inv No', CONCAT(c.code,' - ',c.name) AS 'Name',
                                                    s.payment_method AS 'Payment Method',s.total AS 'Amount Before Vat', s.vat AS 'Vat Amount', s.net AS 'Total Amount'
                                                    FROM tbl_purchase_return s, tbl_vendor c WHERE s.vendor_id = c.id AND s.vat>0
                                                    UNION ALL
                                                    SELECT s.id,'Credit Note' TYPE,s.date AS DATE,s.invoice_id AS 'Inv No', CONCAT(c.code,' - ',c.name) AS 'Name',
                                                    '' AS 'Payment Method',s.amount AS 'Amount Before Vat', s.vat AS 'Vat Amount', s.total AS 'Total Amount' 
                                                    FROM tbl_credit_note s, tbl_customer c, tbl_sales p
                                                    WHERE p.invoice_id = s.invoice_id and p.customer_id = c.id AND s.vat>0 AND s.DATE BETWEEN @fromDate AND @toDate";

                        parameters.Add(new MySqlParameter("@fromDate", fromDate?.Date));
                        parameters.Add(new MySqlParameter("@toDate", toDate?.Date));
                    }
                }

                var result = new List<object>();

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    if (parameters.Count > 0)
                        cmd.Parameters.AddRange(parameters.ToArray());

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        result.Add(new
                        {
                            Id = reader["id"],
                            //Type = reader["Type"] == DBNull.Value ? "" : reader["Type"].ToString(),
                            Type = reader["Type"],
                            Date = reader["Date"],
                            InvNo = reader["Inv No"],
                            Name = reader["Name"],
                            PaymentMethod = reader["Payment Method"],
                            AmountBeforeVat = reader["Amount Before Vat"],
                            VatAmount = reader["Vat Amount"],
                            TotalAmount = reader["Total Amount"]
                        });
                    }
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Cost Center Summary

        public IActionResult CostCenterSummary()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetCostCenterReport(DateTime? fromDate = null, DateTime? toDate = null, bool includeAllDates = false, int? costCenterId = null)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Prepare SQL query
                string query = @"
            SELECT 
                c.id, 
                c.name, 
                COUNT(t.id) AS Num, 
                SUM(t.debit) AS Total_Debit, 
                SUM(t.credit) AS Total_Credit
            FROM tbl_cost_center c 
            JOIN tbl_cost_center_transaction t ON c.id = t.cost_center_id
            WHERE c.id > 0";

                var parameters = new List<MySqlParameter>();

                // Date filter
                if (!includeAllDates && fromDate.HasValue && toDate.HasValue)
                {
                    query += " AND t.date >= @fromDate AND t.date <= @toDate";
                    parameters.Add(new MySqlParameter("@fromDate", fromDate.Value.Date));
                    parameters.Add(new MySqlParameter("@toDate", toDate.Value.Date));
                }

                // Cost Center filter
                if (costCenterId.HasValue && costCenterId > 0)
                {
                    query += " AND c.id = @cId";
                    parameters.Add(new MySqlParameter("@cId", costCenterId.Value));
                }

                query += " GROUP BY c.id, c.name ORDER BY c.id";

                var result = new List<object>();
                decimal totalBalance = 0;

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    if (parameters.Count > 0)
                        cmd.Parameters.AddRange(parameters.ToArray());

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        decimal debit = reader["Total_Debit"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Total_Debit"]);
                        decimal credit = reader["Total_Credit"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Total_Credit"]);
                        decimal balance = debit - credit;
                        totalBalance += balance;

                        result.Add(new
                        {
                            Id = reader["id"],
                            Name = reader["name"],
                            VoucherNo = reader["Num"],
                            Debit = debit.ToString("N2"),
                            Credit = credit.ToString("N2"),
                            Balance = balance.ToString("N2")
                        });
                    }
                }

                return Ok(new
                {
                    status = true,
                    totalBalance = totalBalance.ToString("N2"),
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        public IActionResult CostCenterDetails()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCostCenterTransactions(
    int? costCenterId = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    bool includeAllDates = false)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                if (!costCenterId.HasValue || costCenterId.Value <= 0)
                    return BadRequest(new { status = false, message = "Cost Center ID is required." });

                // Prepare SQL query
                string query = @"
            SELECT 
                t.id, 
                c.name, 
                DATE_FORMAT(t.date, '%M %d %Y') AS Date,
                t.type,
                t.debit, 
                t.credit, 
                t.ref_id AS VoucherNo, 
                t.description,
                0 AS Balance
            FROM tbl_cost_center_transaction t
            JOIN tbl_cost_center c ON c.id = t.cost_center_id
            WHERE c.id = @cId";

                var parameters = new List<MySqlParameter>
        {
            new MySqlParameter("@cId", costCenterId.Value)
        };

                if (!includeAllDates && fromDate.HasValue && toDate.HasValue)
                {
                    query += " AND t.date >= @fromDate AND t.date <= @toDate";
                    parameters.Add(new MySqlParameter("@fromDate", fromDate.Value.Date));
                    parameters.Add(new MySqlParameter("@toDate", toDate.Value.Date));
                }

                query += " ORDER BY t.date, t.id";

                var result = new List<object>();
                decimal runningBalance = 0;

                await using var cmd = new MySqlCommand(query, conn);
                if (parameters.Count > 0)
                    cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    decimal debit = reader["debit"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["debit"]);
                    decimal credit = reader["credit"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["credit"]);
                    runningBalance += (debit - credit);

                    result.Add(new
                    {
                        Id = reader["id"],
                        Date = reader["Date"],
                        Description = reader["name"].ToString(),
                        VoucherNo = reader["VoucherNo"].ToString(),
                        Debit = debit.ToString("N2"),
                        Credit = credit.ToString("N2"),
                        Balance = runningBalance.ToString("N2"),
                        Type = reader["type"]?.ToString(),
                        TransactionDescription = reader["description"]?.ToString()
                    });
                }

                return Ok(new
                {
                    status = true,
                    data = result,
                    totalBalance = runningBalance.ToString("N2")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Fixed Asset Summary

        public IActionResult FixedAssetSummary()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetFixedAssets(DateTime startDate, DateTime endDate, bool includeAllDates = false)
        {
            try
            {
                // Build connection string based on active database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Handle optional date filter
                string dateFilter = includeAllDates ? "" : "WHERE f.purchase_date BETWEEN @startDate AND @endDate";

                string query = $@"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY f.id) AS SN,
                f.id,
                f.name AS `Asset Name`,
                f.purchase_date AS `Date`,
                f.purchase_price AS `Total Price`,
                f.depreciation_life AS `Depreciation Life`
            FROM tbl_fixed_assets f
            {dateFilter};";

                var result = new List<Dictionary<string, object>>();

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    // Add parameters
                    cmd.Parameters.AddWithValue("@startDate", startDate.Date);
                    cmd.Parameters.AddWithValue("@endDate", endDate.Date.AddDays(1).AddSeconds(-1)); 

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>
                        {
                            ["SN"] = reader["SN"],
                            ["id"] = reader["id"],
                            ["AssetName"] = reader["Asset Name"],
                            ["Date"] = Convert.ToDateTime(reader["Date"]).ToString("MMM dd, yyyy"),
                            ["TotalPrice"] = Convert.ToDecimal(reader["Total Price"]).ToString("N2"),
                            ["DepreciationLife"] = reader["Depreciation Life"] 
                        };
                        result.Add(row);
                    }
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        public IActionResult FixedAssetDetails()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetFixedAssetDetails(string transactionId, DateTime startDate, DateTime endDate, bool includeAllDates = false)
        {
            try
            {
                // Build connection string dynamically based on active DB in session
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Handle optional date filter
                string dateFilter = includeAllDates ? "" : " AND t.date BETWEEN @startDate AND @endDate";

                string query = $@"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY t.date) AS SN,
                t.date,
                t.transaction_id,
                t.type AS `Type`,
                c.code AS `A/C CODE`,
                c.name AS `A/C NAME`,
                t.debit AS `DEBIT`,
                t.credit AS `CREDIT`
            FROM 
                tbl_transaction t
            INNER JOIN 
                tbl_coa_level_4 c ON t.account_id = c.id
            WHERE 
                t.transaction_id = @transaction_id
                AND t.type = 'Fixed Assets'
                {dateFilter};";

                var result = new List<Dictionary<string, object>>();
                decimal totalDebit = 0;
                decimal totalCredit = 0;

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@transaction_id", transactionId);
                    cmd.Parameters.AddWithValue("@startDate", startDate.Date);
                    cmd.Parameters.AddWithValue("@endDate", endDate.Date.AddDays(1).AddSeconds(-1));

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var debit = reader["DEBIT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DEBIT"]);
                        var credit = reader["CREDIT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CREDIT"]);

                        totalDebit += debit;
                        totalCredit += credit;

                        var row = new Dictionary<string, object>
                        {
                            ["SN"] = reader["SN"],
                            ["Date"] = Convert.ToDateTime(reader["date"]).ToString("MMM dd, yyyy"),
                            ["TransactionId"] = reader["transaction_id"].ToString(),
                            ["AccountCode"] = reader["A/C CODE"].ToString(),
                            ["AccountName"] = reader["A/C NAME"].ToString(),
                            ["Debit"] = debit.ToString("N2"),
                            ["Credit"] = credit.ToString("N2"),
                            ["Type"] = reader["Type"].ToString()
                        };

                        result.Add(row);
                    }
                }

                // Add total summary row at the end
                result.Add(new Dictionary<string, object>
                {
                    ["SN"] = "",
                    ["Date"] = "",
                    ["TransactionId"] = "",
                    ["AccountCode"] = "TOTAL",
                    ["AccountName"] = "",
                    ["Debit"] = totalDebit.ToString("N2"),
                    ["Credit"] = totalCredit.ToString("N2"),
                    ["Type"] = ""
                });

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Prepaid Expense Summary

        public IActionResult PrepaidExpenseSummary()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetPrepaidExpenses(DateTime startDate, DateTime endDate, bool includeAllDates = false)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string dateFilter = includeAllDates ? "" : "WHERE p.start_date BETWEEN @startDate AND @endDate";

                string query = $@"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY p.id) AS SN,
                p.id,
                p.name AS `Asset Name`,
                p.start_date AS `Start Date`,
                p.end_date AS `End Date`,
                p.total AS `Total Price`
            FROM tbl_prepaid_expense p
            {dateFilter};";

                var result = new List<Dictionary<string, object>>();

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    if (!includeAllDates)
                    {
                        cmd.Parameters.AddWithValue("@startDate", startDate.Date);
                        cmd.Parameters.AddWithValue("@endDate", endDate.Date.AddDays(1).AddSeconds(-1));
                    }

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>
                        {
                            ["SN"] = reader["SN"],
                            ["id"] = reader["id"],
                            ["AssetName"] = reader["Asset Name"]?.ToString(),
                            ["StartDate"] = reader["Start Date"] != DBNull.Value
                                ? Convert.ToDateTime(reader["Start Date"]).ToString("MMM dd, yyyy")
                                : "",
                            ["EndDate"] = reader["End Date"] != DBNull.Value
                                ? Convert.ToDateTime(reader["End Date"]).ToString("MMM dd, yyyy")
                                : "",
                            ["TotalPrice"] = reader["Total Price"] != DBNull.Value
                                ? Convert.ToDecimal(reader["Total Price"]).ToString("N2")
                                : "0.00"
                        };

                        result.Add(row);
                    }
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        public IActionResult PrepaidExpenseDetails()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPrepaidExpenseDetails(string transactionId, DateTime startDate, DateTime endDate, bool includeAllDates = false)
        {
            try
            {
                // Build connection string dynamically based on active database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Handle optional date filtering
                string dateFilter = includeAllDates ? "" : " AND t.date BETWEEN @startDate AND @endDate";

                string query = $@"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY t.date) AS SN,
                t.date,
                t.transaction_id,
                t.type AS `Type`,
                c.code AS `A/C CODE`,
                c.name AS `A/C NAME`,
                t.debit AS `DEBIT`,
                t.credit AS `CREDIT`
            FROM 
                tbl_transaction t
            INNER JOIN 
                tbl_coa_level_4 c ON t.account_id = c.id
            WHERE 
                t.transaction_id = @transaction_id
                AND t.type = 'Prepaid Expense'
                {dateFilter};";

                var result = new List<Dictionary<string, object>>();
                decimal totalDebit = 0;
                decimal totalCredit = 0;

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    // Add parameters
                    cmd.Parameters.AddWithValue("@transaction_id", transactionId);
                    cmd.Parameters.AddWithValue("@startDate", startDate.Date);
                    cmd.Parameters.AddWithValue("@endDate", endDate.Date.AddDays(1).AddSeconds(-1));

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var debit = reader["DEBIT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["DEBIT"]);
                        var credit = reader["CREDIT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CREDIT"]);

                        totalDebit += debit;
                        totalCredit += credit;

                        var row = new Dictionary<string, object>
                        {
                            ["SN"] = reader["SN"],
                            ["Date"] = Convert.ToDateTime(reader["date"]).ToString("MMM dd, yyyy"),
                            ["TransactionId"] = reader["transaction_id"].ToString(),
                            ["AccountCode"] = reader["A/C CODE"].ToString(),
                            ["AccountName"] = reader["A/C NAME"].ToString(),
                            ["Debit"] = debit.ToString("N2"),
                            ["Credit"] = credit.ToString("N2"),
                            ["Type"] = reader["Type"].ToString()
                        };

                        result.Add(row);
                    }
                }

                // ✅ Add total summary row at the bottom
                result.Add(new Dictionary<string, object>
                {
                    ["SN"] = "",
                    ["Date"] = "",
                    ["TransactionId"] = "",
                    ["AccountCode"] = "TOTAL",
                    ["AccountName"] = "",
                    ["Debit"] = totalDebit.ToString("N2"),
                    ["Credit"] = totalCredit.ToString("N2"),
                    ["Type"] = ""
                });

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region VAT Category Report

        public IActionResult MasterVAT()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetMasterVATReport(string reportType)
        {
            try
            {
                // Build connection string based on active session database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = string.Empty;

                // Handle all report types (matches your cmbState options)
                switch (reportType.Trim())
                {
                    case "All":
                        query = @"
                    SELECT 
                        'Purchase' AS `Type`,
                        CONCAT(t.name,' ',t.value,' %') AS `Tax Name`,
                        t.description AS `Tax Description`,
                        IFNULL(SUM(p.total),0) AS `Amount`,
                        IFNULL(SUM(p.vat),0) AS `VAT Amount`
                    FROM tbl_purchase p
                    JOIN tbl_purchase_details s ON p.id = s.purchase_id
                    JOIN tbl_items i ON i.id = s.item_id
                    JOIN tbl_tax t ON t.id = s.vat
                    GROUP BY t.name,t.value, t.description

                    UNION ALL

                    SELECT 
                        'Purchase Return',
                        CONCAT(t.name,' ',t.value,' %'),
                        t.description,
                        IFNULL(SUM(p.total),0),
                        IFNULL(SUM(p.vat),0)
                    FROM tbl_purchase_return p
                    JOIN tbl_purchase_return_details s ON p.id = s.purchase_id
                    JOIN tbl_items i ON i.id = s.item_id
                    JOIN tbl_tax t ON t.id = s.vat
                    GROUP BY t.name,t.value, t.description

                    UNION ALL

                    SELECT 
                        'Sales',
                        CONCAT(t.name,' ',t.value,' %'),
                        t.description,
                        IFNULL(SUM(p.total),0),
                        IFNULL(SUM(p.vat),0)
                    FROM tbl_sales p
                    JOIN tbl_sales_details s ON p.id = s.sales_id
                    JOIN tbl_items i ON i.id = s.item_id
                    JOIN tbl_tax t ON t.id = s.vat
                    GROUP BY t.name,t.value, t.description

                    UNION ALL

                    SELECT 
                        'Sales Return',
                        CONCAT(t.name,' ',t.value,' %'),
                        t.description,
                        IFNULL(SUM(p.total),0),
                        IFNULL(SUM(p.vat),0)
                    FROM tbl_sales_return p
                    JOIN tbl_sales_return_details s ON p.id = s.sales_id
                    JOIN tbl_items i ON i.id = s.item_id
                    JOIN tbl_tax t ON t.id = s.vat
                    GROUP BY t.name,t.value, t.description

                    UNION ALL

                    SELECT 
                        'Debit Note',
                        'VAT 5 %',
                        '',
                        IFNULL(SUM(s.amount),0),
                        IFNULL(SUM(s.vat),0)
                    FROM tbl_debit_note_details s

                    UNION ALL

                    SELECT 
                        'Credit Note',
                        'VAT 5 %',
                        '',
                        IFNULL(SUM(s.amount),0),
                        IFNULL(SUM(s.vat),0)
                    FROM tbl_credit_note_details s

                    UNION ALL

                    SELECT 
                        'Petty Cash',
                        'VAT 5 %',
                        '',
                        SUM(total_before_vat),
                        SUM(total_vat)
                    FROM tbl_petty_cash_submition s;
                ";
                        break;

                    case "Sales":
                        query = @"
                    SELECT 
                        'Sales' AS `Type`,
                        CONCAT(t.name,' ',t.value,' %') AS `Tax Name`,
                        t.description AS `Tax Description`,
                        IFNULL(SUM(p.total),0) AS `Amount`,
                        IFNULL(SUM(p.vat),0) AS `VAT Amount`
                    FROM tbl_sales p
                    JOIN tbl_sales_details s ON p.id = s.sales_id
                    JOIN tbl_items i ON i.id = s.item_id
                    JOIN tbl_tax t ON t.id = s.vat
                    GROUP BY t.name,t.value, t.description;";
                        break;

                    case "Sales Return":
                        query = @"
                    SELECT 
                        'Sales Return' AS `Type`,
                        CONCAT(t.name,' ',t.value,' %') AS `Tax Name`,
                        t.description AS `Tax Description`,
                        IFNULL(SUM(p.total),0) AS `Amount`,
                        IFNULL(SUM(p.vat),0) AS `VAT Amount`
                    FROM tbl_sales_return p
                    JOIN tbl_sales_return_details s ON p.id = s.sales_id
                    JOIN tbl_items i ON i.id = s.item_id
                    JOIN tbl_tax t ON t.id = s.vat
                    GROUP BY t.name,t.value, t.description;";
                        break;

                    case "Purchase":
                        query = @"
                    SELECT 
                        'Purchase' AS `Type`,
                        CONCAT(t.name,' ',t.value,' %') AS `Tax Name`,
                        t.description AS `Tax Description`,
                        IFNULL(SUM(p.total),0) AS `Amount`,
                        IFNULL(SUM(p.vat),0) AS `VAT Amount`
                    FROM tbl_purchase p
                    JOIN tbl_purchase_details s ON p.id = s.purchase_id
                    JOIN tbl_items i ON i.id = s.item_id
                    JOIN tbl_tax t ON t.id = s.vat
                    GROUP BY t.name,t.value, t.description;";
                        break;

                    case "Purchase Return":
                        query = @"
                    SELECT 
                        'Purchase Return' AS `Type`,
                        CONCAT(t.name,' ',t.value,' %') AS `Tax Name`,
                        t.description AS `Tax Description`,
                        IFNULL(SUM(p.total),0) AS `Amount`,
                        IFNULL(SUM(p.vat),0) AS `VAT Amount`
                    FROM tbl_purchase_return p
                    JOIN tbl_purchase_return_details s ON p.id = s.purchase_id
                    JOIN tbl_items i ON i.id = s.item_id
                    JOIN tbl_tax t ON t.id = s.vat
                    GROUP BY t.name,t.value, t.description;";
                        break;

                    case "Debit Note":
                        query = @"
                    SELECT 
                        'Debit Note' AS `Type`,
                        'VAT 5 %' AS `Tax Name`,
                        '' AS `Tax Description`,
                        IFNULL(SUM(s.amount),0) AS `Amount`,
                        IFNULL(SUM(s.vat),0) AS `VAT Amount`
                    FROM tbl_debit_note_details s;";
                        break;

                    case "Credit Note":
                        query = @"
                    SELECT 
                        'Credit Note' AS `Type`,
                        'VAT 5 %' AS `Tax Name`,
                        '' AS `Tax Description`,
                        IFNULL(SUM(s.amount),0) AS `Amount`,
                        IFNULL(SUM(s.vat),0) AS `VAT Amount`
                    FROM tbl_credit_note_details s;";
                        break;

                    case "Petty Cash":
                        query = @"
                    SELECT 
                        'Petty Cash' AS `Type`,
                        'VAT 5 %' AS `Tax Name`,
                        '' AS `Tax Description`,
                        SUM(total_before_vat) AS `Amount`,
                        SUM(total_vat) AS `VAT Amount`
                    FROM tbl_petty_cash_submition s;";
                        break;

                    default:
                        return BadRequest(new { status = false, message = "Invalid report type." });
                }

                // Execute and build result
                var result = new List<Dictionary<string, object>>();
                decimal totalAmount = 0, totalVat = 0;

                await using (var cmd = new MySqlCommand(query, conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        decimal amount = reader["Amount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Amount"]);
                        decimal vatAmount = reader["VAT Amount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["VAT Amount"]);

                        totalAmount += amount;
                        totalVat += vatAmount;

                        var row = new Dictionary<string, object>
                        {
                            ["Type"] = reader["Type"].ToString(),
                            ["TaxName"] = reader["Tax Name"].ToString(),
                            ["TaxDescription"] = reader["Tax Description"].ToString(),
                            ["Amount"] = amount.ToString("N2"),
                            ["VatAmount"] = vatAmount.ToString("N2")
                        };
                        result.Add(row);
                    }
                }

                // Add total row
                result.Add(new Dictionary<string, object>
                {
                    ["Type"] = "",
                    ["TaxName"] = "",
                    ["TaxDescription"] = "TOTAL",
                    ["Amount"] = totalAmount.ToString("N2"),
                    ["VatAmount"] = totalVat.ToString("N2")
                });

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Petty Cash Summary

        public IActionResult PettyCashBalanceSummary()
        {
            return View();
        }

        //Already I have this API
        [HttpGet]
        public async Task<IActionResult> GetPettyCashReport(bool enableApproval, int id, DateTime startDate, DateTime endDate)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query;
                List<MySqlParameter> parameters = new List<MySqlParameter>();

                if (!enableApproval)
                {
                    // Query for transactions without approval
                    query = @"
                SELECT 
                    DATE_FORMAT(t.date, '%Y-%m-%d') AS `Date`,
                    t.transaction_id,
                    CASE 
                        WHEN t.type = 'Petty Cash Request' THEN 
                            (SELECT CONCAT('Petty Cash Approval - REF - ', pr.request_ref)
                             FROM tbl_petty_cash_request pr 
                             WHERE pr.id = t.transaction_id
                             LIMIT 1)
                        WHEN t.type = 'Petty Cash Submission' THEN 'Petty Cash Submission NO.'
                        WHEN t.type = 'Employee Petty Cash Payment' THEN 'Employee Petty Cash Payment'
                        ELSE ''
                    END AS `Num`,
                    t.type AS `Type`,
                    coa.name AS `Account`,
                    (t.credit - t.debit) AS `Amount`,
                    SUM(t.credit - t.debit) OVER (PARTITION BY t.hum_id ORDER BY t.date, t.id) AS `Balance`
                FROM tbl_transaction t
                JOIN tbl_coa_level_4 coa ON t.account_id = coa.id
                WHERE t.hum_id = @id
                  AND t.type IN ('Petty Cash Request', 'Petty Cash Submission', 'Employee Petty Cash Payment')
                  AND t.date >= @startDate AND t.date <= @endDate
                ORDER BY t.date, t.id;
            ";

                    parameters.Add(new MySqlParameter("@id", id));
                    parameters.Add(new MySqlParameter("@startDate", startDate));
                    parameters.Add(new MySqlParameter("@endDate", endDate));
                }
                else
                {
                    // Query for transactions with approval
                    query = @"
                SELECT 
                    p.id,
                    p.voucher_date AS `Date`,
                    'Petty Cash Request' AS `Type`,
                    CONCAT('Petty Cash Approval - REF - ', p.id) AS `Num`,
                    e.name AS `Account`,
                    pd.amount AS `Amount`,
                    '' AS `Balance`
                FROM tbl_petty_cash p
                INNER JOIN tbl_employee e ON p.employee_id = e.id
                INNER JOIN tbl_petty_cash_details pd ON pd.petty_cash_id = p.id
                INNER JOIN tbl_petty_cash_card pcc ON CAST(pcc.name AS UNSIGNED) = e.id
                WHERE p.voucher_date >= @startDate AND p.voucher_date <= @endDate
                ORDER BY p.voucher_date, p.id;
            ";

                    parameters.Add(new MySqlParameter("@startDate", startDate));
                    parameters.Add(new MySqlParameter("@endDate", endDate));
                }

                var result = new List<Dictionary<string, object>>();
                decimal totalAmount = 0, totalBalance = 0;

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());

                    await using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader["Date"] == DBNull.Value)
                                continue;

                            decimal amount = reader["Amount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Amount"]);
                            decimal balance = reader["Balance"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Balance"]);

                            totalAmount += amount;
                            totalBalance += balance;

                            var row = new Dictionary<string, object>
                            {
                                ["TransactionId"] = reader["transaction_id"]?.ToString() ?? reader["id"]?.ToString(),
                                ["Date"] = Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                                ["Type"] = reader["Type"].ToString(),
                                ["Num"] = reader["Num"].ToString(),
                                ["Account"] = reader["Account"].ToString(),
                                ["Amount"] = amount.ToString("N2"),
                                ["Balance"] = balance != 0 ? balance.ToString("N2") + " ◀" : ""
                            };

                            result.Add(row);
                        }
                    }
                }

                // Add total row
                result.Add(new Dictionary<string, object>
                {
                    ["TransactionId"] = "",
                    ["Date"] = "",
                    ["Type"] = "",
                    ["Num"] = "",
                    ["Account"] = "TOTAL",
                    ["Amount"] = totalAmount.ToString("N2"),
                    ["Balance"] = totalBalance != 0 ? totalBalance.ToString("N2") : ""
                });

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Item Moving Report
        
        public IActionResult ItemMovingReport()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetItemMovingReport(DateTime? startDate = null, DateTime? endDate = null, bool isDateChecked = true)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY i.id) AS `SN`,
                i.code,
                i.name,
                i.barcode,
                i.cost_price AS CostPrice,
                i.sales_price AS SalesPrice,
                SUM(CASE WHEN t.type = 'Opening Qty' THEN t.qty_in ELSE 0 END) AS OpeningQty,
                SUM(CASE WHEN t.type = 'Purchase Invoice' THEN t.qty_in ELSE 0 END) AS Purchase,
                SUM(CASE WHEN t.type = 'Sales Invoice' THEN t.qty_out ELSE 0 END) AS Sales,
                SUM(CASE WHEN t.type = 'Purchase Return Invoice' THEN t.qty_out ELSE 0 END) AS PurchaseReturn,
                SUM(CASE WHEN t.type = 'Sales Return Invoice' THEN t.qty_in ELSE 0 END) AS SalesReturn,
                SUM(CASE WHEN t.type = 'Damage' THEN t.qty_out ELSE 0 END) AS Damage,
                SUM(t.qty_in - t.qty_out) AS BalanceQty
            FROM tbl_items i
            INNER JOIN tbl_item_transaction t ON i.id = t.item_id
            WHERE i.type = '11 - Inventory Part' AND i.active = 0
        ";

                var parameters = new List<MySqlParameter>();

                if (isDateChecked && startDate.HasValue && endDate.HasValue)
                {
                    query += " AND t.date >= @dateFrom AND t.date <= @dateTo ";
                    parameters.Add(new MySqlParameter("@dateFrom", startDate.Value.Date));
                    parameters.Add(new MySqlParameter("@dateTo", endDate.Value.Date));
                }

                query += @"
            GROUP BY i.id, i.code, i.name, i.barcode, i.cost_price, i.sales_price
            ORDER BY i.id;
        ";

                var invoices = new List<InvoiceDto>();

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    await using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        invoices.Add(new InvoiceDto
                        {
                            SN = reader.GetInt32("SN"),
                            Code = reader["code"].ToString(),
                            Name = reader["name"].ToString(),
                            Barcode = reader["barcode"].ToString(),//   <td>${row.barcode ?? ''}</td>
                            CostPrice = reader.GetDecimal("CostPrice"),
                            SalesPrice = reader.GetDecimal("SalesPrice"),
                            OpeningQty = reader.GetDecimal("OpeningQty"),
                            Purchase = reader.GetDecimal("Purchase"),
                            Sales = reader.GetDecimal("Sales"),
                            PurchaseReturn = reader.GetDecimal("PurchaseReturn"),
                            SalesReturn = reader.GetDecimal("SalesReturn"),
                            Damage = reader.GetDecimal("Damage"),
                            BalanceQty = reader.GetDecimal("BalanceQty")
                        });
                    }
                }

                return Ok(new { status = true, data = invoices });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Item Wise Profit Statement

        public IActionResult ItemWiseProfitStatement()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetItemWiseProfit(DateTime? startDate = null, DateTime? endDate = null, bool isDateChecked = true)
        {
            try
            {
                // Build connection string dynamically using session or default database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Base query
                string query = @"
            SELECT 
                ts.date AS `Date`,
                ts.invoice_id AS `InvNo`,
                CONCAT(tc.code,' - ',tc.name) AS `CustomerName`,
                CONCAT(ti.code,' - ',ti.name) AS `ItemName`,
                tsd.cost_price AS `CostPrice`, 
                tsd.price AS `SalesPrice`,
                tsd.qty AS `Qty`,
                tsd.cost_price * tsd.qty AS `CostAmount`,
                tsd.price * tsd.qty AS `SalesAmount`,
                (tsd.price - tsd.cost_price) * tsd.qty AS `Profit`
            FROM tbl_sales_details tsd
            INNER JOIN tbl_items ti ON tsd.item_id = ti.id
            INNER JOIN tbl_sales ts ON tsd.sales_id = ts.id
            INNER JOIN tbl_customer tc ON ts.customer_id = tc.id
            WHERE 1=1
        ";

                var parameters = new List<MySqlParameter>();

                // Apply date filter if checkbox is unchecked
                if (isDateChecked && startDate.HasValue && endDate.HasValue)
                {
                    query += " AND ts.date >= @dateFrom AND ts.date <= @dateTo ";
                    parameters.Add(new MySqlParameter("@dateFrom", startDate.Value.Date));
                    parameters.Add(new MySqlParameter("@dateTo", endDate.Value.Date));
                }

                var result = new List<ItemMovingDto>();

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    if (parameters.Any())
                        cmd.Parameters.AddRange(parameters.ToArray());

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        result.Add(new ItemMovingDto
                        {
                            Date = reader.GetDateTime("Date"),
                            InvNo = reader["InvNo"].ToString(),
                            CustomerName = reader["CustomerName"].ToString(),
                            ItemName = reader["ItemName"].ToString(),
                            CostPrice = reader.GetDecimal("CostPrice"),
                            SalesPrice = reader.GetDecimal("SalesPrice"),
                            Qty = reader.GetDecimal("Qty"),
                            CostAmount = reader.GetDecimal("CostAmount"),
                            SalesAmount = reader.GetDecimal("SalesAmount"),
                            Profit = reader.GetDecimal("Profit")
                        });
                    }
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }



        #endregion

        #region Warehouse Summary

        public IActionResult WareHouseSummary()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetWarehouseItems(int? warehouseId = null)
        {
            try
            {
                // Build connection string dynamically using session or default database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Base query
                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY wt.id DESC) AS SN,
                wt.id AS Id,
                w.Name AS WarehouseName,
                i.id AS ItemId,
                CONCAT(i.code,' - ',i.name) AS ItemName,
                wt.qty AS Qty
            FROM tbl_items_warehouse wt
            INNER JOIN tbl_warehouse w ON wt.warehouse_id = w.id
            INNER JOIN tbl_items i ON wt.item_id = i.id AND i.state = 0
            WHERE 1=1
        ";

                var parameters = new List<MySqlParameter>();

                if (warehouseId.HasValue)
                {
                    query += " AND wt.warehouse_id = @warehouseId";
                    parameters.Add(new MySqlParameter("@warehouseId", warehouseId.Value));
                }

                query += " ORDER BY wt.id DESC";

                var result = new List<WarehouseItemDto>();

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    if (parameters.Any())
                        cmd.Parameters.AddRange(parameters.ToArray());

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        result.Add(new WarehouseItemDto
                        {
                            SN = reader.GetInt32("SN"),
                            Id = reader.GetInt32("Id"),
                            WarehouseName = reader["WarehouseName"].ToString(),
                            ItemId = reader.GetInt32("ItemId"),
                            ItemName = reader["ItemName"].ToString(),
                            Qty = reader.GetDecimal("Qty")
                        });
                    }
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        public IActionResult WareHouseHistory()
        {
            return View();
        }

        #endregion

        #region Inventory Transaction Report

        public IActionResult InventoryTransaction()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoices(
    int? itemId = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null,
    bool includeAllItems = true,
    bool includeAllDates = true)
        {
            try
            {
                // Build connection string dynamically using session or fallback to default
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY it.date) AS SN,
                DATE_FORMAT(it.date, '%d/%m/%Y') AS `Date`,
                it.type AS Type,
                CONCAT('000', it.reference) AS `RefId`,
                i.id AS Id,
                i.name AS Name,
                i.code AS Code,
                it.item_id AS ItemId,
                i.cost_price AS CostPrice,
                i.sales_price AS SellingPrice,
                it.qty_in AS QtyIn,
                it.qty_out AS QtyOut,
                it.description AS Description
            FROM 
                tbl_item_transaction it
            INNER JOIN 
                tbl_items i ON it.item_id = i.id
            WHERE 
                i.state = 0
                AND (@itemId IS NULL OR i.id = @itemId)
                AND (@dateFilter = 0 OR (it.date BETWEEN @dateFrom AND @dateTo))
            ORDER BY 
                it.date;
        ";

                // Prepare parameters
                var parameters = new List<MySqlParameter>
        {
            new("@itemId", includeAllItems ? DBNull.Value : (object?)itemId ?? DBNull.Value),
            new("@dateFrom", dateFrom ?? DateTime.MinValue),
            new("@dateTo", dateTo ?? DateTime.MaxValue),
            new("@dateFilter", includeAllDates ? 0 : 1)
        };

                var result = new List<InvoiceViewModels>();

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        result.Add(new InvoiceViewModels
                        {
                            SN = reader.GetInt32("SN"),
                            Id = reader.GetInt32("Id"),
                            Date = reader["Date"].ToString(),
                            Type = reader["Type"].ToString(),
                            RefId = reader["RefId"].ToString(),
                            Name = reader["Name"].ToString(),
                            Code = reader["Code"].ToString(),
                            ItemId = reader.GetInt32("ItemId"),
                            CostPrice = reader.GetDecimal("CostPrice"),
                            SellingPrice = reader.GetDecimal("SellingPrice"),
                            QtyIn = reader.GetDecimal("QtyIn"),
                            QtyOut = reader.GetDecimal("QtyOut"),
                            Description = reader["Description"].ToString()
                        });
                    }
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region City List

        public IActionResult CityList()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCitys()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
               *
            FROM tbl_city
            ORDER BY id;";

                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var projectList = new List<object>();
                int sn = 1;
                while (await reader.ReadAsync())
                {
                    projectList.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        Name = reader["name"].ToString(),
                        Country_Id = reader.GetInt32("country_id"),
                    });
                }

                return Ok(new { status = true, data = projectList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Cheque Report

        public IActionResult ChequeReport()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetChequeNumbers()
        {
            try
            {
                // Build connection string dynamically using session or fallback
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT DISTINCT check_no 
            FROM tbl_check_details 
            WHERE check_no > 0;";

                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var chequeList = new List<object>();
                int sn = 1;
                while (await reader.ReadAsync())
                {
                    chequeList.Add(new
                    {
                        SN = sn++,
                        CheckNo = reader["check_no"].ToString()
                    });
                }

                return Ok(new { status = true, data = chequeList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetChequeDetails(int? chequeNo)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT
                cd.pvc_no AS id,
                cd.check_type AS type,
                cd.date,
                coa.name AS account_name,
                cd.amount AS amount,
                b.name AS bank_name
            FROM tbl_check_details cd
            JOIN tbl_bank_card bc ON cd.check_id = bc.id
            JOIN tbl_bank b ON bc.bank_id = b.id
            JOIN tbl_coa_level_4 coa ON coa.id = bc.account_id
        ";

                if (chequeNo.HasValue)
                {
                    query += " WHERE cd.check_no = @id";
                }

                await using var cmd = new MySqlCommand(query, conn);
                if (chequeNo.HasValue)
                {
                    cmd.Parameters.AddWithValue("@id", chequeNo.Value);
                }

                await using var reader = await cmd.ExecuteReaderAsync();

                var resultList = new List<object>();
                int rowIndex = 1;

                while (await reader.ReadAsync())
                {
                    resultList.Add(new
                    {
                        SN = rowIndex++,
                        Id = reader["id"].ToString(),
                        Type = reader["type"].ToString(),
                        Date = Convert.ToDateTime(reader["date"]).ToShortDateString(),
                        AccountName = reader["account_name"].ToString(),
                       Amount = Convert.ToDecimal(reader["amount"]).ToString("N2"),
                        BankName = reader["bank_name"].ToString()
                    });
                }

                return Ok(new { status = true, data = resultList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }



        #endregion

        #region Bank Balance Summary

        public IActionResult BankBalanceSummary()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetBankBalances()
        {
            try
            {
                // Build connection string based on active database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                b.name AS Bank,
                bc.account_name AS Account,
                SUM(t.debit - t.credit) AS Balance
            FROM tbl_transaction t
            JOIN tbl_bank_card bc ON t.account_id = bc.account_id
            JOIN tbl_bank b ON bc.bank_id = b.id
            WHERE t.`type` IN ('PDC Payable', 'PDC Receivable')
            GROUP BY b.name, bc.account_name;
        ";

                var result = new List<Dictionary<string, object>>();
                decimal totalBalance = 0;
                int rowId = 1;

                await using (var cmd = new MySqlCommand(query, conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        string bankName = reader["Bank"].ToString();
                        string accountName = reader["Account"].ToString();
                        decimal balance = reader["Balance"] != DBNull.Value ? Convert.ToDecimal(reader["Balance"]) : 0;

                        result.Add(new Dictionary<string, object>
                        {
                            ["SN"] = rowId++,
                            ["Bank"] = bankName,
                            ["Account"] = accountName,
                            ["Balance"] = balance.ToString("N2")
                        });

                        totalBalance += balance;
                    }
                }

                // Add total row at the end
                result.Add(new Dictionary<string, object>
                {
                    ["SN"] = "",
                    ["Bank"] = "",
                    ["Account"] = "TOTAL",
                    ["Balance"] = totalBalance.ToString("N2")
                });

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region PDC Outstanding Summary
        //It returns only future-dated checks

        public IActionResult PDCOutstandingSummary()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetHoldChecks(DateTime? startDate = null, DateTime? endDate = null, bool includeAllDates = false)
        {
            try
            {
                // Build connection string dynamically
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Base query
                string query = @"
            SELECT 
                cd.id,
                cd.check_no,
                cd.check_date,
                cd.amount,
                cd.check_type,
                cd.state
            FROM tbl_check_details cd
            WHERE cd.state = 'Hold'
        ";

                // ✅ Apply date filter only when includeAllDates is false
                if (!includeAllDates && startDate.HasValue && endDate.HasValue)
                {
                    query += " AND cd.check_date BETWEEN @startDate AND @endDate";
                }
                else if (!includeAllDates && startDate.HasValue)
                {
                    query += " AND cd.check_date >= @startDate";
                }
                else if (!includeAllDates && endDate.HasValue)
                {
                    query += " AND cd.check_date <= @endDate";
                }

                query += " ORDER BY cd.check_date ASC;";

                var result = new List<Dictionary<string, object>>();
                decimal totalAmount = 0;
                int rowId = 1;

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    if (!includeAllDates)
                    {
                        if (startDate.HasValue)
                            cmd.Parameters.AddWithValue("@startDate", startDate.Value);

                        if (endDate.HasValue)
                            cmd.Parameters.AddWithValue("@endDate", endDate.Value);
                    }

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string checkNo = reader["check_no"].ToString();
                        string checkDate = Convert.ToDateTime(reader["check_date"]).ToString("dd/MM/yyyy");
                        decimal amount = reader["amount"] != DBNull.Value ? Convert.ToDecimal(reader["amount"]) : 0;
                        string checkType = reader["check_type"].ToString();
                        string state = reader["state"].ToString();

                        result.Add(new Dictionary<string, object>
                        {
                            ["SN"] = rowId++,
                            ["CheckNo"] = checkNo,
                            ["CheckDate"] = checkDate,
                            ["Amount"] = amount.ToString("N2"),
                            ["CheckType"] = checkType,
                            ["State"] = state
                        });

                        totalAmount += amount;
                    }
                }

                // ✅ Add total row
                result.Add(new Dictionary<string, object>
                {
                    ["SN"] = "",
                    ["CheckNo"] = "",
                    ["CheckDate"] = "TOTAL",
                    ["Amount"] = totalAmount.ToString("N2"),
                    ["CheckType"] = "",
                    ["State"] = ""
                });

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region PDC Cleared History
        //It returns only Cleared(Pass Status) Cheque

        public IActionResult PDCClearedHistory()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPassChecks(DateTime? startDate = null, DateTime? endDate = null, bool includeAllDates = false)
        {
            try
            {
                // 🔹 Build connection string dynamically
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Base query — only "Pass" state
                string query = @"
            SELECT 
                cd.id,
                cd.check_no,
                cd.check_date,
                cd.pass_date,
                cd.amount
            FROM tbl_check_details cd
            WHERE cd.state = 'Pass'
        ";

                // 🔹 Apply optional date filters
                if (!includeAllDates && startDate.HasValue && endDate.HasValue)
                {
                    query += " AND cd.pass_date BETWEEN @startDate AND @endDate";
                }
                else if (!includeAllDates && startDate.HasValue)
                {
                    query += " AND cd.pass_date >= @startDate";
                }
                else if (!includeAllDates && endDate.HasValue)
                {
                    query += " AND cd.pass_date <= @endDate";
                }

                query += " ORDER BY cd.pass_date ASC;";

                var result = new List<Dictionary<string, object>>();
                decimal totalAmount = 0;
                int rowId = 1;

                // 🔹 Execute query
                await using (var cmd = new MySqlCommand(query, conn))
                {
                    if (!includeAllDates)
                    {
                        if (startDate.HasValue)
                            cmd.Parameters.AddWithValue("@startDate", startDate.Value);

                        if (endDate.HasValue)
                            cmd.Parameters.AddWithValue("@endDate", endDate.Value);
                    }

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string checkNo = reader["check_no"]?.ToString();
                        string checkDate = reader["check_date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["check_date"]).ToString("dd/MM/yyyy")
                            : "";
                        string passDate = reader["pass_date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["pass_date"]).ToString("dd/MM/yyyy")
                            : "";
                        decimal amount = reader["amount"] != DBNull.Value
                            ? Convert.ToDecimal(reader["amount"])
                            : 0;

                        result.Add(new Dictionary<string, object>
                        {
                            ["SN"] = rowId++,
                            ["CheckNo"] = checkNo,
                            ["CheckDate"] = checkDate,
                            ["PassDate"] = passDate,
                            ["Amount"] = amount.ToString("N2")
                        });

                        totalAmount += amount;
                    }
                }

                // 🔹 Add total row
                result.Add(new Dictionary<string, object>
                {
                    ["SN"] = "",
                    ["CheckNo"] = "",
                    ["CheckDate"] = "TOTAL",
                    ["PassDate"] = "",
                    ["Amount"] = totalAmount.ToString("N2")
                });

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region PDC Returned Cheque Report
        //It returns only Returned Cheque

        public IActionResult ReturnedChequeReport()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetReturnChecks(DateTime? startDate = null, DateTime? endDate = null, bool includeAllDates = false)
        {
            try
            {
                // 🔹 Build connection string dynamically
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Base query — only "Return" state
                string query = @"
            SELECT 
                cd.id,
                cd.check_no,
                cd.check_date,
                cd.return_date,
                cd.amount
            FROM tbl_check_details cd
            WHERE cd.state = 'Return'
        ";

                // 🔹 Apply optional date filters
                if (!includeAllDates && startDate.HasValue && endDate.HasValue)
                {
                    query += " AND cd.return_date BETWEEN @startDate AND @endDate";
                }
                else if (!includeAllDates && startDate.HasValue)
                {
                    query += " AND cd.return_date >= @startDate";
                }
                else if (!includeAllDates && endDate.HasValue)
                {
                    query += " AND cd.return_date <= @endDate";
                }

                query += " ORDER BY cd.return_date ASC;";

                var result = new List<Dictionary<string, object>>();
                decimal totalAmount = 0;
                int rowId = 1;

                // 🔹 Execute query
                await using (var cmd = new MySqlCommand(query, conn))
                {
                    if (!includeAllDates)
                    {
                        if (startDate.HasValue)
                            cmd.Parameters.AddWithValue("@startDate", startDate.Value);
                        if (endDate.HasValue)
                            cmd.Parameters.AddWithValue("@endDate", endDate.Value);
                    }

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string checkNo = reader["check_no"]?.ToString();
                        string checkDate = reader["check_date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["check_date"]).ToString("dd/MM/yyyy")
                            : "";
                        string returnDate = reader["return_date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["return_date"]).ToString("dd/MM/yyyy")
                            : "";
                        decimal amount = reader["amount"] != DBNull.Value
                            ? Convert.ToDecimal(reader["amount"])
                            : 0;

                        result.Add(new Dictionary<string, object>
                        {
                            ["SN"] = rowId++,
                            ["CheckNo"] = checkNo,
                            ["CheckDate"] = checkDate,
                            ["ReturnDate"] = returnDate,
                            ["Amount"] = amount.ToString("N2")
                        });

                        totalAmount += amount;
                    }
                }

                // 🔹 Add total row
                result.Add(new Dictionary<string, object>
                {
                    ["SN"] = "",
                    ["CheckNo"] = "",
                    ["CheckDate"] = "",
                    ["ReturnDate"] = "TOTAL",
                    ["Amount"] = totalAmount.ToString("N2")
                });

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Cheque Linked to Payment/Receipt

        public IActionResult ChequeLinked()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllChecks(DateTime? startDate = null, DateTime? endDate = null, bool includeAllDates = false)
        {
            try
            {
                // 🔹 Build connection string dynamically
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Base query
                string query = @"
            SELECT 
                IFNULL(cd.check_no, '') AS check_no,
                cd.check_date,
                cd.amount,
                cd.check_type,
                IFNULL(pv.code, '') AS PaymentVoucher,
                IFNULL(rv.code, '') AS ReceiptVoucher
            FROM tbl_check_details cd
            LEFT JOIN tbl_payment_voucher pv ON cd.pvc_no = pv.id AND cd.check_type = 'Payment'
            LEFT JOIN tbl_receipt_voucher rv ON cd.pvc_no = rv.id AND cd.check_type = 'Receipt'
            WHERE 1=1
        ";

                // 🔹 Optional date filters on check_date
                if (!includeAllDates && startDate.HasValue && endDate.HasValue)
                {
                    query += " AND cd.check_date BETWEEN @startDate AND @endDate";
                }
                else if (!includeAllDates && startDate.HasValue)
                {
                    query += " AND cd.check_date >= @startDate";
                }
                else if (!includeAllDates && endDate.HasValue)
                {
                    query += " AND cd.check_date <= @endDate";
                }

                query += " ORDER BY cd.check_date ASC;";

                var result = new List<Dictionary<string, object>>();
                decimal totalAmount = 0;
                int rowId = 1;

                // 🔹 Execute query
                await using (var cmd = new MySqlCommand(query, conn))
                {
                    if (!includeAllDates)
                    {
                        if (startDate.HasValue)
                            cmd.Parameters.AddWithValue("@startDate", startDate.Value);
                        if (endDate.HasValue)
                            cmd.Parameters.AddWithValue("@endDate", endDate.Value);
                    }

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        string checkNo = reader["check_no"]?.ToString();
                        string checkDate = reader["check_date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["check_date"]).ToString("dd/MM/yyyy")
                            : "";
                        decimal amount = reader["amount"] != DBNull.Value
                            ? Convert.ToDecimal(reader["amount"])
                            : 0;
                        string checkType = reader["check_type"]?.ToString() ?? "";
                        string paymentVoucher = reader["PaymentVoucher"]?.ToString() ?? "";
                        string receiptVoucher = reader["ReceiptVoucher"]?.ToString() ?? "";

                        result.Add(new Dictionary<string, object>
                        {
                            ["SN"] = rowId++,
                            ["CheckNo"] = checkNo,
                            ["CheckDate"] = checkDate,
                            ["Amount"] = amount.ToString("N2"),
                            ["CheckType"] = checkType,
                            ["PaymentVoucher"] = paymentVoucher,
                            ["ReceiptVoucher"] = receiptVoucher
                        });

                        totalAmount += amount;
                    }
                }

                // 🔹 Add total row
                result.Add(new Dictionary<string, object>
                {
                    ["SN"] = "",
                    ["CheckNo"] = "",
                    ["CheckDate"] = "TOTAL",
                    ["Amount"] = totalAmount.ToString("N2"),
                    ["CheckType"] = "",
                    ["PaymentVoucher"] = "",
                    ["ReceiptVoucher"] = ""
                });

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion



    }

}


