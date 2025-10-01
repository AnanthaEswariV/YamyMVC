using System.Data;
using YamyProject.Core.Models;
using YamyProject.Core.Models.DTOs;
using static Org.BouncyCastle.Math.EC.ECCurve;

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

        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> SaveCostCenter([FromBody] CostCenterRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (!model.IsMain && !model.IsSub)
                return BadRequest(new { status = false, message = "Choose one Account type first" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter account name" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if name already exists
                var checkQuery = "SELECT COUNT(*) FROM tbl_cost_center WHERE name = @name";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name);
                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    if (exists)
                        return BadRequest(new { status = false, message = "Account name already exists. Enter another name." });
                }

                if (model.IsMain)
                {
                    // Get last main code
                    int mainCode = 101;
                    var getCodeQuery = @"SELECT code FROM tbl_cost_center ORDER BY CAST(SUBSTRING_INDEX(code, '-', -1) AS UNSIGNED) DESC LIMIT 1";
                    using (var codeCmd = new MySqlCommand(getCodeQuery, conn))
                    using (var reader = await codeCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync() && reader["code"] != DBNull.Value)
                            mainCode = Convert.ToInt32(reader["code"]) + 1;
                    }

                    var insertQuery = @"INSERT INTO tbl_cost_center (name, code, project_id) 
                                    VALUES (@name, @code, @project_id)";
                    using (var insertCmd = new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@name", model.Name);
                        insertCmd.Parameters.AddWithValue("@code", mainCode);
                        insertCmd.Parameters.AddWithValue("@project_id", model.ProjectId);

                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new { status = true, message = "Main Cost Center inserted successfully", code = mainCode });
                }
                else if (model.IsSub)
                {
                    // Get main code
                    string mainCode = "";
                    var getMainQuery = "SELECT code FROM tbl_cost_center WHERE id = @id";
                    using (var mainCmd = new MySqlCommand(getMainQuery, conn))
                    {
                        mainCmd.Parameters.AddWithValue("@id", model.MainId);
                        using var reader = await mainCmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                            mainCode = reader["code"].ToString();
                        else
                            return BadRequest(new { status = false, message = "Main cost center not found" });
                    }

                    int newSubCode = 1;
                    var getSubQuery = "SELECT MAX(code) FROM tbl_sub_cost_center WHERE code LIKE @mainCode";
                    using (var subCmd = new MySqlCommand(getSubQuery, conn))
                    {
                        subCmd.Parameters.AddWithValue("@mainCode", mainCode + "%");
                        var result = await subCmd.ExecuteScalarAsync();
                        if (result != DBNull.Value && result != null)
                            newSubCode = int.Parse(result.ToString().Substring(mainCode.Length)) + 1;
                    }

                    string formattedSubCode = mainCode + newSubCode.ToString("D3");

                    var insertSubQuery = @"INSERT INTO tbl_sub_cost_center (code, name, main_id, project_id) 
                                       VALUES (@code, @name, @main_id, @project_id)";
                    using (var insertSubCmd = new MySqlCommand(insertSubQuery, conn))
                    {
                        insertSubCmd.Parameters.AddWithValue("@code", formattedSubCode);
                        insertSubCmd.Parameters.AddWithValue("@name", model.Name);
                        insertSubCmd.Parameters.AddWithValue("@main_id", model.MainId);
                        insertSubCmd.Parameters.AddWithValue("@project_id", model.ProjectId);

                        await insertSubCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new { status = true, message = "Sub Cost Center inserted successfully", code = formattedSubCode });
                }

                return BadRequest(new { status = false, message = "Invalid request" });
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

        public IActionResult Item()
        {
            return View();
        }

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
                   CONCAT(code, ' - ', name) AS ItemName,
                   barcode AS Barcode,
                   type AS Type,
                   cost_price as Cost_Price
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
                        cost_price =reader.GetDecimal("Cost_Price")
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


        [HttpPut]
        public async Task<IActionResult> EditItem([FromBody] ItemRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Invalid item" });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId == 0) return Unauthorized(new { status = false, message = "User not logged in" });

            var connStr = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            { Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase") };

            using var conn = new MySqlConnection(connStr.ConnectionString);
            await conn.OpenAsync();

            // Duplicate check
            var exists = Convert.ToInt32(await new MySqlCommand(
                "SELECT COUNT(*) FROM tbl_items WHERE name=@name AND id<>@id", conn)
            { Parameters = { new MySqlParameter("@name", model.Name), new MySqlParameter("@id", model.Id) } }
                .ExecuteScalarAsync()) > 0;

            if (exists) return BadRequest(new { status = false, message = "Item already exists" });

            // Generate next code
            long lastCode = 0;
            using (var reader = await new MySqlCommand(
                "SELECT Code FROM tbl_items WHERE LENGTH(Code)=9 ORDER BY CAST(Code AS UNSIGNED) DESC LIMIT 1", conn)
                .ExecuteReaderAsync())
            {
                if (await reader.ReadAsync()) lastCode = long.Parse(reader["Code"].ToString());
            }
            string newCode = (lastCode + 1).ToString("D9");

            // Insert main item
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

            // Insert Units
            foreach (var u in model.Units)
            {
                using var unitCmd = new MySqlCommand(
                    @"INSERT INTO tbl_items_unit (item_id, unit_id, factor) VALUES (@id,@unit_id,@factor)", conn);
                unitCmd.Parameters.AddWithValue("@id", model.Id);
                unitCmd.Parameters.AddWithValue("@unit_id", u.UnitId);
                unitCmd.Parameters.AddWithValue("@factor", u.Factor);
                await unitCmd.ExecuteNonQueryAsync();
            }

            // Insert Assemblies only for "13 - Inventory Assembly"
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


    }

}

