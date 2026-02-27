using Microsoft.CodeAnalysis;
using Mysqlx.Crud;
using Org.BouncyCastle.Utilities;
using static Umbraco.Core.Collections.TopoGraph;

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
                // Build connection string using session database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"SELECT id, name,code 
                      FROM tbl_item_category 
                      ORDER BY id DESC";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var categories = new List<object>();

                while (await reader.ReadAsync())
                {
                    categories.Add(new
                    {
                        Id = reader.GetInt32("id"),
                        Code = reader["code"].ToString(),
                        Name = reader["name"].ToString(),     // PURE NAME
                        CategoryName = reader["code"] + " - " + reader["name"]   // DISPLAY NAME
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
        public async Task<IActionResult> AddCategory([FromBody] ItemCatoryViewModel model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.CategoryName))
                return BadRequest(new { status = false, message = "Category name is required" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Check if category already exists (case-insensitive)
                string existsQuery = "SELECT COUNT(*) FROM tbl_item_category WHERE LOWER(name) = LOWER(@name)";
                using (var existsCmd = new MySqlCommand(existsQuery, conn))
                {
                    existsCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                    int count = Convert.ToInt32(await existsCmd.ExecuteScalarAsync());
                    if (count > 0)
                        return Conflict(new { status = false, message = "Category name already exists" });
                }
                // STEP 1: Insert category without code
                string insertQuery = @"
            INSERT INTO tbl_item_category (name)
            VALUES (@name);
            SELECT LAST_INSERT_ID();";

                using var insertCmd = new MySqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());

                int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                // STEP 2: Generate code
                string categoryCode = newId.ToString("D3");

                // STEP 3: Update category with generated code
                string updateQuery = "UPDATE tbl_item_category SET code=@code WHERE id=@id;";
                using var updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@code", categoryCode);
                updateCmd.Parameters.AddWithValue("@id", newId);
                await updateCmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    status = true,
                    message = "Category added successfully",
                    id = newId,
                    categoryName = model.CategoryName.Trim(),
                    code = categoryCode
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory([FromBody] ItemCatoryViewModel model)
        {
            if (model == null || model.Id <= 0 || string.IsNullOrWhiteSpace(model.CategoryName))
                return BadRequest(new { status = false, message = "Invalid data." });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Check for duplicate name
                string existsQuery = @"SELECT COUNT(*) FROM tbl_item_category 
                               WHERE LOWER(name) = LOWER(@name) AND id != @id";
                using (var existsCmd = new MySqlCommand(existsQuery, conn))
                {
                    existsCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                    existsCmd.Parameters.AddWithValue("@id", model.Id);
                    int count = Convert.ToInt32(await existsCmd.ExecuteScalarAsync());
                    if (count > 0)
                        return Conflict(new { status = false, message = "Category name already exists" });
                }

                // ✅ Update category
                string updateQuery = @"UPDATE tbl_item_category SET name=@name WHERE id=@id";
                using var cmd = new MySqlCommand(updateQuery, conn);
                cmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                cmd.Parameters.AddWithValue("@id", model.Id);
                int rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? Ok(new { status = true, message = "Category updated successfully", id = model.Id, categoryName = model.CategoryName.Trim() })
                    : NotFound(new { status = false, message = "Category not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid category ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Check if category is used in tbl_items
                string checkQuery = "SELECT COUNT(1) FROM tbl_items WHERE category_id = @id";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                    if (count > 0)
                        return Ok(new { status = false, message = "Category already used in items." });
                }

                // ✅ Delete category
                string deleteQuery = "DELETE FROM tbl_item_category WHERE id = @id";
                using var deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@id", id);

                int rows = await deleteCmd.ExecuteNonQueryAsync();

                if (rows > 0)
                    return Ok(new { status = true, message = "Category deleted successfully." });
                else
                    return NotFound(new { status = false, message = "Category not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
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
                // Build connection string using session database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };


                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = "SELECT id, Name, value, Description FROM tbl_tax WHERE state = 0";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var taxes = new List<object>();

                while (await reader.ReadAsync())
                {
                    taxes.Add(new
                    {
                        Id = reader["id"],
                        Name = reader["Name"],
                        Value = reader["value"],
                        Description = reader["Description"]
                    });
                }

                return Ok(new { status = true, data = taxes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddTax([FromBody] TaxViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Name))
                    return BadRequest(new { status = false, message = "Tax name is required" });
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Check if tax already exists
                string existsQuery = "SELECT COUNT(*) FROM tbl_tax WHERE LOWER(Name) = LOWER(@name)";
                using (var existsCmd = new MySqlCommand(existsQuery, conn))
                {
                    existsCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    int count = Convert.ToInt32(await existsCmd.ExecuteScalarAsync());
                    if (count > 0)
                        return Conflict(new { status = false, message = "Tax name already exists" });
                }

                var query = "INSERT INTO tbl_tax (Name, Value, Description, state) VALUES (@name, @value, @description, 0)";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", model.Name);
                cmd.Parameters.AddWithValue("@value", model.Value);
                cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                await cmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Tax added successfully" });
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditTax( [FromBody] TaxViewModel model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Name))
                    return BadRequest(new { status = false, message = "Tax name is required" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                                              ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Check if tax already exists
                string existsQuery = "SELECT COUNT(*) FROM tbl_tax WHERE LOWER(Name) = LOWER(@name) AND id <> @id";
                using (var existsCmd = new MySqlCommand(existsQuery, conn))
                {
                    existsCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    existsCmd.Parameters.AddWithValue("@id", model.Id);
                    int count = Convert.ToInt32(await existsCmd.ExecuteScalarAsync());
                    if (count > 0)
                        return Conflict(new { status = false, message = "Tax name already exists" });
                }

                var query = "UPDATE tbl_tax SET Name=@name, Value=@value, Description=@description WHERE id=@id";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", model.Id);
                cmd.Parameters.AddWithValue("@name", model.Name);
                cmd.Parameters.AddWithValue("@value", model.Value);
                cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0)
                    return NotFound(new { status = false, message = "Tax not found" });

                return Ok(new { status = true, message = "Tax updated successfully" });
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteTax(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid tax ID" });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName")
                                               ?? _config.GetConnectionString("DefaultDatabase")
            };

            await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            // ✅ Soft delete tax (state = -1)
            var deleteQuery = "UPDATE tbl_tax SET state = -1 WHERE id = @id";
            await using var deleteCmd = new MySqlCommand(deleteQuery, conn);
            deleteCmd.Parameters.AddWithValue("@id", id);

            var rows = await deleteCmd.ExecuteNonQueryAsync();

            if (rows > 0)
            {
                return Ok(new { status = true, message = "Tax deleted successfully." });
            }
            else
            {
                return NotFound(new { status = false, message = "Tax not found." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDeletedTaxes()
        {
            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName")
                                               ?? _config.GetConnectionString("DefaultDatabase")
            };

            using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            var query = "SELECT id, Name, value, Description FROM tbl_tax WHERE state = -1";
            using var cmd = new MySqlCommand(query, conn);

            var deletedTaxes = new List<object>();
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    deletedTaxes.Add(new
                    {
                        id = reader["id"],
                        name = reader["Name"],
                        value = reader["value"],
                        description = reader["Description"]
                    });
                }
            }

            return Ok(new { status = true, data = deletedTaxes });
        }
        [HttpPost]
        public async Task<IActionResult> RestoreTax([FromBody] RestoreTaxViewModel model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { status = false, message = "Invalid tax ID" });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName")
                          ?? _config.GetConnectionString("DefaultDatabase")
            };

            using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            var query = "UPDATE tbl_tax SET state = 0 WHERE id = @id";
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", model.Id);

            var rows = await cmd.ExecuteNonQueryAsync();

            if (rows > 0)
                return Ok(new { status = true, message = "Tax restored successfully." });
            else
                return NotFound(new { status = false, message = "Tax not found." });
        }

        #endregion

        #region Unit 

        public IActionResult Unit()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetUnits()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var units = new List<UnitRequest>();

                // MAIN UNITS
                string mainQuery = "SELECT id, name FROM tbl_unit";
                using (var cmd = new MySqlCommand(mainQuery, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        units.Add(new UnitRequest
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Name = reader["name"]?.ToString(),
                            IsMain = true,
                            IsSub = false
                        });
                    }
                }

                // SUB UNITS
                string subQuery = "SELECT id, code, name, main_id FROM tbl_sub_unit ORDER BY code";
                using (var cmd = new MySqlCommand(subQuery, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        units.Add(new UnitRequest
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            Code = reader["code"]?.ToString(),
                            Name = reader["name"]?.ToString(),
                            IsMain = false,
                            IsSub = true,
                            MainId = Convert.ToInt32(reader["main_id"])
                        });
                    }
                }

                return Ok(units);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMainUnits()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var mainUnits = new List<object>();

                var query = "SELECT id, name FROM tbl_unit ORDER BY name";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    mainUnits.Add(new
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        Name = reader["name"].ToString(),
                    });
                }

                return Ok(new { status = true, data = mainUnits });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> SaveUnit([FromBody] UnitRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Enter Unit Name" });

            var connStrBuilder = new MySqlConnectionStringBuilder(
                _config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName")
                           ?? _config.GetConnectionString("DefaultDatabase")
            };

            try
            {
                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // MAIN unit
                if (model.IsMain)
                {
                    // Duplicate check (case-insensitive)
                    string existsQuery = model.Id > 0
                        ? "SELECT COUNT(*) FROM tbl_unit WHERE LOWER(name) = LOWER(@name) AND id <> @id"
                        : "SELECT COUNT(*) FROM tbl_unit WHERE LOWER(name) = LOWER(@name)";

                    using (var existsCmd = new MySqlCommand(existsQuery, conn))
                    {
                        existsCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                        if (model.Id > 0) existsCmd.Parameters.AddWithValue("@id", model.Id);

                        int count = Convert.ToInt32(await existsCmd.ExecuteScalarAsync());
                        if (count > 0)
                            return Conflict(new { status = false, message = "Main unit name already exists." });
                    }

                    if (model.Id > 0)
                    {
                        string update = "UPDATE tbl_unit SET name=@name WHERE id=@id";
                        using var cmd = new MySqlCommand(update, conn);
                        cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                        cmd.Parameters.AddWithValue("@id", model.Id);
                        await cmd.ExecuteNonQueryAsync();

                        return Ok(new { status = true, id = model.Id, name = model.Name.Trim(), message = "Main unit updated successfully." });
                    }
                    else
                    {
                        string insert = "INSERT INTO tbl_unit (name) VALUES (@name); SELECT LAST_INSERT_ID();";
                        using var cmd = new MySqlCommand(insert, conn);
                        cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                        var newIdObj = await cmd.ExecuteScalarAsync();
                        int newId = Convert.ToInt32(newIdObj);
                        return Ok(new { status = true, id = newId, name = model.Name.Trim(), message = "Main unit added successfully." });
                    }
                }
                else // SUB unit
                {
                    if (model.MainId == null || model.MainId <= 0)
                        return BadRequest(new { status = false, message = "Select Main Unit" });

                    // Duplicate check within same main unit (case-insensitive)
                    string existsSubQuery = model.Id > 0
                        ? "SELECT COUNT(*) FROM tbl_sub_unit WHERE LOWER(name) = LOWER(@name) AND main_id = @mainId AND id <> @id"
                        : "SELECT COUNT(*) FROM tbl_sub_unit WHERE LOWER(name) = LOWER(@name) AND main_id = @mainId";

                    using (var existsCmd = new MySqlCommand(existsSubQuery, conn))
                    {
                        existsCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                        existsCmd.Parameters.AddWithValue("@mainId", model.MainId);
                        if (model.Id > 0) existsCmd.Parameters.AddWithValue("@id", model.Id);

                        int count = Convert.ToInt32(await existsCmd.ExecuteScalarAsync());
                        if (count > 0)
                            return Conflict(new { status = false, message = "Sub unit name already exists for the selected main unit." });
                    }

                    if (model.Id > 0)
                    {
                        string update = "UPDATE tbl_sub_unit SET name=@name, main_id=@main WHERE id=@id";
                        using var cmd = new MySqlCommand(update, conn);
                        cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                        cmd.Parameters.AddWithValue("@main", model.MainId);
                        cmd.Parameters.AddWithValue("@id", model.Id);
                        await cmd.ExecuteNonQueryAsync();

                        return Ok(new { status = true, id = model.Id, name = model.Name.Trim(), message = "Sub unit updated successfully." });
                    }
                    else
                    {
                        string insert = "INSERT INTO tbl_sub_unit (name, main_id) VALUES (@name,@main); SELECT LAST_INSERT_ID();";
                        using var cmd = new MySqlCommand(insert, conn);
                        cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                        cmd.Parameters.AddWithValue("@main", model.MainId);
                        var newIdObj = await cmd.ExecuteScalarAsync();
                        int newId = Convert.ToInt32(newIdObj);
                        return Ok(new { status = true, id = newId, name = model.Name.Trim(), message = "Sub unit added successfully." });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> EditUnit([FromBody] UnitViewModel model)
        {
            try
            {
                if (model.Id <= 0 || string.IsNullOrWhiteSpace(model.Name))
                    return BadRequest(new { status = false, message = "Invalid data." });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                              ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Check if the name already exists in another row
                string existsQuery = "SELECT COUNT(*) FROM tbl_unit WHERE LOWER(Name) = LOWER(@name) AND id != @id";
                using (var existsCmd = new MySqlCommand(existsQuery, conn))
                {
                    existsCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    existsCmd.Parameters.AddWithValue("@id", model.Id);
                    int count = Convert.ToInt32(await existsCmd.ExecuteScalarAsync());
                    if (count > 0)
                        return Conflict(new { status = false, message = "Unit name already exists" });
                }

                // ✅ Update unit
                var query = "UPDATE tbl_unit SET Name = @name WHERE id = @id";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                cmd.Parameters.AddWithValue("@id", model.Id);
                var rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? Ok(new
                    {
                        status = true,
                        message = "Unit updated successfully.",
                        id = model.Id,
                        name = model.Name.Trim()
                    })
                    : NotFound(new { status = false, message = "Unit not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteMainUnit([FromBody] int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid main unit ID" });

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

                // Check if main unit is used in items
                var checkQuery = "SELECT COUNT(1) FROM tbl_items WHERE unit_id = @id";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                        return Ok(new { status = false, message = "Main unit is already used in items and cannot be deleted." });
                }

                // Check if main unit has sub units
                var checkSubQuery = "SELECT COUNT(1) FROM tbl_sub_unit WHERE main_id = @id";
                using (var checkSubCmd = new MySqlCommand(checkSubQuery, conn))
                {
                    checkSubCmd.Parameters.AddWithValue("@id", id);
                    var subCount = Convert.ToInt32(await checkSubCmd.ExecuteScalarAsync());
                    if (subCount > 0)
                        return Ok(new { status = false, message = "Main unit has sub units. Delete sub units first." });
                }

                // Delete main unit
                var deleteQuery = "DELETE FROM tbl_unit WHERE id = @id";
                using (var delCmd = new MySqlCommand(deleteQuery, conn))
                {
                    delCmd.Parameters.AddWithValue("@id", id);
                    var rows = await delCmd.ExecuteNonQueryAsync();
                    return rows > 0
                        ? Ok(new { status = true, message = "Main unit deleted successfully." })
                        : NotFound(new { status = false, message = "Main unit not found." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubUnit([FromBody] int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid sub unit ID" });

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

                // Check if sub unit is used in items_unit
                var checkQuery = "SELECT COUNT(1) FROM tbl_items_unit WHERE unit_id = @id";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                        return Ok(new { status = false, message = "Sub unit is already used in items and cannot be deleted." });
                }

                // Delete sub unit
                var deleteQuery = "DELETE FROM tbl_sub_unit WHERE id = @id";
                using (var delCmd = new MySqlCommand(deleteQuery, conn))
                {
                    delCmd.Parameters.AddWithValue("@id", id);
                    var rows = await delCmd.ExecuteNonQueryAsync();
                    return rows > 0
                        ? Ok(new { status = true, message = "Sub unit deleted successfully." })
                        : NotFound(new { status = false, message = "Sub unit not found." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUnit([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { status = false, message = "Invalid unit id." });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                                 ?? _config.GetConnectionString("DefaultDatabase")
                };
                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if unit is used
                var checkQuery = "SELECT COUNT(1) FROM tbl_items WHERE unit_id = @id";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                    if (count > 0)
                        return Ok(new { status = false, message = "Unit is already used in items and cannot be deleted." });
                }

                // Delete if not used
                var deleteQuery = "DELETE FROM tbl_unit WHERE id = @id";
                using (var delCmd = new MySqlCommand(deleteQuery, conn))
                {
                    delCmd.Parameters.AddWithValue("@id", id);
                    var rows = await delCmd.ExecuteNonQueryAsync();

                    return rows > 0
                        ? Ok(new { status = true, message = "Unit deleted successfully." })
                        : Ok(new { status = false, message = "Unit not found." });
                }
            }
            catch(Exception ex)
            {
                throw ex;
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
                int costCenter = model.CostCenter ?? 0;
                // ✅ 6️⃣ Insert into Level 4 table
                int newId;
                string insertQuery = @"
            INSERT INTO tbl_coa_level_4 (name, code, main_id, debit, credit, date, costcenter)
            VALUES (@name, @code, @main_id, @debit, @credit, @date, costcenter);
            SELECT LAST_INSERT_ID();";

                using (var insertCmd = new MySqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    insertCmd.Parameters.AddWithValue("@code", newCode);
                    insertCmd.Parameters.AddWithValue("@main_id", model.Level3Id);
                    insertCmd.Parameters.AddWithValue("@debit", debit);
                    insertCmd.Parameters.AddWithValue("@credit", credit);
                    insertCmd.Parameters.AddWithValue("@date", model.Date ?? DateTime.Now);
                    insertCmd.Parameters.AddWithValue("@costcenter", model.CostCenter ?? 0);
                    newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                }

                // ✅ 7️⃣ Insert opening balance transactions if any
                if (debit > 0 || credit > 0)
                {
                    int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    if (userId <= 0)
                        return Unauthorized(new { status = false, message = "User not logged in" });

                    await InsertTransactionsAsync(conn, newId, newCode, debit, credit, model.Date ?? DateTime.Now, userId, costCenter);
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


        private async Task InsertTransactionsAsync(MySqlConnection conn, int refId, string code, decimal debit, decimal credit, DateTime date, int userId, int costCenter)
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
                    "Opening Balance Equity - Ledger", userId, DateTime.Now.Date, "", costCenter);

                await AddTransactionAsync(conn, date, accountId, 0, credit,
                    refId.ToString(), "0", "GENERAL LEDGER OPENING BALANCE", "General Ledger Opening Balance",
                    "Account Payable - Ledger Code", userId, DateTime.Now.Date, "", costCenter);
            }

            // ✅ Debit entries
            if (debit > 0)
            {
                await AddTransactionAsync(conn, date, openingBalanceEquityId, 0, debit,
                    refId.ToString(), "0", "GENERAL LEDGER OPENING BALANCE", "General Ledger Opening Balance",
                    "Opening Balance Equity - Ledger Code", userId, DateTime.Now.Date, "" ,costCenter);

                await AddTransactionAsync(conn, date, accountId, debit, 0,
                    refId.ToString(), "0", "GENERAL LEDGER OPENING BALANCE", "General Ledger Opening Balance",
                    "Account Payable - Ledger Code", userId, DateTime.Now.Date, "", costCenter);
            }
        }


        private async Task AddTransactionAsync(MySqlConnection conn, DateTime date, string accountId, decimal debit, decimal credit,
                               string transactionId, string humId, string type, string voucher_name, string description,
                               int createdBy, DateTime createdDate, string VoucherNo, int costCenter)
        {
            try
            {
                string query = @"
INSERT INTO tbl_transaction 
(date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no, costcenter) 
VALUES (@date, @accountId, @debit, @credit, @transactionId, @hum_id, @tType, @type, @description, @createdBy, @createdDate, 0, @voucher_no, @costcenter);";

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
                cmd.Parameters.AddWithValue("@costcenter", costCenter);

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
                // Build connection string using session database
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                string connStr = connStrBuilder.ConnectionString;

                // ---------------- Level 1 ----------------
                List<CoaNode> level1 = new();
                using (var conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "SELECT id, code, name FROM tbl_coa_level_1 ORDER BY id";

                    using var cmd = new MySqlCommand(query, conn);
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        level1.Add(new CoaNode
                        {
                            Id = reader.GetInt32("id"),
                            Code = reader["code"].ToString(),
                            Name = reader["name"].ToString(),
                            Children = new List<CoaNode>()
                        });
                    }
                }

                // ---------------- Level 2 ----------------
                var level2Dict = new Dictionary<int, List<CoaNode>>();
                using (var conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "SELECT id, code, name, main_id FROM tbl_coa_level_2 ORDER BY id";

                    using var cmd = new MySqlCommand(query, conn);
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var node = new CoaNode
                        {
                            Id = reader.GetInt32("id"),
                            Code = reader["code"].ToString(),
                            Name = reader["name"].ToString(),
                            Children = new List<CoaNode>()
                        };

                        int parent = reader.GetInt32("main_id");
                        if (!level2Dict.ContainsKey(parent))
                            level2Dict[parent] = new List<CoaNode>();

                        level2Dict[parent].Add(node);
                    }
                }

                // Attach level 2
                foreach (var l1 in level1)
                    if (level2Dict.ContainsKey(l1.Id))
                        l1.Children.AddRange(level2Dict[l1.Id]);

                // ---------------- Level 3 ----------------
                var level3Dict = new Dictionary<int, List<CoaNode>>();
                using (var conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "SELECT id, code, name, main_id FROM tbl_coa_level_3 ORDER BY id";

                    using var cmd = new MySqlCommand(query, conn);
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var node = new CoaNode
                        {
                            Id = reader.GetInt32("id"),
                            Code = reader["code"].ToString(),
                            Name = reader["name"].ToString(),
                            Children = new List<CoaNode>()
                        };

                        int parent = reader.GetInt32("main_id");
                        if (!level3Dict.ContainsKey(parent))
                            level3Dict[parent] = new List<CoaNode>();

                        level3Dict[parent].Add(node);
                    }
                }

                // Attach level 3
                foreach (var l1 in level1)
                    foreach (var l2 in l1.Children)
                        if (level3Dict.ContainsKey(l2.Id))
                            l2.Children.AddRange(level3Dict[l2.Id]);

                // ---------------- Level 4 ----------------
                var level4Dict = new Dictionary<int, List<CoaNode>>();
                using (var conn = new MySqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = "SELECT id, code, name, main_id, debit, credit, date, costcenter FROM tbl_coa_level_4 ORDER BY id";

                    using var cmd = new MySqlCommand(query, conn);
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var node = new CoaNode
                        {
                            Id = reader.GetInt32("id"),
                            Code = reader["code"].ToString(),
                            Name = reader["name"].ToString(),
                            Debit = reader["debit"] != DBNull.Value ? reader.GetDecimal("debit") : 0,
                            Credit = reader["credit"] != DBNull.Value ? reader.GetDecimal("credit") : 0,
                            Date = reader["date"] != DBNull.Value ? reader.GetDateTime("date") : null,
                            CostCenter = reader.IsDBNull(reader.GetOrdinal("costcenter"))?  0 : reader.GetInt32(reader.GetOrdinal("costcenter"))

                        };

                        int parent = reader.GetInt32("main_id");
                        if (!level4Dict.ContainsKey(parent))
                            level4Dict[parent] = new List<CoaNode>();

                        level4Dict[parent].Add(node);
                    }
                }

                // Attach level 4
                foreach (var l1 in level1)
                    foreach (var l2 in l1.Children)
                        foreach (var l3 in l2.Children)
                            if (level4Dict.ContainsKey(l3.Id))
                                l3.Children.AddRange(level4Dict[l3.Id]);

                return Ok(new { status = true, data = level1 });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
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
                return BadRequest(new { status = false, message = "Account name is required." });

            if (model.Date.HasValue && model.Date.Value.Date < DateTime.Today)
                return BadRequest(new { status = false, message = "Date cannot be earlier than today." });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                        ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Load existing code (we will not change it)
                string existingCode = "";
                using (var cmd = new MySqlCommand("SELECT code FROM tbl_coa_level_4 WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    var res = await cmd.ExecuteScalarAsync();
                    if (res == null)
                        return NotFound(new { status = false, message = "Account not found" });

                    existingCode = res.ToString();
                }

                // 🔹 Check duplicate name (except same record)
                using (var cmd = new MySqlCommand(
                    "SELECT id FROM tbl_coa_level_4 WHERE name = @name AND id != @id", conn))
                {
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    var exists = await cmd.ExecuteScalarAsync();
                    if (exists != null)
                        return BadRequest(new { status = false, message = "Account name already exists." });
                }

                // In EditCoaLevel4, update the SQL:
                using (var cmd = new MySqlCommand(@"
    UPDATE tbl_coa_level_4
    SET name = @name,
        main_id = @level3Id, -- allow changing parent
        debit = @debit,
        credit = @credit,
        date = @date,
        costCenter = @costCenter
    WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@level3Id", model.Level3Id); // new parent
                    cmd.Parameters.AddWithValue("@debit", model.Debit ?? 0);
                    cmd.Parameters.AddWithValue("@credit", model.Credit ?? 0);
                    cmd.Parameters.AddWithValue("@date", model.Date ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@costCenter", model.CostCenter ?? 0);
                    await cmd.ExecuteNonQueryAsync();
                }
                // 🔹 Handle Opening Balance
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                // Delete previous entries
                using (var cmd = new MySqlCommand(
                    "DELETE FROM tbl_transaction WHERE transaction_id = @tid AND t_type = 'GENERAL LEDGER OPENING BALANCE'", conn))
                {
                    cmd.Parameters.AddWithValue("@tid", model.Id);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Insert if needed
                if ((model.Debit ?? 0) > 0 || (model.Credit ?? 0) > 0)
                {
                    await InsertTransactionsAsync(
                        conn,
                        model.Id,
                        existingCode,                   
                        model.Debit ?? 0,
                        model.Credit ?? 0,
                        model.Date ?? DateTime.Now,
                        userId,
                        model.CostCenter ?? 0
                    );
                }

                // 🔹 Return response
                return Ok(new
                {
                    status = true,
                    message = "Account updated successfully.",
                    data = new
                    {
                        Id = model.Id,
                        Name = model.Name,
                        Code = existingCode,
                        Debit = model.Debit ?? 0,
                        Credit = model.Credit ?? 0,
                        Date = model.Date,
                        CostCenter = model.CostCenter ?? 0
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetLevel4AccountById(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid Level 4 ID" });

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
                l4.id AS Level4Id,
                l4.name AS Level4Name,
                l4.code AS Level4Code,
                l4.debit AS Debit,
                l4.credit AS Credit,
                l4.date AS Date,
                IFNULL(l4.costcenter, 0) AS CostCenter,
                l4.main_id AS Level3Id,
                l3.name AS Level3Name,
                l3.main_id AS Level2Id,
                l2.name AS Level2Name,
                l2.main_id AS Level1Id,
                l1.name AS Level1Name
            FROM tbl_coa_level_4 l4
            INNER JOIN tbl_coa_level_3 l3 ON l4.main_id = l3.id
            INNER JOIN tbl_coa_level_2 l2 ON l3.main_id = l2.id
            INNER JOIN tbl_coa_level_1 l1 ON l2.main_id = l1.id
            WHERE l4.id = @id
            LIMIT 1;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var data = new
                    {
                        Level4Id = reader["Level4Id"] is DBNull ? 0 : Convert.ToInt32(reader["Level4Id"]),
                        Level4Name = reader["Level4Name"]?.ToString() ?? "",
                        Level4Code = reader["Level4Code"]?.ToString() ?? "",
                        Debit = reader["Debit"] != DBNull.Value ? Convert.ToDecimal(reader["Debit"]) : 0,
                        Credit = reader["Credit"] != DBNull.Value ? Convert.ToDecimal(reader["Credit"]) : 0,
                        Date = reader["Date"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["Date"]),
                        CostCenter = reader["CostCenter"] != DBNull.Value ? Convert.ToInt32(reader["CostCenter"]) : 0,
                        Level3Id = reader["Level3Id"] != DBNull.Value ? Convert.ToInt32(reader["Level3Id"]) : 0,
                        Level3Name = reader["Level3Name"]?.ToString() ?? "",
                        Level2Id = reader["Level2Id"] != DBNull.Value ? Convert.ToInt32(reader["Level2Id"]) : 0,
                        Level2Name = reader["Level2Name"]?.ToString() ?? "",
                        Level1Id = reader["Level1Id"] != DBNull.Value ? Convert.ToInt32(reader["Level1Id"]) : 0,
                        Level1Name = reader["Level1Name"]?.ToString() ?? ""
                    };

                    return Ok(new { status = true, data });
                }
                else
                {
                    return NotFound(new { status = false, message = "Level 4 account not found." });
                }
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

        [HttpGet]
        public async Task<IActionResult> GetFixedAssetItem(DateTime? dateFrom, DateTime? dateTo, bool ignoreDate = false)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
               ?? _config.GetConnectionString("DefaultDatabase")
                };


                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY t.date DESC) AS SN,
                DATE_FORMAT(t.date, '%d/%m/%Y') AS `Date`,
                t.reference AS `RefId`,
                i.id As 'Id',
                i.name AS `Name`,
                i.code AS `Code`,
                i.id AS `ItemId`,
                (SELECT name FROM tbl_coa_level_4 WHERE id = i.asset_account_id) AS `Account`,
                t.cost_price AS `CostPrice`,
                t.description AS `Description`
            FROM tbl_items i
            LEFT JOIN tbl_item_transaction t ON t.id = (
                SELECT id 
                FROM tbl_item_transaction 
                WHERE item_id = i.id
                /**DATE_FILTER**/
                ORDER BY date DESC 
                LIMIT 1
            )
            WHERE i.item_type = 'Fixed Assets'
              AND i.state = 0
            ORDER BY t.date DESC;
        ";

                var parameters = new List<MySqlParameter>();
                if (!ignoreDate && dateFrom.HasValue && dateTo.HasValue)
                {
                    query = query.Replace("/**DATE_FILTER**/", "AND date BETWEEN @dateFrom AND @dateTo");
                    parameters.Add(new MySqlParameter("@dateFrom", dateFrom.Value));
                    parameters.Add(new MySqlParameter("@dateTo", dateTo.Value));
                }
                else
                {
                    query = query.Replace("/**DATE_FILTER**/", "");
                }

                var results = new List<InvoiceViewModel>();

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                using var cmd = new MySqlCommand(query, conn);
                if (parameters.Count > 0)
                    cmd.Parameters.AddRange(parameters.ToArray());

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(new InvoiceViewModel
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        SN = Convert.ToInt32(reader["SN"]),
                        Date = reader["Date"].ToString(),
                        RefId = reader["RefId"].ToString(),
                        Name = reader["Name"].ToString(),
                        Code = reader["Code"].ToString(),
                        ItemId = Convert.ToInt32(reader["ItemId"]),
                        Account = reader["Account"].ToString(),
                        CostPrice = reader["CostPrice"] != DBNull.Value ? Convert.ToDecimal(reader["CostPrice"]) : 0,
                        Description = reader["Description"].ToString()
                    });
                }

                return Ok(new { status = true, data = results });
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        #endregion

        #region Cost Center

        public IActionResult CostCenter()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCostCentersss()
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
                //var mainQuery = "SELECT id, code, name FROM tbl_cost_center ORDER BY code";
                //using (var cmd = new MySqlCommand(mainQuery, conn))
                //using (var reader = await cmd.ExecuteReaderAsync())
                //{
                //    while (await reader.ReadAsync())
                //    {
                //        costCenters.Add(new CostCenterRequest
                //        {
                //            Id = Convert.ToInt32(reader["id"]),
                //            Code = reader["code"].ToString(),
                //            Name = reader["name"].ToString(),
                //            IsMain = true,
                //            IsSub = false,
                //            //MainId = null
                //        });
                //    }
                //}

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
                        int newMainCode = 1;
                        string getMaxMainCode = "SELECT MAX(CAST(code AS UNSIGNED)) FROM tbl_cost_center";
                        using (var cmd = new MySqlCommand(getMaxMainCode, conn))
                        {
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != DBNull.Value && result != null)
                                newMainCode = Convert.ToInt32(result) + 1;
                        }

                        // Pad to always 2 digits
                        string formattedMainCode = newMainCode.ToString("D2");

                        string insertQuery = "INSERT INTO tbl_cost_center (name, code, project_id) VALUES (@name,@code,@project_id)";
                        using var cmdInsert = new MySqlCommand(insertQuery, conn);
                        cmdInsert.Parameters.AddWithValue("@name", model.Name);
                        cmdInsert.Parameters.AddWithValue("@code", formattedMainCode);
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
                        int newSubCode = 1;
                        string getMaxSubQuery = "SELECT MAX(code) FROM tbl_sub_cost_center WHERE main_id=@mainId";
                        using (var cmd = new MySqlCommand(getMaxSubQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@mainId", model.MainId);
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != DBNull.Value && result != null)
                                newSubCode = Convert.ToInt32(result) + 1;
                        }
                        string formattedSubCode = newSubCode.ToString("D3");
                        string insertSubQuery = "INSERT INTO tbl_sub_cost_center (name, code, main_id, project_id) VALUES (@name,@code,@main_id,@project_id)";
                        using var cmdInsert = new MySqlCommand(insertSubQuery, conn);
                        cmdInsert.Parameters.AddWithValue("@name", model.Name);
                        cmdInsert.Parameters.AddWithValue("@code", formattedSubCode);
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

                string checkQuery2 = @"SELECT COUNT(1) 
                               FROM tbl_transaction 
                               WHERE costcenter=@id";

                using (var checkCmd2 = new MySqlCommand(checkQuery2, conn))
                {
                    checkCmd2.Parameters.AddWithValue("@id", id);
                    var count2 = Convert.ToInt32(await checkCmd2.ExecuteScalarAsync());
                    if (count2 > 0)
                        return BadRequest(new { status = false, message = "This cost center is already used in journal transactions." });
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
            code as Code,
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
                        item_type as Item_type,
                       costcenter as CostCenter
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
                        code = reader.GetString("code"),
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
                        active = reader.IsDBNull("Active") ? (int?)null : reader.GetInt32("Active"),
                        method = reader.GetString("Method"),
                        category_id = reader.IsDBNull("Category_id") ? (int?)null : reader.GetInt32("Category_id"),
                        posItem = reader.IsDBNull("PosItem") ? (int?)null : reader.GetInt32("PosItem"),
                        item_type = reader.GetString("Item_type"),
                        costcenter = reader.IsDBNull("CostCenter") ? (int?)null: reader.GetInt32("CostCenter"),
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
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Validation
                var validationResult = await ValidateItemDataAsync(conn, model, 0);
                if (!validationResult.isValid)
                    return BadRequest(new { status = false, message = validationResult.message });

                // Check if item already exists
                using (var cmdCheck = new MySqlCommand("SELECT * FROM tbl_items WHERE name = @name", conn))
                {
                    cmdCheck.Parameters.AddWithValue("@name", model.Name);
                    using var reader = await cmdCheck.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        return BadRequest(new { status = false, message = "Item Already Exists. Enter Another Name." });
                    }
                }

                // Generate next code
                string itemCode = await GetNextCodeAsync(conn, model.Type, model.CategoryId);

                // Insert Item
                var insertQuery = @"
                    INSERT INTO `tbl_items`(
                        `code`, `warehouse_id`, `type`, `category_id`, `name`, `unit_id`, `barcode`, 
                        `cost_price`, `cogs_account_id`, `vendor_id`, `sales_price`, `income_account_id`, 
                        `asset_account_id`, `min_amount`, `max_amount`, `on_hand`, `method`, `total_value`, 
                        `date`, `img`, `active`, `state`, `created_By`, `created_date`, `Item_type`, `costcenter`
                    ) VALUES (
                        @code, @warehouseId, @type, @category, @name, @unit_id, @barcode, @cost_price, 
                        @cogs_account_id, @vendor_id, @sales_price, @income_account_id, @asset_account_id, 
                        @min_amount, @max_amount, @on_hand, @method, @total_value, @date, @img, @active, 
                        @state, @created_By, @created_date, @Item_type, @costcenter
                    ); 
                    SELECT LAST_INSERT_ID();";

                int itemId;
                using (var cmd = new MySqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@code", itemCode);
                    cmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                    cmd.Parameters.AddWithValue("@type", model.Type);
                    cmd.Parameters.AddWithValue("@category", model.CategoryId);
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@unit_id", model.UnitId == 0 ? (object)DBNull.Value : model.UnitId);
                    cmd.Parameters.AddWithValue("@barcode", model.Barcode ?? "");
                    cmd.Parameters.AddWithValue("@cost_price", model.CostPrice == 0 ? "0" : model.CostPrice.ToString());
                    cmd.Parameters.AddWithValue("@cogs_account_id", model.CogsAccountId);
                    cmd.Parameters.AddWithValue("@vendor_id", model.VendorId ?? 0);
                    cmd.Parameters.AddWithValue("@sales_price", model.SalesPrice == 0 ? "0" : model.SalesPrice.ToString());
                    cmd.Parameters.AddWithValue("@income_account_id", model.IncomeAccountId);
                    cmd.Parameters.AddWithValue("@asset_account_id", model.AssetAccountId);
                    cmd.Parameters.AddWithValue("@min_amount", model.MinAmount);
                    cmd.Parameters.AddWithValue("@max_amount", model.MaxAmount);
                    cmd.Parameters.AddWithValue("@on_hand", model.OnHand);
                    cmd.Parameters.AddWithValue("@method", model.Method);
                    cmd.Parameters.AddWithValue("@total_value", model.TotalValue);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@img", "");
                    cmd.Parameters.AddWithValue("@active", model.Active ? 0 : 1);
                    cmd.Parameters.AddWithValue("@state", 0);
                    cmd.Parameters.AddWithValue("@created_By", model.CreatedBy);
                    cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@Item_type", model.ItemType ?? "");
                    cmd.Parameters.AddWithValue("@costcenter", model.CostCenter ?? 0);

                    itemId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Insert Item Units
                if (model.Type != "12 - Service" && model.UnitId != 0)
                {
                    await InsertItemUnitsAsync(conn, itemId, model);
                }

                // Insert Item Assembly
                if (model.Type == "13 - Inventory Assembly" && model.Assemblies != null && model.Assemblies.Any())
                {
                    await InsertItemsAssemblyAsync(conn, itemId, model.Assemblies);
                }

                // Insert Item Transaction and Journal
                if (model.OnHand != 0 && model.Type != "12 - Service")
                {
                    await InsertItemTransactionAsync(conn, itemId, model, itemCode);
                    await InsertItemJournalAsync(conn, itemId, model, itemCode);
                }


                return Ok(new { status = true, message = "Item added successfully", itemId, itemCode });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateItem([FromBody] ItemRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (model.Id <= 0)
                return BadRequest(new { status = false, message = "Invalid item ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Validation
                var validationResult = await ValidateItemDataAsync(conn, model, model.Id);
                if (!validationResult.isValid)
                    return BadRequest(new { status = false, message = validationResult.message });

                // Check if item name already exists (excluding current item)
                using (var cmdCheck = new MySqlCommand("SELECT * FROM tbl_items WHERE name = @name", conn))
                {
                    cmdCheck.Parameters.AddWithValue("@name", model.Name);
                    using var reader = await cmdCheck.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        int existingId = Convert.ToInt32(reader["id"]);
                        if (existingId != model.Id)
                        {
                            return BadRequest(new { status = false, message = "Item Already Exists. Enter Another Name." });
                        }
                    }
                }

                // Update Item
                var updateQuery = @"
                    UPDATE tbl_items SET 
                        type = @type, category_id = @category, name = @name, unit_id = @unit_id, 
                        barcode = @barcode, cost_price = @cost_price, cogs_account_id = @cogs_account_id, 
                        vendor_id = @vendor_id, sales_price = @sales_price, income_account_id = @income_account_id, 
                        asset_account_id = @asset_account_id, min_amount = @min_amount, max_amount = @max_amount, 
                        on_hand = @on_hand, method = @method, total_value = @total_value, date = @date, 
                        img = @img, active = @active, state = @state, created_By = @created_By, 
                        created_date = @created_date, Item_type = @Item_type , costcenter = @costcenter
                    WHERE id = @id";

                using (var cmd = new MySqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@type", model.Type);
                    cmd.Parameters.AddWithValue("@name", model.Name);
                    cmd.Parameters.AddWithValue("@category", model.CategoryId);
                    cmd.Parameters.AddWithValue("@unit_id", model.UnitId == 0 ? (object)DBNull.Value : model.UnitId);
                    cmd.Parameters.AddWithValue("@barcode", model.Barcode ?? "");
                    cmd.Parameters.AddWithValue("@cost_price", model.CostPrice == 0 ? null : model.CostPrice.ToString());
                    cmd.Parameters.AddWithValue("@cogs_account_id", model.CogsAccountId);
                    cmd.Parameters.AddWithValue("@vendor_id", model.VendorId ?? 0);
                    cmd.Parameters.AddWithValue("@sales_price", model.SalesPrice == 0 ? null : model.SalesPrice.ToString());
                    cmd.Parameters.AddWithValue("@income_account_id", model.IncomeAccountId);
                    cmd.Parameters.AddWithValue("@asset_account_id", model.AssetAccountId);
                    cmd.Parameters.AddWithValue("@min_amount", model.MinAmount);
                    cmd.Parameters.AddWithValue("@max_amount", model.MaxAmount);
                    cmd.Parameters.AddWithValue("@on_hand", model.OnHand);
                    cmd.Parameters.AddWithValue("@method", model.Method);
                    cmd.Parameters.AddWithValue("@total_value", model.TotalValue);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@img", "");
                    cmd.Parameters.AddWithValue("@active", model.Active ? 0 : 1);
                    cmd.Parameters.AddWithValue("@state", 0);
                    cmd.Parameters.AddWithValue("@created_By", model.CreatedBy);
                    cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@Item_type", model.ItemType ?? "");
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@costcenter", model.CostCenter ?? 0);

                    await cmd.ExecuteNonQueryAsync();
                }

                // Delete old units and assembly
                using (var cmdDeleteUnits = new MySqlCommand("DELETE FROM tbl_items_unit WHERE item_id = @id", conn))
                {
                    cmdDeleteUnits.Parameters.AddWithValue("@id", model.Id);
                    await cmdDeleteUnits.ExecuteNonQueryAsync();
                }

                using (var cmdDeleteAssembly = new MySqlCommand("DELETE FROM tbl_item_assembly WHERE assembly_id = @id", conn))
                {
                    cmdDeleteAssembly.Parameters.AddWithValue("@id", model.Id);
                    await cmdDeleteAssembly.ExecuteNonQueryAsync();
                }

                // Re-insert Item Units
                if (model.Type != "12 - Service" && model.UnitId != 0)
                {
                    await InsertItemUnitsAsync(conn, model.Id, model);
                }

                // Re-insert Item Assembly
                if (model.Type == "13 - Inventory Assembly" && model.Assemblies != null && model.Assemblies.Any())
                {
                    await InsertItemsAssemblyAsync(conn, model.Id, model.Assemblies);
                }

                // Delete old transactions and journal entries
                using (var cmdDeleteTrans = new MySqlCommand(
                    @"DELETE FROM tbl_item_transaction WHERE type = 'Opening Qty' AND item_id = @id;
                      DELETE FROM tbl_transaction WHERE type = 'Opening Qty' AND transaction_id = @id;
                      DELETE FROM tbl_item_card_details WHERE trans_type = 'Opening Qty' AND itemId = @id;", conn))
                {
                    cmdDeleteTrans.Parameters.AddWithValue("@id", model.Id);
                    await cmdDeleteTrans.ExecuteNonQueryAsync();
                }

                // Re-insert transactions and journal
                await InsertItemTransactionAsync(conn, model.Id, model, model.Code);
                await InsertItemJournalAsync(conn, model.Id, model, model.Code);

                return Ok(new { status = true, message = "Item updated successfully", itemId = model.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task<(bool isValid, string message)> ValidateItemDataAsync(MySqlConnection conn, ItemRequest model, int currentId)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return (false, "Enter Item Name First.");

            if (model.WarehouseId == 0)
                return (false, "Enter Warehouse First");

            if (model.CategoryId == 0)
                return (false, "Enter Category First");

            if (model.OnHand != 0 && model.CostPrice == 0)
                return (false, "Enter Cost Price");

            if (model.Type != "12 - Service")
            {
                if (model.Date > DateTime.Now.Date && model.OnHand != 0)
                    return (false, "Date Must Be Less Or Equal Today");

                //// Check default accounts
                //if (!await AreDefaultAccountsSetAsync(conn, new List<string> { "COGS", "Sales", "Inventory" }))
                //    return (false, "Default accounts for Item are not properly configured. Please check your settings.");

                //// Check if main unit is in sub units
                //if (model.Units != null && model.Units.Any())
                //{
                //    foreach (var unit in model.Units)
                //    {
                //        if (unit.UnitId.HasValue && unit.UnitId.Value == model.UnitId)
                //            return (false, "Main Unit Can't Be Included As Sub Unit");
                //    }
                //}
                //foreach (var unit in model.Units)  // Units = list of SUB units only
                //{
                //    if (unit.UnitId.HasValue && unit.UnitId.Value == model.UnitId)  // Check if sub unit = main unit
                //        return (false, "Main Unit Can't Be Included As Sub Unit");
                //}
            }

            // Validate assembly items
            if (model.Type == "13 - Inventory Assembly")
            {
                if (model.Assemblies == null || !model.Assemblies.Any())
                    return (false, "Item Assembly Can't be 0 or empty");

                foreach (var item in model.Assemblies)
                {
                    if (!item.Qty.HasValue || item.Qty <= 0 || !item.ItemId.HasValue || item.ItemId == 0)
                        return (false, "Item Assembly Can't be 0 or empty");
                }
            }

            return (true, string.Empty);
        }


        private async Task<string> GetNextCodeAsync(MySqlConnection conn, string type, int categoryId)
        {
            // Get category text first
            string categoryText = "";
            //using (var cmdCat = new MySqlCommand("SELECT name FROM tbl_categories WHERE id = @id", conn))
            //{
            //    cmdCat.Parameters.AddWithValue("@id", categoryId);
            //    var result = await cmdCat.ExecuteScalarAsync();
            //    categoryText = result?.ToString() ?? "";
            //}

            string typeCategory = type.Split('-')[0].Trim() + categoryText.Split('-')[0].Trim();
            string query = "SELECT MAX(RIGHT(code, 4)) FROM tbl_items WHERE LEFT(code, 5) = @typeCategory";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@typeCategory", typeCategory);

            var queryResult = await cmd.ExecuteScalarAsync();
            int nextSerial = (queryResult == DBNull.Value || queryResult == null) ? 1 : Convert.ToInt32(queryResult) + 1;
            string formattedSerial = nextSerial.ToString().PadLeft(4, '0');
            return typeCategory + formattedSerial;
        }

        private async Task InsertItemUnitsAsync(MySqlConnection conn, int itemId, ItemRequest model)
        {
            // Insert main unit
            var query = @"INSERT INTO tbl_items_unit (item_id, unit_id, factor) 
                         VALUES (@id, @unit_id, @factor)";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@id", itemId);
                cmd.Parameters.AddWithValue("@unit_id", model.UnitId);
                cmd.Parameters.AddWithValue("@factor", 1);
                await cmd.ExecuteNonQueryAsync();
            }
            // Insert sub units
            if (model.Units != null && model.Units.Any())
            {
                foreach (var unit in model.Units)
                {
                    if (unit.UnitId.HasValue && unit.Factor.HasValue)
                    {
                        using var cmd = new MySqlCommand(query, conn);
                        cmd.Parameters.AddWithValue("@id", itemId);
                        cmd.Parameters.AddWithValue("@unit_id", unit.UnitId.Value);
                        cmd.Parameters.AddWithValue("@factor", unit.Factor.Value);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        private async Task InsertItemsAssemblyAsync(MySqlConnection conn, int itemId, List<AssemblyRequest> assemblyItems)
        {
            var query = @"INSERT INTO tbl_item_assembly (assembly_id, item_id, qty) 
                         VALUES (@assembly_id, @item_id, @qty)";

            foreach (var item in assemblyItems)
            {
                if (item.ItemId.HasValue && item.Qty.HasValue)
                {
                    using var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@assembly_id", itemId);
                    cmd.Parameters.AddWithValue("@item_id", item.ItemId.Value);
                    cmd.Parameters.AddWithValue("@qty", item.Qty.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task InsertItemTransactionAsync(MySqlConnection conn, int itemId, ItemRequest model, string itemCode)
        {
            await InsertItemTransactionHelperAsync(conn, model.Date, "Opening Qty", "0", itemId.ToString(),
                model.CostPrice.ToString(), model.OnHand.ToString(), "0", "0", model.OnHand.ToString(),
                "Opening Balance", model.WarehouseId.ToString());
        }

        private async Task InsertItemTransactionHelperAsync(MySqlConnection conn, DateTime date, string type,
            string reference, string itemId, string costPrice, string qtyIn, string salesPrice, string qtyOut,
            string qtyInc, string description, string warehouseId)
        {
            var query = @"INSERT INTO tbl_item_transaction 
                         (date, type, reference, item_id, cost_price, qty_in, sales_price, qty_out, qty_inc, description, warehouse_id) 
                         VALUES (@date, @type, @reference, @itemId, @costPrice, @qtyIn, @sales_price, @qtyOut, @qtyInc, @description, @warehouseId);";

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@date", date);
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@reference", reference);
                cmd.Parameters.AddWithValue("@itemId", itemId);
                cmd.Parameters.AddWithValue("@costPrice", costPrice);
                cmd.Parameters.AddWithValue("@sales_price", salesPrice);
                cmd.Parameters.AddWithValue("@qtyIn", qtyIn);
                cmd.Parameters.AddWithValue("@qtyOut", qtyOut);
                cmd.Parameters.AddWithValue("@qtyInc", qtyInc);
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@warehouseId", warehouseId);
                await cmd.ExecuteNonQueryAsync();
            }

            await UpdateOnHandItemAsync(conn, itemId);
            await AddItemCardDetailsAsync(conn, date, type, reference, itemId, costPrice, qtyIn,
                salesPrice, qtyOut, qtyInc, description, warehouseId);
        }

        private async Task UpdateOnHandItemAsync(MySqlConnection conn, string itemId)
        {
            var query = @"UPDATE tbl_items 
                         SET on_hand = (SELECT SUM(qty_in - qty_out) FROM tbl_item_transaction WHERE item_id = @itemId) 
                         WHERE id = @itemId";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@itemId", itemId);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task AddItemCardDetailsAsync(MySqlConnection conn, DateTime date, string type, string reference,
            string itemId, string costPrice, string qtyIn, string salesPrice, string qtyOut, string qtyInc,
            string description, string warehouseId)
        {
            string invoiceNo = "INV-" + reference;
            string transNo = reference;
            string transType = type;
            decimal qtyBalance = 0, debit = 0, credit = 0, price = decimal.Parse(costPrice), balance = 0, fifoQty = 0, fifoCost = 0;
            decimal _qtyIn = 0, _qtyOut = 0;

            if (!string.IsNullOrEmpty(qtyIn) && decimal.Parse(qtyIn) > 0)
            {
                debit = decimal.Parse(qtyIn) * decimal.Parse(costPrice);
                _qtyIn = decimal.Parse(qtyIn);
            }

            if (!string.IsNullOrEmpty(qtyOut) && decimal.Parse(qtyOut) > 0)
            {
                credit = decimal.Parse(qtyOut) * decimal.Parse(costPrice);
                _qtyOut = decimal.Parse(qtyOut);
            }

            // Get current balances
            using (var cmdQty = new MySqlCommand(
                "SELECT IFNULL(SUM(qty_in - qty_out), 0) FROM tbl_item_card_details WHERE itemId = @id", conn))
            {
                cmdQty.Parameters.AddWithValue("@id", itemId);
                var result = await cmdQty.ExecuteScalarAsync();
                decimal _QtyBalance = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                qtyBalance = _QtyBalance + (_qtyIn - _qtyOut);
            }

            using (var cmdBal = new MySqlCommand(
                "SELECT IFNULL(SUM(debit - credit), 0) FROM tbl_item_card_details WHERE itemId = @id", conn))
            {
                cmdBal.Parameters.AddWithValue("@id", itemId);
                var result = await cmdBal.ExecuteScalarAsync();
                decimal _Balance = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                balance = _Balance + (debit - credit);
            }

            // Insert card details
            var insertQuery = @"INSERT INTO tbl_item_card_details (
                               itemId, date, wharehouse_id, inv_no, trans_no, trans_type, description,
                               price, qty_in, qty_out, qty_balance, debit, credit, balance, fifo_qty, fifo_cost
                           ) VALUES (
                               @itemId, @date, @wharehouse_id, @inv_no, @trans_no, @trans_type, @description,
                               @price, @qty_in, @qty_out, @qty_balance, @debit, @credit, @balance, @fifo_qty, @fifo_cost
                           );";

            using var cmd = new MySqlCommand(insertQuery, conn);
            cmd.Parameters.AddWithValue("@itemId", itemId);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@wharehouse_id", warehouseId);
            cmd.Parameters.AddWithValue("@inv_no", invoiceNo);
            cmd.Parameters.AddWithValue("@trans_no", transNo);
            cmd.Parameters.AddWithValue("@trans_type", transType);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@price", price);
            cmd.Parameters.AddWithValue("@qty_in", qtyIn);
            cmd.Parameters.AddWithValue("@qty_out", qtyOut);
            cmd.Parameters.AddWithValue("@qty_balance", qtyBalance);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@balance", balance);
            cmd.Parameters.AddWithValue("@fifo_qty", fifoQty);
            cmd.Parameters.AddWithValue("@fifo_cost", fifoCost);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertItemJournalAsync(MySqlConnection conn, int itemId, ItemRequest model, string itemCode)
        {
            // Get default accounts
            string inventoryAccountId = await SelectDefaultLevelAccountAsync(conn, "Inventory");
            string equityAccountId = await SelectDefaultLevelAccountAsync(conn, "Opening Balance Equity");

            await InsertTransactionEntryAsync(conn, model.Date, inventoryAccountId, model.TotalValue.ToString(), "0",
                itemId.ToString(), itemId.ToString(), "Opening Qty",
                $"Opening Balance - Item Code - {itemCode}", model.CreatedBy, DateTime.Now.Date, model.CostCenter ?? 0);

            await InsertTransactionEntryAsync(conn, model.Date, equityAccountId, "0", model.TotalValue.ToString(),
                itemId.ToString(), "0", "Opening Qty",
                $"Opening Balance Equity - Item Code - {itemCode}", model.CreatedBy, DateTime.Now.Date, model.CostCenter ?? 0);
        }

        private async Task<string> SelectDefaultLevelAccountAsync(MySqlConnection conn, string accountType)
        {
            var query = @"
        SELECT account_id 
        FROM tbl_coa_config 
        WHERE category = @type 
        LIMIT 1";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@type", accountType);

            var result = await cmd.ExecuteScalarAsync();

            return result?.ToString() ?? "0";
        }


        private async Task InsertTransactionEntryAsync(MySqlConnection conn, DateTime date, string accountId,
            string debit, string credit, string transactionId, string humId, string type, string description,
            int createdBy, DateTime createdDate, int costcenter)
        {
            var query = @"INSERT INTO tbl_transaction 
                         (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, costcenter) 
                         VALUES (@date, @accountId, @debit, @credit, @transactionId, @hum_id, @tType, @type, @description, @createdBy, @createdDate, 0, @costcenter);";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transactionId", transactionId);
            cmd.Parameters.AddWithValue("@type", type.Trim());
            cmd.Parameters.AddWithValue("@tType", "");
            cmd.Parameters.AddWithValue("@hum_id", humId);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@createdBy", createdBy);
            cmd.Parameters.AddWithValue("@createdDate", createdDate);
            cmd.Parameters.AddWithValue("@costcenter", costcenter);
            await cmd.ExecuteNonQueryAsync();
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

        [HttpGet]
        public async Task<IActionResult> GetAllLevel4Accounts()
        {
            try
            {
                var list = new List<object>();

                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var cmd = new MySqlCommand(
                    "SELECT id, code, name FROM tbl_coa_level_4 ORDER BY code",
                    conn);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        id = reader["id"],
                        code = reader["code"],
                        name = reader["name"]
                    });
                }

                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDefaultAccountByCategory(
            [FromQuery] string category)
        {
            try
            {
                int accountId = 0;

                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var cmd = new MySqlCommand(
                    "SELECT account_id FROM tbl_coa_config WHERE category=@cat LIMIT 1",
                    conn);

                cmd.Parameters.AddWithValue("@cat", category);

                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    accountId = Convert.ToInt32(result);

                return Ok(new { status = true, accountId });
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
       [FromQuery] string? selectionMethod = "Default",
       [FromQuery] string? type = null,
       [FromQuery] int? transactionId = null,
       [FromQuery] bool filterByDate = false,
       [FromQuery] DateTime? fromDate = null,
       [FromQuery] DateTime? toDate = null)
        {
            Console.WriteLine($"selectionMethod={selectionMethod}, type={type}, transactionId={transactionId}, filterByDate={filterByDate}");

            try
            {
                // Build connection with dynamic DB from session
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                // Enable MySQL user variables
                connStrBuilder.AllowUserVariables = true;


                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = "";
                var parameters = new List<MySqlParameter>();
                string condition = "";

                // Build base condition based on selectionMethod
                if (selectionMethod == "General Ledger")
                {
                    condition = " AND t.state = 0 AND t.hum_id != 0 ";
                }
                else if (selectionMethod == "Default")
                {
                    condition = " AND t.state = 0 ";
                }
                else if (selectionMethod == "Inventory Opening Stock")
                {
                    condition = " AND t.state = 0 AND t.type = 'Opening Balance' ";
                }
                else if (!string.IsNullOrEmpty(type))
                {
                    // For types that don’t require transactionId
                    query = @"
        SELECT 
            t.transaction_id AS TransactionId,
            t.type AS Type,
            c.name AS ACName,
            t.description AS Description,
            t.debit AS Debit,
            t.credit AS Credit,
            t.date AS Date
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 c ON t.account_id = c.id
        INNER JOIN tbl_customer h ON t.hum_id = h.id
        WHERE t.type = @type
        ORDER BY t.date, t.transaction_id
    ";

                    parameters.Add(new MySqlParameter("@type", type));
                }
                else if (transactionId.HasValue)
                {
                    query = @"
        SELECT ...
        WHERE t.transaction_id = @id
    ";
                    parameters.Add(new MySqlParameter("@id", transactionId.Value));
                }

                else
                {
                    condition = " AND t.state = 0 AND t.type = @selType";
                    parameters.Add(new MySqlParameter("@selType", selectionMethod));
                }

                // Add transaction ID parameter if provided
                if (transactionId.HasValue)
                {
                    parameters.Add(new MySqlParameter("@id", transactionId.Value));
                }

                // Add date filter if enabled
                if (filterByDate && fromDate.HasValue && toDate.HasValue)
                {
                    condition += " AND tbl_transaction.date >= @fromDate AND tbl_transaction.date <= @toDate";
                    parameters.Add(new MySqlParameter("@fromDate", fromDate.Value.Date));
                    parameters.Add(new MySqlParameter("@toDate", toDate.Value.Date));
                }

                // Build query based on selectionMethod or type
                if (selectionMethod == "Sales Invoice")
                {
                    query = @"SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS `SN`,
                CONCAT('000', transaction_id) AS `RefId`,
                tbl_transaction.date AS `Date`, 
                tbl_transaction.transaction_id,
                tbl_transaction.type AS `Type`,
                tbl_coa_level_4.code AS 'ACCode',
                tbl_coa_level_4.name AS `ACName`, 
                tbl_transaction.description AS `Description`,
                tbl_transaction.debit AS `Debit`, 
                tbl_transaction.credit AS `Credit`,
                tbl_transaction.costcenter AS `CostCenter`,
                 tbl_sub_cost_center.name AS CostCenterName
            FROM tbl_transaction 
            INNER JOIN tbl_coa_level_4 
                ON tbl_transaction.account_id = tbl_coa_level_4.id 
            LEFT JOIN tbl_sub_cost_center ON tbl_transaction.costcenter = tbl_sub_cost_center.id
            WHERE (tbl_transaction.type = 'Sales Invoice Cash' OR tbl_transaction.type = 'Sales Invoice') 
                AND tbl_transaction.state = 0";
                }
                else if (selectionMethod == "Purchase Invoice")
                {
                    query = @"SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS `SN`,
                CONCAT('000', transaction_id) AS `RefId`,
                tbl_transaction.date AS `Date`, 
                tbl_transaction.transaction_id,
                tbl_transaction.type AS `Type`,
                tbl_coa_level_4.code AS 'ACCode',
                tbl_coa_level_4.name AS `ACName`, 
                tbl_transaction.description AS `Description`,
                tbl_transaction.debit AS `Debit`, 
                tbl_transaction.credit AS `Credit`,
                tbl_transaction.costcenter AS `CostCenter`,
                tbl_sub_cost_center.name AS CostCenterName
            FROM tbl_transaction 
            INNER JOIN tbl_coa_level_4 
                ON tbl_transaction.account_id = tbl_coa_level_4.id 
             LEFT JOIN tbl_sub_cost_center ON tbl_transaction.costcenter = tbl_sub_cost_center.id
            WHERE (tbl_transaction.type = 'Purchase Invoice Cash' OR tbl_transaction.type = 'Purchase Invoice') 
                AND tbl_transaction.state = 0";
                }
                else if (type?.ToLower() == "sales")
                {
                    query = @"SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS `SN`,
                CONCAT('000', transaction_id) AS `RefId`,
                tbl_transaction.date AS `Date`, 
                tbl_transaction.transaction_id,
                tbl_transaction.type AS `Type`,  
                tbl_coa_level_4.code AS 'ACCode', 
                tbl_coa_level_4.name AS 'ACName', 
                tbl_transaction.description AS Description, 
                tbl_transaction.debit AS Debit, 
                tbl_transaction.credit AS Credit, 
                tbl_transaction.costcenter AS `CostCenter`,
                tbl_sub_cost_center.name AS CostCenterName,
                tbl_customer.name AS Partner
            FROM tbl_transaction  
            INNER JOIN tbl_coa_level_4 ON tbl_transaction.account_id = tbl_coa_level_4.id 
            INNER JOIN tbl_sales ON tbl_transaction.transaction_id = tbl_sales.id 
            INNER JOIN tbl_customer ON tbl_sales.customer_id = tbl_customer.id 
            LEFT JOIN tbl_sub_cost_center ON tbl_transaction.costcenter = tbl_sub_cost_center.id
            WHERE transaction_id = @id AND tbl_transaction.t_type = 'SALES'";
                }
                else if (type?.ToLower() == "purchase")
                {
                    query = @"SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS `SN`,
                CONCAT('000', transaction_id) AS `RefId`,
                tbl_transaction.date AS `Date`, 
                tbl_transaction.transaction_id,
                tbl_transaction.type AS `Type`,  
                tbl_coa_level_4.code AS 'ACCode', 
                tbl_coa_level_4.name AS 'ACName', 
                tbl_transaction.description AS Description, 
                tbl_transaction.debit AS Debit, 
                tbl_transaction.credit AS Credit, 
                tbl_transaction.costcenter AS `CostCenter`,
                tbl_sub_cost_center.name AS CostCenterName,
                tbl_vendor.name AS Partner
            FROM tbl_transaction
            INNER JOIN tbl_coa_level_4 ON tbl_transaction.account_id = tbl_coa_level_4.id
            INNER JOIN tbl_purchase ON tbl_transaction.transaction_id = tbl_purchase.id
            INNER JOIN tbl_vendor ON tbl_purchase.vendor_id = tbl_vendor.id
            LEFT JOIN tbl_sub_cost_center ON tbl_transaction.costcenter = tbl_sub_cost_center.id
            WHERE transaction_id = @id AND t_type = 'PURCHASE'";
                }
                else if (type?.ToLower() == "purchase return")
                {
                    query = @"SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS `SN`,
                CONCAT('000', transaction_id) AS `RefId`,
                tbl_transaction.date AS `Date`, 
                tbl_transaction.transaction_id,
                tbl_transaction.type AS `Type`,  
                tbl_coa_level_4.code AS 'ACCode', 
                tbl_coa_level_4.name AS 'ACName', 
                tbl_transaction.description AS Description, 
                tbl_transaction.debit AS Debit, 
                tbl_transaction.credit AS Credit,
                tbl_transaction.costcenter AS `CostCenter`,
                tbl_sub_cost_center.name AS CostCenterName,
                tbl_vendor.name AS Partner
            FROM tbl_transaction
            INNER JOIN tbl_coa_level_4 ON tbl_transaction.account_id = tbl_coa_level_4.id
            INNER JOIN tbl_purchase_return ON tbl_transaction.transaction_id = tbl_purchase_return.id
            INNER JOIN tbl_vendor ON tbl_purchase_return.vendor_id = tbl_vendor.id
            LEFT JOIN tbl_sub_cost_center ON tbl_transaction.costcenter = tbl_sub_cost_center.id
            WHERE transaction_id = @id AND t_type = 'PURCHASE RETURN'";
                }
                else if (type?.ToLower() == "payment")
                {
                    query = @"SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS `SN`,
                CONCAT('000', transaction_id) AS `RefId`,
                tbl_transaction.date AS `Date`, 
                tbl_transaction.transaction_id,
                tbl_transaction.type AS `Type`,  
                tbl_coa_level_4.code AS 'ACCode', 
                tbl_coa_level_4.name AS 'ACName', 
                tbl_transaction.description AS Description, 
                tbl_transaction.debit AS Debit, 
                tbl_transaction.credit AS Credit,
                tbl_transaction.costcenter AS `CostCenter`,
                tbl_sub_cost_center.name AS CostCenterName,
                CASE 
                    WHEN tbl_payment_voucher.type = 'Vendor' THEN (
                        SELECT name FROM tbl_vendor
                        WHERE id = (SELECT hum_id FROM tbl_payment_voucher_details
                                    WHERE payment_id = tbl_payment_voucher.id)
                    )
                    WHEN tbl_payment_voucher.type = 'Employee' THEN (
                        SELECT name FROM tbl_employee
                        WHERE id = (SELECT hum_id FROM tbl_payment_voucher_details
                                    WHERE payment_id = tbl_payment_voucher.id)
                    )
                    WHEN tbl_payment_voucher.type = 'General' THEN (
                        SELECT name FROM tbl_coa_level_4
                        WHERE id = (SELECT hum_id FROM tbl_payment_voucher_details
                                    WHERE payment_id = tbl_payment_voucher.id)
                    )
                    ELSE '' 
                END AS Partner
            FROM tbl_transaction
            INNER JOIN tbl_coa_level_4 ON tbl_transaction.account_id = tbl_coa_level_4.id
            INNER JOIN tbl_payment_voucher ON tbl_transaction.transaction_id = tbl_payment_voucher.id
            LEFT JOIN tbl_sub_cost_center ON tbl_transaction.costcenter = tbl_sub_cost_center.id
            WHERE transaction_id = @id AND t_type = 'PAYMENT'";
                }
                else if (type?.ToLower() == "receipt")
                {
                    query = @"SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS `SN`,
                CONCAT('000', transaction_id) AS `RefId`,
                tbl_transaction.date AS `Date`, 
                tbl_transaction.transaction_id,
                tbl_transaction.type AS `Type`,  
                tbl_coa_level_4.code AS 'ACCode', 
                tbl_coa_level_4.name AS 'ACName', 
                tbl_transaction.description AS Description, 
                tbl_transaction.debit AS Debit, 
                tbl_transaction.credit AS Credit, 
                tbl_transaction.costcenter AS `CostCenter`,
                tbl_sub_cost_center.name AS CostCenterName,
                CASE 
                    WHEN tbl_receipt_voucher.type = 'Customer' THEN (
                        SELECT name FROM tbl_customer
                        WHERE id = (SELECT hum_id FROM tbl_receipt_voucher_details
                                    WHERE payment_id = tbl_receipt_voucher.id LIMIT 1)
                    )
                    WHEN tbl_receipt_voucher.type = 'General' THEN (
                        SELECT name FROM tbl_coa_level_4
                        WHERE id = (SELECT hum_id FROM tbl_receipt_voucher_details
                                    WHERE payment_id = tbl_receipt_voucher.id)
                    )
                    ELSE '' 
                END AS Partner
            FROM tbl_transaction
            INNER JOIN tbl_coa_level_4 ON tbl_transaction.account_id = tbl_coa_level_4.id
            INNER JOIN tbl_receipt_voucher ON tbl_transaction.transaction_id = tbl_receipt_voucher.id
            LEFT JOIN tbl_sub_cost_center ON tbl_transaction.costcenter = tbl_sub_cost_center.id
            WHERE transaction_id = @id AND t_type = 'RECEIPT'";
                }
                else if (type?.ToLower() == "journal")
                {
                    query = @"SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS `SN`,
                CONCAT('000', transaction_id) AS `RefId`,
                tbl_transaction.date AS `Date`, 
                tbl_transaction.transaction_id,
                tbl_transaction.type AS `Type`, 
                tbl_coa_level_4.code AS 'ACCode', 
                tbl_coa_level_4.name AS 'ACName', 
                tbl_transaction.description AS Description, 
                tbl_transaction.debit AS Debit, 
                tbl_transaction.credit AS Credit, 
                tbl_transaction.costcenter AS `CostCenter`,
                tbl_sub_cost_center.name AS CostCenterName,
                (SELECT partner FROM tbl_journal_voucher_details
                 WHERE inv_id = tbl_journal_voucher.id LIMIT 1) AS Partner
            FROM tbl_transaction
            INNER JOIN tbl_coa_level_4 ON tbl_transaction.account_id = tbl_coa_level_4.id
            INNER JOIN tbl_journal_voucher ON tbl_transaction.transaction_id = tbl_journal_voucher.id
            LEFT JOIN tbl_sub_cost_center ON tbl_transaction.costcenter = tbl_sub_cost_center.id
            WHERE transaction_id = @id AND t_type = 'JOURNAL'";
                }
                else if (type?.ToLower() == "inventory")
                {
                    query = @"SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS `SN`,
                CONCAT('000', transaction_id) AS `RefId`,
                tbl_transaction.date AS `Date`, 
                tbl_transaction.transaction_id,
                tbl_transaction.type AS `Type`,
                tbl_coa_level_4.code AS 'ACCode',
                tbl_coa_level_4.name AS `ACName`, 
                tbl_transaction.description AS `Description`,
                tbl_transaction.debit AS `Debit`, 
                tbl_transaction.credit AS `Credit`,
                tbl_transaction.costcenter AS `CostCenter`,
                tbl_sub_cost_center.name AS CostCenterName
            FROM tbl_transaction 
            INNER JOIN tbl_coa_level_4  ON tbl_transaction.account_id = tbl_coa_level_4.id 
            LEFT JOIN tbl_sub_cost_center ON tbl_transaction.costcenter = tbl_sub_cost_center.id
            WHERE description LIKE @itemCode";

                    parameters.Add(new MySqlParameter("@itemCode", $"%- Item Code - {transactionId}"));
                }
                else if (type?.ToLower() == "vat input" || type?.ToLower() == "vat output")
                {
                    query = @"SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS `SN`,
                CONCAT('000', transaction_id) AS `RefId`,
                tbl_transaction.date AS `Date`, 
                tbl_transaction.transaction_id,
                tbl_transaction.type AS `Type`,
                tbl_coa_level_4.code AS 'ACCode',
                tbl_coa_level_4.name AS `ACName`, 
                tbl_transaction.description AS `Description`,
                tbl_transaction.debit AS `Debit`, 
                tbl_transaction.credit AS `Credit`,
                tbl_transaction.costcenter AS `CostCenter`,
                tbl_sub_cost_center.name AS CostCenterName
            FROM tbl_transaction 
            INNER JOIN tbl_coa_level_4 
                ON tbl_transaction.account_id = tbl_coa_level_4.id
            LEFT JOIN tbl_sub_cost_center ON tbl_transaction.costcenter = tbl_sub_cost_center.id
            WHERE 1=1 " + condition + @" 
                AND tbl_transaction.account_id = (
                    SELECT account_id FROM tbl_coa_config WHERE category = @vatType
                )";

                    parameters.Add(new MySqlParameter("@vatType", type));
                }
                else if (new[] { "Sales All", "Purchase All", "Purchase Return All", "Sales Return All",
                         "Debit Note All", "Credit Note All", "Petty Cash All" }
                         .Contains(type, StringComparer.OrdinalIgnoreCase))
                {
                    string typePrefix = type!.Replace(" All", "");
                    query = @"SELECT 
                ROW_NUMBER() OVER (ORDER BY tbl_transaction.date) AS `SN`,
                CONCAT('000', transaction_id) AS `RefId`,
                tbl_transaction.date AS `Date`, 
                tbl_transaction.transaction_id,
                tbl_transaction.type AS `Type`,
                tbl_coa_level_4.code AS 'ACCode',
                tbl_coa_level_4.name AS `ACName`, 
                tbl_transaction.description AS `Description`,
                tbl_transaction.debit AS `Debit`, 
                tbl_transaction.credit AS `Credit`,
                tbl_transaction.costcenter AS `CostCenter`,
                tbl_sub_cost_center.name AS CostCenterName
            FROM tbl_transaction 
            INNER JOIN tbl_coa_level_4 
                ON tbl_transaction.account_id = tbl_coa_level_4.id 
            LEFT JOIN tbl_sub_cost_center ON tbl_transaction.costcenter = tbl_sub_cost_center.id
            WHERE 1=1 " + condition + " AND tbl_transaction.type LIKE @typePattern";

                    parameters.Add(new MySqlParameter("@typePattern", $"{typePrefix}%"));
                }
                else
                {
                     query = @"
    SELECT 
        (@sn := @sn + 1) AS `SN`,
        CONCAT('000', t.transaction_id) AS `RefId`,
        t.date AS `Date`,
        t.transaction_id,
        t.type AS `Type`,
        c.code AS `ACCode`,
        c.code AS `ACCode`,
        c.name AS `ACName`,
        t.description AS `Description`,
        t.debit AS `Debit`,
        t.credit AS `Credit`,
        t.costcenter AS `CostCenter`,
        s.name AS CostCenterName
    FROM tbl_transaction t
    INNER JOIN tbl_coa_level_4 c ON t.account_id = c.id
    LEFT JOIN tbl_sub_cost_center s ON t.costcenter = s.id
    CROSS JOIN (SELECT @sn := 0) AS init
    WHERE 1=1 " + condition + @"
    ORDER BY t.date;
";

                }

                // Execute query
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                var journalEntries = new List<Dictionary<string, object?>>();
                using var reader = await cmd.ExecuteReaderAsync();

                int sn = 1;
                while (await reader.ReadAsync())
                {
                    var entry = new Dictionary<string, object?>();
                    entry["SN"] = sn++;  // Row number in C#
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string fieldName = reader.GetName(i);
                        if (fieldName != "SN") // Skip if SN is already added
                            entry[fieldName] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    journalEntries.Add(entry);
                }

                // Calculate totals
                if (journalEntries.Any())
                {
                    decimal totalDebit = journalEntries
                        .Where(x => x.ContainsKey("Debit") && x["Debit"] != null)
                        .Sum(x => Convert.ToDecimal(x["Debit"]));

                    decimal totalCredit = journalEntries
                        .Where(x => x.ContainsKey("Credit") && x["Credit"] != null)
                        .Sum(x => Convert.ToDecimal(x["Credit"]));

                    // Add total row
                    var totalRow = new Dictionary<string, object?>();
                    foreach (var key in journalEntries[0].Keys)
                    {
                        if (key == "ACName" || key == "Description")
                            totalRow[key] = "TOTAL";
                        else if (key == "Debit")
                            totalRow[key] = totalDebit;
                        else if (key == "Credit")
                            totalRow[key] = totalCredit;
                        else
                            totalRow[key] = null;
                    }
                    journalEntries.Add(totalRow);
                }

                return Ok(new { status = true, data = journalEntries });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { status = false, message = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        public IActionResult GeneralJournal()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetJournalDetails(int refId, string vType, string date)
        {
            try
            {
                // Build connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Prepare the query based on voucher type
                string query = string.Empty;
                if (vType == "Sales Invoice" || vType == "Sales Invoice Cash")
                {
                    query = @"
                SELECT t.*, 
                       ac.*, 
                       ac.code AS `Account Code`,
                        ac.name AS 'Account Name',
                          t.costcenter AS `CostCenter`,
                            scc.name AS `CostCenterName`,
                       CASE 
                           WHEN t.type IN ('Customer Receipt', 'Sales Invoice', 'Sales Invoice Cash', 'Customer Opening Balance', 'Check Cancel (Customer)', 'Customer%', 'Sales%') 
                                THEN IFNULL((SELECT CONCAT(CODE, ' - ', NAME) FROM tbl_customer WHERE id = hum_id), '')
                           WHEN t.type IN ('Vendor Payment', 'Purchase Invoice', 'Purchase Invoice Cash', 'Vendor Opening Balance', 'Check Cancel (Vendor)', 'Vendor%', 'Purchase%') 
                                THEN IFNULL((SELECT CONCAT(CODE, ' - ', NAME) FROM tbl_vendor WHERE id = hum_id), '')
                           WHEN t.type IN ('Employee Salary', 'Employee Payment') 
                                THEN IFNULL((SELECT CONCAT(CODE, ' - ', NAME) FROM tbl_employee WHERE id = hum_id), '')
                           ELSE '' 
                       END AS `hum name`
                FROM tbl_transaction t 
                INNER JOIN tbl_coa_level_4 ac ON ac.id = t.account_id
                LEFT JOIN tbl_sub_cost_center scc  ON t.costcenter = scc.id  
                WHERE (t.type = 'Sales Invoice Cash' or t.type = 'Sales Invoice') 
                  AND t.transaction_id = @id;";
                }
                else if (vType == "Purchase Invoice" || vType == "Purchase Invoice Cash")
                {
                    query = @"
                SELECT t.*, 
                       ac.*, 
                       ac.code AS `Account Code`,
                       ac.name AS 'Account Name',
                         t.costcenter AS `CostCenter`,
                         scc.name AS `CostCenterName`,
                       CASE 
                           WHEN t.type IN ('Customer Receipt', 'Sales Invoice', 'Sales Invoice Cash', 'Customer Opening Balance', 'Check Cancel (Customer)', 'Customer%', 'Sales%') 
                                THEN IFNULL((SELECT CONCAT(CODE, ' - ', NAME) FROM tbl_customer WHERE id = hum_id), '')
                           WHEN t.type IN ('Vendor Payment', 'Purchase Invoice', 'Purchase Invoice Cash', 'Vendor Opening Balance', 'Check Cancel (Vendor)', 'Vendor%', 'Purchase%') 
                                THEN IFNULL((SELECT CONCAT(CODE, ' - ', NAME) FROM tbl_vendor WHERE id = hum_id), '')
                           WHEN t.type IN ('Employee Salary', 'Employee Payment') 
                                THEN IFNULL((SELECT CONCAT(CODE, ' - ', NAME) FROM tbl_employee WHERE id = hum_id), '')
                           ELSE '' 
                       END AS `hum name`
                FROM tbl_transaction t 
                INNER JOIN tbl_coa_level_4 ac ON ac.id = t.account_id
                LEFT JOIN tbl_sub_cost_center scc ON t.costcenter = scc.id   
                WHERE (t.type = 'Purchase Invoice Cash' or t.type = 'Purchase Invoice')
                  AND t.transaction_id = @id;";
                }
                else
                {
                    query = @"
                SELECT t.*, 
                       ac.*, 
                       ac.code AS `Account Code`,
                       ac.name AS 'Account Name',
                         t.costcenter AS `CostCenter`,
                            scc.name AS `CostCenterName`,
                       CASE 
                           WHEN t.type IN ('Customer Receipt', 'Sales Invoice', 'Sales Invoice Cash', 'Customer Opening Balance', 'Check Cancel (Customer)', 'Customer%', 'Sales%') 
                                THEN IFNULL((SELECT CONCAT(CODE, ' - ', NAME) FROM tbl_customer WHERE id = hum_id), '')
                           WHEN t.type IN ('Vendor Payment', 'Purchase Invoice', 'Purchase Invoice Cash', 'Vendor Opening Balance', 'Check Cancel (Vendor)', 'Vendor%', 'Purchase%') 
                                THEN IFNULL((SELECT CONCAT(CODE, ' - ', NAME) FROM tbl_vendor WHERE id = hum_id), '')
                           WHEN t.type IN ('Employee Salary', 'Employee Payment') 
                                THEN IFNULL((SELECT CONCAT(CODE, ' - ', NAME) FROM tbl_employee WHERE id = hum_id), '')
                           WHEN t.type IN ('SubContract Payment','Subcontractor Opening Balance','Check Cancel (SubContract)') 
                                THEN IFNULL((SELECT CONCAT(CODE, ' - ', NAME) FROM tbl_vendor WHERE id = hum_id), '')
                           ELSE '' 
                       END AS `hum name`
                FROM tbl_transaction t 
                INNER JOIN tbl_coa_level_4 ac ON ac.id = t.account_id
                 LEFT JOIN tbl_sub_cost_center scc ON t.costcenter = scc.id 
                WHERE t.TYPE = @type 
                  AND t.transaction_id = @id AND t.date = @date;";
                }

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", refId);
                cmd.Parameters.AddWithValue("@type", vType);
                cmd.Parameters.AddWithValue("@date", DateTime.Parse(date));

                await using var reader = await cmd.ExecuteReaderAsync();

                var journalDetailsList = new List<object>();
                int count = 1;
                while (await reader.ReadAsync())
                {
                    journalDetailsList.Add(new
                    {
                        SN = count++,
                        Date = reader["date"].ToString(),
                        TransactionId = reader["id"].ToString(),
                        HumName = reader["hum name"].ToString(),
                        AccountId = reader["account_id"].ToString(),
                        AccountCode = reader["Account Code"].ToString(),
                        AccountName = reader["Account Name"].ToString(),
                        Debit = reader.IsDBNull(reader.GetOrdinal("Debit")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Debit")),
                        Credit = reader.IsDBNull(reader.GetOrdinal("Credit")) ? 0 : reader.GetDecimal(reader.GetOrdinal("Credit")),
                        Description = reader["Description"].ToString(),
                        Id = reader["id"].ToString(),
                        HumId = reader["hum_id"].ToString(),
                        CostCenter = reader.IsDBNull(reader.GetOrdinal("CostCenter"))? 0  : reader.GetInt32(reader.GetOrdinal("CostCenter")),
                        CostCenterName = reader["CostCenterName"] == DBNull.Value ? "" : reader["CostCenterName"].ToString()
                    });
                }

                return Ok(new { status = true, data = journalDetailsList });
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        //[HttpPost]
        //public async Task<IActionResult> UpdateJournal([FromBody] JournalUpdateRequest request)
        //{

        //    if (request == null || request.Entries == null || request.Entries.Count == 0)
        //        return Json(new { status = false, message = "No journal entries provided." });

        //    try
        //    {
        //        var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
        //        {
        //            Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
        //        };
        //        int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        //        if (userId <= 0)
        //            return Unauthorized(new { status = false, message = "User not logged in" });

        //        await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
        //        await conn.OpenAsync();

        //        using var transaction = await conn.BeginTransactionAsync();

        //        decimal totalDebit = 0, totalCredit = 0;

        //        foreach (var row in request.Entries)
        //        {
        //            if (row.AccountId <= 0)
        //                continue;


        //            decimal debit = row.Debit ?? 0;
        //            decimal credit = row.Credit ?? 0;
        //            totalDebit += debit;
        //            totalCredit += credit;

        //            string updateQuery = @"
        //        UPDATE tbl_transaction 
        //        SET 
        //            date = @date,
        //            account_id = @accountId,
        //            debit = @debit,
        //            credit = @credit,
        //            transaction_id = @transactionId,
        //            hum_id = @humId,
        //            type = @type,
        //            description = @description,
        //            modified_by = @modifiedBy,
        //            modified_date = NOW()
        //        WHERE id = @id;
        //    ";

        //            await using (var cmd = new MySqlCommand(updateQuery, conn, (MySqlTransaction)transaction))
        //            {
        //                cmd.Parameters.AddWithValue("@date", row.Date);
        //                cmd.Parameters.AddWithValue("@accountId", row.AccountId);
        //                cmd.Parameters.AddWithValue("@debit", debit);
        //                cmd.Parameters.AddWithValue("@credit", credit);
        //                cmd.Parameters.AddWithValue("@transactionId", row.Id);
        //                cmd.Parameters.AddWithValue("@humId", row.HumId ?? "0");
        //                cmd.Parameters.AddWithValue("@type", request.VoucherType);
        //                cmd.Parameters.AddWithValue("@description", row.Description ?? "");
        //               cmd.Parameters.AddWithValue("@modifiedBy", userId);
        //                cmd.Parameters.AddWithValue("@id", row.Id); 
        //                await cmd.ExecuteNonQueryAsync();
        //            }

        //            // --- Handle type-specific account updates ---
        //            string type = request.VoucherType;
        //            string secondaryQuery = type switch
        //            {
        //                "Customer Opening Balance" => "UPDATE tbl_customer SET account_id = @acId WHERE id = @hId;",
        //                "Vendor Opening Balance" => "UPDATE tbl_vendor SET account_id = @acId WHERE id = @hId;",
        //                "Subcontractor Opening Balance" => "UPDATE tbl_vendor SET account_id = @acId WHERE id = @hId;",
        //                _ => ""
        //            };

        //            if (!string.IsNullOrEmpty(secondaryQuery))
        //            {
        //                await using var cmd2 = new MySqlCommand(secondaryQuery, conn, (MySqlTransaction)transaction);
        //                cmd2.Parameters.AddWithValue("@acId", row.AccountId);
        //                cmd2.Parameters.AddWithValue("@hId", row.HumId);
        //                await cmd2.ExecuteNonQueryAsync();
        //            }

        //        }

        //        // --- Update Balance Table ---
        //        decimal amount = totalDebit > totalCredit ? totalDebit : totalCredit;
        //        await UpdateVoucherTablesAsync(conn, transaction, request.VoucherType, amount, request.CurrentHumId, request.CurrentVoucherId, request.ModifiedBy);

        //        await transaction.CommitAsync();

        //        return Ok(new
        //        {
        //            status = true,
        //            message = "Journal updated successfully",
        //            totalDebit,
        //            totalCredit
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(500, new { status = false, message = ex.Message });
        //    }
        //}

        //private async Task UpdateVoucherTablesAsync(MySqlConnection conn, MySqlTransaction transaction, string type, decimal amount, string humId, string voucherId, int modifiedBy)
        //{
        //    try
        //    {
        //        string query = type switch
        //        {
        //            "Customer Opening Balance" => "UPDATE tbl_customer SET Balance=@amount WHERE id=@hId;",
        //            "Vendor Opening Balance" => "UPDATE tbl_vendor SET Balance=@amount WHERE id=@hId;",
        //            "Subcontractor Opening Balance" => "UPDATE tbl_vendor SET Balance=@amount WHERE id=@hId;",
        //            _ => ""
        //        };

        //        if (!string.IsNullOrEmpty(query))
        //        {
        //            await using var cmd = new MySqlCommand(query, conn, transaction);
        //            cmd.Parameters.AddWithValue("@amount", amount);
        //            cmd.Parameters.AddWithValue("@hId", humId);
        //            await cmd.ExecuteNonQueryAsync();
        //        }

        //    }
        //    catch(Exception ex)
        //    {
        //        throw ex;
        //    }

        //}

        [HttpPost]
        public async Task<IActionResult> UpdateJournal([FromBody] JournalVoucherRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (model.Id <= 0)
                return BadRequest(new { status = false, message = "Invalid journal voucher ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Calculate totals
                var totals = CalculateTotals(model.JournalEntries);
                decimal totalDebit = totals.totalDebit;
                decimal totalCredit = totals.totalCredit;

                // Validate Debit and Credit amounts
                if (!ValidateDebitAndCreditAmount(totalDebit, totalCredit))
                {
                    return BadRequest(new { status = false, message = "Debit Total and Credit Total are not equal." });
                }

                // Check required data
                if (!CheckRequiredData(model.JournalEntries))
                {
                    return BadRequest(new { status = false, message = "Invalid account data in one or more rows." });
                }

                // Update details
                await UpdateDetailsAsync(conn, model);

                return Ok(new { status = true, message = "Journal voucher updated successfully", journalId = model.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private (decimal totalDebit, decimal totalCredit) CalculateTotals(List<JournalEntryItem> entries)
        {
            decimal totalDebit = 0, totalCredit = 0;

            if (entries == null || !entries.Any())
                return (totalDebit, totalCredit);

            foreach (var entry in entries)
            {
                // Check if debit or credit values are null or empty
                if (entry.Debit == null && entry.Credit == null)
                    continue;

                if (string.IsNullOrEmpty(entry.Debit?.ToString().Trim()) && string.IsNullOrEmpty(entry.Credit?.ToString().Trim()))
                    continue;

                // Parse and sum debit and credit
                if (!string.IsNullOrEmpty(entry.Debit?.ToString()))
                {
                    totalDebit += decimal.Parse(entry.Debit.ToString());
                }

                if (!string.IsNullOrEmpty(entry.Credit?.ToString()))
                {
                    totalCredit += decimal.Parse(entry.Credit.ToString());
                }
            }

            return (totalDebit, totalCredit);
        }

        private bool ValidateDebitAndCreditAmount(decimal totalDebit, decimal totalCredit)
        {
            // Check if both are zero
            if (totalDebit == 0 && totalCredit == 0)
            {
                return false;
            }

            // Check if debit equals credit
            if (totalDebit == totalCredit)
            {
                return true;
            }

            return false;
        }

        private bool CheckRequiredData(List<JournalEntryItem> entries)
        {
            if (entries == null || !entries.Any())
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                // Check if account_name is null, empty, or zero
                if (entry.AccountName == null ||
                    string.IsNullOrEmpty(entry.AccountName.ToString()) ||
                    (decimal.TryParse(entry.AccountName.ToString(), out decimal val) && val == 0))
                {
                    // Row index is i + 1 for user-friendly message
                    return false;
                }
            }

            return true;
        }

        private async Task UpdateDetailsAsync(MySqlConnection conn, JournalVoucherRequest model)
        {
            decimal totalDebitAmount = 0, totalCreditAmount = 0;
            string currenthumId = "";

            foreach (var entry in model.JournalEntries)
            {
                // Skip if account_id is null or empty
                if (string.IsNullOrEmpty(entry.AccountId?.ToString()))
                    continue;

                // Skip if both Debit and Credit are null or empty
                if ((entry.Debit == null || string.IsNullOrEmpty(entry.Debit.ToString())) &&
                    (entry.Credit == null || string.IsNullOrEmpty(entry.Credit.ToString())))
                    continue;

                decimal debitAmount = entry.Debit == null ? 0 : Convert.ToDecimal(entry.Debit);
                decimal creditAmount = entry.Credit == null ? 0 : Convert.ToDecimal(entry.Credit);
                string accountId = entry.AccountId.ToString();
                string description = entry.Description ?? "";
                string humId = entry.HumId ?? "0";
                string tId = entry.TId ?? "0";
                DateTime date = model.Date.Date;

                // Update transaction
                var updateQuery = @"UPDATE tbl_transaction 
                SET date = @date, 
                    account_id = @accountId, 
                   
                    modified_by = @modifiedBy, 
                    modified_date = @modifiedDate
                WHERE id = @journalId;";

                using (var cmd = new MySqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@date", date);
                    cmd.Parameters.AddWithValue("@accountId", accountId);
                    //cmd.Parameters.AddWithValue("@debit", debitAmount);
                    //cmd.Parameters.AddWithValue("@credit", creditAmount);
                    ////cmd.Parameters.AddWithValue("@transactionId", 1);
                    //cmd.Parameters.AddWithValue("@transactionId", model.Id);
                    //cmd.Parameters.AddWithValue("@hum_id", humId);
                    //cmd.Parameters.AddWithValue("@type", model.VType);
                    //cmd.Parameters.AddWithValue("@description", description);
                    cmd.Parameters.AddWithValue("@modifiedBy", model.UserId);
                    cmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now.Date);
                    cmd.Parameters.AddWithValue("@journalId", tId);

                    await cmd.ExecuteNonQueryAsync();
                }

                totalDebitAmount += debitAmount;
                totalCreditAmount += creditAmount;
                currenthumId = humId;

                // Update related tables based on voucher type
                string type = model.CurrentVoucherName;
                string query = "";

                if (type == "Customer Opening Balance")
                {
                    query = @"UPDATE tbl_customer SET account_id = @acId WHERE id = @hId";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@hId", humId);
                        cmd.Parameters.AddWithValue("@acId", accountId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                else if (type == "Vendor Opening Balance")
                {
                    query = @"UPDATE tbl_vendor SET account_id = @acId WHERE id = @hId";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@hId", humId);
                        cmd.Parameters.AddWithValue("@acId", accountId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                else if (type == "Subcontractor Opening Balance")
                {
                    query = @"UPDATE tbl_vendor SET account_id = @acId WHERE id = @hId";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@hId", humId);
                        cmd.Parameters.AddWithValue("@acId", accountId);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                else if (type == "Purchase Invoice Cash" || type == "Purchase Invoice")
                {
                   
                }
                else if (type == "Sales Invoice Cash" || type == "Sales Invoice")
                {
                }
                else if (type.StartsWith("SalesReturn"))
                {
                }
                else if (type.StartsWith("PurchaseReturn"))
                {
                }
                else if (type == "Vendor Payment" || type == "Employee Loan Payment" ||
                         type == "Employee Petty Cash Payment" || type == "Employee Salary Payment")
                {
                }
                else if (type == "Petty Cash")
                {
                }
                else if (type == "Customer Receipt" || type == "General Receipt")
                {
                }
                else if (type == "SALES RETURN")
                {
                }
                else if (type == "PURCHASE RETURN")
                {
                }
                else
                {
                    query = "";
                }

            }

            // Update voucher tables if totals are valid
            if (totalDebitAmount >= 0 || totalCreditAmount >= 0)
            {
                string amount = totalDebitAmount > totalCreditAmount ? totalDebitAmount.ToString() : totalCreditAmount.ToString();
                await UpdateVoucherTablesAsync(conn, model.CurrentVoucherName, amount, currenthumId, model.CurrentVoucherId, model.UserId);
            }
        }

        private async Task UpdateVoucherTablesAsync(MySqlConnection conn, string type, string amount, string humId, string voucherId, string userId)
        {
            string query = "";

            if (type == "Customer Opening Balance")
            {
                query = @"UPDATE tbl_customer SET Balance = @amount WHERE id = @humId";
            }
            else if (type == "Vendor Opening Balance")
            {
                query = @"UPDATE tbl_vendor SET Balance = @amount WHERE id = @humId";
            }
            else if (type == "Subcontractor Opening Balance")
            {
                query = @"UPDATE tbl_vendor SET Balance = @amount WHERE id = @humId";
            }
            else if (type == "Purchase Invoice Cash" || type == "Purchase Invoice")
            {
            }
            else if (type == "Sales Invoice Cash" || type == "Sales Invoice")
            {
            }
            else if (type.StartsWith("SalesReturn"))
            {
            }
            else if (type.StartsWith("PurchaseReturn"))
            {
            }
            else if (type == "Vendor Payment" || type == "Employee Loan Payment" ||
                     type == "Employee Petty Cash Payment" || type == "Employee Salary Payment")
            {
            }
            else if (type == "Customer Receipt" || type == "General Receipt")
            {
            }
            else if (type == "SALES RETURN")
            {
            }
            else if (type == "PURCHASE RETURN")
            {
            }

            if (!string.IsNullOrEmpty(query))
            {
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@amount", amount);
                    cmd.Parameters.AddWithValue("@humId", humId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            voucherId = string.IsNullOrEmpty(voucherId) ? "0" : voucherId;
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
            if (model == null || string.IsNullOrWhiteSpace(model.CategoryName))
                return BadRequest(new { status = false, message = "Please enter category name" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
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

                // ✅ Insert and get new ID
                var insertQuery = @"
            INSERT INTO tbl_fixed_assets_category 
                (category_name, assets_account_id, depreciation_account_id, expence_account_id)
            VALUES 
                (@name, @assetsId, @depreciationId, @expenceId);
            SELECT LAST_INSERT_ID();";

                int newId;
                using (var insertCmd = new MySqlCommand(insertQuery, conn))
                {
                    insertCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                    insertCmd.Parameters.AddWithValue("@assetsId", model.AssetsAccountId);
                    insertCmd.Parameters.AddWithValue("@depreciationId", model.DepreciationAccountId);
                    insertCmd.Parameters.AddWithValue("@expenceId", model.ExpenceAccountId);

                    newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                }

                return Ok(new
                {
                    status = true,
                    message = "Category added successfully",
                    id = newId,
                    categoryName = model.CategoryName.Trim()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpPut]
        public async Task<IActionResult> EditFixedAssetsCategory([FromBody] FixedAssetsCategoryRequest model)
        {
            if (model == null || model.Id <= 0 || string.IsNullOrWhiteSpace(model.CategoryName))
                return BadRequest(new { status = false, message = "Invalid request or category name" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Check duplicate excluding current
                var checkQuery = "SELECT COUNT(*) FROM tbl_fixed_assets_category WHERE category_name = @name AND id != @id";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                    checkCmd.Parameters.AddWithValue("@id", model.Id);
                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                    if (exists)
                        return BadRequest(new { status = false, message = "Category name already exists." });
                }

                // ✅ Update
                var updateQuery = @"
            UPDATE tbl_fixed_assets_category 
            SET category_name = @name, 
                assets_account_id = @assetsAccountId, 
                depreciation_account_id = @depreciationAccountId, 
                expence_account_id = @expenseAccountId
            WHERE id = @id";

                using var updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@name", model.CategoryName.Trim());
                updateCmd.Parameters.AddWithValue("@assetsAccountId", model.AssetsAccountId);
                updateCmd.Parameters.AddWithValue("@depreciationAccountId", model.DepreciationAccountId);
                updateCmd.Parameters.AddWithValue("@expenseAccountId", model.ExpenceAccountId);
                updateCmd.Parameters.AddWithValue("@id", model.Id);

                await updateCmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    status = true,
                    message = "Fixed Assets Category updated successfully",
                    id = model.Id,
                    categoryName = model.CategoryName.Trim()
                });
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
                        ManufactureStatus = reader["manufactureStatus"].ToString(),
                        CreatedBy = reader["created_by"],
                        ModifiedBy = reader["modified_by"],
                        CreatedDate= reader["created_date"] != DBNull.Value ? DateTime.Parse(reader["created_date"].ToString()) : (DateTime?)null,
                        ModifiedDate= reader["modified_date"] != DBNull.Value ? DateTime.Parse(reader["modified_date"].ToString()) : (DateTime?)null,
                        State= reader["state"],
                        CostCenter = reader["costcenter"]

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
                return Json(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { status = false, message = "Enter asset name" });

            if (model.CategoryId == 0)
                return Json(new { status = false, message = "Select category first" });

            if (model.PurchasePrice <= 0)
                return Json(new { status = false, message = "Enter purchase price" });

            if (model.DepreciationLife <= 0)
                return Json(new { status = false, message = "Enter depreciation life" });

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
                (date, code, name, brand, category_id, model, supplier, status, invoice_number, purchase_date, end_date, depreciation_life, purchase_price, debit_account_id, credit_account_id, expence_account_id, created_by, created_date, state, manufacture, manufactureStatus, costcenter)
            VALUES
                (@date, @code, @name, @brand, @category_id, @model, @supplier, @status, @invoice_number, @purchase_date, @end_date, @depreciation_life, @purchase_price, @debit_account_id, @credit_account_id, @expence_account_id, @created_by, @created_date, 0, @manufacture, 'Draft', @costcenter);
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
                    cmd.Parameters.AddWithValue("@costcenter", model.CostCenter ?? 0);

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
                return Json(500, new { status = false, message = ex.Message });
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

            //// Debit & Credit entry at purchase
            //if (!string.IsNullOrEmpty(model.Supplier))
            //{
                await CommonInsertTransactionAsync(conn, startDate, model.DebitAccountId.ToString(), totalAmount, 0, assetId.ToString(), 0, "Fixed Assets", $"{model.Name} - Fixed Assets No. {assetId}", model.CreatedBy, model.CostCenter ?? 0);
                await CommonInsertTransactionAsync(conn, startDate, model.CreditAccountId.ToString(), 0, totalAmount, assetId.ToString(), 0, "Fixed Assets", $"{model.Name} - Fixed Assets No. {assetId}", model.CreatedBy, model.CostCenter ?? 0);
            //}

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

                await CommonInsertTransactionAsync(conn, periodEnd, expenseAccountId, amount, 0, assetId.ToString(), 0, "Fixed Assets", $"{model.Name} - Fixed Assets No. {assetId}", model.CreatedBy, model.CostCenter ?? 0);
                await CommonInsertTransactionAsync(conn, periodEnd, model.CreditAccountId.ToString(), 0, amount, assetId.ToString(), 0, "Fixed Assets", $"{model.Name} - Fixed Assets No. {assetId}", model.CreatedBy, model.CostCenter ?? 0);

                currentMonthStart = currentMonthStart.AddMonths(1);
                currentMonthStart = new DateTime(currentMonthStart.Year, currentMonthStart.Month, 1);
            }
        }

        private async Task CommonInsertTransactionAsync(MySqlConnection conn, DateTime date, string accountId, decimal debit, decimal credit,
                                                         string transactionId, int humId, string type, string description, int createdBy, int costcenter)
        {
            var query = @"INSERT INTO tbl_transaction 
                  (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, costcenter) 
                  VALUES (@date, @accountId, @debit, @credit, @transactionId, @humId, '', @type, @description, @createdBy, @createdDate, 0, @costcenter);";

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
            cmd.Parameters.AddWithValue("@costcenter", costcenter);

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
                manufactureStatus='Draft',
                costcenter = @costcenter
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
                    cmd.Parameters.AddWithValue("@costcenter", model.CostCenter ?? 0);

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
    pc.name AS employeeId,
    e.name AS employeeName,
    pc.account_id AS accountId,
    a.name AS accountName,
    pc.mobile,
    pc.whatsapp_no,
    pc.email,
    e.code AS empCode
FROM 
    tbl_petty_cash_card pc
LEFT JOIN 
    tbl_employee e ON e.id = CAST(pc.name AS UNSIGNED)
LEFT JOIN 
    tbl_coa_level_4 a ON a.id = pc.account_id
WHERE 
    e.code IS NOT NULL;
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
                        empCode = reader["empCode"] == DBNull.Value ? 0 : Convert.ToInt32(reader["empCode"])
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
                return Json(new { status = false, message = "Invalid request" });

            // --- Equivalent to chkRequireData() ---
            if (model.CashAccountId <= 0)
                return Json(new { status = false, message = "Please select a petty cash account" });
            if (model.EmployeeId <= 0)
                return Json(new { status = false, message = "Please select an employee" });
            if (model.Total <= 0)
                return Json(new { status = false, message = "Please enter a valid total amount" });
            if (model.VoucherDate == DateTime.MinValue)
                return Json(new { status = false, message = "Invalid voucher date" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Json(new { status = false, message = "User not logged in" });

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
                    (code, voucher_date, cash_account_id, employee_id, notes, total, created_by, vendor_id)
                VALUES 
                    (@code, @voucher_date, @cash_account_id, @employee_id, @notes, @total, @created_by, @vendor_id);
                SELECT LAST_INSERT_ID();";

                    using var cmdInsert = new MySqlCommand(insertQuery, conn);
                    cmdInsert.Parameters.AddWithValue("@code", pettyCashCode);
                    cmdInsert.Parameters.AddWithValue("@voucher_date", model.VoucherDate);
                    cmdInsert.Parameters.AddWithValue("@cash_account_id", model.CashAccountId);
                    cmdInsert.Parameters.AddWithValue("@employee_id", model.EmployeeId);
                    cmdInsert.Parameters.AddWithValue("@notes", model.Notes ?? "");
                    cmdInsert.Parameters.AddWithValue("@total", model.Total);
                    cmdInsert.Parameters.AddWithValue("@created_by", userId);
                    cmdInsert.Parameters.AddWithValue("@vendor_id", model.VendorId ?? 0);
                    pettyCashId = Convert.ToInt32(await cmdInsert.ExecuteScalarAsync());
                }
                else
                {
                    // --- UPDATE ---
                    string updateQuery = @"
                UPDATE tbl_petty_cash 
                SET  voucher_date=@voucher_date, cash_account_id=@cash_account_id, 
                    employee_id=@employee_id, total=@total, notes=@notes , vendor_id=@vendor_id
                WHERE id=@id;";

                    using var cmdUpdate = new MySqlCommand(updateQuery, conn);
                    cmdUpdate.Parameters.AddWithValue("@id", model.Id);
                    cmdUpdate.Parameters.AddWithValue("@code", pettyCashCode);
                    cmdUpdate.Parameters.AddWithValue("@voucher_date", model.VoucherDate);
                    cmdUpdate.Parameters.AddWithValue("@cash_account_id", model.CashAccountId);
                    cmdUpdate.Parameters.AddWithValue("@employee_id", model.EmployeeId);
                    cmdUpdate.Parameters.AddWithValue("@total", model.Total);
                    cmdUpdate.Parameters.AddWithValue("@notes", model.Notes ?? "");
                    cmdUpdate.Parameters.AddWithValue("@vendor_id", model.VendorId ?? 0);
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
                        var ProjectId = string.IsNullOrWhiteSpace(d.ProjectId) ? "0" : d.ProjectId;
                        var VendorId = string.IsNullOrWhiteSpace(d.VendorId) ? "0" : d.VendorId;
                        string insertDetailQuery = @"
                    INSERT INTO tbl_petty_cash_details  
                        ( petty_cash_id, hum_id, entry_date, hum_name, ref_id, cost_center_id, amount,project_id, vendor_id, description, category, note)
                    VALUES
                        ( @petty_cash_id, @hum_id, @entry_date, @hum_name, @ref_id, @cost_center_id, @amount,@project_id, @vendor_id, @description, @category, @note);";

                        using var cmdDetail = new MySqlCommand(insertDetailQuery, conn);
                        cmdDetail.Parameters.AddWithValue("@petty_cash_id", pettyCashId);
                        cmdDetail.Parameters.AddWithValue("@hum_id", humId);
                        cmdDetail.Parameters.AddWithValue("@entry_date", DateTime.Now);
                        cmdDetail.Parameters.AddWithValue("@hum_name", humName);
                        cmdDetail.Parameters.AddWithValue("@ref_id", refId);
                        cmdDetail.Parameters.AddWithValue("@cost_center_id", costCenterId);
                        cmdDetail.Parameters.AddWithValue("@amount", amount);
                        cmdDetail.Parameters.AddWithValue("@description", description);
                        cmdDetail.Parameters.AddWithValue("@category", category);
                        cmdDetail.Parameters.AddWithValue("@project_id", ProjectId);
                        cmdDetail.Parameters.AddWithValue("@vendor_id", VendorId);
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
                return Json(500, new { status = false, message = ex.Message });
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
                   dt.cost_center_id, dt.description, dt.amount, dt.project_id, dt.vendor_id, dt.note,
            c.name AS categoryName,
            p.name AS projectName,
            v.name AS vendorName
            FROM tbl_petty_cash_details dt
                LEFT JOIN tbl_petty_cash_category c ON c.id = dt.category
                LEFT JOIN tbl_projects p ON p.id = dt.project_id
                LEFT JOIN tbl_vendor v ON v.id = dt.vendor_id
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
                            VendorId = reader["vendor_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["vendor_id"]),
                            note = reader["note"]?.ToString() ?? "",
                            projectName = reader["projectName"]?.ToString() ?? "",
                            vendorName = reader["vendorName"]?.ToString() ?? ""
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
                int pettyCashAccountId = Convert.ToInt32(cashAccountId);
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

                string pettyCashAccountQuery = @"
    SELECT a.id AS AccountId, a.name AS AccountName
    FROM tbl_petty_cash_card pc
    JOIN tbl_coa_level_4 a ON a.id = pc.account_id
    WHERE pc.id = @id
    LIMIT 1;
";

                using var pettyCashAccountCmd = new MySqlCommand(pettyCashAccountQuery, conn);
                pettyCashAccountCmd.Parameters.AddWithValue("@id", cashAccountId);

                string pettyCashAccountName = "";

                using (var pettyCashReader = await pettyCashAccountCmd.ExecuteReaderAsync())
                {
                    try{
                        // 🔥 Required — Read first row
                        if (await pettyCashReader.ReadAsync())
                        {
                            pettyCashAccountId = Convert.ToInt32(pettyCashReader["AccountId"]);
                            pettyCashAccountName = pettyCashReader["AccountName"].ToString();
                        }
                    }catch(Exception ex)
                    {
                        throw ex;
                    }
                  
                }



                // --- STEP 3: Load Petty Cash Details ---
                string detailsQuery = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY entry_date) AS SN,
                dt.id, dt.petty_cash_id, dt.entry_date, dt.ref_id, dt.hum_id, dt.category,
                dt.cost_center_id, dt.description, dt.amount, dt.project_id, dt.vendor_id, dt.note
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
                    int Project_Id = Convert.ToInt32(detailsReader["project_id"]);
                    string costCenterId = detailsReader["cost_center_id"]?.ToString();
                    string humId = detailsReader["hum_id"]?.ToString();
                    string vendor_id = detailsReader["vendor_id"]?.ToString();
                    string note = detailsReader["note"]?.ToString();

                    detailsList.Add(new
                    {
                        SN = detailsReader["SN"],
                        id = detailId,
                        description,
                        amount,
                        costCenterId,
                        vendor_id,
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
                        pettyCashAccountName,
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

                // ----------------------------
                // 1️⃣ VALIDATION (matches chkRequireData)
                // ----------------------------
                if (total <= 0)
                    return Ok(new { status = false, message = "Please Enter PettyCash Amount." });

                if ((total <= 0) && (model.Details == null || model.Details.Count == 0))
                    return Ok(new { status = false, message = "Enter Amount" });

                if (model.Details == null || model.Details.Count == 0)
                    return Ok(new { status = false, message = "Enter Amount / Details Required." });

                foreach (var d in model.Details)
                {
                    if (d.Amount > 0 && string.IsNullOrWhiteSpace(d.Category))
                        return Ok(new { status = false, message = "Choose an account" });
                }

                // ----------------------------
                // 2️⃣ CHECK IF OLD VOUCHER
                // ----------------------------
                if (pettyCashId > 0)
                {
                    var checkCmd = new MySqlCommand("SELECT * FROM tbl_petty_cash WHERE id=@id", conn, tx);
                    checkCmd.Parameters.AddWithValue("@id", pettyCashId);

                    await using var reader = await checkCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        code = reader["code"].ToString();
                        voucherDate = Convert.ToDateTime(reader["voucher_date"]);
                        pettyCashId = Convert.ToInt32(reader["id"]);
                        isOldBill = true;
                    }
                }
                else if (!string.IsNullOrEmpty(code))
                {
                    var codeCmd = new MySqlCommand("SELECT * FROM tbl_petty_cash WHERE code=@code", conn, tx);
                    codeCmd.Parameters.AddWithValue("@code", code);

                    await using var reader = await codeCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        pettyCashId = Convert.ToInt32(reader["id"]);
                        voucherDate = Convert.ToDateTime(reader["voucher_date"]);
                        isOldBill = true;
                    }
                }

                // ----------------------------
                // 3️⃣ DELETE OLD TRANSACTIONS if old
                // ----------------------------
                if (isOldBill)
                {
                    // Delete journal transactions
                    var delTrans = new MySqlCommand(
                        "DELETE FROM tbl_transaction WHERE t_type='Petty Cash' AND transaction_id=@id",
                        conn, tx);
                    delTrans.Parameters.AddWithValue("@id", pettyCashId);
                    await delTrans.ExecuteNonQueryAsync();

                    // Delete cost center entries
                    var delCost = new MySqlCommand(
                        "DELETE FROM tbl_cost_center_transaction WHERE type='PettyCash' AND ref_id=@id",
                        conn, tx);
                    delCost.Parameters.AddWithValue("@id", pettyCashId.ToString());
                    await delCost.ExecuteNonQueryAsync();

                    // Delete petty cash details
                    var delDetails = new MySqlCommand(
                        "DELETE FROM tbl_petty_cash_details WHERE petty_cash_id=@id",
                        conn, tx);
                    delDetails.Parameters.AddWithValue("@id", pettyCashId);
                    await delDetails.ExecuteNonQueryAsync();

                    // Update header
                    var updateCmd = new MySqlCommand(@"
                UPDATE tbl_petty_cash SET 
                    voucher_date=@date,
                    cash_account_id=@cash,
                    employee_id=@emp,
                    total=@total,
                    status=1
                WHERE id=@id", conn, tx);

                    updateCmd.Parameters.AddWithValue("@date", voucherDate);
                    updateCmd.Parameters.AddWithValue("@cash", model.CashAccountId);
                    updateCmd.Parameters.AddWithValue("@emp", model.EmployeeId);
                    updateCmd.Parameters.AddWithValue("@total", total);
                    updateCmd.Parameters.AddWithValue("@id", pettyCashId);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // ----------------------------
                    // 4️⃣ INSERT HEADER if new
                    // ----------------------------
                    var insertCmd = new MySqlCommand(@"
                INSERT INTO tbl_petty_cash
                (code, voucher_date, cash_account_id, employee_id, notes, total, created_by,vendor_id, status)
                VALUES (@code, @date, @cash  @emp, @notes, @total, @created_by, @vendor_id, 1)", conn, tx);

                    insertCmd.Parameters.AddWithValue("@code", code);
                    insertCmd.Parameters.AddWithValue("@date", voucherDate);
                    insertCmd.Parameters.AddWithValue("@cash", model.CashAccountId);
                    insertCmd.Parameters.AddWithValue("@emp", model.EmployeeId);
                    insertCmd.Parameters.AddWithValue("@total", total);
                    insertCmd.Parameters.AddWithValue("@notes", model.Notes);
                    insertCmd.Parameters.AddWithValue("@created_by", userId);
                    insertCmd.Parameters.AddWithValue("@vendor_id", model.VendorId ?? 0);

                    await insertCmd.ExecuteNonQueryAsync();
                    pettyCashId = (int)insertCmd.LastInsertedId;
                }


                // ----------------------------
                // 5️⃣ INSERT DETAIL ROWS + JOURNAL + COST CENTER + petty_cash_details
                // ----------------------------
                foreach (var d in model.Details)
                {
                    if (d.Amount <= 0) continue;

                    string amount = d.Amount.ToString();
                    string costCenter = d.CostCenterId ?? "0";
                    string accountId = d.Category ?? "0";
                    string description = d.Description ?? "";
                    string humId = d.VendorId ?? "0";
                    var humName = d.HumName ?? "";
                    var refId = string.IsNullOrWhiteSpace(d.RefId) ? "0" : d.RefId;
                    var category = string.IsNullOrWhiteSpace(d.Category) ? "0" : d.Category;
                    var note = d.Note ?? "";
                    var ProjectId = string.IsNullOrWhiteSpace(d.ProjectId) ? "0" : d.ProjectId;
                    var VendorId = string.IsNullOrWhiteSpace(d.VendorId) ? "0" : d.VendorId;

                    // Cost Center Debit
                    var ccCmd = new MySqlCommand(@"
                INSERT INTO tbl_cost_center_transaction
                (type, date, ref_id, debit, credit, description, cost_center_id)
                VALUES ('PettyCash', @date, @ref, @debit, '0', 'PettyCash Entry', @cc)", conn, tx);

                    ccCmd.Parameters.AddWithValue("@date", voucherDate);
                    ccCmd.Parameters.AddWithValue("@ref", pettyCashId.ToString());
                    ccCmd.Parameters.AddWithValue("@debit", amount);
                    ccCmd.Parameters.AddWithValue("@cc", costCenter);
                    await ccCmd.ExecuteNonQueryAsync();

                    // Journal Debit
                    var jCmd = new MySqlCommand(@"
                INSERT INTO tbl_transaction
                (date, account_id, debit, credit, transaction_id, hum_id, t_type, type,
                 description, created_by, created_date, state, voucher_no)
                VALUES (@date, @acc, @debit, '0', @id, @hum, 'Petty Cash', 'Petty Cash',
                        @desc, @user, NOW(), 0, @code)", conn, tx);

                    jCmd.Parameters.AddWithValue("@date", voucherDate);
                    jCmd.Parameters.AddWithValue("@acc", accountId);
                    jCmd.Parameters.AddWithValue("@debit", amount);
                    jCmd.Parameters.AddWithValue("@id", pettyCashId.ToString());
                    jCmd.Parameters.AddWithValue("@hum", humId);
                    jCmd.Parameters.AddWithValue("@desc", description);
                    jCmd.Parameters.AddWithValue("@user", model.EmployeeId);
                    jCmd.Parameters.AddWithValue("@code", code);
                    await jCmd.ExecuteNonQueryAsync();

                    var detailsCmd = new MySqlCommand(@"
INSERT INTO tbl_petty_cash_details
(petty_cash_id, description, amount, category, hum_id, hum_name, ref_id,project_id,vendor_id, note, cost_center_id, entry_date)
VALUES (@id, @desc, @amt, @cat, @hum, @hum_name, @ref_id, @project_id,@vendor_id, @note, @cc, @entryDate)", conn, tx);

                    detailsCmd.Parameters.AddWithValue("@id", pettyCashId);
                    detailsCmd.Parameters.AddWithValue("@desc", description);
                    detailsCmd.Parameters.AddWithValue("@amt", d.Amount);
                    detailsCmd.Parameters.AddWithValue("@cat", category);
                    detailsCmd.Parameters.AddWithValue("@hum", humId);
                    detailsCmd.Parameters.AddWithValue("@hum_name", humName);
                    detailsCmd.Parameters.AddWithValue("@ref_id", refId);
                    detailsCmd.Parameters.AddWithValue("@project_id", ProjectId);
                    detailsCmd.Parameters.AddWithValue("@vendor_id", VendorId);
                    detailsCmd.Parameters.AddWithValue("@note", note);
                    detailsCmd.Parameters.AddWithValue("@cc", costCenter);
                    detailsCmd.Parameters.AddWithValue("@entryDate", DateTime.Now); 
                    await detailsCmd.ExecuteNonQueryAsync();


 
                }

                // ----------------------------
                // 6️⃣ TOTAL CREDIT
                // ----------------------------
                var creditCmd = new MySqlCommand(@"
            INSERT INTO tbl_transaction
            (date, account_id, debit, credit, transaction_id, hum_id, t_type, type,
             description, created_by, created_date, state, voucher_no)
            VALUES (@date, @acc, '0', @credit, @id, '0', 'Petty Cash', 'Petty Cash',
                    @desc, @user, NOW(), 0, @code)", conn, tx);

                creditCmd.Parameters.AddWithValue("@date", voucherDate);
                creditCmd.Parameters.AddWithValue("@acc", model.CashAccountId.ToString());
                creditCmd.Parameters.AddWithValue("@credit", total.ToString());
                creditCmd.Parameters.AddWithValue("@id", pettyCashId.ToString());
                creditCmd.Parameters.AddWithValue("@desc", $"PETTY CASH NO.{code}");
                creditCmd.Parameters.AddWithValue("@user", model.EmployeeId);
                creditCmd.Parameters.AddWithValue("@code", code);
                await creditCmd.ExecuteNonQueryAsync();

                // ----------------------------
                // 7️⃣ COMMIT TRANSACTION
                // ----------------------------
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

        [HttpGet]
        public async Task<IActionResult> GetPettyCashTransactions(int pettyCashId, string description)
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
                t.transaction_id AS id,
                a.name AS name,
                t.debit AS debit,
                0.00 AS credit
            FROM tbl_transaction t
            INNER JOIN tbl_petty_cash_details pd 
                ON t.transaction_id = pd.petty_cash_id
                AND (t.description COLLATE utf8mb4_general_ci = pd.description COLLATE utf8mb4_general_ci OR @Description IS NULL)
            INNER JOIN tbl_coa_level_4 a ON t.account_id = a.id
            WHERE 
                t.type = 'Petty Cash'
                AND pd.petty_cash_id = @PettyCashId
                AND (@Description IS NULL OR pd.description = @Description COLLATE utf8mb4_general_ci)

            UNION ALL

            SELECT
                t.transaction_id AS id,
                a.name AS name,
                0.00 AS debit,
                pd.amount AS credit
            FROM tbl_transaction t
            INNER JOIN tbl_petty_cash_details pd 
                ON t.transaction_id = pd.petty_cash_id
                AND t.credit > 0
            INNER JOIN tbl_coa_level_4 a ON t.account_id = a.id
            WHERE 
                t.type = 'Petty Cash'
                AND pd.petty_cash_id = @PettyCashId
                AND (@Description = '' OR pd.description = @Description)
            ORDER BY id, name;";

                string desc = description ?? ""; 

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@PettyCashId", pettyCashId);
                cmd.Parameters.AddWithValue("@Description", desc);
                using var reader = await cmd.ExecuteReaderAsync();
                var transactions = new List<object>();
                while (await reader.ReadAsync())
                {
                    transactions.Add(new
                    {
                        id = reader["id"],
                        name = reader["name"]?.ToString(),
                        debit = reader["debit"] != DBNull.Value ? Convert.ToDecimal(reader["debit"]) : 0,
                        credit = reader["credit"] != DBNull.Value ? Convert.ToDecimal(reader["credit"]) : 0
                    });
                }

                return Ok(new { status = true, data = transactions });
            }
            catch (Exception ex)
            {
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
                        Total = reader.GetDecimal("total"),
                        CostCenter = reader.IsDBNull("costcenter") ? 0 : reader.GetInt32("costcenter")

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
                (code, name, category_id, debit_account_id, credit_account_id, start_date, end_date, amount, fee, total, created_by, created_date, costcenter)
                VALUES
                (@code, @name, @category_id, @debit_account_id, @credit_account_id, @start_date, @end_date, @amount, @fee, @total, @created_by, @created_date, @costcenter);
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
                    cmd.Parameters.AddWithValue("@costcenter", model.CostCenter ?? 0);

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
                    modified_by=@modified_by, modified_date=@modified_date, costcenter = @costcenter
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
                    cmd.Parameters.AddWithValue("@costcenter", model.CostCenter ?? 0);

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
                model.Name + " - Prepaid Expense No. " + pId, userId, DateTime.Now, model.CostCenter ?? 0);

            await CommonInsert.InsertTransactionEntryAsync(conn, lastDateOfFirstMonth,
                model.CreditAccountId.ToString(), "0", firstMonthAmount.ToString(), pId.ToString(), "0", "Prepaid Expense",
                model.Name + " - Prepaid Expense No. " + pId, userId, DateTime.Now, model.CostCenter ?? 0);

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
                    model.Name + " - Prepaid Expense No. " + pId, userId, DateTime.Now, model.CostCenter ?? 0);

                await CommonInsert.InsertTransactionEntryAsync(conn, lastDateOfMonth,
                    model.CreditAccountId.ToString(), "0", monthlyAmount.ToString(), pId.ToString(), "0", "Prepaid Expense",
                    model.Name + " - Prepaid Expense No. " + pId, userId, DateTime.Now, model.CostCenter ?? 0);

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
        DateTime createdDate, int costcenter)
    {
        string query = @"
            INSERT INTO tbl_transaction 
            (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, costcenter)
            VALUES 
            (@date, @accountId, @debit, @credit, @transactionId, @hum_id, @tType, @type, @description, @createdBy, @createdDate, 0, @costcenter);";

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
        cmd.Parameters.AddWithValue("@costcenter", costcenter);

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

        #region Customer List

        public IActionResult CustomerList()
        {
            return View();
        }

        #endregion

        #region Customer Category List

        public IActionResult CustomerCategoryList()
        {
            return View();
        }

        #endregion

        #region Vendor List

        public IActionResult VendorList()
        {
            return View();
        }

        #endregion

        #region Vendor CateogoryList

        public IActionResult VendorCategoryList()
        {
            return View();
        }

        #endregion



    }

}

