using Microsoft.CodeAnalysis.Elfie.Diagnostics;

namespace YamyProject.Controllers
{
    [Route("Lists/[action]")]
    public class ListsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly MySqlConnection _connection;
        public ListsController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _connection = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        }

        #region Item Category
        public IActionResult ItemCategory()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetItemCategory()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                // ✅ read from session
                var databaseName = HttpContext.Session.GetString("DatabaseName");

                if (string.IsNullOrEmpty(databaseName))
                {
                    return Json(new { status = false, message = "No database selected. Please login again." });
                }

                // ✅ pass database name in query string
                var response = await client.GetAsync($"api/Lists/GetCategories?databaseName={databaseName}");

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { status = false, message = "Failed to fetch categories" });
                }

                var data = await response.Content.ReadAsStringAsync();
                var categories = JsonConvert.DeserializeObject<List<ItemCatoryViewModel>>(data);

                return Json(new { status = true, data = categories });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> AddCategory(string categoryName)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var requestObj = new { CategoryName = categoryName };
                var json = JsonConvert.SerializeObject(requestObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"api/Lists/AddCategory?databaseName={database}", content);

                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> EditCategory(int id, string categoryName)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var requestObj = new { Id = id, CategoryName = categoryName };
                var json = JsonConvert.SerializeObject(requestObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Call API (PUT method)
                var response = await client.PutAsync($"api/Lists/EditCategory?databaseName={database}", content);

                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var response = await client.DeleteAsync($"api/Lists/DeleteCategory/{id}?databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Tax

        public IActionResult Tax()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetTaxes()
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var response = await client.GetAsync($"api/Lists/GetTaxes?databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddTax(string name, decimal value, string description)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var requestObj = new { Name = name, Value = value, Description = description };
                var json = JsonConvert.SerializeObject(requestObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"api/Lists/AddTax?databaseName={database}", content);

                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> EditTax(int id, string name, decimal value, string description)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var requestObj = new { Id = id, Name = name, Value = value, Description = description };
                var json = JsonConvert.SerializeObject(requestObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"api/Lists/EditTax?databaseName={database}", content);

                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteTax(int id)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var content = new StringContent(JsonConvert.SerializeObject(id), Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"api/Lists/DeleteTax/{id}?databaseName={database}", content);
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDeletedTaxes()
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var response = await client.GetAsync($"api/Lists/GetDeletedTaxes?databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> RestoreTax(int id)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var response = await client.PostAsync($"api/Lists/RestoreTax/{id}?databaseName={database}", null);
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Unit 

        public IActionResult Unit()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetUnit()
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.GetAsync($"api/Lists/GetUnit?databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddUnit(string name)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var payload = new { Name = name };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload),
                                                System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"api/Lists/AddUnit?databaseName={database}", content);
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditUnit(int id, string name)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var payload = new { Id = id, Name = name };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload),
                                                System.Text.Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"api/Lists/EditUnit?databaseName={database}", content);
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.DeleteAsync($"api/Lists/DeleteUnit?id={id}&databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region ChartOfAccount
        public IActionResult ChartOfAccount()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> AddLevel4Account([FromBody] CoaLevel4Request model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (model.Date.HasValue && model.Date.Value.Date < DateTime.Today)
                return BadRequest(new { status = false, message = "Date cannot be earlier than today." });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter Level 4 account name first." });

            if (model.Level3Id <= 0)
                return BadRequest(new { status = false, message = "Please select Level 3 first." });

            try
            {
                // ✅ 1️⃣ Build connection string dynamically
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ 2️⃣ Check for duplicate account name
                string checkDuplicate = "SELECT id FROM tbl_coa_level_4 WHERE name = @name";
                using (var checkCmd = new MySqlCommand(checkDuplicate, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    var exists = await checkCmd.ExecuteScalarAsync();
                    if (exists != null)
                        return BadRequest(new { status = false, message = "Account already exists. Enter another name." });
                }

                // ✅ 3️⃣ Get parent (Level 3) code
                string level3Code;
                using (var codeCmd = new MySqlCommand("SELECT code FROM tbl_coa_level_3 WHERE id = @id", conn))
                {
                    codeCmd.Parameters.AddWithValue("@id", model.Level3Id);
                    var result = await codeCmd.ExecuteScalarAsync();
                    if (result == null)
                        return BadRequest(new { status = false, message = "Invalid Level 3 selection." });
                    level3Code = result.ToString();
                }

                // ✅ 4️⃣ Find next code
                string newCode;
                using (var maxCmd = new MySqlCommand("SELECT MAX(code) FROM tbl_coa_level_4 WHERE code LIKE @prefix", conn))
                {
                    maxCmd.Parameters.AddWithValue("@prefix", $"{level3Code}%");
                    var maxCodeObj = await maxCmd.ExecuteScalarAsync();

                    if (maxCodeObj != DBNull.Value && maxCodeObj != null && !string.IsNullOrEmpty(maxCodeObj.ToString()))
                        newCode = (int.Parse(maxCodeObj.ToString()) + 1).ToString();
                    else
                        newCode = $"{level3Code}001";
                }

                // ✅ 5️⃣ Default debit/credit
                decimal debit = model.Debit ?? 0;
                decimal credit = model.Credit ?? 0;

                // ✅ 6️⃣ Insert into Level 4 table
                int newId;
                string insertQuery = @"
            INSERT INTO tbl_coa_level_4 (name, code, main_id, debit, credit, date)
            VALUES (@name, @code, @main_id, @debit, @credit, @date);
            SELECT LAST_INSERT_ID();";

                using (var insertCmd = new MySqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    insertCmd.Parameters.AddWithValue("@code", newCode);
                    insertCmd.Parameters.AddWithValue("@main_id", model.Level3Id);
                    insertCmd.Parameters.AddWithValue("@debit", debit);
                    insertCmd.Parameters.AddWithValue("@credit", credit);
                    insertCmd.Parameters.AddWithValue("@date", model.Date ?? DateTime.Now);
                    newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                }

                // ✅ 7️⃣ Insert opening balance transactions if any
                if (debit > 0 || credit > 0)
                {
                    int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    if (userId <= 0)
                        return Unauthorized(new { status = false, message = "User not logged in" });

                    await InsertTransactionsAsync(conn, newId, newCode, debit, credit, model.Date ?? DateTime.Now, userId);
                }

                // ✅ 8️⃣ Return success
                return Ok(new
                {
                    status = true,
                    message = "Level 4 account added successfully.",
                    id = newId,
                    code = newCode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        private async Task InsertTransactionsAsync(MySqlConnection conn, int refId, string code, decimal debit, decimal credit, DateTime date, int userId)
        {
            string accountId = refId.ToString();

            // ✅ Find "Opening Balance Equity"
            string openingBalanceEquityId = "";
            using (var getEquityCmd = new MySqlCommand("SELECT id FROM tbl_coa_level_4 WHERE name = 'Opening Balance Equity'", conn))
            {
                var result = await getEquityCmd.ExecuteScalarAsync();
                if (result == null)
                    throw new Exception("Cannot make opening balance without 'Opening Balance Equity' account.");
                openingBalanceEquityId = result.ToString();
            }

            // ✅ Delete existing opening entries for that account
            string deleteQuery = "DELETE FROM tbl_transaction WHERE transaction_id = @refId AND t_type = 'GENERAL LEDGER OPENING BALANCE'";
            using (var deleteCmd = new MySqlCommand(deleteQuery, conn))
            {
                deleteCmd.Parameters.AddWithValue("@refId", refId);
                await deleteCmd.ExecuteNonQueryAsync();
            }

            // ✅ Credit entries
            if (credit > 0)
            {
                await AddTransactionAsync(conn, date, openingBalanceEquityId, credit, 0,
                    refId.ToString(), "0", "GENERAL LEDGER OPENING BALANCE", "General Ledger Opening Balance",
                    "Opening Balance Equity - Ledger", userId, DateTime.Now.Date, "");

                await AddTransactionAsync(conn, date, accountId, 0, credit,
                    refId.ToString(), "0", "GENERAL LEDGER OPENING BALANCE", "General Ledger Opening Balance",
                    "Account Payable - Ledger Code", userId, DateTime.Now.Date, "");
            }

            // ✅ Debit entries
            if (debit > 0)
            {
                await AddTransactionAsync(conn, date, openingBalanceEquityId, 0, debit,
                    refId.ToString(), "0", "GENERAL LEDGER OPENING BALANCE", "General Ledger Opening Balance",
                    "Opening Balance Equity - Ledger Code", userId, DateTime.Now.Date, "");

                await AddTransactionAsync(conn, date, accountId, debit, 0,
                    refId.ToString(), "0", "GENERAL LEDGER OPENING BALANCE", "General Ledger Opening Balance",
                    "Account Payable - Ledger Code", userId, DateTime.Now.Date, "");
            }
        }


        private async Task AddTransactionAsync(MySqlConnection conn, DateTime date, string accountId, decimal debit, decimal credit,
                               string transactionId, string humId, string type, string voucher_name, string description,
                               int createdBy, DateTime createdDate, string VoucherNo)
        {
            try
            {
                string query = @"
INSERT INTO tbl_transaction 
(date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no) 
VALUES (@date, @accountId, @debit, @credit, @transactionId, @hum_id, @tType, @type, @description, @createdBy, @createdDate, 0, @voucher_no);";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@date", date);
                cmd.Parameters.AddWithValue("@accountId", accountId);
                cmd.Parameters.AddWithValue("@debit", debit);
                cmd.Parameters.AddWithValue("@credit", credit);
                cmd.Parameters.AddWithValue("@transactionId", transactionId);
                cmd.Parameters.AddWithValue("@hum_id", humId);
                cmd.Parameters.AddWithValue("@tType", type);
                cmd.Parameters.AddWithValue("@type", voucher_name);
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@createdBy", createdBy);
                cmd.Parameters.AddWithValue("@createdDate", createdDate);
                cmd.Parameters.AddWithValue("@voucher_no", VoucherNo);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error while inserting transaction: " + ex.Message, ex);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCOA()
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                // call your API
                var response = await client.GetAsync($"api/Lists/GetCOAHierarchy?databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLevel3AccountsByLevel2(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid Level 2 ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                // Build connection string using user-specific database (if any)
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Fetch Level 3 accounts under the given Level 2
                string query = @"
            SELECT 
                id, 
                name, 
                main_id AS Level2Id
            FROM tbl_coa_level_3
            WHERE main_id = @id
            ORDER BY name ASC";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        id = reader["id"],
                        name = reader["name"],
                        level2Id = reader["Level2Id"]
                    });
                }

                return Ok(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> EditCoaLevel4([FromBody] CoaLevel4Request model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { status = false, message = "Invalid account request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Account name is required" });

            if (model.Date.HasValue && model.Date.Value.Date < DateTime.Today)
                return BadRequest(new { status = false, message = "Date cannot be earlier than today" });


            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if account exists
                string checkQuery = "SELECT COUNT(*) FROM tbl_coa_level_4 WHERE id = @id";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", model.Id);
                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    if (!exists)
                        return NotFound(new { status = false, message = "Account not found" });
                }

                // Update the account with the values provided in model
                string updateQuery = @"
            UPDATE tbl_coa_level_4
            SET name = @name,
                debit = @debit,
                credit = @credit,
                date = @date
            WHERE id = @id";

                using var updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                updateCmd.Parameters.AddWithValue("@debit", model.Debit ?? (object)DBNull.Value);
                updateCmd.Parameters.AddWithValue("@credit", model.Credit ?? (object)DBNull.Value);
                updateCmd.Parameters.AddWithValue("@date", model.Date ?? (object)DBNull.Value);
                updateCmd.Parameters.AddWithValue("@id", model.Id);

                int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                    return NotFound(new { status = false, message = "No changes applied or account not found" });

                // Return the **updated values from the model**, not old DB values
                var result = new
                {
                    Id = model.Id,
                    Name = model.Name,
                    Debit = model.Debit ?? 0,
                    Credit = model.Credit ?? 0,
                    Date = model.Date
                };

                return Ok(new { status = true, data = result, message = "Account updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCoaLevel4(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid account id" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if account is used in transactions (excluding 'General Ledger Opening Balance')
                string checkQuery = @"SELECT COUNT(1) 
                              FROM tbl_transaction 
                              WHERE type != 'General Ledger Opening Balance' 
                                AND account_id = @id";

                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var countObj = await checkCmd.ExecuteScalarAsync();
                    int recordCount = 0;
                    if (countObj != null && countObj != DBNull.Value)
                        recordCount = Convert.ToInt32(countObj);

                    if (recordCount > 0)
                        return BadRequest(new { status = false, message = "Account is already used in transactions" });
                }

                // Delete account and any 'General Ledger Opening Balance' transaction
                string deleteQuery = @"
            DELETE FROM tbl_coa_level_4 WHERE id = @id;
            DELETE FROM tbl_transaction WHERE type = 'General Ledger Opening Balance' AND transaction_id = @id;
        ";

                using var deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@id", id);
                await deleteCmd.ExecuteNonQueryAsync();

                // Optional: trigger an event if needed
                // EventHub.Refreshlvl4Account(); // you may need to adapt this for API/web

                return Ok(new { status = true, message = "Account deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region ChartOfAccount L1

        public IActionResult ChartOfAccountL1()
        {
            return View();
        }

        public IActionResult GetAccountTypes()
        {
            try
            {
                var list = (from AccountType type in Enum.GetValues(typeof(AccountType))
                            select new
                            {
                                Id = (int)type,
                                Name = type.ToString()
                            }).ToList();

                return Json(list);
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }   

        }
        [HttpGet]
        public async Task<IActionResult> GetLevel1Accounts()
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                id,
                name,
                category_code
            FROM tbl_coa_level_1
            ORDER BY id ASC;
        ";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();
                int sn = 1;

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        sn = sn++,
                        id = reader.GetInt32("id"),
                        name = reader["name"].ToString(),
                        categoryCode = reader["category_code"].ToString()
                    });
                }

                return Ok(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveLevel1Account([FromBody] Level1AccountRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter Level 1 name first" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                // 🔹 Build connection string for correct DB
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Check for duplicate name
                string checkQuery = "SELECT id FROM tbl_coa_level_1 WHERE name=@name LIMIT 1";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name);
                    var existingIdObj = await checkCmd.ExecuteScalarAsync();

                    if (existingIdObj != null)
                    {
                        int existingId = Convert.ToInt32(existingIdObj);
                        if (model.Id == 0 || model.Id != existingId)
                            return BadRequest(new { status = false, message = "Name already exists. Enter another name." });
                    }
                }

                // 🔹 New record
                if (model.Id == 0)
                {
                    int newCode = 1;
                    string maxQuery = "SELECT MAX(code) AS code FROM tbl_coa_level_1";
                    using (var cmd = new MySqlCommand(maxQuery, conn))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != DBNull.Value && result != null)
                            newCode = Convert.ToInt32(result) + 1;
                    }

                    string insertQuery = @"INSERT INTO tbl_coa_level_1 (name, code, category_code)
                                   VALUES (@name, @code, @categoryCode)";
                    using (var insertCmd = new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@name", model.Name);
                        insertCmd.Parameters.AddWithValue("@code", newCode);
                        insertCmd.Parameters.AddWithValue("@categoryCode", model.CategoryCode ?? "");
                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new { status = true, message = "Level 1 Account added successfully" });
                }
                else
                {
                    // 🔹 Update existing
                    string updateQuery = @"UPDATE tbl_coa_level_1
                                   SET name=@name, category_code=@categoryCode
                                   WHERE id=@id";
                    using (var updateCmd = new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@id", model.Id);
                        updateCmd.Parameters.AddWithValue("@name", model.Name);
                        updateCmd.Parameters.AddWithValue("@categoryCode", model.CategoryCode ?? "");
                        int affected = await updateCmd.ExecuteNonQueryAsync();

                        if (affected == 0)
                            return NotFound(new { status = false, message = "Level 1 Account not found" });
                    }

                    return Ok(new { status = true, message = "Level 1 Account updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteLevel1Account(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid Level 1 ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                // 🔹 Build connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Check if Level 1 is used in Level 2
                string checkQuery = "SELECT COUNT(*) FROM tbl_coa_level_2 WHERE main_id=@id";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                var countObj = await checkCmd.ExecuteScalarAsync();
                int count = Convert.ToInt32(countObj);

                if (count > 0)
                    return BadRequest(new { status = false, message = "This Level 1 account is used in Level 2. Cannot delete." });

                // 🔹 Delete Level 1
                string deleteQuery = "DELETE FROM tbl_coa_level_1 WHERE id=@id";
                using var deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@id", id);
                int affected = await deleteCmd.ExecuteNonQueryAsync();

                if (affected == 0)
                    return NotFound(new { status = false, message = "Level 1 account not found" });

                return Ok(new { status = true, message = "Level 1 account deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region ChartOfAccount L2

        public IActionResult ChartOfAccountL2()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetLevel2Accounts()
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                l2.id, 
                l2.name, 
                l2.main_id, 
                l1.name AS level1Name
            FROM tbl_coa_level_2 l2
            INNER JOIN tbl_coa_level_1 l1 ON l2.main_id = l1.id
            ORDER BY l2.id ASC;
        ";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();
                int sn = 1;

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        sn = sn++,
                        id = reader.GetInt32("id"),
                        name = reader["name"].ToString(),
                        level1Id = reader.GetInt32("main_id"),
                        level1Name = reader["level1Name"].ToString()
                    });
                }

                return Ok(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> SaveLevel2Account([FromBody] Level2AccountRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter Level 2 name first" });

            if (model.Level1Id <= 0)
                return BadRequest(new { status = false, message = "Please select Level 1 account first" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Check duplicate name
                string checkQuery = "SELECT id FROM tbl_coa_level_2 WHERE name=@name LIMIT 1";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name);
                    var existingIdObj = await checkCmd.ExecuteScalarAsync();

                    if (existingIdObj != null)
                    {
                        int existingId = Convert.ToInt32(existingIdObj);
                        if (model.Id == 0 || model.Id != existingId)
                            return BadRequest(new { status = false, message = "Name already exists. Enter another name." });
                    }
                }

                if (model.Id != 0)
                {
                    // 🔹 Update existing Level 2
                    string updateQuery = "UPDATE tbl_coa_level_2 SET name=@name WHERE id=@id";
                    using var updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);
                    updateCmd.Parameters.AddWithValue("@name", model.Name);

                    int affected = await updateCmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Level 2 Account not found" });

                    // Optionally: LogAudit(userId, "Update Level 2 Account", model.Id, model.Name);

                    return Ok(new { status = true, message = "Level 2 Account updated successfully" });
                }
                else
                {
                    // 🔹 Insert new Level 2
                    string level1CodeQuery = "SELECT code FROM tbl_coa_level_1 WHERE id=@id LIMIT 1";
                    string level1Code = "";
                    using (var cmd = new MySqlCommand(level1CodeQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", model.Level1Id);
                        var result = await cmd.ExecuteScalarAsync();
                        level1Code = result?.ToString() ?? "";
                    }

                    // Get max Level 2 code for this Level 1
                    string maxCodeQuery = "SELECT MAX(code) AS code FROM tbl_coa_level_2 WHERE code LIKE @codePattern";
                    string newCode = level1Code + "1";
                    using (var cmd = new MySqlCommand(maxCodeQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@codePattern", level1Code + "%");
                        var maxCodeObj = await cmd.ExecuteScalarAsync();
                        if (maxCodeObj != null && maxCodeObj != DBNull.Value)
                        {
                            int nextNum = int.Parse(maxCodeObj.ToString()) + 1;
                            newCode = nextNum.ToString();
                        }
                    }

                    string insertQuery = "INSERT INTO tbl_coa_level_2 (name, code, main_id) VALUES (@name, @code, @mainId)";
                    using var insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@name", model.Name);
                    insertCmd.Parameters.AddWithValue("@code", newCode);
                    insertCmd.Parameters.AddWithValue("@mainId", model.Level1Id);

                    int newId = await insertCmd.ExecuteNonQueryAsync();

                    // Optionally: LogAudit(userId, "Add Level 2 Account", newId, model.Name);

                    return Ok(new { status = true, message = "Level 2 Account added successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteLevel2Account(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid Level 2 ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if Level 2 is used in Level 3
                string checkQuery = "SELECT COUNT(*) FROM tbl_coa_level_3 WHERE main_id=@id";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                var countObj = await checkCmd.ExecuteScalarAsync();
                int count = Convert.ToInt32(countObj);

                if (count > 0)
                    return BadRequest(new { status = false, message = "This Level 2 account is used in Level 3. Cannot delete." });

                // Delete Level 2
                string deleteQuery = "DELETE FROM tbl_coa_level_2 WHERE id=@id";
                using var deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@id", id);
                int affected = await deleteCmd.ExecuteNonQueryAsync();

                if (affected == 0)
                    return NotFound(new { status = false, message = "Level 2 account not found" });

                return Ok(new { status = true, message = "Level 2 account deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region ChartOfAccount L3

        public IActionResult ChartOfAccountL3()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetLevel3ById(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid Level 3 ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                l3.id,
                l3.name,
                l3.main_id AS lvl2Id,
                l2.main_id AS lvl1Id
            FROM tbl_coa_level_3 l3
            INNER JOIN tbl_coa_level_2 l2 ON l3.main_id = l2.id
            WHERE l3.id = @id
            LIMIT 1
        ";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var data = new
                    {
                        id = reader.GetInt32("id"),
                        name = reader["name"].ToString(),
                        lvl2Id = reader.GetInt32("lvl2Id"),
                        lvl1Id = reader.GetInt32("lvl1Id")
                    };

                    return Ok(new { status = true, data });
                }
                else
                {
                    return NotFound(new { status = false, message = "Level 3 Account not found" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLevel2AccountsByLevel1(int level1Id)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"SELECT id, name FROM tbl_coa_level_2 WHERE main_id=@level1Id ORDER BY id ASC";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@level1Id", level1Id);

                using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<object>();
                while (await reader.ReadAsync())
                {
                    list.Add(new { id = reader.GetInt32("id"), name = reader["name"].ToString() });
                }

                return Ok(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetLevel3Accounts()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                l3.id, l3.name AS level3Name,
                l2.id AS level2Id, l2.name AS level2Name,
                l1.id AS level1Id, l1.name AS level1Name
            FROM tbl_coa_level_3 l3
            INNER JOIN tbl_coa_level_2 l2 ON l3.main_id = l2.id
            INNER JOIN tbl_coa_level_1 l1 ON l2.main_id = l1.id
            ORDER BY l3.id ASC;";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();
                int sn = 1;
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        sn = sn++,
                        id = reader.GetInt32("id"),
                        level3Name = reader["level3Name"].ToString(),
                        level2Id = reader.GetInt32("level2Id"),
                        level2Name = reader["level2Name"].ToString(),
                        level1Id = reader.GetInt32("level1Id"),
                        level1Name = reader["level1Name"].ToString()
                    });
                }

                return Ok(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveLevel3Account([FromBody] Level3AccountRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter Level 3 name first" });

            if (model.Level2Id <= 0)
                return BadRequest(new { status = false, message = "Please select Level 2 account first" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Check for duplicate Level 3 name
                string checkQuery = "SELECT id FROM tbl_coa_level_3 WHERE name=@name LIMIT 1";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@name", model.Name);
                var existingIdObj = await checkCmd.ExecuteScalarAsync();
                if (existingIdObj != null)
                {
                    int existingId = Convert.ToInt32(existingIdObj);
                    if (model.Id == 0 || model.Id != existingId)
                        return BadRequest(new { status = false, message = "Name already exists. Enter another name." });
                }

                if (model.Id != 0)
                {
                    // 🔹 Update existing Level 3
                    string updateQuery = "UPDATE tbl_coa_level_3 SET name=@name WHERE id=@id";
                    using var updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);
                    updateCmd.Parameters.AddWithValue("@name", model.Name);

                    int affected = await updateCmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Level 3 Account not found" });

                    return Ok(new { status = true, message = "Level 3 Account updated successfully" });
                }
                else
                {
                    // 🔹 Get parent Level 2 code
                    string level2CodeQuery = "SELECT code FROM tbl_coa_level_2 WHERE id=@id LIMIT 1";
                    string level2Code = "";
                    using (var cmd = new MySqlCommand(level2CodeQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", model.Level2Id);
                        var result = await cmd.ExecuteScalarAsync();
                        level2Code = result?.ToString() ?? "";
                    }

                    // 🔹 Get max Level 3 code for this Level 2
                    string maxCodeQuery = $"SELECT MAX(code) AS code FROM tbl_coa_level_3 WHERE code LIKE @codePattern";
                    string newCode = level2Code + "01";
                    using (var cmd = new MySqlCommand(maxCodeQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@codePattern", level2Code + "%");
                        var maxCodeObj = await cmd.ExecuteScalarAsync();
                        if (maxCodeObj != null && maxCodeObj != DBNull.Value)
                        {
                            int nextNum = int.Parse(maxCodeObj.ToString()) + 1;
                            newCode = nextNum.ToString();
                        }
                    }

                    // 🔹 Insert Level 3
                    string insertQuery = "INSERT INTO tbl_coa_level_3 (name, code, main_id) VALUES (@name, @code, @mainId)";
                    using var insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@name", model.Name);
                    insertCmd.Parameters.AddWithValue("@code", newCode);
                    insertCmd.Parameters.AddWithValue("@mainId", model.Level2Id);

                    int newId = await insertCmd.ExecuteNonQueryAsync();


                    return Ok(new { status = true, message = "Level 3 Account added successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteLevel3Account(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid Level 3 ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if Level 3 is used in Level 4
                string checkQuery = "SELECT COUNT(*) FROM tbl_coa_level_4 WHERE main_id=@id";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                var countObj = await checkCmd.ExecuteScalarAsync();
                int count = Convert.ToInt32(countObj);

                if (count > 0)
                    return BadRequest(new { status = false, message = "This Level 3 account is used in Level 4. Cannot delete." });

                // Delete Level 3
                string deleteQuery = "DELETE FROM tbl_coa_level_3 WHERE id=@id";
                using var deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@id", id);
                int affected = await deleteCmd.ExecuteNonQueryAsync();

                if (affected == 0)
                    return NotFound(new { status = false, message = "Level 3 account not found" });

                return Ok(new { status = true, message = "Level 3 account deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }



        #endregion

        #region Fixed Asset Item List

        public IActionResult FixedAssetItem()
        {
            return View();
        }

        // Fetch data from API
        [HttpGet]
        public async Task<IActionResult> GetFixedAssetItem(DateTime? dateFrom, DateTime? dateTo, bool ignoreDate = false)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                string url = $"api/Lists/fixed-assets?databaseName={database}&ignoreDate={ignoreDate}";
                if (!ignoreDate && dateFrom.HasValue && dateTo.HasValue)
                {
                    url += $"&dateFrom={dateFrom:yyyy-MM-dd}&dateTo={dateTo:yyyy-MM-dd}";
                }

                var response = await client.GetAsync(url);
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Cost Center

        public IActionResult CostCenter()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCostCenters()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var costCenters = new List<CostCenterRequest>();

                // Get main cost centers
                var mainQuery = "SELECT id, code, name FROM tbl_cost_center ORDER BY code";
                using (var cmd = new MySqlCommand(mainQuery, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        costCenters.Add(new CostCenterRequest
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Code = reader["code"].ToString(),
                            Name = reader["name"].ToString(),
                            IsMain = true,
                            IsSub = false,
                            //MainId = null
                        });
                    }
                }

                // Get sub cost centers
                var subQuery = "SELECT id, code, name, main_id FROM tbl_sub_cost_center ORDER BY code";
                using (var cmd = new MySqlCommand(subQuery, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        costCenters.Add(new CostCenterRequest
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Code = reader["code"].ToString(),
                            Name = reader["name"].ToString(),
                            IsMain = false,
                            IsSub = true,
                            MainId = Convert.ToInt32(reader["main_id"])
                        });
                    }
                }

                return Ok(costCenters);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMainCostCenters()
        {
            try
            {
                // 🧠 Build dynamic connection string (based on active DB)
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var mainCostCenters = new List<object>();

                // ✅ Only main cost centers (type = 'main')
                var query = "SELECT id, code, name FROM tbl_cost_center ORDER BY code";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        mainCostCenters.Add(new
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Code = reader["code"].ToString(),
                            Name = reader["name"].ToString()
                        });
                    }
                }

                return Ok(new { status = true, data = mainCostCenters });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveCostCenter([FromBody] CostCenterRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter Account Name first." });

            if (!model.IsMain && (model.MainId <= 0))
                return BadRequest(new { status = false, message = "Please select a Main Cost Center for Sub Account." });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check for duplicate name excluding current Id if updating
                string checkQuery = model.IsMain
                    ? "SELECT id FROM tbl_cost_center WHERE name=@name AND id!=@id LIMIT 1"
                    : "SELECT id FROM tbl_sub_cost_center WHERE name=@name AND id!=@id LIMIT 1";

                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name);
                    checkCmd.Parameters.AddWithValue("@id", model.Id);
                    var existing = await checkCmd.ExecuteScalarAsync();
                    if (existing != null)
                        return BadRequest(new { status = false, message = "Account Name already exists." });
                }

                if (model.IsMain)
                {
                    if (model.Id > 0)
                    {
                        // 🔹 UPDATE Main Cost Center
                        string updateQuery = "UPDATE tbl_cost_center SET name=@name, project_id=@project_id WHERE id=@id";
                        using var cmd = new MySqlCommand(updateQuery, conn);
                        cmd.Parameters.AddWithValue("@name", model.Name);
                        int projectIdValue = model.ProjectId ?? 0;
                        cmd.Parameters.AddWithValue("@project_id", projectIdValue);
                        cmd.Parameters.AddWithValue("@id", model.Id);
                        await cmd.ExecuteNonQueryAsync();

                        return Ok(new { status = true, message = "Main Cost Center updated successfully" });
                    }
                    else
                    {
                        // 🔹 INSERT Main Cost Center
                        int newMainCode = 101;
                        string getMaxMainCode = "SELECT MAX(code) FROM tbl_cost_center";
                        using (var cmd = new MySqlCommand(getMaxMainCode, conn))
                        {
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != DBNull.Value && result != null)
                                newMainCode = Convert.ToInt32(result) + 1;
                        }

                        string insertQuery = "INSERT INTO tbl_cost_center (name, code, project_id) VALUES (@name,@code,@project_id)";
                        using var cmdInsert = new MySqlCommand(insertQuery, conn);
                        cmdInsert.Parameters.AddWithValue("@name", model.Name);
                        cmdInsert.Parameters.AddWithValue("@code", newMainCode);
                        int projectIdValue = model.ProjectId ?? 0;
                        cmdInsert.Parameters.AddWithValue("@project_id", projectIdValue);
                        await cmdInsert.ExecuteNonQueryAsync();

                        return Ok(new { status = true, message = "Main Cost Center added successfully", code = newMainCode });
                    }
                }
                else
                {
                    if (model.Id > 0)
                    {
                        // 🔹 UPDATE Sub Cost Center
                        string updateSubQuery = "UPDATE tbl_sub_cost_center SET name=@name, main_id=@main_id, project_id=@project_id WHERE id=@id";
                        using var cmd = new MySqlCommand(updateSubQuery, conn);
                        cmd.Parameters.AddWithValue("@name", model.Name);
                        cmd.Parameters.AddWithValue("@main_id", model.MainId);
                        int projectIdValue = model.ProjectId ?? 0;
                        cmd.Parameters.AddWithValue("@project_id", projectIdValue);
                        cmd.Parameters.AddWithValue("@id", model.Id);
                        await cmd.ExecuteNonQueryAsync();

                        return Ok(new { status = true, message = "Sub Cost Center updated successfully" });
                    }
                    else
                    {
                        // 🔹 INSERT Sub Cost Center
                        int newSubCode = 101;
                        string getMaxSubQuery = "SELECT MAX(code) FROM tbl_sub_cost_center WHERE main_id=@mainId";
                        using (var cmd = new MySqlCommand(getMaxSubQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@mainId", model.MainId);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != DBNull.Value && result != null)
                                newSubCode = Convert.ToInt32(result) + 1;
                        }

                        string insertSubQuery = "INSERT INTO tbl_sub_cost_center (name, code, main_id, project_id) VALUES (@name,@code,@main_id,@project_id)";
                        using var cmdInsert = new MySqlCommand(insertSubQuery, conn);
                        cmdInsert.Parameters.AddWithValue("@name", model.Name);
                        cmdInsert.Parameters.AddWithValue("@code", newSubCode);
                        cmdInsert.Parameters.AddWithValue("@main_id", model.MainId);
                        int projectIdValue = model.ProjectId ?? 0;
                        cmdInsert.Parameters.AddWithValue("@project_id", projectIdValue);
                        await cmdInsert.ExecuteNonQueryAsync();

                        return Ok(new { status = true, message = "Sub Cost Center added successfully", code = newSubCode });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubCostCenter([FromBody] int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid Cost Center Id" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if sub cost center is used in transactions
                string checkQuery = @"SELECT COUNT(1) FROM tbl_cost_center_transaction WHERE cost_center_id=@id";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                        return BadRequest(new { status = false, message = "Already used in transactions" });
                }

                // Delete sub cost center
                string deleteQuery = "DELETE FROM tbl_sub_cost_center WHERE id=@id";
                using (var cmd = new MySqlCommand(deleteQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

                return Ok(new { status = true, message = "Sub Cost Center deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Department

        public IActionResult Department()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                // Build connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = "SELECT Id, Name FROM tbl_departments ORDER BY Name";
                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var departments = new List<object>();
                while (await reader.ReadAsync())
                {
                    departments.Add(new
                    {
                        id =reader.GetInt32("Id"),
                        name = reader.GetString("Name"),
                    });
                }

                return Ok(new { status = true, data = departments });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddDepartment([FromBody] DepartmentRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter department name" });

            try
            {
                // Build connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check duplicate by Name
                var checkQuery = "SELECT COUNT(*) FROM tbl_departments WHERE name = @name";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name);
                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

                    if (exists && model.Id == 0)
                        return BadRequest(new { status = false, message = "Department name already exists. Enter another name." });
                }

                // Generate new department code

                var insertQuery = @"
            INSERT INTO tbl_departments ( Name)
            VALUES (@name)";

                using var insertCmd = new MySqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@name", model.Name.Trim());

                await insertCmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Department added successfully"});
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> EditDepartment([FromBody] DepartmentRequest model)

        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Department name is required" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check for duplicate name (excluding current record)
                var checkQuery = "SELECT COUNT(*) FROM tbl_departments WHERE Name = @name AND Id != @id";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    checkCmd.Parameters.AddWithValue("@id", model.Id);

                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    if (exists)
                        return BadRequest(new { status = false, message = "Department name already exists." });
                }

                var updateQuery = "UPDATE tbl_departments SET Name = @name WHERE Id = @id";
                using var updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                updateCmd.Parameters.AddWithValue("@id", model.Id);

                await updateCmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Department updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid department ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var deleteQuery = "DELETE FROM tbl_departments WHERE Id = @id";
                using var cmd = new MySqlCommand(deleteQuery, conn);
                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Department deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }



        #endregion

        #region Position 

        public IActionResult Position()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPositions()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"
            SELECT p.id AS PositionId, p.name AS PositionName, p.department_id AS DepartmentId, d.name AS DepartmentName
            FROM tbl_position p
            INNER JOIN tbl_departments d ON p.department_id = d.id
            ORDER BY p.name";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var positions = new List<object>();
                while (await reader.ReadAsync())
                {
                    positions.Add(new
                    {
                        id = reader.GetInt32("PositionId"),
                        name = reader.GetString("PositionName"),
                        departmentId = reader.GetInt32("DepartmentId"),
                        departmentName = reader.GetString("DepartmentName")
                    });
                }

                return Ok(new { status = true, data = positions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> AddPosition([FromBody] PositionRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name) || model.DepartmentId <= 0)
                return BadRequest(new { status = false, message = "Please enter position name and select department" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check duplicate
                var checkQuery = "SELECT COUNT(*) FROM tbl_position WHERE Name = @name AND department_id = @department_id";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                checkCmd.Parameters.AddWithValue("@department_id", model.DepartmentId);
                var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                if (exists) return BadRequest(new { status = false, message = "Position already exists in this department." });

                var insertQuery = "INSERT INTO tbl_position (Name, department_id) VALUES (@name, @department_id)";
                using var insertCmd = new MySqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                insertCmd.Parameters.AddWithValue("@department_id", model.DepartmentId);

                await insertCmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Position added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> EditPosition([FromBody] PositionRequest model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name) || model.DepartmentId <= 0)
                return BadRequest(new { status = false, message = "Position name and department are required" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check duplicate excluding current record
                var checkQuery = "SELECT COUNT(*) FROM tbl_position WHERE Name = @name AND department_id = @department_id AND Id != @id";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                checkCmd.Parameters.AddWithValue("@department_id", model.DepartmentId);
                checkCmd.Parameters.AddWithValue("@id", model.Id);
                var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                if (exists) return BadRequest(new { status = false, message = "Position already exists in this department." });

                var updateQuery = "UPDATE tbl_position SET Name = @name, department_id = @department_id WHERE Id = @id";
                using var updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                updateCmd.Parameters.AddWithValue("@department_id", model.DepartmentId);
                updateCmd.Parameters.AddWithValue("@id", model.Id);

                await updateCmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Position updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePosition(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid position ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var deleteQuery = "DELETE FROM tbl_position WHERE Id = @id";
                using var cmd = new MySqlCommand(deleteQuery, conn);
                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Position deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }



        #endregion

        #region Item List

        [HttpGet]
        public async Task<IActionResult> GetItems(string? categoryId = null, string? type = null, string state = "All")
        {
            try
            {
                // Build connection with dynamic DB from session
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Base query
                var query = @"
            SELECT id,
                  name as ItemName,
                   barcode AS Barcode,
                   type AS Type,
                   cost_price as Cost_Price,
                    warehouse_id as Warehouse_id,
                    unit_id as Unit_id,
                    cogs_account_id as Cogs_account_id,
                    vendor_id as Vendor_id,
                    sales_price as Sales_price,
                    tax_code_id as Tax_code_id,
                    income_account_id as Income_account_id,
                     asset_account_id as Asset_account_id,
                    min_amount as Min_amount,
                      max_amount as Max_amount,
                        on_hand as On_hand,
                        total_value as Total_value,
                        date as Date,
                        img as Img,
                        ItemImg as ItemImg,
                        active as Active,
                        method as Method,
                        category_id as Category_id,
                        posItem as PosItem,
                        item_type as Item_type
            FROM tbl_items
            WHERE state = 0";

                var parameters = new List<MySqlParameter>();
                // Filters
                if (!string.IsNullOrEmpty(categoryId))
                {
                    query += " AND category_id = @category_id";
                    parameters.Add(new MySqlParameter("@category_id", categoryId));
                }
                if (!string.IsNullOrEmpty(type))
                {
                    query += " AND type = @type";
                    parameters.Add(new MySqlParameter("@type", type));
                }
                if (state == "Active")
                {
                    query += " AND active = 0";
                }
                else if (state != "All")
                {
                    query += " AND active != 0";
                }

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                var items = new List<object>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new
                    {
                        id = reader.GetInt32("id"),
                        itemName = reader.GetString("ItemName"),
                        barcode = reader.IsDBNull("Barcode") ? null : reader.GetString("Barcode"),
                        type = reader.GetString("Type"),
                        cost_price = reader.GetDecimal("Cost_Price"),
                        warehouse_id = reader.IsDBNull("Warehouse_id") ? (int?)null : reader.GetInt32("Warehouse_id"),
                        unit_id = reader.IsDBNull("unit_id") ? (int?)null : reader.GetInt32("unit_id"),
                        cogs_account_id = reader.IsDBNull("cogs_account_id") ? (int?)null : reader.GetInt32("cogs_account_id"),
                        vendor_id = reader.IsDBNull("Vendor_id") ? (int?)null : reader.GetInt32("Vendor_id"),
                        sales_price = reader.GetDecimal("Sales_price"),
                        tax_code_id = reader.IsDBNull("Tax_code_id") ? (int?)null : reader.GetInt32("Tax_code_id"),
                        income_account_id = reader.IsDBNull("income_account_id") ? (int?)null : reader.GetInt32("income_account_id"),
                        asset_account_id = reader.IsDBNull("asset_account_id") ? (int?)null : reader.GetInt32("asset_account_id"),
                        min_amount = reader.GetDecimal("Min_amount"),
                        max_amount = reader.GetDecimal("Max_amount"),
                        on_hand = reader.GetDecimal("On_hand"),
                        total_value = reader.GetDecimal("Total_value"),
                        date = reader.GetDateOnly("Date"),
                        active = reader.GetInt32("Active"),
                        method = reader.GetString("Method"),
                        category_id = reader.IsDBNull("Category_id") ? (int?)null : reader.GetInt32("Category_id"),
                        posItem = reader.IsDBNull("PosItem") ? (int?)null : reader.GetInt32("PosItem"),
                        item_type = reader.GetString("Item_type")
                    });

                }

                return Ok(new { status = true, data = items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        public IActionResult ItemList()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveItem([FromBody] ItemRequest model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Name))
                    return BadRequest(new { status = false, message = "Invalid item" });

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId == 0) return Unauthorized(new { status = false, message = "User not logged in" });

                var connStr = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStr.ConnectionString);
                await conn.OpenAsync();

                // ✅ Duplicate check (ignore same Id)
                var exists = Convert.ToInt32(await new MySqlCommand(
                    "SELECT COUNT(*) FROM tbl_items WHERE name=@name AND id<>@id", conn)
                {
                    Parameters = { new MySqlParameter("@name", model.Name), new MySqlParameter("@id", model.Id) }
                }.ExecuteScalarAsync()) > 0;

                if (exists) return BadRequest(new { status = false, message = "Item already exists" });

                // ✅ UPDATE
                if (model.Id > 0)
                {
                    var updateQuery = @"UPDATE tbl_items SET 
                warehouse_id=@warehouseId,
                type=@type,
                category_id=@category,
                name=@name,
                unit_id=@unit_id,
                barcode=@barcode,
                cost_price=@cost_price,
                cogs_account_id=@cogs_account_id,
                vendor_id=@vendor_id,
                sales_price=@sales_price,
                income_account_id=@income_account_id,
                asset_account_id=@asset_account_id,
                min_amount=@min_amount,
                max_amount=@max_amount,
                on_hand=@on_hand,
                method=@method,
                total_value=@total_value,
                date=@date,
                active=@active,
                item_type=@item_type
            WHERE id=@id";

                    using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                    cmd.Parameters.AddWithValue("@type", model.Type);
                    cmd.Parameters.AddWithValue("@category", model.CategoryId);
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@unit_id", model.UnitId);
                    cmd.Parameters.AddWithValue("@barcode", model.Barcode ?? "");
                    cmd.Parameters.AddWithValue("@cost_price", model.CostPrice);
                    cmd.Parameters.AddWithValue("@cogs_account_id", model.CogsAccountId);
                    cmd.Parameters.AddWithValue("@vendor_id", model.VendorId ?? 0);
                    cmd.Parameters.AddWithValue("@sales_price", model.SalesPrice);
                    cmd.Parameters.AddWithValue("@income_account_id", model.IncomeAccountId);
                    cmd.Parameters.AddWithValue("@asset_account_id", model.AssetAccountId);
                    cmd.Parameters.AddWithValue("@min_amount", model.MinAmount);
                    cmd.Parameters.AddWithValue("@max_amount", model.MaxAmount);
                    cmd.Parameters.AddWithValue("@on_hand", model.OnHand);
                    cmd.Parameters.AddWithValue("@method", model.Method);
                    cmd.Parameters.AddWithValue("@total_value", model.TotalValue);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@active", model.Active ? 1 : 0);
                    cmd.Parameters.AddWithValue("@item_type", model.ItemType);

                    await cmd.ExecuteNonQueryAsync();

                    // ✅ Update Units (delete old → insert new)
                    using (var delUnitCmd = new MySqlCommand("DELETE FROM tbl_items_unit WHERE item_id=@id", conn))
                    {
                        delUnitCmd.Parameters.AddWithValue("@id", model.Id);
                        await delUnitCmd.ExecuteNonQueryAsync();
                    }
                    foreach (var u in model.Units)
                    {
                        using var unitCmd = new MySqlCommand(
                            @"INSERT INTO tbl_items_unit (item_id, unit_id, factor) VALUES (@id,@unit_id,@factor)", conn);
                        unitCmd.Parameters.AddWithValue("@id", model.Id);
                        unitCmd.Parameters.AddWithValue("@unit_id", u.UnitId);
                        unitCmd.Parameters.AddWithValue("@factor", u.Factor);
                        await unitCmd.ExecuteNonQueryAsync();
                    }

                    // ✅ Update Assemblies only for "Assembly"
                    using (var delAsmCmd = new MySqlCommand("DELETE FROM tbl_item_assembly WHERE assembly_id=@id", conn))
                    {
                        delAsmCmd.Parameters.AddWithValue("@id", model.Id);
                        await delAsmCmd.ExecuteNonQueryAsync();
                    }
                    if (model.Type.Contains("Assembly"))
                    {
                        foreach (var asm in model.Assemblies)
                        {
                            using var asmCmd = new MySqlCommand(
                                "INSERT INTO tbl_item_assembly (assembly_id,item_id,qty) VALUES (@assembly_id,@item_id,@qty)", conn);
                            asmCmd.Parameters.AddWithValue("@assembly_id", model.Id);
                            asmCmd.Parameters.AddWithValue("@item_id", asm.ItemId);
                            asmCmd.Parameters.AddWithValue("@qty", asm.Qty);
                            await asmCmd.ExecuteNonQueryAsync();
                        }
                    }

                    return Ok(new { status = true, message = "Item updated successfully", id = model.Id });
                }
                else
                {
                    // ✅ INSERT (your original logic)
                    long lastCode = 0;
                    using (var reader = await new MySqlCommand(
                        "SELECT Code FROM tbl_items WHERE LENGTH(Code)=9 ORDER BY CAST(Code AS UNSIGNED) DESC LIMIT 1", conn)
                        .ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync()) lastCode = long.Parse(reader["Code"].ToString());
                    }
                    string newCode = (lastCode + 1).ToString("D9");

                    var insertQuery = @"INSERT INTO tbl_items
                (code, warehouse_id, type, category_id, name, unit_id, barcode, cost_price, cogs_account_id, vendor_id, sales_price, income_account_id, asset_account_id, min_amount, max_amount, on_hand, method, total_value, date, active, created_by, created_date, item_type)
                VALUES (@code,@warehouseId,@type,@category,@name,@unit_id,@barcode,@cost_price,@cogs_account_id,@vendor_id,@sales_price,@income_account_id,@asset_account_id,@min_amount,@max_amount,@on_hand,@method,@total_value,@date,@active,@created_by,@created_date,@item_type);
                SELECT LAST_INSERT_ID();";

                    using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@code", newCode);
                    cmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                    cmd.Parameters.AddWithValue("@type", model.Type);
                    cmd.Parameters.AddWithValue("@category", model.CategoryId);
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@unit_id", model.UnitId);
                    cmd.Parameters.AddWithValue("@barcode", model.Barcode ?? "");
                    cmd.Parameters.AddWithValue("@cost_price", model.CostPrice);
                    cmd.Parameters.AddWithValue("@cogs_account_id", model.CogsAccountId);
                    cmd.Parameters.AddWithValue("@vendor_id", model.VendorId ?? 0);
                    cmd.Parameters.AddWithValue("@sales_price", model.SalesPrice);
                    cmd.Parameters.AddWithValue("@income_account_id", model.IncomeAccountId);
                    cmd.Parameters.AddWithValue("@asset_account_id", model.AssetAccountId);
                    cmd.Parameters.AddWithValue("@min_amount", model.MinAmount);
                    cmd.Parameters.AddWithValue("@max_amount", model.MaxAmount);
                    cmd.Parameters.AddWithValue("@on_hand", model.OnHand);
                    cmd.Parameters.AddWithValue("@method", model.Method);
                    cmd.Parameters.AddWithValue("@total_value", model.TotalValue);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@active", model.Active ? 1 : 0);
                    cmd.Parameters.AddWithValue("@created_by", userId);
                    cmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@item_type", model.ItemType);

                    model.Id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    model.Code = newCode;

                    foreach (var u in model.Units)
                    {
                        using var unitCmd = new MySqlCommand(
                            @"INSERT INTO tbl_items_unit (item_id, unit_id, factor) VALUES (@id,@unit_id,@factor)", conn);
                        unitCmd.Parameters.AddWithValue("@id", model.Id);
                        unitCmd.Parameters.AddWithValue("@unit_id", u.UnitId);
                        unitCmd.Parameters.AddWithValue("@factor", u.Factor);
                        await unitCmd.ExecuteNonQueryAsync();
                    }

                    if (model.Type.Contains("Assembly"))
                    {
                        foreach (var asm in model.Assemblies)
                        {
                            using var asmCmd = new MySqlCommand(
                                "INSERT INTO tbl_item_assembly (assembly_id,item_id,qty) VALUES (@assembly_id,@item_id,@qty)", conn);
                            asmCmd.Parameters.AddWithValue("@assembly_id", model.Id);
                            asmCmd.Parameters.AddWithValue("@item_id", asm.ItemId);
                            asmCmd.Parameters.AddWithValue("@qty", asm.Qty);
                            await asmCmd.ExecuteNonQueryAsync();
                        }
                    }

                    return Ok(new { status = true, message = "Item added successfully", code = model.Code, id = model.Id });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemTransactions(int itemId)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"
            SELECT id, date, reference, type, qty_in, qty_out, cost_price, sales_price
            FROM tbl_item_transaction
            WHERE item_id = @itemId
            ORDER BY date ASC, id ASC";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@itemId", itemId);

                var transactions = new List<object>();
                decimal qtyBalance = 0;

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var qtyIn = reader.IsDBNull("qty_in") ? 0 : reader.GetDecimal("qty_in");
                    var qtyOut = reader.IsDBNull("qty_out") ? 0 : reader.GetDecimal("qty_out");
                    qtyBalance += (qtyIn - qtyOut);

                    transactions.Add(new
                    {
                        id = reader.GetInt32("id"),
                        date = reader.GetDateTime("date").ToString("dd-MM-yyyy"),
                        reference = reader.IsDBNull("reference") ? "" : reader.GetString("reference"),
                        type = reader.GetString("type"),
                        qtyIn = qtyIn.ToString("N2"),
                        qtyOut = qtyOut.ToString("N2"),
                        costPrice = reader.IsDBNull("cost_price") ? "0.00" : reader.GetDecimal("cost_price").ToString("N2"),
                        salesPrice = reader.IsDBNull("sales_price") ? "0.00" : reader.GetDecimal("sales_price").ToString("N2"),
                        qtyBalance = qtyBalance.ToString("N2")
                    });
                }

                return Ok(new { status = true, data = transactions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemCardTransactions(int itemId)
        {
            try
            {
                // Build connection string with dynamic DB from session
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                                ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 1️⃣ Get item info (on_hand, method)
                string method = "";
                decimal onHand = 0;
                using (var cmdItem = new MySqlCommand(
                    "SELECT on_hand, method FROM tbl_items WHERE id = @itemId", conn))
                {
                    cmdItem.Parameters.AddWithValue("@itemId", itemId);
                    using var itemReader = await cmdItem.ExecuteReaderAsync();
                    if (await itemReader.ReadAsync())
                    {
                        method = itemReader["method"]?.ToString().Trim() ?? "";
                        onHand = itemReader["on_hand"] == DBNull.Value ? 0 : Convert.ToDecimal(itemReader["on_hand"]);
                    }
                }

                // 2️⃣ Get transactions with warehouse name using JOIN
                var transactions = new List<object>();
                decimal qtyBalance = 0;
                decimal currentCostPrice = 0;
                List<decimal> fifoPrices = new List<decimal>();
                List<decimal> fifoQtys = new List<decimal>();
                decimal lastPrice = 0;
                bool fifoSet = false;

                string query = @"
            SELECT icd.id, icd.date, icd.wharehouse_id, icd.inv_no, icd.trans_no, icd.trans_type, icd.description,
                   icd.price, icd.qty_in, icd.qty_out, icd.qty_balance, icd.debit, icd.credit, icd.balance, icd.fifo_qty, icd.fifo_cost,
                   w.name AS warehouseName
            FROM tbl_item_card_details icd
            LEFT JOIN tbl_warehouse w ON icd.wharehouse_id = w.id
            WHERE icd.itemId = @itemId
            ORDER BY icd.date, icd.id";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@itemId", itemId);
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        string warehouseName = reader["warehouseName"]?.ToString() ?? "";
                        decimal qtyIn = reader["qty_in"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["qty_in"]);
                        decimal qtyOut = reader["qty_out"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["qty_out"]);
                        decimal price = reader["price"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["price"]);

                        // FIFO / LIFO / AVG logic
                        if (qtyIn > 0)
                        {
                            fifoPrices.Add(price);
                            fifoQtys.Add(qtyIn);
                            lastPrice = price;

                            if (method.ToUpper() == "FIFO" && !fifoSet)
                            {
                                currentCostPrice = price;
                                fifoSet = true;
                            }
                        }

                        if (method.ToUpper() == "AVG")
                        {
                            decimal qtyBal = reader["qty_balance"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["qty_balance"]);
                            decimal bal = reader["balance"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["balance"]);
                            currentCostPrice = qtyBal != 0 ? bal / qtyBal : 0;
                        }

                        qtyBalance += (qtyIn - qtyOut);

                        transactions.Add(new
                        {
                            id = reader["id"],
                            date = reader["date"] == DBNull.Value ? "" : Convert.ToDateTime(reader["date"]).ToString("dd-MM-yyyy"),
                            warehouseName,
                            invNo = reader["inv_no"]?.ToString() ?? "",
                            transNo = reader["trans_no"]?.ToString() ?? "",
                            transType = reader["trans_type"]?.ToString() ?? "",
                            description = reader["description"]?.ToString() ?? "",
                            price = price.ToString("N2"),
                            qtyIn = qtyIn.ToString("N2"),
                            qtyOut = qtyOut.ToString("N2"),
                            qtyBalance = qtyBalance.ToString("N2"),
                            debit = reader["debit"] == DBNull.Value ? "0.00" : Convert.ToDecimal(reader["debit"]).ToString("N2"),
                            credit = reader["credit"] == DBNull.Value ? "0.00" : Convert.ToDecimal(reader["credit"]).ToString("N2"),
                            balance = reader["balance"] == DBNull.Value ? "0.00" : Convert.ToDecimal(reader["balance"]).ToString("N2")
                        });
                    }
                }

                return Ok(new
                {
                    status = true,
                    item = new
                    {
                        itemId,
                        method,
                        onHand,
                        currentCostPrice = currentCostPrice.ToString("N2")
                    },
                    transactions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid Item ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Check if item is used in transactions (except Opening Qty)
                string checkQuery = @"
            SELECT COUNT(1) 
            FROM tbl_item_transaction 
            WHERE type != 'Opening Qty' AND item_id = @id";

                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var result = await checkCmd.ExecuteScalarAsync();
                    int recordCount = Convert.ToInt32(result ?? 0);

                    if (recordCount > 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Item is already used in transactions and cannot be deleted. You can inactive the item instead."
                        });
                    }
                }

                // 🔹 Delete item and its related transactions (optional: only if needed)
                string deleteQuery = @"
            DELETE FROM tbl_items WHERE id = @id;
            DELETE FROM tbl_item_transaction WHERE item_id = @id";

                using (var deleteCmd = new MySqlCommand(deleteQuery, conn))
                {
                    deleteCmd.Parameters.AddWithValue("@id", id);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                return Ok(new { status = true, message = "Item deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Transacation Journal

        public IActionResult TransacationJournal()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetJournal(
    string? selectionMethod = "Default",
    string? type = null,
    int? transactionId = null,
    bool filterByDate = false,
    DateTime? fromDate = null,
    DateTime? toDate = null)
        {
            try
            {
                // Build connection with dynamic DB from session
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Base query
                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS SN,
                CONCAT('000', transaction_id) AS RefId,
                tbl_transaction.date AS Date,
                tbl_transaction.transaction_id,
                tbl_transaction.type AS Type,
                tbl_coa_level_4.code AS `ACCode`,
                tbl_coa_level_4.name AS `ACName`,
                tbl_transaction.description AS Description,
                tbl_transaction.debit AS Debit,
                tbl_transaction.credit AS Credit
            FROM tbl_transaction
            INNER JOIN tbl_coa_level_4 
                ON tbl_transaction.account_id = tbl_coa_level_4.id
            WHERE tbl_transaction.state = 0";

                var parameters = new List<MySqlParameter>();

                // Build condition dynamically
                if (selectionMethod == "General Ledger")
                {
                    query += " AND tbl_transaction.hum_id != 0";
                }
                else if (selectionMethod == "Default")
                {
                    // keep base
                }
                else if (selectionMethod == "Inventory Opening Stock")
                {
                    query += " AND tbl_transaction.type = 'Opening Balance'";
                }
                else
                {
                    query += " AND tbl_transaction.type = @selType";
                    parameters.Add(new MySqlParameter("@selType", selectionMethod));
                }

                if (filterByDate && fromDate.HasValue && toDate.HasValue)
                {
                    query += " AND tbl_transaction.date >= @fromDate AND tbl_transaction.date <= @toDate";
                    parameters.Add(new MySqlParameter("@fromDate", fromDate.Value));
                    parameters.Add(new MySqlParameter("@toDate", toDate.Value));
                }

                if (transactionId.HasValue)
                {
                    query += " AND tbl_transaction.transaction_id = @id";
                    parameters.Add(new MySqlParameter("@id", transactionId.Value));
                }

                // Run query
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                var journalEntries = new List<object>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    journalEntries.Add(new
                    {
                        SN = reader["SN"] != DBNull.Value ? Convert.ToInt64(reader["SN"]) : (long?)null,
                        RefId = reader["RefId"]?.ToString() ?? "",
                        Date = reader["Date"] != DBNull.Value ? (DateTime)reader["Date"] : (DateTime?)null,
                        //TransactionId = reader["transaction_id"]?.ToString() ?? "",
                        Type = reader["Type"]?.ToString() ?? "",
                        ACCode = reader["ACCode"]?.ToString() ?? "",
                        ACName = reader["ACName"]?.ToString() ?? "",
                        //Description = reader["Description"]?.ToString(),
                        Debit = reader["Debit"] != DBNull.Value ? Convert.ToDecimal(reader["Debit"]) : 0,
                        Credit = reader["Credit"] != DBNull.Value ? Convert.ToDecimal(reader["Credit"]) : 0
                    });
                }


                // Add total row if data exists
                if (journalEntries.Any())
                {
                    decimal totalDebit = journalEntries.Sum(x => (decimal)x.GetType().GetProperty("Debit")!.GetValue(x)!);
                    decimal totalCredit = journalEntries.Sum(x => (decimal)x.GetType().GetProperty("Credit")!.GetValue(x)!);

                    journalEntries.Add(new
                    {
                        SN = (int?)null,
                        RefId = "",
                        Date = (DateTime?)null,
                        //TransactionId = (int?)null,
                        Type = "",
                        ACCode = "",
                        ACName = "TOTAL",
                        //Description = "",
                        Debit = totalDebit,
                        Credit = totalCredit
                    });
                }

                return Ok(new { status = true, data = journalEntries });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Fixed Asset Category

        public IActionResult FixedAssetsCategory()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetFixedAssetsCategory()
        {
            try
            {
                // Build connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = "SELECT * FROM tbl_fixed_assets_category ORDER BY Category_Name";
                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var category = new List<object>();
                while (await reader.ReadAsync())
                {
                    category.Add(new
                    {
                        id = reader.GetInt32("Id"),
                        category_name = reader.GetString("Category_Name"),
                        assets_account_id = reader.GetInt32("Assets_Account_Id"),
                        depreciation_account_id = reader.GetInt32("Depreciation_Account_Id"),
                        expence_account_id= reader.GetInt32("Expence_Account_Id"),
                    });
                }

                return Ok(new { status = true, data = category });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddFixedAssetsCategory([FromBody] FixedAssetsCategoryRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.CategoryName))
                return BadRequest(new { status = false, message = "Please enter category name" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Check duplicate
                var checkQuery = "SELECT COUNT(*) FROM tbl_fixed_assets_category WHERE category_name = @name";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

                    if (exists && model.Id == 0)
                        return BadRequest(new { status = false, message = "Category already exists. Enter another name." });
                }

                // ✅ Insert new
                var insertQuery = @"
            INSERT INTO tbl_fixed_assets_category 
                (category_name, assets_account_id, depreciation_account_id, expence_account_id)
            VALUES 
                (@name, @assetsId, @depreciationId, @expenceId)";
                using var insertCmd = new MySqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                insertCmd.Parameters.AddWithValue("@assetsId", model.AssetsAccountId);
                insertCmd.Parameters.AddWithValue("@depreciationId", model.DepreciationAccountId);
                insertCmd.Parameters.AddWithValue("@expenceId", model.ExpenceAccountId);

                await insertCmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Category added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> EditFixedAssetsCategory([FromBody] FixedAssetsCategoryRequest model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.CategoryName))
                return BadRequest(new { status = false, message = "Category name is required" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Check for duplicate category name
                var checkQuery = "SELECT COUNT(*) FROM tbl_fixed_assets_category WHERE Category_Name = @name AND Id != @id";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                    checkCmd.Parameters.AddWithValue("@id", model.Id);

                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    if (exists)
                        return BadRequest(new { status = false, message = "Category name already exists." });
                }

                // ✅ Update record
                var updateQuery = @"UPDATE tbl_fixed_assets_category 
                            SET Category_Name = @name, 
                                Assets_Account_Id = @assetsAccountId, 
                                Depreciation_Account_Id = @depreciationAccountId, 
                                Expence_Account_Id = @expenseAccountId
                            WHERE Id = @id";

                using var updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                updateCmd.Parameters.AddWithValue("@assetsAccountId", model.AssetsAccountId);
                updateCmd.Parameters.AddWithValue("@depreciationAccountId", model.DepreciationAccountId);
                updateCmd.Parameters.AddWithValue("@expenseAccountId", model.ExpenceAccountId);
                updateCmd.Parameters.AddWithValue("@id", model.Id);

                await updateCmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Fixed Assets Category updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFixedAssetCategory(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid category id" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 1️⃣ Check if category is used in tbl_fixed_assets
                string checkQuery = "SELECT COUNT(*) FROM tbl_fixed_assets WHERE category_id = @categoryId";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@categoryId", id);

                var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (count > 0)
                {
                    return BadRequest(new { status = false, message = "Cannot delete category. It is used by existing fixed assets." });
                }

                // 2️⃣ Delete category
                string deleteQuery = "DELETE FROM tbl_fixed_assets_category WHERE Id = @categoryId";
                using var deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@categoryId", id);

                int rowsAffected = await deleteCmd.ExecuteNonQueryAsync();
                if (rowsAffected > 0)
                    return Ok(new { status = true, message = "Category deleted successfully" });
                else
                    return NotFound(new { status = false, message = "Category not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Fixed Assets

        public IActionResult FixedAssets()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFixedAssets()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = "SELECT * FROM tbl_fixed_assets WHERE state = 0 ORDER BY id DESC; ";
                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var assets = new List<object>();

                while (await reader.ReadAsync())
                {
                    assets.Add(new
                    {
                        Id = reader["id"],
                        Code = reader["code"].ToString(),
                        Name = reader["name"].ToString(),
                        Brand = reader["brand"].ToString(),
                        CategoryId = reader["category_id"].ToString(),
                        Model = reader["model"].ToString(),
                        Supplier = reader["supplier"].ToString(),
                        Status = reader["status"].ToString(),
                        InvoiceNumber = reader["invoice_number"].ToString(),
                        PurchaseDate = reader["purchase_date"] != DBNull.Value ? DateTime.Parse(reader["purchase_date"].ToString()) : (DateTime?)null,
                        EndDate = reader["end_date"] != DBNull.Value ? DateTime.Parse(reader["end_date"].ToString()) : (DateTime?)null,
                        DepreciationLife = reader["depreciation_life"] != DBNull.Value ? int.Parse(reader["depreciation_life"].ToString()) : 0,
                        PurchasePrice = reader["purchase_price"] != DBNull.Value ? Convert.ToDecimal(reader["purchase_price"]) : 0,
                        DebitAccountId = reader["debit_account_id"].ToString(),
                        CreditAccountId = reader["credit_account_id"].ToString(),
                        ExpenceAccountId = reader["expence_account_id"].ToString(),
                        Date = reader["date"] != DBNull.Value ? DateTime.Parse(reader["date"].ToString()) : (DateTime?)null,
                        Manufacture = reader["manufacture"] != DBNull.Value && int.Parse(reader["manufacture"].ToString()) == 1,
                        ManufactureStatus = reader["manufactureStatus"].ToString()
                    });
                }

                return Ok(new { status = true, data = assets });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddFixedAsset([FromBody] FixedAssetRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Enter asset name" });

            if (model.CategoryId == 0)
                return BadRequest(new { status = false, message = "Select category first" });

            if (model.PurchasePrice <= 0)
                return BadRequest(new { status = false, message = "Enter purchase price" });

            if (model.DepreciationLife <= 0)
                return BadRequest(new { status = false, message = "Enter depreciation life" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Generate next code
                string code;
                using (var cmdCode = new MySqlCommand("SELECT MAX(CAST(code AS UNSIGNED)) AS lastCode FROM tbl_fixed_assets", conn))
                {
                    var lastCode = await cmdCode.ExecuteScalarAsync();
                    int nextCode = lastCode != DBNull.Value ? Convert.ToInt32(lastCode) + 1 : 1;
                    code = nextCode.ToString("D5");
                }

                // Insert Fixed Asset
                var insertQuery = @"
            INSERT INTO tbl_fixed_assets
                (date, code, name, brand, category_id, model, supplier, status, invoice_number, purchase_date, end_date, depreciation_life, purchase_price, debit_account_id, credit_account_id, expence_account_id, created_by, created_date, state, manufacture, manufactureStatus)
            VALUES
                (@date, @code, @name, @brand, @category_id, @model, @supplier, @status, @invoice_number, @purchase_date, @end_date, @depreciation_life, @purchase_price, @debit_account_id, @credit_account_id, @expence_account_id, @created_by, @created_date, 0, @manufacture, 'Draft');
            SELECT LAST_INSERT_ID();";

                int assetId;
                using (var cmd = new MySqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@date", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@brand", model.Brand);
                    cmd.Parameters.AddWithValue("@category_id", model.CategoryId);
                    cmd.Parameters.AddWithValue("@model", model.Model);
                    cmd.Parameters.AddWithValue("@supplier", model.Supplier);
                    cmd.Parameters.AddWithValue("@status", model.Status);
                    cmd.Parameters.AddWithValue("@invoice_number", model.InvoiceNumber);
                    cmd.Parameters.AddWithValue("@purchase_date", model.PurchaseDate);
                    cmd.Parameters.AddWithValue("@end_date", model.EndDate);
                    cmd.Parameters.AddWithValue("@depreciation_life", model.DepreciationLife);
                    cmd.Parameters.AddWithValue("@purchase_price", model.PurchasePrice);
                    cmd.Parameters.AddWithValue("@debit_account_id", model.DebitAccountId);
                    cmd.Parameters.AddWithValue("@credit_account_id", model.CreditAccountId);
                    cmd.Parameters.AddWithValue("@expence_account_id", model.ExpenceAccountId);
                    cmd.Parameters.AddWithValue("@created_by", model.CreatedBy);
                    cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@manufacture", model.ManufactureId);

                    assetId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Insert Journal if required
                if (model.CreateJournal)
                {
                    await InsertJournalAsync(conn, assetId, model);
                }

                return Ok(new { status = true, message = "Asset added successfully", assetId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task InsertJournalAsync(MySqlConnection conn, int assetId, FixedAssetRequest model)
        {
            DateTime startDate = model.PurchaseDate;
            DateTime endDate = model.EndDate;
            decimal totalAmount = model.PurchasePrice;
            int totalDays = (endDate - startDate).Days + 1;
            string expenseAccountId = model.ExpenceAccountId.ToString();
            decimal actualTotal = 0;

            // Debit & Credit entry at purchase
            if (!string.IsNullOrEmpty(model.Supplier))
            {
                await CommonInsertTransactionAsync(conn, startDate, model.DebitAccountId.ToString(), totalAmount, 0, assetId.ToString(), 0, "Fixed Assets", $"{model.Name} - Fixed Assets No. {assetId}", model.CreatedBy);
                await CommonInsertTransactionAsync(conn, startDate, model.CreditAccountId.ToString(), 0, totalAmount, assetId.ToString(), 0, "Fixed Assets", $"{model.Name} - Fixed Assets No. {assetId}", model.CreatedBy);
            }

            DateTime currentMonthStart = startDate;

            while (currentMonthStart <= endDate)
            {
                DateTime currentMonthEnd = new DateTime(currentMonthStart.Year, currentMonthStart.Month, DateTime.DaysInMonth(currentMonthStart.Year, currentMonthStart.Month));
                if (currentMonthEnd > endDate)
                    currentMonthEnd = endDate;

                DateTime periodStart = currentMonthStart < startDate ? startDate : currentMonthStart;
                DateTime periodEnd = currentMonthEnd > endDate ? endDate : currentMonthEnd;

                int daysInPeriod = (periodEnd - periodStart).Days + 1;
                decimal amount = Math.Round((totalAmount / totalDays) * daysInPeriod, 2);

                if (periodEnd == endDate)
                    amount = totalAmount - actualTotal;

                actualTotal += amount;

                await CommonInsertTransactionAsync(conn, periodEnd, expenseAccountId, amount, 0, assetId.ToString(), 0, "Fixed Assets", $"{model.Name} - Fixed Assets No. {assetId}", model.CreatedBy);
                await CommonInsertTransactionAsync(conn, periodEnd, model.CreditAccountId.ToString(), 0, amount, assetId.ToString(), 0, "Fixed Assets", $"{model.Name} - Fixed Assets No. {assetId}", model.CreatedBy);

                currentMonthStart = currentMonthStart.AddMonths(1);
                currentMonthStart = new DateTime(currentMonthStart.Year, currentMonthStart.Month, 1);
            }
        }

        private async Task CommonInsertTransactionAsync(MySqlConnection conn, DateTime date, string accountId, decimal debit, decimal credit,
                                                         string transactionId, int humId, string type, string description, int createdBy)
        {
            var query = @"INSERT INTO tbl_transaction 
                  (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state) 
                  VALUES (@date, @accountId, @debit, @credit, @transactionId, @humId, '', @type, @description, @createdBy, @createdDate, 0);";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transactionId", transactionId);
            cmd.Parameters.AddWithValue("@humId", humId);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@createdBy", createdBy);
            cmd.Parameters.AddWithValue("@createdDate", DateTime.Now.Date);

            await cmd.ExecuteNonQueryAsync();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateFixedAsset([FromBody] FixedAssetRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (model.Id <= 0)
                return BadRequest(new { status = false, message = "Invalid asset ID" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Enter asset name" });

            if (model.CategoryId == 0)
                return BadRequest(new { status = false, message = "Select category first" });

            if (model.PurchasePrice <= 0)
                return BadRequest(new { status = false, message = "Enter purchase price" });

            if (model.DepreciationLife <= 0)
                return BadRequest(new { status = false, message = "Enter depreciation life" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Update Fixed Asset
                var updateQuery = @"
            UPDATE tbl_fixed_assets SET
                date=@date,
                name=@name,
                brand=@brand,
                category_id=@category_id,
                model=@model,
                supplier=@supplier,
                status=@status,
                invoice_number=@invoice_number,
                purchase_date=@purchase_date,
                end_date=@end_date,
                depreciation_life=@depreciation_life,
                purchase_price=@purchase_price,
                debit_account_id=@debit_account_id,
                credit_account_id=@credit_account_id,
                expence_account_id=@expence_account_id,
                modified_by=@modified_by,
                modified_date=@modified_date,
                manufacture=@manufacture,
                manufactureStatus='Draft'
            WHERE id=@id;";

                using (var cmd = new MySqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@date", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@brand", model.Brand);
                    cmd.Parameters.AddWithValue("@category_id", model.CategoryId);
                    cmd.Parameters.AddWithValue("@model", model.Model);
                    cmd.Parameters.AddWithValue("@supplier", model.Supplier);
                    cmd.Parameters.AddWithValue("@status", model.Status);
                    cmd.Parameters.AddWithValue("@invoice_number", model.InvoiceNumber);
                    cmd.Parameters.AddWithValue("@purchase_date", model.PurchaseDate);
                    cmd.Parameters.AddWithValue("@end_date", model.EndDate);
                    cmd.Parameters.AddWithValue("@depreciation_life", model.DepreciationLife);
                    cmd.Parameters.AddWithValue("@purchase_price", model.PurchasePrice);
                    cmd.Parameters.AddWithValue("@debit_account_id", model.DebitAccountId);
                    cmd.Parameters.AddWithValue("@credit_account_id", model.CreditAccountId);
                    cmd.Parameters.AddWithValue("@expence_account_id", model.ExpenceAccountId);
                    cmd.Parameters.AddWithValue("@modified_by", model.CreatedBy); // or ModifiedBy field
                    cmd.Parameters.AddWithValue("@modified_date", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@manufacture", model.ManufactureId);

                    await cmd.ExecuteNonQueryAsync();
                }

                // Delete old journal entries
                using (var cmdDelete = new MySqlCommand("DELETE FROM tbl_transaction WHERE transaction_id=@id AND type='Fixed Assets';", conn))
                {
                    cmdDelete.Parameters.AddWithValue("@id", model.Id);
                    await cmdDelete.ExecuteNonQueryAsync();
                }

                // Insert new journal if required
                if (model.CreateJournal)
                {
                    await InsertJournalAsync(conn, model.Id, model);
                }

                return Ok(new { status = true, message = "Asset updated successfully", assetId = model.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFixedAssetsForGrid(int assetId)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 1️⃣ Get the fixed asset details
                var assetQuery = @"
            SELECT id, name, purchase_date, end_date, purchase_price
            FROM tbl_fixed_assets
            WHERE id = @assetId AND state = 0";

                using var assetCmd = new MySqlCommand(assetQuery, conn);
                assetCmd.Parameters.AddWithValue("@assetId", assetId);

                using var reader = await assetCmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return NotFound(new { status = false, message = "Asset not found" });

                var assetName = reader["name"].ToString();
                var startDate = reader["purchase_date"] != DBNull.Value ? DateTime.Parse(reader["purchase_date"].ToString()) : (DateTime?)null;
                var endDate = reader["end_date"] != DBNull.Value ? DateTime.Parse(reader["end_date"].ToString()) : (DateTime?)null;
                var totalAmount = reader["purchase_price"] != DBNull.Value ? Convert.ToDecimal(reader["purchase_price"]) : 0;

                if (startDate == null || endDate == null)
                    return BadRequest(new { status = false, message = "Invalid purchase or end date" });

                // 2️⃣ Calculate schedule
                var totalDays = (endDate.Value - startDate.Value).Days + 1;
                var actualTotal = 0m;
                var currentMonthStart = startDate.Value;
                int serialNumber = 1;

                var schedule = new List<object>();

                while (currentMonthStart <= endDate.Value)
                {
                    var currentMonthEnd = new DateTime(currentMonthStart.Year, currentMonthStart.Month, DateTime.DaysInMonth(currentMonthStart.Year, currentMonthStart.Month));
                    if (currentMonthEnd > endDate.Value)
                        currentMonthEnd = endDate.Value;

                    var periodStart = currentMonthStart < startDate.Value ? startDate.Value : currentMonthStart;
                    var periodEnd = currentMonthEnd > endDate.Value ? endDate.Value : currentMonthEnd;

                    var daysInPeriod = (periodEnd - periodStart).Days + 1;
                    decimal amount = Math.Round((totalAmount / totalDays) * daysInPeriod, 2);

                    if (periodEnd == endDate.Value)
                        amount = totalAmount - actualTotal;

                    actualTotal += amount;

                    schedule.Add(new
                    {
                        SN = serialNumber,
                        Date = periodEnd.ToString("yyyy-MM-dd"),
                        Days = daysInPeriod,
                        Description = assetName,
                        Amount = amount.ToString("N2"),
                        Note = "Fixed Asset REGISTER"
                    });

                    serialNumber++;
                    currentMonthStart = currentMonthStart.AddMonths(1);
                    currentMonthStart = new DateTime(currentMonthStart.Year, currentMonthStart.Month, 1);
                }

                // Add summary row
                schedule.Add(new
                {
                    SN = serialNumber,
                    Date = "",
                    Days = totalDays,
                    Description = "",
                    Amount = actualTotal.ToString("N2"),
                    Note = ""
                });

                return Ok(new { status = true, data = schedule });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteFixedAsset(int assetId)
        {
            if (assetId==null)
                return BadRequest(new { status = false, message = "Invalid Asset ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Soft delete: set state = -1
                var query = "UPDATE tbl_fixed_assets SET state = -1 WHERE id = @id";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", assetId);

                int rowsAffected = await cmd.ExecuteNonQueryAsync();
                if (rowsAffected == 0)
                    return NotFound(new { status = false, message = "Asset not found" });

                return Ok(new { status = true, message = "Fixed Asset deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFixedAssetsCategoryList()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"SELECT id, category_name, assets_account_id, depreciation_account_id, expence_account_id
                         FROM tbl_fixed_assets_category
                        ;";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var categories = new List<object>();

                while (await reader.ReadAsync())
                {
                    categories.Add(new
                    {
                        Id = reader["id"],
                        Category_Name = reader["category_name"].ToString(),
                        AssetsAccountId = reader["assets_account_id"] != DBNull.Value ? Convert.ToInt32(reader["assets_account_id"]) : 0,
                        DepreciationAccountId = reader["depreciation_account_id"] != DBNull.Value ? Convert.ToInt32(reader["depreciation_account_id"]) : 0,
                        ExpenceAccountId = reader["expence_account_id"] != DBNull.Value ? Convert.ToInt32(reader["expence_account_id"]) : 0
                    });
                }

                return Ok(new { status = true, data = categories });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }



        #endregion

        #region Petty Cash Catogory

        public IActionResult PettyCashCategory()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPettyCashCategories()
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"SELECT id, name, description FROM tbl_petty_cash_category ORDER BY id ASC";
                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();
                int sn = 1;
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        sn = sn++,
                        id = Convert.ToInt32(reader["id"]),
                        name = reader["name"].ToString(),
                        description = reader["description"]?.ToString()
                    });
                }

                return Ok(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePettyCashCategory([FromBody] PettyCashCategoryRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter category name" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                //  Check for duplicates
                var checkQuery = "SELECT id FROM tbl_petty_cash_category WHERE name=@name AND id<>@id";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    checkCmd.Parameters.AddWithValue("@id", model.Id);
                    var exists = await checkCmd.ExecuteScalarAsync();
                    if (exists != null)
                        return BadRequest(new { status = false, message = "Category already exists. Enter another name." });
                }

                if (model.Id == 0) // ➝ INSERT
                {
                    var insertQuery = @"
                INSERT INTO tbl_petty_cash_category (name, description) 
                VALUES (@name, @description)";
                    using var insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    insertCmd.Parameters.AddWithValue("@description", model.Description?.Trim() ?? "");
                    //insertCmd.Parameters.AddWithValue("@created_by", userId);
                    //insertCmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    await insertCmd.ExecuteNonQueryAsync();

                    return Ok(new { status = true, message = "Petty Cash Category added successfully" });
                }
                else // ➝ UPDATE
                {
                    var updateQuery = @"
                UPDATE tbl_petty_cash_category 
                SET name=@name, description=@description 
                WHERE id=@id";
                    using var updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    updateCmd.Parameters.AddWithValue("@description", model.Description?.Trim() ?? "");
                    updateCmd.Parameters.AddWithValue("@id", model.Id);

                    int affected = await updateCmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Category not found" });

                    return Ok(new { status = true, message = "Petty Cash Category updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Petty Cash

        public IActionResult PettyCash()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SavePettyCashCard([FromBody] PettyCashCardRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (model.EmployeeId <= 0)
                return BadRequest(new { status = false, message = "Select employee first" });

            if (model.AccountId <= 0)
                return BadRequest(new { status = false, message = "Select account first" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Check duplicate employee
                string checkQuery = @"SELECT id FROM tbl_petty_cash_card WHERE name=@employeeId AND id<>@id LIMIT 1";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@employeeId", model.EmployeeId);
                    checkCmd.Parameters.AddWithValue("@id", model.Id); // 0 for insert
                    var exists = await checkCmd.ExecuteScalarAsync();
                    if (exists != null)
                        return BadRequest(new { status = false, message = "This employee already has a Petty Cash Card." });
                }

                if (model.Id == 0) // ➝ INSERT
                {
                    // 🔹 Generate new code
                    string newCode = "PC-0001";
                    var getLastCodeQuery = @"SELECT code FROM tbl_petty_cash_card 
                                     ORDER BY CAST(SUBSTRING_INDEX(code, '-', -1) AS UNSIGNED) DESC 
                                     LIMIT 1";
                    using (var getCmd = new MySqlCommand(getLastCodeQuery, conn))
                    {
                        var lastCodeObj = await getCmd.ExecuteScalarAsync();
                        if (lastCodeObj != null && !string.IsNullOrEmpty(lastCodeObj.ToString()))
                        {
                            int lastNum = int.Parse(lastCodeObj.ToString().Replace("PC-", ""));
                            newCode = "PC-" + (lastNum + 1).ToString("0000");
                        }
                    }

                    var insertQuery = @"
                INSERT INTO tbl_petty_cash_card (code, name, mobile, whatsapp_no, email, account_id)
                VALUES (@code, @name, @mobile, @whatsapp, @email, @accountId)";
                    using var insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@code", newCode);
                    insertCmd.Parameters.AddWithValue("@name", model.EmployeeId);
                    insertCmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
                    insertCmd.Parameters.AddWithValue("@whatsapp", model.WhatsappNo ?? "");
                    insertCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                    insertCmd.Parameters.AddWithValue("@accountId", model.AccountId);

                    await insertCmd.ExecuteNonQueryAsync();

                    return Ok(new { status = true, message = "Petty Cash Card created successfully" });
                }
                else // ➝ UPDATE
                {
                    var updateQuery = @"
                UPDATE tbl_petty_cash_card 
                SET code=@code, name=@name, mobile=@mobile, whatsapp_no=@whatsapp, email=@email, account_id=@accountId 
                WHERE id=@id";
                    using var updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);
                    updateCmd.Parameters.AddWithValue("@code", model.Code ?? "");
                    updateCmd.Parameters.AddWithValue("@name", model.EmployeeId);
                    updateCmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
                    updateCmd.Parameters.AddWithValue("@whatsapp", model.WhatsappNo ?? "");
                    updateCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                    updateCmd.Parameters.AddWithValue("@accountId", model.AccountId);

                    int affected = await updateCmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Petty Cash Card not found" });

                    return Ok(new { status = true, message = "Petty Cash Card updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetPettyCashCards()
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
         SELECT 
  pc.id,
  pc.code,
  pc.name           AS employeeId,      
  e.name            AS employeeName,
  pc.account_id     AS accountId,
  a.name            AS accountName,
  pc.mobile,
  pc.whatsapp_no,
  pc.email,
   e.code           AS empCode
FROM tbl_petty_cash_card pc
LEFT JOIN tbl_employee      e ON e.id = pc.name
LEFT JOIN tbl_coa_level_4  a ON a.id = pc.account_id
ORDER BY pc.id ASC;
";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();
                int sn = 1;
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        sn = sn++,
                        id = reader.GetInt32("id"),
                        code = reader["code"].ToString(),
                        employeeId = reader["employeeId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["employeeId"]),
                        employeeName = reader["employeeName"]?.ToString() ?? "",
                        accountId = reader["accountId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["accountId"]),
                        accountName = reader["accountName"]?.ToString() ?? "",
                        mobile = reader["mobile"]?.ToString() ?? "",
                        whatsapp_no = reader["whatsapp_no"]?.ToString() ?? "",
                        email = reader["email"]?.ToString() ?? "",
                        empCode = reader.GetInt32("empCode")
                       
                    });
                }


                return Ok(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePettyCashCard(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid Petty Cash Card ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // First check if the card is used in petty cash vouchers
                string checkQuery = @"
            SELECT id 
            FROM tbl_petty_cash 
            WHERE employee_id IN (SELECT c.name FROM tbl_petty_cash_card c WHERE c.id = @id)";

                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    using var reader = await checkCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Cannot delete this Petty Cash Card because it is referenced in Petty Cash Vouchers."
                        });
                    }
                }

                // If not used, delete
                string deleteQuery = "DELETE FROM tbl_petty_cash_card WHERE id = @id";
                using (var deleteCmd = new MySqlCommand(deleteQuery, conn))
                {
                    deleteCmd.Parameters.AddWithValue("@id", id);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                // optional: log audit here if needed
                return Ok(new { status = true, message = "Petty Cash Card deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Petty Cash Voucher

        public IActionResult PettyCashVoucher()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SavePettyCashVoucher([FromBody] PettyCashVoucherRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            // --- Equivalent to chkRequireData() ---
            if (model.CashAccountId <= 0)
                return BadRequest(new { status = false, message = "Please select a petty cash account" });
            if (model.EmployeeId <= 0)
                return BadRequest(new { status = false, message = "Please select an employee" });
            if (model.Total <= 0)
                return BadRequest(new { status = false, message = "Please enter a valid total amount" });
            if (model.VoucherDate == DateTime.MinValue)
                return BadRequest(new { status = false, message = "Invalid voucher date" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // --- Generate next Petty Cash Code ---
                string newCode = "PC-0001";
                using (var cmdLast = new MySqlCommand("SELECT MAX(CAST(SUBSTRING(code, 4) AS UNSIGNED)) FROM tbl_petty_cash", conn))
                {
                    var lastCode = await cmdLast.ExecuteScalarAsync();
                    if (lastCode != DBNull.Value && lastCode != null)
                    {
                        int nextCode = Convert.ToInt32(lastCode) + 1;
                        newCode = "PC-" + nextCode.ToString("D4");
                    }
                }

                int pettyCashId = model.Id;
                string pettyCashCode = model.Code ?? newCode;

                // --- INSERT OR UPDATE MAIN PETTY CASH VOUCHER ---
                if (model.Id == 0)
                {
                    string insertQuery = @"
                INSERT INTO tbl_petty_cash 
                    (code, voucher_date, cash_account_id, employee_id, notes, total, created_by)
                VALUES 
                    (@code, @voucher_date, @cash_account_id, @employee_id, @notes, @total, @created_by);
                SELECT LAST_INSERT_ID();";

                    using var cmdInsert = new MySqlCommand(insertQuery, conn);
                    cmdInsert.Parameters.AddWithValue("@code", pettyCashCode);
                    cmdInsert.Parameters.AddWithValue("@voucher_date", model.VoucherDate);
                    cmdInsert.Parameters.AddWithValue("@cash_account_id", model.CashAccountId);
                    cmdInsert.Parameters.AddWithValue("@employee_id", model.EmployeeId);
                    cmdInsert.Parameters.AddWithValue("@notes", model.Notes ?? "");
                    cmdInsert.Parameters.AddWithValue("@total", model.Total);
                    cmdInsert.Parameters.AddWithValue("@created_by", userId);

                    pettyCashId = Convert.ToInt32(await cmdInsert.ExecuteScalarAsync());
                }
                else
                {
                    // --- UPDATE ---
                    string updateQuery = @"
                UPDATE tbl_petty_cash 
                SET code=@code, voucher_date=@voucher_date, cash_account_id=@cash_account_id, 
                    employee_id=@employee_id, total=@total, notes=@notes 
                WHERE id=@id;";

                    using var cmdUpdate = new MySqlCommand(updateQuery, conn);
                    cmdUpdate.Parameters.AddWithValue("@id", model.Id);
                    cmdUpdate.Parameters.AddWithValue("@code", pettyCashCode);
                    cmdUpdate.Parameters.AddWithValue("@voucher_date", model.VoucherDate);
                    cmdUpdate.Parameters.AddWithValue("@cash_account_id", model.CashAccountId);
                    cmdUpdate.Parameters.AddWithValue("@employee_id", model.EmployeeId);
                    cmdUpdate.Parameters.AddWithValue("@total", model.Total);
                    cmdUpdate.Parameters.AddWithValue("@notes", model.Notes ?? "");
                    await cmdUpdate.ExecuteNonQueryAsync();

                    // Delete old petty cash details
                    using (var cmdDel = new MySqlCommand("DELETE FROM tbl_petty_cash_details WHERE petty_cash_id=@id", conn))
                    {
                        cmdDel.Parameters.AddWithValue("@id", model.Id);
                        await cmdDel.ExecuteNonQueryAsync();
                    }

                    // Delete old transaction entries
                    using (var cmdDelTrx = new MySqlCommand("DELETE FROM tbl_transaction WHERE t_type=@tType AND transaction_id=@id", conn))
                    {
                        cmdDelTrx.Parameters.AddWithValue("@tType", "PettyCash");
                        cmdDelTrx.Parameters.AddWithValue("@id", model.Id);
                        await cmdDelTrx.ExecuteNonQueryAsync();
                    }

                    // Delete cost center transactions
                    using (var cmdDelCC = new MySqlCommand("DELETE FROM tbl_cost_center_transaction WHERE type=@type AND ref_id=@id", conn))
                    {
                        cmdDelCC.Parameters.AddWithValue("@type", "PettyCash");
                        cmdDelCC.Parameters.AddWithValue("@id", model.Id);
                        await cmdDelCC.ExecuteNonQueryAsync();
                    }
                }

                // --- INSERT PETTY CASH DETAILS ---
                if (model.Details != null && model.Details.Any())
                {
                    foreach (var d in model.Details)
                    {
                        if (d.Amount == null || d.Amount <= 0)
                            continue;

                        // Defensive null/empty handling
                        var entryDate = d.EntryDate == DateTime.MinValue ? DateTime.Now : d.EntryDate;
                        var humId = string.IsNullOrWhiteSpace(d.HumId) ? "0" : d.HumId;
                        var humName = d.HumName ?? "";
                        var refId = string.IsNullOrWhiteSpace(d.RefId) ? "0" : d.RefId;
                        var costCenterId = string.IsNullOrWhiteSpace(d.CostCenterId) ? "0" : d.CostCenterId;
                        var description = d.Description ?? "";
                        var category = string.IsNullOrWhiteSpace(d.Category) ? "0" : d.Category;
                        var note = d.Note ?? "";
                        var amount = d.Amount ?? 0;

                        string insertDetailQuery = @"
                    INSERT INTO tbl_petty_cash_details  
                        (entry_date, petty_cash_id, hum_id, hum_name, ref_id, cost_center_id, amount, description, category, note)
                    VALUES
                        (@entry_date, @petty_cash_id, @hum_id, @hum_name, @ref_id, @cost_center_id, @amount, @description, @category, @note);";

                        using var cmdDetail = new MySqlCommand(insertDetailQuery, conn);
                        cmdDetail.Parameters.AddWithValue("@entry_date", entryDate);
                        cmdDetail.Parameters.AddWithValue("@petty_cash_id", pettyCashId);
                        cmdDetail.Parameters.AddWithValue("@hum_id", humId);
                        cmdDetail.Parameters.AddWithValue("@hum_name", humName);
                        cmdDetail.Parameters.AddWithValue("@ref_id", refId);
                        cmdDetail.Parameters.AddWithValue("@cost_center_id", costCenterId);
                        cmdDetail.Parameters.AddWithValue("@amount", amount);
                        cmdDetail.Parameters.AddWithValue("@description", description);
                        cmdDetail.Parameters.AddWithValue("@category", category);
                        cmdDetail.Parameters.AddWithValue("@note", note);

                        await cmdDetail.ExecuteNonQueryAsync();
                    }
                }

                // --- SUCCESS RESPONSE ---
                string message = model.Id == 0
                    ? "The Petty Cash Voucher has been paid successfully"
                    : "The Petty Cash Voucher has been updated successfully";

                return Ok(new
                {
                    status = true,
                    message,
                    pettyCashId,
                    code = pettyCashCode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPettyCashVoucher(int id)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Get main voucher info
                string voucherQuery = @"
            SELECT pc.id, pc.code, pc.voucher_date, pc.cash_account_id, pc.employee_id, pc.total
            FROM tbl_petty_cash pc
            WHERE pc.id = @id";

                using var voucherCmd = new MySqlCommand(voucherQuery, conn);
                voucherCmd.Parameters.AddWithValue("@id", id);

                object voucherData = null;

                using (var reader = await voucherCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        voucherData = new
                        {
                            id = reader.GetInt32("id"),
                            code = reader["code"].ToString(),
                            voucherDate = reader["voucher_date"] == DBNull.Value ? null : reader.GetDateTime("voucher_date").ToString("yyyy-MM-dd"),
                            cashAccountId = reader["cash_account_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["cash_account_id"]),
                            employeeId = reader["employee_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["employee_id"]),
                            total = reader["total"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["total"])
                        };
                    }
                }

                if (voucherData == null)
                    return NotFound(new { status = false, message = "Voucher not found" });

                // Get voucher detail data
                string detailsQuery = @"
            SELECT dt.id, dt.petty_cash_id, dt.entry_date, dt.ref_id, dt.hum_id, dt.hum_name, dt.category,
                   dt.cost_center_id, dt.description, dt.amount, dt.project_id, dt.note,
            c.name AS categoryName
            FROM tbl_petty_cash_details dt
                LEFT JOIN tbl_petty_cash_category c ON c.id = dt.category
            WHERE dt.petty_cash_id = @id
            ORDER BY dt.entry_date";

                using var detailsCmd = new MySqlCommand(detailsQuery, conn);
                detailsCmd.Parameters.AddWithValue("@id", id);

                var detailsList = new List<object>();
                decimal totalAmount = 0;

                using (var reader = await detailsCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        decimal amount = reader["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["amount"]);
                        totalAmount += amount;

                        detailsList.Add(new
                        {
                            id = reader.GetInt32("id"),
                            pettyCashId = reader["petty_cash_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["petty_cash_id"]),
                            entryDate = reader["entry_date"] == DBNull.Value ? null : Convert.ToDateTime(reader["entry_date"]).ToString("yyyy-MM-dd"),
                            refId = reader["ref_id"]?.ToString() ?? "",
                            humId = reader["hum_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["hum_id"]),
                            humName = reader["hum_name"]?.ToString() ?? "",
                            //category = reader["category"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["category"]),
                            category = reader["categoryName"]?.ToString() ?? "",
                            costCenterId = reader["cost_center_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["cost_center_id"]),
                            description = reader["description"]?.ToString() ?? "",
                            amount = amount,
                            projectId = reader["project_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["project_id"]),
                            note = reader["note"]?.ToString() ?? ""
                        });
                    }
                }

                // Return voucher + details + calculated total
                return Ok(new
                {
                    status = true,
                    data = new
                    {
                        voucher = voucherData,
                        details = detailsList,
                        totalAmount
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePettyCashVoucher([FromBody] int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid ID" });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Unauthorized(new { status = false, message = "User not logged in" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                using var transaction = await conn.BeginTransactionAsync();

                // Backup main voucher
                var dtVoucher = new DataTable();
                using (var cmd = new MySqlCommand("SELECT * FROM tbl_petty_cash WHERE id=@id", conn, transaction))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using var reader = await cmd.ExecuteReaderAsync();
                    dtVoucher.Load(reader);
                }

                foreach (DataRow row in dtVoucher.Rows)
                {
                    using var cmd = new MySqlCommand(
                        "INSERT INTO tbl_deleted_records (table_name, record_data, deleted_by) VALUES (@table, @data, @user)",
                        conn, transaction
                    );
                    cmd.Parameters.AddWithValue("@table", "tbl_petty_cash");
                    cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(row));
                    cmd.Parameters.AddWithValue("@user", userId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Delete related records
                string[] deleteQueries =
                {
            "DELETE FROM tbl_transaction WHERE t_type=@type AND transaction_id=@id",
            "DELETE FROM tbl_cost_center_transaction WHERE type=@type AND ref_id=@id",
            "DELETE FROM tbl_petty_cash_details WHERE Petty_Cash_id=@id",
            "DELETE FROM tbl_petty_cash WHERE id=@id"
        };

                foreach (var query in deleteQueries)
                {
                    using var cmd = new MySqlCommand(query, conn, transaction);
                    cmd.Parameters.AddWithValue("@id", id);
                    if (query.Contains("@type"))
                        cmd.Parameters.AddWithValue("@type", "PettyCash");

                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return Ok(new { status = true, message = "Petty Cash voucher deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPettyCashVoucherJournal(int id)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // --- STEP 1: Get Voucher Header ---
                string voucherQuery = @"SELECT * FROM tbl_petty_cash WHERE id = @id;";
                using var voucherCmd = new MySqlCommand(voucherQuery, conn);
                voucherCmd.Parameters.AddWithValue("@id", id);

                using var reader = await voucherCmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                    return Ok(new { status = false, message = "Voucher not found" });

                string code = reader["code"].ToString();
                DateTime voucherDate = Convert.ToDateTime(reader["voucher_date"]);
                string cashAccountId = reader["cash_account_id"].ToString();
                decimal total = Convert.ToDecimal(reader["total"]);
                bool isOldBill = reader["status"].ToString().ToLower() == "1";
                int pettyCashId = Convert.ToInt32(cashAccountId);
                reader.Close();

                // --- STEP 2: Get Employee Name using employee_id from petty cash ---
                int pettyCashAccountId = 0;
                string employeeName = "";

                // ✅ Get employee_id directly from petty cash table
                int employeeId = 0;
                string employeeQuery = @"SELECT employee_id FROM tbl_petty_cash WHERE id = @id;";
                using (var empIdCmd = new MySqlCommand(employeeQuery, conn))
                {
                    empIdCmd.Parameters.AddWithValue("@id", id);
                    var empIdObj = await empIdCmd.ExecuteScalarAsync();
                    if (empIdObj != null && empIdObj != DBNull.Value)
                        employeeId = Convert.ToInt32(empIdObj);
                }

                // ✅ Now get employee name from tbl_employee using that employee_id
                if (employeeId > 0)
                {
                    string empNameQuery = "SELECT name FROM tbl_employee WHERE id = @id;";
                    using var empNameCmd = new MySqlCommand(empNameQuery, conn);
                    empNameCmd.Parameters.AddWithValue("@id", employeeId);
                    var empNameObj = await empNameCmd.ExecuteScalarAsync();
                    employeeName = empNameObj?.ToString() ?? "";
                }

                // ✅ Get petty cash account id from petty cash card
                string pettyCashAccountQuery = @"SELECT account_id FROM tbl_petty_cash_card WHERE id = @id;";
                using var pettyCashAccountCmd = new MySqlCommand(pettyCashAccountQuery, conn);
                pettyCashAccountCmd.Parameters.AddWithValue("@id", Convert.ToInt32(cashAccountId));
                var pettyCashAccountIdObj = await pettyCashAccountCmd.ExecuteScalarAsync();
                if (pettyCashAccountIdObj != null)
                    pettyCashAccountId = Convert.ToInt32(pettyCashAccountIdObj);

                // --- STEP 3: Load Petty Cash Details ---
                string detailsQuery = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY entry_date) AS SN,
                dt.id, dt.petty_cash_id, dt.entry_date, dt.ref_id, dt.hum_id, dt.category,
                dt.cost_center_id, dt.description, dt.amount, dt.project_id, dt.note
            FROM tbl_petty_cash_details dt
            WHERE dt.petty_cash_id = @id;";

                using var detailsCmd = new MySqlCommand(detailsQuery, conn);
                detailsCmd.Parameters.AddWithValue("@id", id);

                using var detailsReader = await detailsCmd.ExecuteReaderAsync();

                var detailsList = new List<object>();
                decimal totalAmount = 0;

                while (await detailsReader.ReadAsync())
                {
                    int detailId = Convert.ToInt32(detailsReader["id"]);
                    string description = detailsReader["description"]?.ToString() ?? "";
                    decimal amount = detailsReader["amount"] != DBNull.Value ? Convert.ToDecimal(detailsReader["amount"]) : 0;
                    totalAmount += amount;

                    string costCenterId = detailsReader["cost_center_id"]?.ToString();
                    string humId = detailsReader["hum_id"]?.ToString();
                    string note = detailsReader["note"]?.ToString();

                    detailsList.Add(new
                    {
                        SN = detailsReader["SN"],
                        id = detailId,
                        description,
                        amount,
                        costCenterId,
                        humId,
                        note
                    });
                }
                detailsReader.Close();

                // --- STEP 4: Match Account from tbl_transaction ---
                foreach (dynamic detail in detailsList.ToList())
                {
                    string transactionQuery = @"
                SELECT account_id 
                FROM tbl_transaction 
                WHERE transaction_id = @voucherId 
                  AND t_type = 'PettyCash' 
                  AND account_id != @cashId 
                  AND description = @description;";
                    using var trCmd = new MySqlCommand(transactionQuery, conn);
                    trCmd.Parameters.AddWithValue("@voucherId", id);
                    trCmd.Parameters.AddWithValue("@cashId", pettyCashAccountId);
                    trCmd.Parameters.AddWithValue("@description", detail.description);

                    var accountIdObj = await trCmd.ExecuteScalarAsync();
                    int accountId = accountIdObj != null ? Convert.ToInt32(accountIdObj) : 0;

                    detail.GetType().GetProperty("AccountId")?.SetValue(detail, accountId);
                }

                // --- STEP 5: Prepare Final Output ---
                var result = new
                {
                    header = new
                    {
                        id,
                        code,
                        date = voucherDate.ToString("yyyy-MM-dd"),
                        cashAccountId,
                        pettyCashId,
                        pettyCashAccountId,
                        employeeName,
                        total,
                        isOldBill
                    },
                    details = detailsList,
                    totalAmount
                };

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPettyCashCardsByEmployee()
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                //                string query = @"
                //      SELECT 
                //    pc.id,
                //    pc.code,
                //    pc.employee_id AS employeeId,   
                //    e.name AS employeeName
                //FROM tbl_petty_cash pc
                //LEFT JOIN tbl_employee e ON e.id = pc.employee_id
                //ORDER BY pc.id ASC;
                // ";

                string query = @" select Id from tbl_petty_cash";


                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();
                int sn = 1;
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        sn = sn++,
                        id = reader.GetInt32("id"),
                    });
                }


                return Ok(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SavePettyCashVoucherJournal([FromBody] PettyCashVoucherRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid payload" });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Unauthorized(new { status = false, message = "User not logged in" });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName")
                           ?? _config.GetConnectionString("DefaultDatabase")
            };

            await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                bool isOldBill = false;
                int pettyCashId = model.Id;
                string code = model.Code ?? "";
                DateTime voucherDate = model.VoucherDate;
                decimal total = model.Total;

                // ---------------------------
                // 1️⃣ Check if voucher exists
                // ---------------------------
                if (pettyCashId > 0)
                {
                    // Check by ID
                    var checkCmd = new MySqlCommand("SELECT * FROM tbl_petty_cash WHERE id=@id;", conn, tx);
                    checkCmd.Parameters.AddWithValue("@id", pettyCashId);
                    await using (var reader = await checkCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            code = reader["code"].ToString();
                            voucherDate = Convert.ToDateTime(reader["voucher_date"]);
                            pettyCashId = Convert.ToInt32(reader["id"]);
                            isOldBill = true;
                        }
                    }
                }
                else
                {
                    // Check by code to prevent duplicate insertion
                    var codeCheck = new MySqlCommand("SELECT * FROM tbl_petty_cash WHERE code=@code;", conn, tx);
                    codeCheck.Parameters.AddWithValue("@code", code);
                    await using (var reader = await codeCheck.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            pettyCashId = Convert.ToInt32(reader["id"]);
                            voucherDate = Convert.ToDateTime(reader["voucher_date"]);
                            isOldBill = true;
                        }
                    }
                }

                // ---------------------------
                // 2️⃣ Validation
                // ---------------------------
                if (total <= 0)
                {
                    await tx.RollbackAsync();
                    return Ok(new { status = false, message = "Please enter PettyCash amount." });
                }

                if (model.Details == null || model.Details.Count == 0)
                {
                    await tx.RollbackAsync();
                    return Ok(new { status = false, message = "No transaction details found." });
                }

                foreach (var d in model.Details)
                {
                    if (d.Amount != null && d.Amount > 0 && string.IsNullOrEmpty(d.Category))
                    {
                        await tx.RollbackAsync();
                        return Ok(new { status = false, message = "Please choose an account for each detail line." });
                    }
                }

                // ---------------------------
                // 3️⃣ Delete old transactions if updating
                // ---------------------------
                if (isOldBill)
                {
                    var delTrans = new MySqlCommand("DELETE FROM tbl_transaction WHERE transaction_id=@id AND type='PettyCash';", conn, tx);
                    delTrans.Parameters.AddWithValue("@id", pettyCashId);
                    await delTrans.ExecuteNonQueryAsync();

                    var delCost = new MySqlCommand("DELETE FROM tbl_cost_center_transaction WHERE ref_id=@id AND type='PettyCash';", conn, tx);
                    delCost.Parameters.AddWithValue("@id", pettyCashId);
                    await delCost.ExecuteNonQueryAsync();

                    // Update main voucher instead of inserting
                    var updateCmd = new MySqlCommand(@"
                UPDATE tbl_petty_cash 
                SET voucher_date=@voucher_date, cash_account_id=@cash_account_id, employee_id=@employee_id, total=@total, status='1'
                WHERE id=@id;", conn, tx);
                    updateCmd.Parameters.AddWithValue("@voucher_date", voucherDate);
                    updateCmd.Parameters.AddWithValue("@cash_account_id", model.CashAccountId);
                    updateCmd.Parameters.AddWithValue("@employee_id", model.EmployeeId);
                    updateCmd.Parameters.AddWithValue("@total", total);
                    updateCmd.Parameters.AddWithValue("@id", pettyCashId);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new voucher
                    var insertCmd = new MySqlCommand(@"
                INSERT INTO tbl_petty_cash (code, voucher_date, cash_account_id, employee_id, total, status)
                VALUES (@code,@voucher_date,@cash_account_id,@employee_id,@total,'1');", conn, tx);
                    insertCmd.Parameters.AddWithValue("@code", code);
                    insertCmd.Parameters.AddWithValue("@voucher_date", voucherDate);
                    insertCmd.Parameters.AddWithValue("@cash_account_id", model.CashAccountId);
                    insertCmd.Parameters.AddWithValue("@employee_id", model.EmployeeId);
                    insertCmd.Parameters.AddWithValue("@total", total);
                    await insertCmd.ExecuteNonQueryAsync();

                    pettyCashId = (int)insertCmd.LastInsertedId;
                }

                // ---------------------------
                // 4️⃣ Insert transaction lines
                // ---------------------------
                foreach (var d in model.Details)
                {
                    if (d.Amount == null || d.Amount <= 0) continue;

                    string costCenterId = d.CostCenterId ?? "0";
                    string accountId = d.Category ?? "0";
                    string description = d.Description ?? "";
                    string humId = d.HumId ?? "0";
                    string amount = d.Amount?.ToString() ?? "0";

                    // Cost center debit
                    var costCmd = new MySqlCommand(@"
                INSERT INTO tbl_cost_center_transaction 
                (type,date,ref_id,debit,credit,description,cost_center_id)
                VALUES (@type,@date,@ref,@debit,@credit,@description,@cost_center_id);", conn, tx);
                    costCmd.Parameters.AddWithValue("@type", "PettyCash");
                    costCmd.Parameters.AddWithValue("@date", voucherDate);
                    costCmd.Parameters.AddWithValue("@ref", pettyCashId.ToString());
                    costCmd.Parameters.AddWithValue("@debit", amount);
                    costCmd.Parameters.AddWithValue("@credit", "0");
                    costCmd.Parameters.AddWithValue("@description", "PettyCash Entry");
                    costCmd.Parameters.AddWithValue("@cost_center_id", costCenterId);
                    await costCmd.ExecuteNonQueryAsync();

                    // Journal debit
                    var journalCmd = new MySqlCommand(@"
                INSERT INTO tbl_transaction 
                (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no)
                VALUES (@date,@accountId,@debit,@credit,@transactionId,@humId,@tType,@type,@description,@createdBy,@createdDate,0,@voucherNo);", conn, tx);
                    journalCmd.Parameters.AddWithValue("@date", voucherDate);
                    journalCmd.Parameters.AddWithValue("@accountId", accountId);
                    journalCmd.Parameters.AddWithValue("@debit", amount);
                    journalCmd.Parameters.AddWithValue("@credit", "0");
                    journalCmd.Parameters.AddWithValue("@transactionId", pettyCashId.ToString());
                    journalCmd.Parameters.AddWithValue("@humId", humId);
                    journalCmd.Parameters.AddWithValue("@tType", "Petty Cash");
                    journalCmd.Parameters.AddWithValue("@type", "PettyCash");
                    journalCmd.Parameters.AddWithValue("@description", description);
                    journalCmd.Parameters.AddWithValue("@createdBy", model.EmployeeId);
                    journalCmd.Parameters.AddWithValue("@createdDate", DateTime.Now);
                    journalCmd.Parameters.AddWithValue("@voucherNo", code);
                    await journalCmd.ExecuteNonQueryAsync();
                }

                // ---------------------------
                // 5️⃣ Insert total credit line
                // ---------------------------
                var totalCmd = new MySqlCommand(@"
            INSERT INTO tbl_transaction
            (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no)
            VALUES (@date,@accountId,@debit,@credit,@transactionId,@humId,@tType,@type,@description,@createdBy,@createdDate,0,@voucherNo);", conn, tx);
                totalCmd.Parameters.AddWithValue("@date", voucherDate);
                totalCmd.Parameters.AddWithValue("@accountId", model.CashAccountId.ToString());
                totalCmd.Parameters.AddWithValue("@debit", "0");
                totalCmd.Parameters.AddWithValue("@credit", total.ToString());
                totalCmd.Parameters.AddWithValue("@transactionId", pettyCashId.ToString());
                totalCmd.Parameters.AddWithValue("@humId", "0");
                totalCmd.Parameters.AddWithValue("@tType", "Petty Cash");
                totalCmd.Parameters.AddWithValue("@type", "PettyCash");
                totalCmd.Parameters.AddWithValue("@description", $"PETTY CASH NO.{code}");
                totalCmd.Parameters.AddWithValue("@createdBy", model.EmployeeId);
                totalCmd.Parameters.AddWithValue("@createdDate", DateTime.Now);
                totalCmd.Parameters.AddWithValue("@voucherNo", code);
                await totalCmd.ExecuteNonQueryAsync();

                await tx.CommitAsync();

                return Ok(new
                {
                    status = true,
                    message = isOldBill ? "The PettyCash Journal Updated!" : "The PettyCash Journal Created!"
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Petty Cash Log

        [HttpGet]
        public async Task<IActionResult> GetEmployeePettyCash(string empCode)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                //bool enableApproval = HttpContext.Session.GetInt32("EnableApproval") == 1;

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query;

                //if (!enableApproval)
                //{
                //    query = @"
                //SELECT 
                //    d.id,
                //    d.date AS `Date`,
                //    ps.code,
                //    coa.name AS `Account`,
                //    d.total AS `TotalWithVAT`,
                //    cat.name AS `Category`,
                //    d.note
                //FROM 
                //    tbl_petty_cash_submition_details d
                //INNER JOIN 
                //    tbl_petty_cash_submition ps ON d.petty_id = ps.id
                //INNER JOIN 
                //    tbl_employee e ON ps.name = e.id
                //INNER JOIN tbl_petty_cash_category cat ON d.category = cat.id
                //INNER JOIN tbl_coa_level_4 coa ON d.account_id = coa.id
                //WHERE e.code = @empCode;";
                //}
                //else
                //{
                    query = @"
                SELECT 
                    p.id,
                    p.code AS `Code`,
                    p.voucher_date AS `Date`,
                    p.total AS `Total`,
                    CASE 
                        WHEN p.status = 0 THEN 'Pending'
                        ELSE 'Confirmed'
                    END AS `Status`
                FROM tbl_petty_cash p
                INNER JOIN tbl_petty_cash_card c ON p.employee_id = c.name
                INNER JOIN tbl_employee e ON e.id = p.employee_id
                WHERE e.code = @empCode;";
                //}

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@empCode", empCode);

                var result = new List<object>();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    //if (!enableApproval)
                    //{
                    //    result.Add(new
                    //    {
                    //        id = reader["id"],
                    //        date = reader["Date"] == DBNull.Value ? null : Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                    //        code = reader["code"]?.ToString(),
                    //        account = reader["Account"]?.ToString(),
                    //        totalWithVAT = reader["TotalWithVAT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalWithVAT"]),
                    //        category = reader["Category"]?.ToString(),
                    //        note = reader["note"]?.ToString()
                    //    });
                    //}
                    //else
                    //{
                        result.Add(new
                        {
                            id = reader["id"],
                            code = reader["Code"]?.ToString(),
                            date = reader["Date"] == DBNull.Value ? null : Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                            total = reader["Total"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Total"]),
                            status = reader["Status"]?.ToString()
                        });
                    //}
                }

                if (result.Count == 0)
                    return Ok(new { status = true, data = new List<object>(), message = "No records found" });

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetEmployeePettyCashTransactions(string empCode)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                //bool enableApproval = HttpContext.Session.GetInt32("EnableApproval") == 1;

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                //if (!enableApproval)
                //{
                //    string query = @"
                //SELECT 
                //    d.id,
                //    d.date AS `Date`,
                //    ps.code,
                //    coa.name AS `Account`,
                //    d.total AS `TotalWithVAT`,
                //    cat.name AS `Category`,
                //    d.note
                //FROM 
                //    tbl_petty_cash_submition_details d
                //INNER JOIN 
                //    tbl_petty_cash_submition ps ON d.petty_id = ps.id
                //INNER JOIN 
                //    tbl_employee e ON ps.name = e.id
                //INNER JOIN tbl_petty_cash_category cat ON d.category = cat.id
                //INNER JOIN tbl_coa_level_4 coa ON d.account_id = coa.id
                //WHERE e.code = @empCode;";

                //    using var cmd = new MySqlCommand(query, conn);
                //    cmd.Parameters.AddWithValue("@empCode", empCode);

                //    var list = new List<object>();

                //    using var reader = await cmd.ExecuteReaderAsync();
                //    while (await reader.ReadAsync())
                //    {
                //        list.Add(new
                //        {
                //            id = reader["id"],
                //            date = reader["Date"] == DBNull.Value ? null : Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                //            code = reader["code"]?.ToString(),
                //            account = reader["Account"]?.ToString(),
                //            totalWithVAT = reader["TotalWithVAT"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["TotalWithVAT"]),
                //            category = reader["Category"]?.ToString(),
                //            note = reader["note"]?.ToString()
                //        });
                //    }

                //    return Ok(new { status = true, data = list });
                //}
                //else
                //{
                    string query = @"
                SELECT 
                    t.id,
                    t.Date,
                    t.voucher_no AS `No`,
                    t.type AS `Type`,
                    t.transaction_id AS `InvoiceId`,
                    coa.name AS `Description`,
                    t.debit AS `Debit`,
                    t.credit AS `Credit`
                FROM 
                    tbl_transaction t
                INNER JOIN tbl_coa_level_4 coa ON t.account_id = coa.id
                WHERE 
                    t.transaction_id IN (
                        SELECT pt.id 
                        FROM tbl_employee e
                        INNER JOIN tbl_petty_cash pt ON pt.employee_id = e.id
                        WHERE e.code = @empCode
                    )
                    AND t.state = 0
                    AND t.type = 'Petty Cash'
                ORDER BY t.Date, t.id;";

                    using var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@empCode", empCode);

                    var list = new List<object>();
                    decimal balance = 0;
                    int sn = 1;

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        decimal debit = reader["Debit"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Debit"]);
                        decimal credit = reader["Credit"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Credit"]);
                        balance += credit - debit;

                        list.Add(new
                        {
                            sn = sn++,
                            date = reader["Date"] == DBNull.Value ? null : Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd"),
                            no = string.IsNullOrEmpty(reader["No"]?.ToString()) ? $"PC-00{reader["id"]}" : reader["No"].ToString(),
                            type = reader["Type"]?.ToString(),
                            invoiceId = reader["InvoiceId"]?.ToString(),
                            description = reader["Description"]?.ToString(),
                            debit,
                            credit,
                            balance
                        });
                    }

                    return Ok(new { status = true, data = list });
                //}
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Prepaid Expense

        public IActionResult PrepaidExpense()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPrepaidExpenses()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"SELECT * FROM tbl_prepaid_expense ORDER BY id DESC";
                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var expenses = new List<object>();
                while (await reader.ReadAsync())
                {
                    expenses.Add(new
                    {
                        Id = reader.GetInt32("id"),
                        Code = reader["code"].ToString(),
                        Name = reader["name"].ToString(),
                        CategoryId = reader["category_id"],
                        DebitAccountId = reader["debit_account_id"],
                        CreditAccountId = reader["credit_account_id"],
                        StartDate = reader.GetDateTime("start_date").ToString("yyyy-MM-dd"),
                        EndDate = reader.GetDateTime("end_date").ToString("yyyy-MM-dd"),
                        Amount = reader.GetDecimal("amount"),
                        Fee = reader.GetDecimal("fee"),
                        Total = reader.GetDecimal("total")
                    });
                }

                return Ok(new { status = true, data = expenses });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePrepaidExpense([FromBody] PrepaidExpenseRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Enter Name First" });

            if (model.Total <= 0)
                return BadRequest(new { status = false, message = "Total Amount can't be null or zero" });

            if (model.StartDate == null || model.EndDate == null)
                return BadRequest(new { status = false, message = "Check Agreement Dates" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // If new record, generate code
                string code = model.Id == 0 ? await GenerateNextPrepaidCode(conn) : model.Code;

                if (model.Id == 0)
                {
                    // Insert new prepaid expense
                    string insertQuery = @"
                INSERT INTO tbl_prepaid_expense
                (code, name, category_id, debit_account_id, credit_account_id, start_date, end_date, amount, fee, total, created_by, created_date)
                VALUES
                (@code, @name, @category_id, @debit_account_id, @credit_account_id, @start_date, @end_date, @amount, @fee, @total, @created_by, @created_date);
                SELECT LAST_INSERT_ID();";

                    using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@category_id", model.CategoryId);
                    cmd.Parameters.AddWithValue("@debit_account_id", model.DebitAccountId);
                    cmd.Parameters.AddWithValue("@credit_account_id", model.CreditAccountId);
                    cmd.Parameters.AddWithValue("@start_date", model.StartDate);
                    cmd.Parameters.AddWithValue("@end_date", model.EndDate);
                    cmd.Parameters.AddWithValue("@amount", model.Amount);
                    cmd.Parameters.AddWithValue("@fee", model.Fee);
                    cmd.Parameters.AddWithValue("@total", model.Total);
                    cmd.Parameters.AddWithValue("@created_by", userId);
                    cmd.Parameters.AddWithValue("@created_date", DateTime.Now);

                    var insertedId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    // Insert Journal Entries
                    await InsertJournal(conn, insertedId, model, userId);

                    return Ok(new { status = true, message = "Prepaid Expense created successfully", id = insertedId });
                }
                else
                {
                    // Update existing prepaid expense
                    string updateQuery = @"
                UPDATE tbl_prepaid_expense
                SET name=@name, category_id=@category_id, debit_account_id=@debit_account_id, credit_account_id=@credit_account_id,
                    start_date=@start_date, end_date=@end_date, amount=@amount, fee=@fee, total=@total,
                    modified_by=@modified_by, modified_date=@modified_date
                WHERE id=@id;";

                    using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@category_id", model.CategoryId);
                    cmd.Parameters.AddWithValue("@debit_account_id", model.DebitAccountId);
                    cmd.Parameters.AddWithValue("@credit_account_id", model.CreditAccountId);
                    cmd.Parameters.AddWithValue("@start_date", model.StartDate);
                    cmd.Parameters.AddWithValue("@end_date", model.EndDate);
                    cmd.Parameters.AddWithValue("@amount", model.Amount);
                    cmd.Parameters.AddWithValue("@fee", model.Fee);
                    cmd.Parameters.AddWithValue("@total", model.Total);
                    cmd.Parameters.AddWithValue("@modified_by", userId);
                    cmd.Parameters.AddWithValue("@modified_date", DateTime.Now);

                    int affected = await cmd.ExecuteNonQueryAsync();

                    if (affected == 0)
                        return NotFound(new { status = false, message = "Prepaid Expense not found" });

                    // Delete old transactions and insert updated journal entries
                    using var delCmd = new MySqlCommand("DELETE FROM tbl_transaction WHERE transaction_id=@id AND type='Prepaid Expense'", conn);
                    delCmd.Parameters.AddWithValue("@id", model.Id);
                    await delCmd.ExecuteNonQueryAsync();

                    await InsertJournal(conn, model.Id, model, userId);

                    return Ok(new { status = true, message = "Prepaid Expense updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateNextPrepaidCode(MySqlConnection conn)
        {
            string query = "SELECT MAX(CAST(code AS UNSIGNED)) AS lastCode FROM tbl_prepaid_expense";
            using var cmd = new MySqlCommand(query, conn);
            var result = await cmd.ExecuteScalarAsync();
            int code = (result != DBNull.Value) ? Convert.ToInt32(result) + 1 : 1;
            return code.ToString("D5");
        }

        private async Task InsertJournal(MySqlConnection conn, int pId, PrepaidExpenseRequest model, int userId)
        {
            DateTime startDate = model.StartDate;
            DateTime endDate = model.EndDate;
            decimal totalAmount = model.Total;
            int totalDays = (endDate - startDate).Days + 1;
            DateTime currentDate = startDate;

            int lastDayOfFirstMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
            DateTime lastDateOfFirstMonth = new DateTime(currentDate.Year, currentDate.Month, lastDayOfFirstMonth);
            if (lastDateOfFirstMonth > endDate) lastDateOfFirstMonth = endDate;

            int daysInFirstMonth = (lastDateOfFirstMonth - startDate).Days + 1;
            decimal firstMonthAmount = Math.Round((totalAmount / totalDays) * daysInFirstMonth, 3);

            await CommonInsert.InsertTransactionEntryAsync(conn, lastDateOfFirstMonth,
                model.DebitAccountId.ToString(), firstMonthAmount.ToString(), "0", pId.ToString(), "0", "Prepaid Expense",
                model.Name + " - Prepaid Expense No. " + pId, userId, DateTime.Now);

            await CommonInsert.InsertTransactionEntryAsync(conn, lastDateOfFirstMonth,
                model.CreditAccountId.ToString(), "0", firstMonthAmount.ToString(), pId.ToString(), "0", "Prepaid Expense",
                model.Name + " - Prepaid Expense No. " + pId, userId, DateTime.Now);

            currentDate = lastDateOfFirstMonth.AddDays(1);

            while (currentDate <= endDate)
            {
                int lastDay = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);
                DateTime lastDateOfMonth = new DateTime(currentDate.Year, currentDate.Month, lastDay);
                if (lastDateOfMonth > endDate) lastDateOfMonth = endDate;

                int daysInMonth = (lastDateOfMonth - currentDate).Days + 1;
                decimal monthlyAmount = Math.Round((totalAmount / totalDays) * daysInMonth, 2);

                await CommonInsert.InsertTransactionEntryAsync(conn, lastDateOfMonth,
                    model.DebitAccountId.ToString(), monthlyAmount.ToString(), "0", pId.ToString(), "0", "Prepaid Expense",
                    model.Name + " - Prepaid Expense No. " + pId, userId, DateTime.Now);

                await CommonInsert.InsertTransactionEntryAsync(conn, lastDateOfMonth,
                    model.CreditAccountId.ToString(), "0", monthlyAmount.ToString(), pId.ToString(), "0", "Prepaid Expense",
                    model.Name + " - Prepaid Expense No. " + pId, userId, DateTime.Now);

                currentDate = lastDateOfMonth.AddDays(1);
            }
        }

        public static class CommonInsert
{
    public static async Task InsertTransactionEntryAsync(
        MySqlConnection conn,
        DateTime date,
        string accountId,
        string debit,
        string credit,
        string transactionId,
        string humId,
        string type,
        string description,
        int createdBy,
        DateTime createdDate)
    {
        string query = @"
            INSERT INTO tbl_transaction 
            (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state)
            VALUES 
            (@date, @accountId, @debit, @credit, @transactionId, @hum_id, @tType, @type, @description, @createdBy, @createdDate, 0);";

        using var cmd = new MySqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@date", date);
        cmd.Parameters.AddWithValue("@accountId", accountId);
        cmd.Parameters.AddWithValue("@debit", debit);
        cmd.Parameters.AddWithValue("@credit", credit);
        cmd.Parameters.AddWithValue("@transactionId", transactionId);
        cmd.Parameters.AddWithValue("@hum_id", humId);
        cmd.Parameters.AddWithValue("@tType", ""); // same as original
        cmd.Parameters.AddWithValue("@type", type?.Trim() ?? "");
        cmd.Parameters.AddWithValue("@description", description);
        cmd.Parameters.AddWithValue("@createdBy", createdBy);
        cmd.Parameters.AddWithValue("@createdDate", createdDate);

        await cmd.ExecuteNonQueryAsync();
    }
}

        [HttpGet]
        public async Task<IActionResult> GetPrepaidJournalEntries(int prepaidId)
        {
            try
            {
                // Build connection string with session-based database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                t.id,
                t.date,
                t.account_id,
                a.name AS AccountName,
                t.debit,
                t.credit,
                t.description
            FROM tbl_transaction t
            LEFT JOIN tbl_coa_level_4 a ON a.id = t.account_id
            WHERE t.transaction_id = @prepaidId
              AND t.type = 'Prepaid Expense'
            ORDER BY t.date;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@prepaidId", prepaidId);

                using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<object>();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        Id = reader["id"],
                        Date = Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd"),
                        AccountName = reader["AccountName"]?.ToString(),
                        Debit = reader["debit"]?.ToString(),
                        Credit = reader["credit"]?.ToString(),
                        Description = reader["description"]?.ToString()
                    });
                }

                return Ok(new { status = true, data = list });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Prepaid Expense Category

        public IActionResult PrepaidExpenseCategory()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = "SELECT id, name FROM tbl_prepaid_expense_category ORDER BY id DESC";
                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var categories = new List<object>();
                while (await reader.ReadAsync())
                {
                    categories.Add(new
                    {
                        Id = reader.GetInt32("id"),
                        CategoryName = reader.GetString("name")
                    });
                }

                return Ok(new { status = true, data = categories });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePrepaidExpenseCategory([FromBody] PrepaidExpenseCategoryRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.CategoryName))
                return BadRequest(new { status = false, message = "Please enter category name" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔍 Check for duplicate category
                string checkQuery = @"SELECT id FROM tbl_prepaid_expense_category WHERE name = @name";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                    var existingId = await checkCmd.ExecuteScalarAsync();

                    if (existingId != null && (model.Id == 0 || Convert.ToInt32(existingId) != model.Id))
                    {
                        return BadRequest(new { status = false, message = "Category already exists. Enter another name." });
                    }
                }

                if (model.Id == 0)
                {
                    // ➕ Insert new category
                    string insertQuery = @"INSERT INTO tbl_prepaid_expense_category (name) VALUES (@name)";
                    using (var insertCmd = new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    

                    return Ok(new { status = true, message = "Category added successfully" });
                }
                else
                {
                    // ✏️ Update existing category
                    string updateQuery = @"UPDATE tbl_prepaid_expense_category SET name = @name WHERE id = @id";
                    using (var updateCmd = new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                        updateCmd.Parameters.AddWithValue("@id", model.Id);

                        int affected = await updateCmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                            return NotFound(new { status = false, message = "Category not found" });
                    }

                    return Ok(new { status = true, message = "Category updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion


    }

}

