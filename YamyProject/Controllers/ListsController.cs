using YamyProject.Core.Models;
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

    }

}

