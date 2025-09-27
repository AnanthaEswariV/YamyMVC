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


        public IActionResult Dashboard()
        {
            return View();
        }

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



    }
}
