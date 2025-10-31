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

    }
}


