namespace YamyProject.Controllers
{
    public class SettingsController : Controller
    {
        private readonly IConfiguration _config;
        private readonly MySqlConnection _connection;

        public SettingsController(IConfiguration config)
        {
            _config = config;
            _connection = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        }


        #region Change Password

        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest model)
        {
            try
            {
                // 1️⃣ Validate model
                if (model == null)
                    return BadRequest(new { status = false, message = "Invalid request" });

                if (model.RequireOldPassword && string.IsNullOrWhiteSpace(model.OldPassword))
                    return BadRequest(new { status = false, message = "Enter Old Password" });

                if (string.IsNullOrWhiteSpace(model.NewPassword))
                    return BadRequest(new { status = false, message = "Enter Your New Password First" });

                if (string.IsNullOrWhiteSpace(model.ConfirmPassword))
                    return BadRequest(new { status = false, message = "Enter Confirm Password" });

                if (model.NewPassword != model.ConfirmPassword)
                    return BadRequest(new { status = false, message = "Confirm Password Mismatch With New Password" });

                // 2️⃣ Get Database from Session
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return BadRequest(new { status = false, message = "No database selected. Please login first." });

                // 3️⃣ Build dynamic connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = database
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 4️⃣ Resolve User Id
                int uId = 0;
                if (!string.IsNullOrEmpty(model.UserName))
                {
                    var getUserQuery = "SELECT id FROM tbl_sec_users WHERE User_Name = @UserName";
                    using var cmd = new MySqlCommand(getUserQuery, conn);
                    cmd.Parameters.AddWithValue("@UserName", model.UserName);

                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null)
                        uId = Convert.ToInt32(result);
                }
                else
                {
                    uId = model.UserId;
                }

                if (uId == 0)
                    return NotFound(new { status = false, message = "User not found" });

                // 5️⃣ Fetch stored hash and salt
                string storedHash = "";
                string storedSalt = "";
                var getPassQuery = "SELECT PasswordHash, Salt FROM tbl_sec_users WHERE Id = @id";

                using (var cmd = new MySqlCommand(getPassQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", uId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        storedHash = reader["PasswordHash"].ToString();
                        storedSalt = reader["Salt"].ToString();
                    }
                    else
                    {
                        return NotFound(new { status = false, message = "User not found" });
                    }
                }

                // 6️⃣ Verify old password if required
                if (model.RequireOldPassword && !PasswordHelper.VerifyPassword(model.OldPassword, storedHash, storedSalt))
                {
                    return BadRequest(new { status = false, message = "Old password is incorrect" });
                }

                // 7️⃣ Hash new password
                string newSalt;
                string newHash = PasswordHelper.HashPassword(model.NewPassword, out newSalt);

                // 8️⃣ Update user password
                var updateQuery = @"
            UPDATE tbl_sec_users 
            SET PasswordHash = @hash, 
                Salt = @salt, 
                password_updated_by = @adminId,
                password_last_update = @date  
            WHERE Id = @id";

                using (var updateCmd = new MySqlCommand(updateQuery, conn))
                {
                    updateCmd.Parameters.AddWithValue("@hash", newHash);
                    updateCmd.Parameters.AddWithValue("@salt", newSalt);
                    updateCmd.Parameters.AddWithValue("@adminId", uId);
                    updateCmd.Parameters.AddWithValue("@date", DateTime.Now);
                    updateCmd.Parameters.AddWithValue("@id", uId);

                    await updateCmd.ExecuteNonQueryAsync();
                }

                // ✅ Success
                return Ok(new { status = true, message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Default Account Configuration

        public IActionResult MasterSettings()
        {
            return View();
        }

        //[HttpGet]
        //public async Task<IActionResult> GetConfigDefaults()
        //{
        //    try
        //    {
        //        var connStr = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
        //        {
        //            Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
        //        };

        //        using var conn = new MySqlConnection(connStr.ConnectionString);
        //        await conn.OpenAsync();

        //        var cmd = new MySqlCommand("SELECT category, account_id FROM tbl_coa_config", conn);

        //        var dict = new Dictionary<string, int>();
        //        using var reader = await cmd.ExecuteReaderAsync();
        //        while (await reader.ReadAsync())
        //        {
        //            dict[reader.GetString("category")] = reader.GetInt32("account_id");
        //        }

        //        return Ok(dict);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = ex.Message });
        //    }
        //}

        //[HttpGet]
        //public async Task<IActionResult> GetAccounts(string category)
        //{
        //    try
        //    {
        //        var connStr = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
        //        {
        //            Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
        //        };

        //        using var conn = new MySqlConnection(connStr.ConnectionString);
        //        await conn.OpenAsync();

        //        var cmd = new MySqlCommand(
        //            "SELECT id, CONCAT(code,' - ',name) AS name FROM tbl_coa_level_4 WHERE name LIKE @pattern ORDER BY code", conn);
        //        cmd.Parameters.AddWithValue("@pattern", $"%{category}%");

        //        var list = new List<object>();
        //        using var reader = await cmd.ExecuteReaderAsync();
        //        while (await reader.ReadAsync())
        //        {
        //            list.Add(new
        //            {
        //                id = reader.GetInt32("id"),
        //                name = reader.GetString("name")
        //            });
        //        }

        //        return Ok(list);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { message = ex.Message });
        //    }
        //}



        [HttpGet]
        public async Task<IActionResult> GetAccounts(string category)
        {
            try
            {
                var connStr = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStr.ConnectionString);
                await conn.OpenAsync();

                // 1️⃣ Get accounts (equivalent to tableLevel4 binding)
                var accountsCmd = new MySqlCommand(@"
            SELECT id, CONCAT(code,' - ',name) AS name
            FROM tbl_coa_level_4
            ORDER BY code", conn);

                var accounts = new List<object>();
                using (var reader = await accountsCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        accounts.Add(new
                        {
                            id = reader.GetInt32("id"),
                            name = reader.GetString("name")
                        });
                    }
                }

                // 2️⃣ Get selected account for category (equivalent to coaConfigDict)
                var selectedCmd = new MySqlCommand(@"
            SELECT account_id
            FROM tbl_coa_config
            WHERE category = @category
            LIMIT 1", conn);

                selectedCmd.Parameters.AddWithValue("@category", category);

                var selectedAccountIdObj = await selectedCmd.ExecuteScalarAsync();
                int? selectedAccountId = selectedAccountIdObj != null
                    ? Convert.ToInt32(selectedAccountIdObj)
                    : (int?)null;

                return Ok(new
                {
                    accounts,
                    selectedAccountId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SaveDefaultAccounts([FromBody] List<DefaultAccountSettingDto> settings)
        {
            if (settings == null || !settings.Any())
                return BadRequest(new { status = false, message = "No settings received" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                foreach (var item in settings)
                {
                    if (item.AccountId <= 0) continue;

                    string query = @"
                INSERT INTO tbl_coa_config (category, account_id)
                VALUES (@category, @account_id)
                ON DUPLICATE KEY UPDATE account_id = @account_id;";

                    using var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@category", item.Category);
                    cmd.Parameters.AddWithValue("@account_id", item.AccountId);
                    await cmd.ExecuteNonQueryAsync();
                }

                return Ok(new { status = true, message = "Default account settings saved successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }



        #endregion


    }

}

