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




    }
}


