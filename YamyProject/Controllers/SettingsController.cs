namespace YamyProject.Controllers
{
    [Route("Settings")]
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

        [HttpGet("ChangePassword")]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePasswords([FromBody] ChangePasswordRequest model)
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

        #endregion


    }

}

