using YamyProject.Core.Models;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace YamyProject.Controllers
{

    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        #region Register


        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Register([FromBody] CompanyViewModel companyViewModel)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                var response = await client.PostAsJsonAsync("api/Account/create", companyViewModel);

                var data = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = JsonConvert.DeserializeObject<dynamic>(data);

                    return Json(new
                    {
                        status = true,
                        message = apiResponse?.Message ?? "Company created successfully!",
                        database = apiResponse?.Database
                    });
                }
                else
                {
                    // 👇 Try to parse a friendly message from the API
                    string errorMessage = "Something went wrong while creating the company.";

                    try
                    {
                        var errorObj = JsonConvert.DeserializeObject<dynamic>(data);
                        if (errorObj != null && errorObj.Message != null)
                        {
                            errorMessage = errorObj.Message.ToString();
                        }
                    }
                    catch
                    {
                        // fallback → don’t expose raw technical error
                    }

                    return Json(new { status = false, message = errorMessage });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "An unexpected error occurred. Please try again." });
            }
        }

        #endregion

        #region CompanyList

        public IActionResult CompanyList()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanyList()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                var response = await client.GetAsync("api/Account/list");

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { status = false, message = "Failed to fetch companies" });
                }

                var data = await response.Content.ReadAsStringAsync();
                var companies = JsonConvert.DeserializeObject<List<CompanyViewModel>>(data);

                return Json(new { status = true, data = companies });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        #endregion

        #region Login

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LoginUser(string username, string password, string database)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                var loginRequest = new
                {
                    UserName = username,
                    Password = password,
                    Database = database
                };

                var json = JsonConvert.SerializeObject(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/Account/login", content);

                if (!response.IsSuccessStatusCode)
                    return Json(new { status = false, message = "Login failed" });

                var result = await response.Content.ReadAsStringAsync();
                var obj = JsonConvert.DeserializeObject<dynamic>(result);
                // ✅ store selected DB in session
                HttpContext.Session.SetString("DatabaseName", database);
                HttpContext.Session.SetString("UserName", username);
                if (obj.status == true)
                {
                    int userId = obj.user.id; // get userId from API response
                    HttpContext.Session.SetInt32("UserId", userId);
                }

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Dashboard
        public IActionResult Dashboard()
        {
            return View();
        }
        #endregion

        #region Bank Center

        public IActionResult BankCenter()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetBanks(string selectionMethod)
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

                int state = selectionMethod == "Register Bank" ? 0 : -1;

                string query = @"SELECT id, code, name, abb_name, ent_id, route_num, country_id
                         FROM tbl_bank WHERE state = @state ORDER BY id DESC";

                var data = new List<object>();
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@state", state);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int sn = 1;
                        while (await reader.ReadAsync())
                        {
                            data.Add(new
                            {
                                sn = sn++,
                                id = reader["id"],
                                code = reader["code"],
                                bankName = reader["name"],
                                abbName = reader["abb_name"],
                                entId = reader["ent_id"],
                                routeNo = reader["route_num"],
                                countryId = reader["country_id"]
                            });
                        }
                    }
                }

                return Ok(new { status = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetCountries()
        {
            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName")
                           ?? _config["ConnectionStrings:DefaultDatabase"]
            };

            using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            var countries = new List<object>();
            using (var cmd = new MySqlCommand("SELECT id, name FROM tbl_country ORDER BY name", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    countries.Add(new
                    {
                        id = reader.GetInt32("id"),
                        name = reader.GetString("name")
                    });
                }
            }

            return Json(countries);
        }


        [HttpPost]
        public async Task<IActionResult> SaveBank([FromBody] BankRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.AbbName))
                return BadRequest(new { status = false, message = "Please enter abbreviation name" });

            if (string.IsNullOrWhiteSpace(model.EntId))
                return BadRequest(new { status = false, message = "Please enter entity ID" });

            if (string.IsNullOrWhiteSpace(model.RouteNum))
                return BadRequest(new { status = false, message = "Please enter route number" });

            if (string.IsNullOrWhiteSpace(model.BankName))
                return BadRequest(new { status = false, message = "Please enter bank name" });

            if (model.CountryId == null || model.CountryId <= 0)
                return BadRequest(new { status = false, message = "Please select a country" });

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

                // 🔎 Check duplicate
                var checkQuery = @"SELECT COUNT(*) FROM tbl_bank 
                           WHERE id <> @id AND (ent_id=@ent_id OR abb_name=@abbName OR route_num=@route)";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", model.Id);
                    checkCmd.Parameters.AddWithValue("@ent_id", model.EntId.Trim());
                    checkCmd.Parameters.AddWithValue("@abbName", model.AbbName.Trim());
                    checkCmd.Parameters.AddWithValue("@route", model.RouteNum.Trim());

                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    if (exists)
                        return BadRequest(new { status = false, message = "Duplicate record exists. Please check abbreviation, entity ID, or route number." });
                }

                if (model.Id == 0) // INSERT
                {
                    // Generate next bank code
                    int lastCode = 0;
                    var codeQuery = "SELECT code FROM tbl_bank ORDER BY CAST(code AS UNSIGNED) DESC LIMIT 1";
                    using (var codeCmd = new MySqlCommand(codeQuery, conn))
                    using (var reader = await codeCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync() && reader["code"] != DBNull.Value)
                            lastCode = int.Parse(reader["code"].ToString());
                    }
                    string newCode = (lastCode + 1).ToString("D4");

                    // Insert bank
                    var insertQuery = @"INSERT INTO tbl_bank 
                (abb_name, ent_id, route_num, state, name, code, country_id, created_by, created_date) 
                VALUES (@abbName, @ent_id, @route, 0, @bankName, @code, @countryId, @createdBy, @createdDate)";
                    using (var insertCmd = new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@abbName", model.AbbName.Trim());
                        insertCmd.Parameters.AddWithValue("@ent_id", model.EntId.Trim());
                        insertCmd.Parameters.AddWithValue("@route", model.RouteNum.Trim());
                        insertCmd.Parameters.AddWithValue("@bankName", model.BankName.Trim());
                        insertCmd.Parameters.AddWithValue("@code", newCode);
                        insertCmd.Parameters.AddWithValue("@countryId", model.CountryId);
                        insertCmd.Parameters.AddWithValue("@createdBy", userId);
                        insertCmd.Parameters.AddWithValue("@createdDate", DateTime.Now);

                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new { status = true, message = "Bank added successfully", code = newCode });
                }
                else // UPDATE
                {
                    var updateQuery = @"UPDATE tbl_bank 
                                SET abb_name=@abbName, ent_id=@ent_id, route_num=@route, 
                                    name=@bankName, country_id=@countryId, state=0 
                                WHERE id=@id";
                    using (var updateCmd = new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@abbName", model.AbbName.Trim());
                        updateCmd.Parameters.AddWithValue("@ent_id", model.EntId.Trim());
                        updateCmd.Parameters.AddWithValue("@route", model.RouteNum.Trim());
                        updateCmd.Parameters.AddWithValue("@bankName", model.BankName.Trim());
                        updateCmd.Parameters.AddWithValue("@countryId", model.CountryId);
                        updateCmd.Parameters.AddWithValue("@id", model.Id);

                        int affected = await updateCmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                            return NotFound(new { status = false, message = "Bank not found" });
                    }

                    return Ok(new { status = true, message = "Bank updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateBank([FromBody] int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid bank ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Check if the bank is used in tbl_bank_card
                var checkQuery = "SELECT COUNT(*) FROM tbl_bank_card WHERE Bank_Id = @id AND state = 0";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                    if (count > 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Cannot deactivate bank. It is referenced in bank cards."
                        });
                    }
                }

                // ✅ Proceed with deactivation if not used
                var updateQuery = "UPDATE tbl_bank SET state = -1 WHERE id = @id";
                using var cmd = new MySqlCommand(updateQuery, conn);
                cmd.Parameters.AddWithValue("@id", id);

                int affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                    return NotFound(new { status = false, message = "Bank not found" });

                return Ok(new { status = true, message = "Bank deactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateBank([FromBody] int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid bank ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var updateQuery = "UPDATE tbl_bank SET state = 0 WHERE id = @id";
                using var cmd = new MySqlCommand(updateQuery, conn);
                cmd.Parameters.AddWithValue("@id", id);

                int affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                    return NotFound(new { status = false, message = "Bank not found" });

                return Ok(new { status = true, message = "Bank has been activated." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCountryById(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid country ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = "SELECT id, name FROM tbl_countries WHERE id = @id LIMIT 1";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return Ok(new
                    {
                        id = reader.GetInt32("id"),
                        name = reader.GetString("name")
                    });
                }

                return NotFound(new { status = false, message = "Country not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Bank Card Center

        public IActionResult BankCardCenter()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetBanksforDropdown()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = "SELECT id, name,code FROM tbl_bank WHERE state = 0 ORDER BY name";
                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var banks = new List<object>();
                while (await reader.ReadAsync())
                {
                    banks.Add(new
                    {
                        Id = reader.GetInt32("id"),
                        Name = reader.GetString("name"),
                        Code =reader.GetString("code")
                    });
                }

                return Ok(new { status = true, data = banks });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult GetLoggedInUser()
        {
            var username = HttpContext.Session.GetString("UserName") ?? "";
            return Ok(new { username });
        }
        [HttpPost]
        public async Task<IActionResult> SaveBankCard([FromBody] BankCardRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            // 🔹 Validation
            if (string.IsNullOrWhiteSpace(model.AccountName))
                return BadRequest(new { status = false, message = "Please enter account name" });

            if (string.IsNullOrWhiteSpace(model.AccountNo))
                return BadRequest(new { status = false, message = "Please enter account number" });

            if (string.IsNullOrWhiteSpace(model.IBANNo))
                return BadRequest(new { status = false, message = "Please enter IBAN number" });

            if (model.AccountId <= 0)
                return BadRequest(new { status = false, message = "Please select an account" });

            if (model.BankId <= 0)
                return BadRequest(new { status = false, message = "Please select a bank" });

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

                // 🔎 Company bank account rule
                if (model.CompanyAc)
                {
                    var checkQuery = "SELECT COUNT(*) FROM tbl_bank_card WHERE company_ac = 1 AND id <> @id";
                    using var checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@id", model.Id);
                    int exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (exists > 0)
                        return BadRequest(new { status = false, message = "Only one company bank account is allowed" });
                }

                if (model.Id == 0) // ➝ INSERT
                {
                    var insertQuery = @"
                INSERT INTO tbl_bank_card 
                (bank_id, account_name, account_type, account_no, swift, iban_no, branch_name, emirates, currency,
                 account_manager, account_sign, account_mob, account_id, state, created_by, created_date, company_ac)
                VALUES
                (@bank_id, @account_name, @account_type, @account_no, @swift, @iban_no, @branch_name, @emirates, @currency,
                 @account_manager, @account_sign, @account_mob, @account_id, 0, @created_by, @created_date, @companyAc)";

                    using var insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@bank_id", model.BankId);
                    insertCmd.Parameters.AddWithValue("@account_name", model.AccountName.Trim());
                    insertCmd.Parameters.AddWithValue("@account_type", model.AccountType?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@account_no", model.AccountNo.Trim());
                    insertCmd.Parameters.AddWithValue("@swift", model.Swift?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@iban_no", model.IBANNo.Trim());
                    insertCmd.Parameters.AddWithValue("@branch_name", model.BranchName?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@emirates", model.Emirates?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@currency", model.Currency?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@account_manager", model.AccountManager?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@account_sign", model.AccountSign?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@account_mob", model.AccountMob?.Trim() ?? "");
                    insertCmd.Parameters.AddWithValue("@account_id", model.AccountId);
                    insertCmd.Parameters.AddWithValue("@created_by", userId);
                    insertCmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    insertCmd.Parameters.AddWithValue("@companyAc", model.CompanyAc ? 1 : 0);

                    await insertCmd.ExecuteNonQueryAsync();
                    return Ok(new { status = true, message = "Bank card added successfully" });
                }
                else // ➝ UPDATE
                {
                    var updateQuery = @"
                UPDATE tbl_bank_card 
                SET bank_id=@bank_id, account_name=@account_name, account_type=@account_type, account_no=@account_no,
                    swift=@swift, iban_no=@iban_no, branch_name=@branch_name, emirates=@emirates, currency=@currency,
                    account_manager=@account_manager, account_sign=@account_sign, account_mob=@account_mob,
                    account_id=@account_id, company_ac=@companyAc, state=0
                WHERE id=@id";

                    using var updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@bank_id", model.BankId);
                    updateCmd.Parameters.AddWithValue("@account_name", model.AccountName.Trim());
                    updateCmd.Parameters.AddWithValue("@account_type", model.AccountType?.Trim() ?? "");
                    updateCmd.Parameters.AddWithValue("@account_no", model.AccountNo.Trim());
                    updateCmd.Parameters.AddWithValue("@swift", model.Swift?.Trim() ?? "");
                    updateCmd.Parameters.AddWithValue("@iban_no", model.IBANNo.Trim());
                    updateCmd.Parameters.AddWithValue("@branch_name", model.BranchName?.Trim() ?? "");
                    updateCmd.Parameters.AddWithValue("@emirates", model.Emirates?.Trim() ?? "");
                    updateCmd.Parameters.AddWithValue("@currency", model.Currency?.Trim() ?? "");
                    updateCmd.Parameters.AddWithValue("@account_manager", model.AccountManager?.Trim() ?? "");
                    updateCmd.Parameters.AddWithValue("@account_sign", model.AccountSign?.Trim() ?? "");
                    updateCmd.Parameters.AddWithValue("@account_mob", model.AccountMob?.Trim() ?? "");
                    updateCmd.Parameters.AddWithValue("@account_id", model.AccountId);
                    updateCmd.Parameters.AddWithValue("@companyAc", model.CompanyAc ? 1 : 0);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);

                    int affected = await updateCmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Bank card not found" });

                    return Ok(new { status = true, message = "Bank card updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeactivateBankCard([FromBody] int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid bank card ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var updateQuery = "UPDATE tbl_bank_card SET state = -1 WHERE id = @id";
                using var cmd = new MySqlCommand(updateQuery, conn);
                cmd.Parameters.AddWithValue("@id", id);

                int affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                    return NotFound(new { status = false, message = "Bank card not found" });

                return Ok(new { status = true, message = "Bank card deactivated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteBankCard([FromBody] int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid ID" });

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

                // Soft delete: mark as inactive (state=1)
                var deleteQuery = "UPDATE tbl_bank_card SET state=1 WHERE id=@id";
                using var cmd = new MySqlCommand(deleteQuery, conn);
                cmd.Parameters.AddWithValue("@id", id);

                int affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                    return NotFound(new { status = false, message = "Bank card not found" });

                return Ok(new { status = true, message = "Bank card deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBankCards()
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
                ROW_NUMBER() OVER (ORDER BY bc.id) AS SN,
                CONCAT(b.code, '-', b.name) AS BankName,
                bc.id,
                bc.bank_id AS BankId,
                bc.account_name AS AcName,
                bc.account_type AS AcType,
                bc.account_no AS AcNo,
                bc.swift AS Swift,
                bc.iban_no AS IbanNo,
                bc.branch_name AS BranchName,
                bc.emirates As Emirates,
                bc.currency As Currency,
                bc.account_manager As AccountManager,
                bc.account_sign As AccountSign,
                bc.account_mob As AccountMob, 
                bc.account_id As AccountId
            FROM tbl_bank_card bc
            INNER JOIN tbl_bank b ON bc.bank_id = b.id
            WHERE bc.state = 0";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var cards = new List<object>();
                while (await reader.ReadAsync())
                {
                    cards.Add(new
                    {
                        Sn = reader.GetInt32("SN"),
                        Id = reader.GetInt32("id"),
                        BankName = reader.GetString("BankName"),
                        AcName = reader.GetString("AcName"),
                        AcType = reader.GetString("AcType"),
                        AcNo = reader.GetString("AcNo"),
                        Swift = reader.GetString("Swift"),
                        IbanNo = reader.GetString("IbanNo"),
                        BranchName = reader.GetString("BranchName"),
                        Emirates = reader.GetString("Emirates"),
                        Currency = reader.GetString("Currency"),
                        AccountManager= reader.GetString("AccountManager"),
                        AccountSign=reader.GetString("AccountSign"),
                        AccountMob=reader.GetString("AccountMob"),
                        AccountId=reader.GetInt32("AccountId"),
                        BankId = reader.GetInt32("BankId")
                    });
                }

                return Ok(new { status = true, data = cards });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RestoreBankCard([FromBody] int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid ID" });

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

                // Soft delete: mark as inactive (state=1)
                var deleteQuery = "UPDATE tbl_bank_card SET state=0 WHERE id=@id";
                using var cmd = new MySqlCommand(deleteQuery, conn);
                cmd.Parameters.AddWithValue("@id", id);

                int affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                    return NotFound(new { status = false, message = "Bank card not found" });

                return Ok(new { status = true, message = "Bank card has been activated" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDeletedBankCards()
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
                ROW_NUMBER() OVER (ORDER BY bc.id) AS SN,
                CONCAT(b.code, '-', b.name) AS BankName,
                bc.id,
                bc.account_name AS AcName,
                bc.account_type AS AcType,
                bc.account_no AS AcNo,
                bc.swift AS Swift,
                bc.iban_no AS IbanNo,
                bc.branch_name AS BranchName,
                bc.emirates As Emirates,
                bc.currency As Currency,
                bc.account_manager As AccountManager,
                bc.account_sign As AccountSign,
                bc.account_mob As AccountMob, 
                bc.account_id As AccountId
            FROM tbl_bank_card bc
            INNER JOIN tbl_bank b ON bc.bank_id = b.id
            WHERE bc.state = 1";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var cards = new List<object>();
                while (await reader.ReadAsync())
                {
                    cards.Add(new
                    {
                        Sn = reader.GetInt32("SN"),
                        Id = reader.GetInt32("id"),
                        BankName = reader.GetString("BankName"),
                        AcName = reader.GetString("AcName"),
                        AcType = reader.GetString("AcType"),
                        AcNo = reader.GetString("AcNo"),
                        Swift = reader.GetString("Swift"),
                        IbanNo = reader.GetString("IbanNo"),
                        BranchName = reader.GetString("BranchName"),
                        Emirates = reader.GetString("Emirates"),
                        Currency = reader.GetString("Currency"),
                        AccountManager = reader.GetString("AccountManager"),
                        AccountSign = reader.GetString("AccountSign"),
                        AccountMob = reader.GetString("AccountMob"),
                        AccountId = reader.GetInt32("AccountId")

                    });
                }

                return Ok(new { status = true, data = cards });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Bank Cheque

        public IActionResult BankCheque()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetChequeBooks()
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
    ROW_NUMBER() OVER (ORDER BY c.id) AS SN,
    c.id,
    c.bank_card_id AS BankCardId,
    CONCAT(b.code, '-', b.name) AS BankName,   -- Bank linked to this bank_card
    bc.account_name AS AcName,
    bc.account_type AS AcType,
    c.chq_book_no AS ChqBookNo,
    c.chq_book_qty AS ChqBookQty,
    c.leaves_start_from AS StartFrom,
    c.leaves_end_in AS EndIn
FROM tbl_cheque c
INNER JOIN tbl_bank_card bc ON c.bank_card_id = bc.id
INNER JOIN tbl_bank b ON b.id = c.bank_card_id
;
"; // Optional order

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var chequeBooks = new List<object>();
                while (await reader.ReadAsync())
                {
                    chequeBooks.Add(new
                    {
                        Sn = reader.GetInt32("SN"),
                        Id = reader.GetInt32("id"),
                        BankName = reader.GetString("BankName"),
                        AcName = reader.GetString("AcName"),
                        AcType = reader.IsDBNull(reader.GetOrdinal("AcType")) ? "" : reader.GetString("AcType"),
                        ChqBookNo = reader.GetInt32("ChqBookNo"),
                        ChqBookQty = reader.GetInt32("ChqBookQty"),
                        StartFrom = reader.GetString("StartFrom"),
                        EndIn = reader.GetString("EndIn")
                    });
                }

                return Ok(new { status = true, data = chequeBooks });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLastChequeNumber(int bankId)
        {
                if (bankId <= 0)
                return BadRequest(new { status = false, lastChequeNo = 0 });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = "SELECT MAX(chq_book_no) FROM tbl_cheque WHERE bank_card_id=@bankCardId";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@bankCardId", bankId);

                var last = await cmd.ExecuteScalarAsync();
                int lastNo = last != DBNull.Value ? Convert.ToInt32(last) : 0;

                return Ok(new { status = true, lastChequeNo = lastNo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetLastLeafNumber(int bankCardId)
        {
            if (bankCardId <= 0)
                return BadRequest(new { status = false, lastChequeNo = 0 });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"SELECT MAX(leaves_end_in) AS LastEndIn 
                         FROM tbl_cheque 
                         WHERE bank_card_id = @bankCardId";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@bankCardId", bankCardId);

                var last = await cmd.ExecuteScalarAsync();
                int lastLeafNo = last != DBNull.Value ? Convert.ToInt32(last) : 0;

                return Ok(new { status = true, lastChequeNo = lastLeafNo });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SaveCheque([FromBody] ChequeRequest model)
        {
            // 🔹 Basic validation
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (model.BankCardId <= 0)
                return BadRequest(new { status = false, message = "Please select a bank card" });

            if (model.ChqBookNo <=0)
                return BadRequest(new { status = false, message = "Please enter cheque book number" });

            if (model.ChqBookQty <= 0)
                return BadRequest(new { status = false, message = "Please enter a valid book quantity" });

            if (model.LeavesStartFrom <= 0 || model.LeavesEndIn <= 0)
                return BadRequest(new { status = false, message = "Please provide valid start and end leaves" });

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

                // 🔹 Insert into tbl_cheque
                var insertQuery = @"
            INSERT INTO tbl_cheque 
            (bank_card_id, chq_book_no, chq_book_qty, leaves_start_from, leaves_end_in, created_by, created_date) 
            VALUES (@bankCardId, @chqBookNo, @chqBookQty, @leavesStartFrom, @leavesEndIn, @createdBy, @createdDate)";

                using var cmd = new MySqlCommand(insertQuery, conn);
                cmd.Parameters.AddWithValue("@bankCardId", model.BankCardId);
                cmd.Parameters.AddWithValue("@chqBookNo", model.ChqBookNo);
                cmd.Parameters.AddWithValue("@chqBookQty", model.ChqBookQty);
                cmd.Parameters.AddWithValue("@leavesStartFrom", model.LeavesStartFrom);
                cmd.Parameters.AddWithValue("@leavesEndIn", model.LeavesEndIn);
                cmd.Parameters.AddWithValue("@createdBy", userId);
                cmd.Parameters.AddWithValue("@createdDate", DateTime.Now);

                await cmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Cheque book added successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNextChequeCode(int bankCardId)
        {
            if (bankCardId <= 0)
                return BadRequest(new { status = false, message = "Invalid Bank Card ID" });

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
                MAX(leaves_end_in) AS LastCode, 
                MAX(chq_book_no) AS LastBookNo
            FROM tbl_cheque
            WHERE bank_card_id = @bankCardId";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@bankCardId", bankCardId);

                using var reader = await cmd.ExecuteReaderAsync();
                int nextStart = 1;
                int nextBookNo = 1;

                if (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(reader.GetOrdinal("LastCode")))
                        nextStart = reader.GetInt32("LastCode") + 1;

                    if (!reader.IsDBNull(reader.GetOrdinal("LastBookNo")))
                        nextBookNo = reader.GetInt32("LastBookNo") + 1;
                }

                return Ok(new
                {
                    status = true,
                    nextStartFrom = nextStart,
                    nextChqBookNo = nextBookNo
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

    }
}
