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


        #endregion

        #region

        public IActionResult FixedAssets()
        {
            return View();
        }

        #endregion


    }

}

