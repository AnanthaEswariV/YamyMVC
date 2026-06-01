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

                // Accounts list
                var accounts = new List<object>();
                var cmd = new MySqlCommand(@"
            SELECT id, CONCAT(code,' - ',name) AS name
            FROM tbl_coa_level_4
            ORDER BY code", conn);

                using (var reader = await cmd.ExecuteReaderAsync())
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

                // Selected account
                var selectedCmd = new MySqlCommand(@"
            SELECT account_id
            FROM tbl_coa_config
            WHERE category = @category
            LIMIT 1", conn);

                selectedCmd.Parameters.AddWithValue("@category", category);
                var selectedObj = await selectedCmd.ExecuteScalarAsync();

                return Ok(new
                {
                    accounts,
                    selectedAccountId = selectedObj != null ? Convert.ToInt32(selectedObj) : (int?)null
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
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

        #region Labour Check-In / Check-Out

        public IActionResult CheckIn()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CheckIn([FromBody] LabourCheckInRequest model)
        {
            try
            {
                // 1️⃣ Validate model
                if (model == null)
                    return BadRequest(new { status = false, message = "Invalid request" });

                if (model.Latitude == 0 && model.Longitude == 0)
                    return BadRequest(new { status = false, message = "Location not captured. Please allow location access." });

                if (string.IsNullOrEmpty(model.Type) || (model.Type != "IN" && model.Type != "OUT"))
                    return BadRequest(new { status = false, message = "Invalid attendance type." });

                // 2️⃣ Get Labour ID from Session
                int labourId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (labourId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                // 3️⃣ Get Database from Session
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return BadRequest(new { status = false, message = "No database selected. Please login first." });

                // 4️⃣ Build connection
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = database
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                if (model.Type == "IN")
                {
                    // 5️⃣ Prevent duplicate Check-In today
                    var checkExistingQuery = @"
                    SELECT COUNT(*) FROM tbl_labour_checkin 
                    WHERE LabourId = @labourId 
                    AND DATE(CheckInTime) = CURDATE()";

                    using (var checkCmd = new MySqlCommand(checkExistingQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@labourId", labourId);
                        var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        if (exists > 0)
                            return BadRequest(new { status = false, message = "Already checked in today." });
                    }

                    // 6️⃣ Insert Check-In
                    var insertQuery = @"
                    INSERT INTO tbl_labour_checkin 
                        (LabourId, CheckInTime, Latitude, Longitude, CheckInAddress)
                    VALUES 
                        (@labourId, @time, @lat, @lng, @address)";
                    DateTime checkInTime = DateTime.Parse(model.DeviceTime);
                    using (var insertCmd = new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@labourId", labourId);
                        insertCmd.Parameters.AddWithValue("@time", checkInTime);
                        insertCmd.Parameters.AddWithValue("@lat", model.Latitude);
                        insertCmd.Parameters.AddWithValue("@lng", model.Longitude);
                        insertCmd.Parameters.AddWithValue("@address", model.LocationAddress ?? "");
                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new
                    {
                        status = true,
                        message = "Check-In successful!",
                        time = checkInTime.ToString("hh:mm tt"),
                        date = checkInTime.ToString("dd MMM yyyy"),
                        type = "IN"
                    });
                }
                else // OUT
                {
                    // 7️⃣ Must have checked in today first
                    int recordId = 0;
                    var getRecordQuery = @"
                    SELECT Id FROM tbl_labour_checkin 
                    WHERE LabourId = @labourId 
                    AND DATE(CheckInTime) = CURDATE()
                    AND CheckOutTime IS NULL";

                    using (var getCmd = new MySqlCommand(getRecordQuery, conn))
                    {
                        getCmd.Parameters.AddWithValue("@labourId", labourId);
                        var result = await getCmd.ExecuteScalarAsync();
                        if (result == null)
                            return BadRequest(new { status = false, message = "No active check-in found for today." });

                        recordId = Convert.ToInt32(result);
                    }

                    // 8️⃣ Update Check-Out
                    var updateQuery = @"
                    UPDATE tbl_labour_checkin 
                    SET CheckOutTime      = @time,
                        CheckOutLatitude  = @lat,
                        CheckOutLongitude = @lng,
                        CheckOutAddress   = @address
                    WHERE Id = @id";
                    DateTime checkOutTime = DateTime.Parse(model.DeviceTime);
                    using (var updateCmd = new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@time", checkOutTime);
                        updateCmd.Parameters.AddWithValue("@lat", model.Latitude);
                        updateCmd.Parameters.AddWithValue("@lng", model.Longitude);
                        updateCmd.Parameters.AddWithValue("@address", model.LocationAddress ?? "");
                        updateCmd.Parameters.AddWithValue("@id", recordId);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new
                    {
                        status = true,
                        message = "Check-Out successful!",
                        time = checkOutTime.ToString("hh:mm tt"),
                        date = checkOutTime.ToString("dd MMM yyyy"),
                        type = "OUT"
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        // 🔄 Get today's status
        [HttpGet]
        public async Task<IActionResult> GetTodayStatus()
        {
            try
            {
                // 1️⃣ Get Labour ID from Session
                int labourId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (labourId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                // 2️⃣ Get Database from Session
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return BadRequest(new { status = false, message = "No database selected. Please login first." });

                // 3️⃣ Build connection
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = database
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 4️⃣ Get today's record
                var query = @"
                SELECT CheckInTime, CheckInAddress, CheckOutTime, CheckOutAddress
                FROM tbl_labour_checkin 
                WHERE LabourId = @labourId 
                AND DATE(CheckInTime) = CURDATE()";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@labourId", labourId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var checkIn = reader["CheckInTime"].ToString();
                    var checkInAddr = reader["CheckInAddress"].ToString();
                    var checkOut = reader["CheckOutTime"] == DBNull.Value ? null : reader["CheckOutTime"].ToString();
                    var checkOutAddr = reader["CheckOutAddress"] == DBNull.Value ? null : reader["CheckOutAddress"].ToString();

                    return Ok(new
                    {
                        status = true,
                        hasCheckedIn = true,
                        hasCheckedOut = checkOut != null,
                        checkInTime = Convert.ToDateTime(checkIn).ToString("hh:mm tt"),
                        checkInAddress = checkInAddr,
                        checkOutTime = checkOut != null ? Convert.ToDateTime(checkOut).ToString("hh:mm tt") : null,
                        checkOutAddress = checkOutAddr
                    });
                }

                return Ok(new { status = true, hasCheckedIn = false, hasCheckedOut = false });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Attendance List

        public IActionResult AttendanceList()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceList()
        {
            try
            {
                // 1️⃣ Get Database from Session
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return BadRequest(new { status = false, message = "No database selected. Please login first." });

                // 2️⃣ Build connection
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = database
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 3️⃣ Get attendance list
                var query = @"
            SELECT 
                u.Id                                        AS UserId,
                u.User_Name                                 AS UserName,
                DATE(a.CheckInTime)                         AS WorkDate,
                TIME_FORMAT(a.CheckInTime, '%h:%i %p')      AS CheckIn,
                TIME_FORMAT(a.CheckOutTime, '%h:%i %p')     AS CheckOut,
                DAYNAME(a.CheckInTime)                      AS DayOfWeek,
                CASE 
                    WHEN a.CheckInTime IS NOT NULL 
                     AND a.CheckOutTime IS NOT NULL THEN 'P'
                    ELSE 'A'
                END                                         AS Status
            FROM tbl_sec_users u
            LEFT JOIN tbl_labour_checkin a 
                ON u.Id = a.LabourId 
                AND DATE(a.CheckInTime) = CURDATE()
            ORDER BY u.User_Name ASC";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        UserId = reader["UserId"],
                        UserName = reader["UserName"].ToString(),
                        WorkDate = reader["WorkDate"] == DBNull.Value ? "-" : Convert.ToDateTime(reader["WorkDate"]).ToString("dd MMM yyyy"),
                        CheckIn = reader["CheckIn"] == DBNull.Value ? "-" : reader["CheckIn"].ToString(),
                        CheckOut = reader["CheckOut"] == DBNull.Value ? "-" : reader["CheckOut"].ToString(),
                        DayOfWeek = reader["DayOfWeek"] == DBNull.Value ? "-" : reader["DayOfWeek"].ToString(),
                        Status = reader["Status"].ToString()
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


    }

}

