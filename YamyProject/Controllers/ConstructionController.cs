using System.Text.RegularExpressions;
using YamyProject.Core.Models;


namespace YamyProject.Controllers
{
    [Route("Construction/[action]")]
    public class ConstructionController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly MySqlConnection _connection;
        public ConstructionController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _connection = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        }

        #region ProjectCenter

        public IActionResult ProjectCenter()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjects()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                id,
                code,
                name,
                category,
                start_date AS StartDate,
                country_id AS CountryId,
                city_id AS CityId,
                end_date AS EndDate,
                description As Description
            FROM tbl_projects
            ORDER BY id;";

                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var projectList = new List<object>();
                int sn = 1;
                while (await reader.ReadAsync())
                {
                    projectList.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        Code = reader["code"].ToString(),
                        Name = reader["name"].ToString(),
                        Category = reader["category"].ToString(),
                        StartDate = reader["StartDate"] != DBNull.Value ? Convert.ToDateTime(reader["StartDate"]).ToString("yyyy-MM-dd") : null,
                        EndDate = reader["EndDate"] != DBNull.Value ? Convert.ToDateTime(reader["EndDate"]).ToString("yyyy-MM-dd") : null,
                        CountryId = reader.GetInt32("CountryId"),
                        CityId = reader.GetInt32("CityId"),
                        Description = reader["Description"].ToString()
                    });
                }

                return Ok(new { status = true, data = projectList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveProject([FromBody] ProjectRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter Project Name" });

            if (model.StartDate >= model.EndDate)
                return BadRequest(new { status = false, message = "Start Date must be before End Date" });


            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 1️⃣ Check if project name already exists
                string checkQuery = "SELECT id FROM tbl_projects WHERE name=@name";
                await using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    var existingId = await checkCmd.ExecuteScalarAsync();
                    if (existingId != null && (model.Id == 0 || model.Id != Convert.ToInt32(existingId)))
                    {
                        return BadRequest(new { status = false, message = "Project Name already in use" });
                    }
                }

                // 2️⃣ Generate project code for new projects
                string projectCode = model.Id == 0 ? await GenerateNextProjectCode(conn) : model.Code;

                if (model.Id == 0)
                {
                    // Insert new project
                    string insertQuery = @"
                INSERT INTO tbl_projects
                (code, name, category, description, start_date, end_date, country_id, city_id)
                VALUES (@code, @name, @category, @description, @start_date, @end_date, @country_id, @city_id);
                SELECT LAST_INSERT_ID();";

                    await using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@code", projectCode);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@category", model.Category);
                    cmd.Parameters.AddWithValue("@description", model.Description);
                    cmd.Parameters.AddWithValue("@start_date", model.StartDate);
                    cmd.Parameters.AddWithValue("@end_date", model.EndDate);
                    cmd.Parameters.AddWithValue("@country_id", model.CountryId ?? 0);
                    cmd.Parameters.AddWithValue("@city_id", model.CityId ?? 0);

                    int projectId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    // Optional: Insert cost center if enabled
                    if (model.IsProjectOption)
                    {
                        int mainId = 0;
                        string mainInsert = @"
                    INSERT INTO tbl_cost_center (name, code, project_id)
                    VALUES (@name, @code, @project_id);
                    SELECT LAST_INSERT_ID();";

                        await using (var mainCmd = new MySqlCommand(mainInsert, conn))
                        {
                            mainCmd.Parameters.AddWithValue("@name", model.Name);
                            mainCmd.Parameters.AddWithValue("@code", projectCode);
                            mainCmd.Parameters.AddWithValue("@project_id", projectId);
                            mainId = Convert.ToInt32(await mainCmd.ExecuteScalarAsync());
                        }

                        string subInsert = @"
                    INSERT INTO tbl_sub_cost_center (code, name, main_id, project_id)
                    VALUES (@code, @name, @main_id, @project_id);";

                        await using (var subCmd = new MySqlCommand(subInsert, conn))
                        {
                            subCmd.Parameters.AddWithValue("@code", projectCode);
                            subCmd.Parameters.AddWithValue("@name", model.Name);
                            subCmd.Parameters.AddWithValue("@main_id", mainId);
                            subCmd.Parameters.AddWithValue("@project_id", projectId);
                            await subCmd.ExecuteNonQueryAsync();
                        }
                    }

                    return Ok(new
                    {
                        status = true,
                        message = "Project created successfully",
                        id = projectId,
                        code = projectCode
                    });
                }
                else
                {
                    // Update existing project
                    string updateQuery = @"
                UPDATE tbl_projects
                SET code=@code, name=@name, category=@category, description=@description,
                    start_date=@start_date, end_date=@end_date, country_id=@country_id, city_id=@city_id
                WHERE id=@id;";

                    await using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@code", projectCode);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@category", model.Category);
                    cmd.Parameters.AddWithValue("@description", model.Description);
                    cmd.Parameters.AddWithValue("@start_date", model.StartDate);
                    cmd.Parameters.AddWithValue("@end_date", model.EndDate);
                    cmd.Parameters.AddWithValue("@country_id", model.CountryId ?? 0);
                    cmd.Parameters.AddWithValue("@city_id", model.CityId ?? 0);

                    int affected = await cmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Project not found" });

                    // Optional: Update Sub Cost Center if enabled
                    if (model.IsProjectOption)
                    {
                        string checkSub = "SELECT id FROM tbl_sub_cost_center WHERE name=@name";
                        await using (var checkCmd = new MySqlCommand(checkSub, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                            var subIdObj = await checkCmd.ExecuteScalarAsync();
                            if (subIdObj != null)
                            {
                                int subId = Convert.ToInt32(subIdObj);
                                string updateSub = "UPDATE tbl_sub_cost_center SET name=@name WHERE id=@id";
                                await using var updateCmd = new MySqlCommand(updateSub, conn);
                                updateCmd.Parameters.AddWithValue("@id", subId);
                                updateCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                                await updateCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    return Ok(new
                    {
                        status = true,
                        message = "Project updated successfully",
                        id = model.Id,
                        code = projectCode
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateNextProjectCode(MySqlConnection conn)
        {
            string query = "SELECT MAX(CAST(code AS UNSIGNED)) FROM tbl_projects";
            await using var cmd = new MySqlCommand(query, conn);
            object result = await cmd.ExecuteScalarAsync();

            int next = 1;
            if (result != DBNull.Value && result != null)
                next = Convert.ToInt32(result) + 1;

            return next.ToString("D3"); // Format: 001, 002, 003
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteProject(int projectId)
        {
            if (projectId <= 0)
                return BadRequest(new { status = false, message = "Invalid Project ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if project is used in tbl_project_tender
                string checkQuery = "SELECT COUNT(1) FROM tbl_project_tender WHERE project_id=@id";
                await using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", projectId);
                    var result = await checkCmd.ExecuteScalarAsync();
                    int recordCount = result != null ? Convert.ToInt32(result) : 0;
                    if (recordCount > 0)
                        return BadRequest(new { status = false, message = "Project already used in tenders and cannot be deleted" });
                }

                // Delete project
                string deleteQuery = "DELETE FROM tbl_projects WHERE id=@id";
                await using (var deleteCmd = new MySqlCommand(deleteQuery, conn))
                {
                    deleteCmd.Parameters.AddWithValue("@id", projectId);
                    int affected = await deleteCmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Project not found" });
                }

                return Ok(new { status = true, message = "Project deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }



        #endregion

        #region TenderCenter

        public IActionResult TenderCenter()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetTenders()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"SELECT id, code, name FROM tbl_tender_names ORDER BY id;";
                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var tenderList = new List<object>();
                int sn = 1;
                while (await reader.ReadAsync())
                {
                    tenderList.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        Code = reader["code"].ToString(),
                        Name = reader["name"].ToString()
                    });
                }

                return Ok(new { status = true, data = tenderList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveTender([FromBody] TenderRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter Tender Name" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if name already exists
                string checkQuery = "SELECT id FROM tbl_tender_names WHERE name=@name";
                await using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    var existingId = await checkCmd.ExecuteScalarAsync();
                    if (existingId != null && (model.Id == 0 || model.Id != Convert.ToInt32(existingId)))
                        return BadRequest(new { status = false, message = "Tender Name already in use" });
                }

                if (model.Id == 0)
                {
                    // Generate next tender code
                    string code = await GenerateNextTenderCode(conn);

                    string insertQuery = @"INSERT INTO tbl_tender_names (code, name) VALUES (@code, @name); SELECT LAST_INSERT_ID();";
                    await using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    int tenderId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    return Ok(new { status = true, message = "Tender added successfully", id = tenderId, code = code });
                }
                else
                {
                    string updateQuery = @"UPDATE tbl_tender_names SET name=@name WHERE id=@id";
                    await using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@id", model.Id);

                    int affected = await cmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Tender not found" });

                    return Ok(new { status = true, message = "Tender updated successfully", id = model.Id });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteTender(int tenderId)
        {
            if (tenderId <= 0)
                return BadRequest(new { status = false, message = "Invalid Tender ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if tender is used in tbl_project_tender
                string checkQuery = "SELECT COUNT(1) FROM tbl_project_tender WHERE tender_name_id=@id";
                await using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", tenderId);
                    var result = await checkCmd.ExecuteScalarAsync();
                    int recordCount = result != null ? Convert.ToInt32(result) : 0;
                    if (recordCount > 0)
                        return BadRequest(new { status = false, message = "Tender already used in projects and cannot be deleted" });
                }

                string deleteQuery = "DELETE FROM tbl_tender_names WHERE id=@id";
                await using (var cmd = new MySqlCommand(deleteQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", tenderId);
                    int affected = await cmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Tender not found" });
                }

                return Ok(new { status = true, message = "Tender deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        // Generate sequential tender code (001, 002, 003)
        private async Task<string> GenerateNextTenderCode(MySqlConnection conn)
        {
            string query = "SELECT MAX(CAST(code AS UNSIGNED)) FROM tbl_tender_names";
            await using var cmd = new MySqlCommand(query, conn);
            object result = await cmd.ExecuteScalarAsync();

            int next = 1;
            if (result != DBNull.Value && result != null)
                next = Convert.ToInt32(result) + 1;

            return next.ToString("D3"); // Format: 001, 002, 003
        }

        #endregion

        #region SiteCenter

        public IActionResult SiteCenter()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetCities()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"SELECT id, name FROM tbl_city ORDER BY name;";
                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var cityList = new List<object>();
                int sn = 1;
                while (await reader.ReadAsync())
                {
                    cityList.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        Name = reader["name"].ToString()
                    });
                }

                return Ok(new { status = true, data = cityList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectSites()
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

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY ps.id) AS SN, 
                ps.id, 
                ps.code, 
                ps.name, 
                COALESCE(c.name, 'Unknown') AS location, 
                ps.plot_number, 
                ps.address,
                c.id AS LocationId
            FROM tbl_project_sites ps
            LEFT JOIN tbl_city c ON c.id = ps.location_id;
        ";

                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        sn = reader.GetInt32("SN"),
                        id = reader.GetInt32("id"),
                        code = reader["code"].ToString(),
                        name = reader["name"].ToString(),
                        location = reader["location"].ToString(),
                        plotNumber = reader["plot_number"].ToString(),
                        address = reader["address"].ToString(),
                        locationId = reader.GetInt32("LocationId")
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
        public async Task<IActionResult> SaveSite([FromBody] SiteRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter Site Name" });

            if (model.LocationId == null || model.LocationId <= 0)
                return BadRequest(new { status = false, message = "Please select a project location" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔍 Check if site name already exists
                string checkQuery = "SELECT id FROM tbl_project_sites WHERE name=@name";
                await using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    var existingId = await checkCmd.ExecuteScalarAsync();
                    if (existingId != null && (model.Id == 0 || model.Id != Convert.ToInt32(existingId)))
                        return BadRequest(new { status = false, message = "Site Name already in use" });
                }

                if (model.Id == 0)
                {
                    // 🆕 Generate next site code
                    string code = await GenerateNextSiteCode(conn);

                    // Insert new site
                    string insertQuery = @"
                INSERT INTO tbl_project_sites (code, name, location_id, plot_number, address)
                VALUES (@code, @name, @location_id, @plot_number, @address);
                SELECT LAST_INSERT_ID();";

                    await using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@location_id", model.LocationId ?? 0);
                    cmd.Parameters.AddWithValue("@plot_number", model.PlotNumber ?? "");
                    cmd.Parameters.AddWithValue("@address", model.Address ?? "");

                    int siteId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    return Ok(new { status = true, message = "Site added successfully", id = siteId, code = code });
                }
                else
                {
                    // ✏️ Update existing site
                    string updateQuery = @"
                UPDATE tbl_project_sites 
                SET name=@name, location_id=@location_id, plot_number=@plot_number, address=@address 
                WHERE id=@id;";

                    await using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@location_id", model.LocationId ?? 0);
                    cmd.Parameters.AddWithValue("@plot_number", model.PlotNumber ?? "");
                    cmd.Parameters.AddWithValue("@address", model.Address ?? "");

                    int affected = await cmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Site not found" });

                    return Ok(new { status = true, message = "Site updated successfully", id = model.Id });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateNextSiteCode(MySqlConnection conn)
        {
            string query = "SELECT MAX(CAST(code AS UNSIGNED)) FROM tbl_project_sites";
            await using var cmd = new MySqlCommand(query, conn);
            object result = await cmd.ExecuteScalarAsync();

            int next = 1;
            if (result != DBNull.Value && result != null)
                next = Convert.ToInt32(result) + 1;

            return next.ToString("D3"); // e.g., 001, 002, 003
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProjectSite(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid Project Site ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if used in tbl_project_planning
                string checkQuery = "SELECT COUNT(1) FROM tbl_project_planning WHERE site = @id";
                await using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    var result = await checkCmd.ExecuteScalarAsync();
                    int count = Convert.ToInt32(result);
                    if (count > 0)
                        return BadRequest(new { status = false, message = "Project site already used in project planning" });
                }

                // Delete project site
                string deleteQuery = "DELETE FROM tbl_project_sites WHERE id=@id";
                await using (var deleteCmd = new MySqlCommand(deleteQuery, conn))
                {
                    deleteCmd.Parameters.AddWithValue("@id", id);
                    int affected = await deleteCmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Project site not found" });
                }

                return Ok(new { status = true, message = "Project site deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Project Tender

        public IActionResult ProjectTender()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectTenders(
                                                    int? projectId = null,
                                                    int? tenderId = null)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY pt.date) AS SN,
                pt.id,
                pt.date AS Date,
                CONCAT(p.code,' - ', p.name) AS ProjectName,
                CONCAT(t.code,' - ', t.name) AS TenderName,
                pt.submission_date AS SubmitDate,
                pt.fees AS Fees,
                pt.project_id As Project_Id,
                pt.account_id As Account_Id,
                pt.warehouse_id As Warehouse_Id,
                pt.tender_name_id As Tender_Name_Id,
                pt.description As Description,
                p.code As Code
            FROM tbl_project_tender pt
            INNER JOIN tbl_projects p ON pt.project_id = p.id
            INNER JOIN tbl_tender_names t ON pt.tender_name_id = t.id
            WHERE pt.state = 0";

                var parameters = new List<MySqlParameter>();

                if (projectId.HasValue)
                {
                    query += " AND pt.project_id = @projectId";
                    parameters.Add(new MySqlParameter("@projectId", projectId.Value));
                }

                if (tenderId.HasValue)
                {
                    query += " AND pt.tender_name_id = @tenderId";
                    parameters.Add(new MySqlParameter("@tenderId", tenderId.Value));
                }

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                var tenders = new List<object>();
                int sn = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tenders.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        Date = reader["Date"] != DBNull.Value ? Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd") : null,
                        ProjectName = reader["ProjectName"].ToString(),
                        TenderName = reader["TenderName"].ToString(),
                        SubmitDate = reader["SubmitDate"] != DBNull.Value ? Convert.ToDateTime(reader["SubmitDate"]).ToString("yyyy-MM-dd") : null,
                        Fees = reader["Fees"] != DBNull.Value ? Convert.ToDecimal(reader["Fees"]) : 0,
                        Project_Id = reader.GetInt32("Project_Id"),
                        Account_Id = reader.GetInt32("Account_Id"),
                        Warehouse_Id = reader.GetInt32("Warehouse_Id"),
                        Tender_Name_Id = reader.GetInt32("Tender_Name_Id"),
                        Description = reader["Description"].ToString(),
                        Code = reader["Code"].ToString()

                    });
                }

                return Ok(new { status = true, data = tenders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTenderItems(int tenderId)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if tender is exported
                string checkQuery = "SELECT COUNT(*) FROM tbl_items_boq WHERE ref_id = @id";
                await using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@id", tenderId);
                int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                bool isExported = count > 0;

                string itemQuery;
                if (isExported)
                {
                    itemQuery = @"
                SELECT CONCAT(ti.sr,' - ',ti.name) AS ItemName,
                       ts.qty AS Qty,
                       ts.rate AS Rate,
                       ts.unit_id AS Unit,
                       ts.amount AS Amount,
                       ti.sr As Sr,
                       ti.name As Name,
                       ti.unit_name As Unit_Name,
                       ti.length As Length,
                       ti.width As Width,
                       ti.thickness As Thickness,
                       ts.margin_percentage As Margin_Percentage,
                       ts.margin_amount As Margin_Amount,
                       ts.total As Total
                FROM tbl_project_tender_details ts
                INNER JOIN tbl_items_boq ti ON ts.item_id = ti.id AND ts.tender_id = ti.ref_id
                WHERE ts.tender_id = @id";
                }
                else
                {
                    itemQuery = @"
                SELECT CONCAT(ti.code,' - ',ti.name) AS ItemName,
                       ts.qty AS Qty,
                       ts.rate AS Rate,
                       (SELECT NAME FROM tbl_unit WHERE id = ts.unit_id) AS Unit,
                       ts.amount AS Amount
                FROM tbl_project_tender_details ts
                INNER JOIN tbl_items ti ON ts.item_id = ti.id
                WHERE ts.tender_id = @id";
                }

                await using var itemCmd = new MySqlCommand(itemQuery, conn);
                itemCmd.Parameters.AddWithValue("@id", tenderId);

                var items = new List<object>();
                await using var reader = await itemCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new
                    {
                        ItemName = reader["ItemName"].ToString(),
                        Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                        Rate = reader["Rate"] != DBNull.Value ? Convert.ToDecimal(reader["Rate"]) : 0,
                        Unit = reader["Unit"].ToString(),
                        Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0,
                        Sr = reader["Sr"].ToString(),
                        Name = reader["Name"].ToString(),
                        Unit_Name = reader["Unit_Name"].ToString(),
                        Length = reader["Length"] != DBNull.Value ? Convert.ToDecimal(reader["Length"]) : 0,
                        Width = reader["Width"] != DBNull.Value ? Convert.ToDecimal(reader["Width"]) : 0,
                        Thickness = reader["Thickness"] != DBNull.Value ? Convert.ToDecimal(reader["Thickness"]) : 0,
                        Margin_Percentage = reader["Margin_Percentage"] != DBNull.Value ? Convert.ToDecimal(reader["Margin_Percentage"]) : 0,
                        Margin_Amount = reader["Margin_Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Margin_Amount"]) : 0,
                        Total = reader["Total"] != DBNull.Value ? Convert.ToDecimal(reader["Total"]) : 0
                    });
                }

                return Ok(new { status = true, data = items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateTender([FromBody] ProjectTenderRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            // 🔹 Validation: Required fields
            if (model.ProjectId <= 0)
                return BadRequest(new { status = false, message = "Please select a Project" });

            if (model.TenderNameId <= 0)
                return BadRequest(new { status = false, message = "Please select a Tender" });

            if (model.WarehouseId <= 0)
                return BadRequest(new { status = false, message = "Please select a Warehouse" });

            if (model.Items == null || model.Items.Count == 0)
                return BadRequest(new { status = false, message = "Please add at least one BOQ item" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Check if tender is estimated already (if updating)
                bool isEstimated = false;
                if (model.Id > 0)
                {
                    string checkEstimate = "SELECT estimate_status FROM tbl_project_tender WHERE id=@id";
                    await using var checkCmd = new MySqlCommand(checkEstimate, conn);
                    checkCmd.Parameters.AddWithValue("@id", model.Id);
                    var estObj = await checkCmd.ExecuteScalarAsync();
                    if (estObj != null && Convert.ToInt32(estObj) == 1)
                        isEstimated = true;

                    if (isEstimated)
                        return BadRequest(new
                        {
                            status = false,
                            message = "Estimate already generated for this tender. Editing is not allowed."
                        });
                }

                int tenderId = 0;

                // 🔹 Insert Mode
                if (model.Id == 0)
                {
                    string insertQuery = @"
                INSERT INTO tbl_project_tender 
                (date, tender_name_id, account_id, project_id, fees, submission_date, description, warehouse_id, created_by, created_date, state)
                VALUES 
                (@date, @tenderNameId, @accountId, @projectId, @fees, @submissionDate, @description, @warehouseId, @createdBy, @createdDate, 0);
                SELECT LAST_INSERT_ID();";

                    await using var insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@date", model.Date);
                    insertCmd.Parameters.AddWithValue("@tenderNameId", model.TenderNameId);
                    insertCmd.Parameters.AddWithValue("@accountId", model.AccountId);
                    insertCmd.Parameters.AddWithValue("@projectId", model.ProjectId);
                    insertCmd.Parameters.AddWithValue("@fees", model.Fees ?? 0);
                    insertCmd.Parameters.AddWithValue("@submissionDate", model.SubmissionDate);
                    insertCmd.Parameters.AddWithValue("@description", model.Description ?? "");
                    insertCmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                    insertCmd.Parameters.AddWithValue("@createdBy", userId);
                    insertCmd.Parameters.AddWithValue("@createdDate", DateTime.Now.Date);

                    tenderId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                }
                else
                {
                    // 🔹 Update Mode
                    string updateQuery = @"
                UPDATE tbl_project_tender 
                SET modified_by=@modifiedBy, modified_date=@modifiedDate,
                    date=@date, project_id=@projectId, submission_date=@submissionDate,
                    description=@description, fees=@fees, warehouse_id=@warehouseId,
                    account_id=@accountId, tender_name_id=@tenderNameId
                WHERE id=@id;";

                    await using var updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);
                    updateCmd.Parameters.AddWithValue("@date", model.Date);
                    updateCmd.Parameters.AddWithValue("@submissionDate", model.SubmissionDate);
                    updateCmd.Parameters.AddWithValue("@projectId", model.ProjectId);
                    updateCmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                    updateCmd.Parameters.AddWithValue("@accountId", model.AccountId);
                    updateCmd.Parameters.AddWithValue("@tenderNameId", model.TenderNameId);
                    updateCmd.Parameters.AddWithValue("@fees", model.Fees ?? 0);
                    updateCmd.Parameters.AddWithValue("@description", model.Description ?? "");
                    updateCmd.Parameters.AddWithValue("@modifiedBy", userId);
                    updateCmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now.Date);

                    int affected = await updateCmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Tender not found" });

                    tenderId = model.Id;

                    // Delete old details
                    string deleteDetails = "DELETE FROM tbl_project_tender_details WHERE tender_id=@id";
                    await using var delCmd = new MySqlCommand(deleteDetails, conn);
                    delCmd.Parameters.AddWithValue("@id", model.Id);
                    await delCmd.ExecuteNonQueryAsync();
                }

                // 🔹 Insert Tender BOQ Items
                foreach (var item in model.Items)
                {
                    string insertItemBoq = @"
                INSERT INTO tbl_items_boq (sr, ref_id, type, name, unit_name, qty, price, amount, length, width, thickness, note)
                VALUES (@sr, @refId, 'BOQ', @name, @unit, @qty, @price, @amount, @length, @width, @thickness, @note);
                SELECT LAST_INSERT_ID();";

                    await using var itemCmd = new MySqlCommand(insertItemBoq, conn);
                    itemCmd.Parameters.AddWithValue("@sr", item.Sr);
                    itemCmd.Parameters.AddWithValue("@refId", tenderId);
                    itemCmd.Parameters.AddWithValue("@name", item.Description ?? "");
                    itemCmd.Parameters.AddWithValue("@unit", item.Unit ?? "");
                    itemCmd.Parameters.AddWithValue("@qty", item.Qty ?? 0);
                    itemCmd.Parameters.AddWithValue("@price", item.Rate ?? 0);
                    itemCmd.Parameters.AddWithValue("@amount", item.Amount ?? 0);
                    itemCmd.Parameters.AddWithValue("@length", item.Length ?? 0);
                    itemCmd.Parameters.AddWithValue("@width", item.Width ?? 0);
                    itemCmd.Parameters.AddWithValue("@thickness", item.Thick ?? 0);
                    itemCmd.Parameters.AddWithValue("@note", item.Note ?? "");

                    int itemId = Convert.ToInt32(await itemCmd.ExecuteScalarAsync());

                    string insertTenderDetails = @"
                INSERT INTO tbl_project_tender_details
                (sr, tender_id, item_id, qty, unit_id, rate, amount, length, width, thickness, note)
                VALUES
                (@sr, @tenderId, @itemId, @qty, 0, @rate, @amount, @length, @width, @thickness, @note);";

                    await using var detailCmd = new MySqlCommand(insertTenderDetails, conn);
                    detailCmd.Parameters.AddWithValue("@sr", item.Sr);
                    detailCmd.Parameters.AddWithValue("@tenderId", tenderId);
                    detailCmd.Parameters.AddWithValue("@itemId", itemId);
                    detailCmd.Parameters.AddWithValue("@qty", item.Qty ?? 0);
                    detailCmd.Parameters.AddWithValue("@rate", item.Rate ?? 0);
                    detailCmd.Parameters.AddWithValue("@amount", item.Amount ?? 0);
                    detailCmd.Parameters.AddWithValue("@length", item.Length ?? 0);
                    detailCmd.Parameters.AddWithValue("@width", item.Width ?? 0);
                    detailCmd.Parameters.AddWithValue("@thickness", item.Thick ?? 0);
                    detailCmd.Parameters.AddWithValue("@note", item.Note ?? "");
                    await detailCmd.ExecuteNonQueryAsync();
                }

                // ✅ Return success
                return Ok(new
                {
                    status = true,
                    message = model.Id == 0
                   ? "Project Tender created successfully"
                   : "Project Tender updated successfully",
                    id = tenderId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = "An unexpected error occurred: " + ex.Message
                });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteTenders(int tenderId)
        {
            if (tenderId <= 0)
                return BadRequest(new { status = false, message = "Invalid Tender ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if tender is estimated (cannot delete if estimated)
                string checkEstimateQuery = "SELECT estimate_status FROM tbl_project_tender WHERE id=@id";
                await using var checkCmd = new MySqlCommand(checkEstimateQuery, conn);
                checkCmd.Parameters.AddWithValue("@id", tenderId);
                var estObj = await checkCmd.ExecuteScalarAsync();
                if (estObj != null && Convert.ToInt32(estObj) == 1)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Cannot delete. Estimate already generated for this tender."
                    });
                }

                // Soft delete: set state = -1
                string deleteQuery = "UPDATE tbl_project_tender SET state=-1 WHERE id=@id";
                await using var cmd = new MySqlCommand(deleteQuery, conn);
                cmd.Parameters.AddWithValue("@id", tenderId);
                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected == 0)
                    return NotFound(new { status = false, message = "Tender not found" });

                return Ok(new { status = true, message = "Tender deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = "Error deleting tender: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDeletedTenders()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
        SELECT 
            ROW_NUMBER() OVER (ORDER BY pt.date) AS SN,
            pt.id,
            pt.date AS Date,
            CONCAT(p.code,' - ', p.name) AS ProjectName,
            CONCAT(t.code,' - ', t.name) AS TenderName,
            pt.submission_date AS SubmitDate,
            pt.fees AS Fees
        FROM tbl_project_tender pt
        INNER JOIN tbl_projects p ON pt.project_id = p.id
        INNER JOIN tbl_tender_names t ON pt.tender_name_id = t.id
        WHERE pt.state = -1";

                await using var cmd = new MySqlCommand(query, conn);

                var tenders = new List<object>();
                int sn = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tenders.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        Date = reader["Date"] != DBNull.Value ? Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd") : null,
                        ProjectName = reader["ProjectName"].ToString(),
                        TenderName = reader["TenderName"].ToString(),
                        SubmitDate = reader["SubmitDate"] != DBNull.Value ? Convert.ToDateTime(reader["SubmitDate"]).ToString("yyyy-MM-dd") : null,
                        Fees = reader["Fees"] != DBNull.Value ? Convert.ToDecimal(reader["Fees"]) : 0,
                    });
                }

                return Ok(new { status = true, data = tenders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RestoreTender(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid tender ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = "UPDATE tbl_project_tender SET state = 0 WHERE id = @id";
                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                int rows = await cmd.ExecuteNonQueryAsync();

                if (rows > 0)
                    return Ok(new { status = true, message = "Tender restored successfully" });
                else
                    return NotFound(new { status = false, message = "Tender not found or already active" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Project Estimation

        public IActionResult ProjectEstimation()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateEstimation([FromBody] ProjectTenderRequest model)
        {
            try
            {
                if (model == null)
                    return Json(new { status = false, message = "Invalid request" });

                if (model.ProjectId <= 0)
                    return Json(new { status = false, message = "Please select a Project" });

                if (model.TenderNameId <= 0)
                    return Json(new { status = false, message = "Please select a Tender" });

                if (model.WarehouseId <= 0)
                    return Json(new { status = false, message = "Please select a Warehouse" });

                if (model.Items == null || model.Items.Count == 0)
                    return Json(new { status = false, message = "Insert Items First." });

                if (model.Fees == null) model.Fees = 0;
                if (model.Amount == null || model.Amount == 0)
                    return Json(new { status = false, message = "Total Must Be Bigger Than Zero" });

                try
                {
                    int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    if (userId <= 0)
                        return Json(new { status = false, message = "User not logged in" });

                    var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                    {
                        Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                    };

                    await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                    await conn.OpenAsync();

                    await using var transaction = await conn.BeginTransactionAsync();

                    try
                    {
                        if (model.Id > 0)
                        {
                            string checkEstimate = "SELECT estimate_status FROM tbl_project_tender WHERE id=@id";
                            await using (var cmd = new MySqlCommand(checkEstimate, conn, (MySqlTransaction)transaction))
                            {
                                cmd.Parameters.AddWithValue("@id", model.Id);
                                var estObj = await cmd.ExecuteScalarAsync();
                                if (estObj != null && Convert.ToInt32(estObj) == 1)
                                {
                                    await transaction.RollbackAsync();
                                    return Json(new { status = false, message = "Estimate already generated for this tender. Editing is not allowed." });
                                }
                            }
                        }

                        int tenderId = 0;

                        string updateTender = @"
                        UPDATE tbl_project_tender 
                        SET modified_by = @modifiedBy, modified_date = @modifiedDate,
                            date = @date, project_id = @projectId, submission_date = @submissionDate,
                            description = @description, fees = @fees, amount = @amount, warehouse_id = @warehouseId, account_id = @accountId, tender_name_id = @tenderNameId,
                        estimate_status=1 WHERE id = @id;";
                        await using (var cmd = new MySqlCommand(updateTender, conn, (MySqlTransaction)transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", model.Id);
                            cmd.Parameters.AddWithValue("@modifiedBy", userId);
                            cmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now.Date);
                            cmd.Parameters.AddWithValue("@date", model.Date.Date);
                            cmd.Parameters.AddWithValue("@projectId", model.ProjectId);
                            cmd.Parameters.AddWithValue("@submissionDate", model.SubmissionDate.Date);
                            cmd.Parameters.AddWithValue("@description", model.Description ?? "");
                            cmd.Parameters.AddWithValue("@fees", model.Fees ?? 0);
                            cmd.Parameters.AddWithValue("@amount", model.Amount ?? 0);
                            cmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                            cmd.Parameters.AddWithValue("@accountId", model.AccountId);
                            cmd.Parameters.AddWithValue("@tenderNameId", model.TenderNameId);

                            int affected = await cmd.ExecuteNonQueryAsync();
                            if (affected == 0)
                            {
                                await transaction.RollbackAsync();
                                return NotFound(new { status = false, message = "Tender not found" });
                            }
                        }

                        tenderId = model.Id;

                        // Delete old details and dependent records (mirrors your WinForms deletes)
                        string deleteDetails = @"
                        DELETE FROM tbl_project_tender_details WHERE tender_id=@tenderId;
                        DELETE FROM tbl_items_boq_details WHERE ref_id IN (SELECT id FROM tbl_items_boq WHERE ref_id=@tenderId);
                        DELETE FROM tbl_item_assembly_bos WHERE assembly_id IN (SELECT id FROM tbl_items_boq WHERE ref_id=@tenderId);
                        DELETE FROM tbl_items_boq WHERE ref_id=@tenderId;
                        DELETE FROM tbl_item_transaction WHERE type = 'Project Tender' AND item_id = @tenderId;
                        DELETE FROM tbl_transaction WHERE type = 'Project Tender' AND transaction_id = @tenderId;
                        DELETE FROM tbl_item_card_details WHERE trans_type = 'Project Tender' AND trans_no = @tenderId;
                    ";
                        await using (var del = new MySqlCommand(deleteDetails, conn, (MySqlTransaction)transaction))
                        {
                            del.Parameters.AddWithValue("@tenderId", tenderId);
                            await del.ExecuteNonQueryAsync();
                        }

                        int nextItemCode = 0;
                        string nextCodeSql = "SELECT IFNULL(MAX(CAST(code AS UNSIGNED)), 0) + 1 AS next_code FROM tbl_items;";
                        await using (var codeCmd = new MySqlCommand(nextCodeSql, conn, (MySqlTransaction)transaction))
                        {
                            var codeObj = await codeCmd.ExecuteScalarAsync();
                            nextItemCode = codeObj != null && codeObj != DBNull.Value ? Convert.ToInt32(codeObj) : 1;
                        }

                        string refSr = "";
                        int subId = 0;
                        int assemblyItemId = 0;

                        foreach (var item in model.Items)
                        {
                            decimal qty = item.Qty ?? 0;
                            decimal rate = item.Rate ?? 0;
                            decimal amount = item.Amount ?? 0;
                            decimal marginAmount = item.MarginAmount ?? 0;
                            decimal marginPercentage = item.MarginPercentage ?? 0;

                            string sr = item.Sr ?? "";
                            string description = item.Description ?? "";

                            if (string.IsNullOrWhiteSpace(sr) || string.IsNullOrWhiteSpace(description))
                            {
                                continue;
                            }

                            // Insert into tbl_items_boq (BOQ row)
                            string insertBoq = @"
                        INSERT INTO tbl_items_boq (sr, ref_id, type, name, unit_name, qty, price, amount, length, width, thickness, note)
                        VALUES (@sr, @refId, 'BOQ', @name, @unit, @qty, @price, @amount, @length, @width, @thickness, @note);
                        SELECT LAST_INSERT_ID();";
                            int boqId;
                            await using (var boqCmd = new MySqlCommand(insertBoq, conn, (MySqlTransaction)transaction))
                            {
                                boqCmd.Parameters.AddWithValue("@sr", sr);
                                boqCmd.Parameters.AddWithValue("@refId", tenderId);
                                boqCmd.Parameters.AddWithValue("@name", description);
                                boqCmd.Parameters.AddWithValue("@unit", item.Unit ?? "");
                                boqCmd.Parameters.AddWithValue("@qty", qty);
                                boqCmd.Parameters.AddWithValue("@price", rate);
                                boqCmd.Parameters.AddWithValue("@amount", amount);
                                boqCmd.Parameters.AddWithValue("@length", item.Length ?? 0);
                                boqCmd.Parameters.AddWithValue("@width", item.Width ?? 0);
                                boqCmd.Parameters.AddWithValue("@thickness", item.Thick ?? 0);
                                boqCmd.Parameters.AddWithValue("@note", item.Note ?? "");
                                boqId = Convert.ToInt32(await boqCmd.ExecuteScalarAsync());
                            }

                            // Insert into tbl_project_tender_details
                            string insertTenderDetail = @"
                        INSERT INTO tbl_project_tender_details (sr, tender_id, item_id, qty, unit_id, rate, amount, length, width, thickness, note, margin_percentage, margin_amount,total)
                        VALUES (@sr, @tenderId, @itemId, @qty, 0, @rate, @amount, @length, @width, @thickness, @note, @margin_percentage, @margin_amount, @total);";
                            await using (var detCmd = new MySqlCommand(insertTenderDetail, conn, (MySqlTransaction)transaction))
                            {
                                detCmd.Parameters.AddWithValue("@sr", sr);
                                detCmd.Parameters.AddWithValue("@tenderId", tenderId);
                                detCmd.Parameters.AddWithValue("@itemId", boqId);
                                detCmd.Parameters.AddWithValue("@qty", qty);
                                detCmd.Parameters.AddWithValue("@rate", rate);
                                detCmd.Parameters.AddWithValue("@amount", amount);
                                detCmd.Parameters.AddWithValue("@length", item.Length ?? 0);
                                detCmd.Parameters.AddWithValue("@width", item.Width ?? 0);
                                detCmd.Parameters.AddWithValue("@thickness", item.Thick ?? 0);
                                detCmd.Parameters.AddWithValue("@note", item.Note ?? "");
                                detCmd.Parameters.AddWithValue("@margin_percentage", marginPercentage);
                                detCmd.Parameters.AddWithValue("@margin_amount", marginAmount);
                                detCmd.Parameters.AddWithValue("@total", amount + marginAmount);
                                await detCmd.ExecuteNonQueryAsync();
                            }

                            // If sr is alphabetic only => treat as assembly header and create an "Inventory Assembly" item row
                            if (!string.IsNullOrEmpty(sr) && Regex.IsMatch(sr, @"^[A-Za-z]+$"))
                            {
                                refSr = sr;
                                // insert into tbl_items (assembly) if not exists, using the pattern from your original code
                                string insertItemSql = @"
                            INSERT INTO tbl_items(
                                code, warehouse_id, type, category_id, name, unit_id, barcode, cost_price, 
                                cogs_account_id, vendor_id, sales_price, income_account_id, asset_account_id, 
                                min_amount, max_amount, on_hand, method, total_value, date, img, active, state, 
                                created_By, created_date, Item_type)
                            SELECT
                                @code, @warehouseId, @type, @category, @name, @unit_id, @barcode, @cost_price, 
                                @cogs_account_id, @vendor_id, @sales_price, @income_account_id, @asset_account_id, 
                                @min_amount, @max_amount, @on_hand, @method, @total_value, @date, @img, @active, @state, 
                                @created_By, @created_date, @Item_type
                            WHERE NOT EXISTS (
                                SELECT 1 FROM tbl_items WHERE name = @name
                            ); SELECT LAST_INSERT_ID();";

                                await using (var itemCmd = new MySqlCommand(insertItemSql, conn, (MySqlTransaction)transaction))
                                {
                                    itemCmd.Parameters.AddWithValue("@code", nextItemCode.ToString());
                                    itemCmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                                    itemCmd.Parameters.AddWithValue("@type", "13 - Inventory Assembly");
                                    itemCmd.Parameters.AddWithValue("@category", 0);
                                    itemCmd.Parameters.AddWithValue("@name", description);
                                    itemCmd.Parameters.AddWithValue("@unit_id", 0);
                                    itemCmd.Parameters.AddWithValue("@barcode", "");
                                    itemCmd.Parameters.AddWithValue("@cost_price", rate);
                                    // NOTE: you had cmbCOGSAccount / cmbIncomeAccount / cmbAssetAccount from UI - adapt below to real account ids
                                    itemCmd.Parameters.AddWithValue("@cogs_account_id", 0);
                                    itemCmd.Parameters.AddWithValue("@vendor_id", 0);
                                    itemCmd.Parameters.AddWithValue("@sales_price", 0);
                                    itemCmd.Parameters.AddWithValue("@income_account_id", 0);
                                    itemCmd.Parameters.AddWithValue("@asset_account_id", 0);
                                    itemCmd.Parameters.AddWithValue("@min_amount", 0);
                                    itemCmd.Parameters.AddWithValue("@max_amount", 0);
                                    itemCmd.Parameters.AddWithValue("@on_hand", qty);
                                    itemCmd.Parameters.AddWithValue("@method", "fifo");
                                    itemCmd.Parameters.AddWithValue("@total_value", 0);
                                    itemCmd.Parameters.AddWithValue("@date", model.Date.Date);
                                    itemCmd.Parameters.AddWithValue("@img", "");
                                    itemCmd.Parameters.AddWithValue("@active", 0);
                                    itemCmd.Parameters.AddWithValue("@state", 0);
                                    itemCmd.Parameters.AddWithValue("@created_By", userId);
                                    itemCmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                                    itemCmd.Parameters.AddWithValue("@Item_type", "Inventory");
                                    var createdItemIdObj = await itemCmd.ExecuteScalarAsync();
                                    assemblyItemId = createdItemIdObj != null && createdItemIdObj != DBNull.Value ? Convert.ToInt32(createdItemIdObj) : 0;
                                }

                                nextItemCode++;

                            }
                            else
                            {
                                // else treat as assembly components (parts). Create parts in tbl_items_boq_details & tbl_items (part) and insert assembly relationships.
                                // create Assembly part record: insert into tbl_items_boq_details
                                // we need to insert the BOQ detail referencing the parent BOQ item (refId -> boqId)
                                string insertBoqDetail = @"
                            INSERT INTO tbl_items_boq_details (code, warehouse_id, type, category_id, name, unit_id, barcode, cost_price, 
                                cogs_account_id, vendor_id, sales_price, income_account_id, asset_account_id, 
                                min_amount, max_amount, on_hand, method, total_value, date, img, active, state, created_By, created_date, ref_id)
                            VALUES (@code, @warehouse_id, @type, @category, @name, @unit_id, @barcode, @cost_price, 
                                @cogs_account_id, @vendor_id, @sales_price, @income_account_id, @asset_account_id, 
                                @min_amount, @max_amount, @on_hand, @method, @total_value, @date, @img, @active, @state, @created_By, @created_date, @refId);
                            SELECT LAST_INSERT_ID();";
                                int assemblyId;
                                string codeForPart = (refSr ?? "") + (subId + 1);
                                await using (var boqDetailCmd = new MySqlCommand(insertBoqDetail, conn, (MySqlTransaction)transaction))
                                {
                                    boqDetailCmd.Parameters.AddWithValue("@code", codeForPart);
                                    boqDetailCmd.Parameters.AddWithValue("@warehouse_id", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@type", "13 - Inventory Assembly");
                                    boqDetailCmd.Parameters.AddWithValue("@category", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@name", description);
                                    boqDetailCmd.Parameters.AddWithValue("@unit_id", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@barcode", "");
                                    boqDetailCmd.Parameters.AddWithValue("@cost_price", rate);
                                    boqDetailCmd.Parameters.AddWithValue("@cogs_account_id", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@vendor_id", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@sales_price", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@income_account_id", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@asset_account_id", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@min_amount", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@max_amount", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@on_hand", qty);
                                    boqDetailCmd.Parameters.AddWithValue("@method", "fifo");
                                    boqDetailCmd.Parameters.AddWithValue("@total_value", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@date", model.Date.Date);
                                    boqDetailCmd.Parameters.AddWithValue("@img", "");
                                    boqDetailCmd.Parameters.AddWithValue("@active", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@state", 0);
                                    boqDetailCmd.Parameters.AddWithValue("@created_By", userId);
                                    boqDetailCmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                                    boqDetailCmd.Parameters.AddWithValue("@refId", boqId);
                                    assemblyId = Convert.ToInt32(await boqDetailCmd.ExecuteScalarAsync());
                                }

                                // insert into tbl_items (part) if not exists
                                string insertPartSql = @"
                            INSERT INTO tbl_items(
                                code, warehouse_id, type, category_id, name, unit_id, barcode, cost_price, 
                                cogs_account_id, vendor_id, sales_price, income_account_id, asset_account_id, 
                                min_amount, max_amount, on_hand, method, total_value, date, img, active, state, 
                                created_By, created_date, Item_type)
                            SELECT 
                                @code, @warehouseId, @type, @category, @name, @unit_id, @barcode, @cost_price, 
                                @cogs_account_id, @vendor_id, @sales_price, @income_account_id, @asset_account_id, 
                                @min_amount, @max_amount, @on_hand, @method, @total_value, @date, @img, @active, @state, 
                                @created_By, @created_date, @Item_type
                            WHERE NOT EXISTS (
                                SELECT 1 FROM tbl_items WHERE name = @name
                            ); SELECT LAST_INSERT_ID();";
                                int itemIdOf;
                                await using (var partCmd = new MySqlCommand(insertPartSql, conn, (MySqlTransaction)transaction))
                                {
                                    partCmd.Parameters.AddWithValue("@code", nextItemCode);
                                    partCmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                                    partCmd.Parameters.AddWithValue("@type", "11 - Inventory Part");
                                    partCmd.Parameters.AddWithValue("@category", 0);
                                    partCmd.Parameters.AddWithValue("@name", description);
                                    partCmd.Parameters.AddWithValue("@unit_id", 0);
                                    partCmd.Parameters.AddWithValue("@barcode", "");
                                    partCmd.Parameters.AddWithValue("@cost_price", rate);
                                    partCmd.Parameters.AddWithValue("@cogs_account_id", 0);
                                    partCmd.Parameters.AddWithValue("@vendor_id", 0);
                                    partCmd.Parameters.AddWithValue("@sales_price", 0);
                                    partCmd.Parameters.AddWithValue("@income_account_id", 0);
                                    partCmd.Parameters.AddWithValue("@asset_account_id", 0);
                                    partCmd.Parameters.AddWithValue("@min_amount", 0);
                                    partCmd.Parameters.AddWithValue("@max_amount", 0);
                                    partCmd.Parameters.AddWithValue("@on_hand", qty);
                                    partCmd.Parameters.AddWithValue("@method", "fifo");
                                    partCmd.Parameters.AddWithValue("@total_value", 0);
                                    partCmd.Parameters.AddWithValue("@date", model.Date.Date);
                                    partCmd.Parameters.AddWithValue("@img", "");
                                    partCmd.Parameters.AddWithValue("@active", 0);
                                    partCmd.Parameters.AddWithValue("@state", 0);
                                    partCmd.Parameters.AddWithValue("@created_By", userId);
                                    partCmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                                    partCmd.Parameters.AddWithValue("@Item_type", "Inventory");
                                    var partObj = await partCmd.ExecuteScalarAsync();
                                    itemIdOf = partObj != null && partObj != DBNull.Value ? Convert.ToInt32(partObj) : 0;
                                }

                                // Insert assembly relationship records
                                string insertAssemblyRel = @"
                            INSERT INTO tbl_item_assembly_bos(assembly_id, item_id, qty) VALUES (@assembly_id, @item_id, @qty);
                            INSERT INTO tbl_item_assembly(assembly_id, item_id, qty) VALUES (@assembly_item_id, @itemId, @qty);";
                                await using (var relCmd = new MySqlCommand(insertAssemblyRel, conn, (MySqlTransaction)transaction))
                                {
                                    relCmd.Parameters.AddWithValue("@assembly_id", boqId.ToString());
                                    relCmd.Parameters.AddWithValue("@assembly_item_id", itemIdOf.ToString());
                                    relCmd.Parameters.AddWithValue("@item_id", assemblyId.ToString());
                                    relCmd.Parameters.AddWithValue("@itemId", assemblyItemId.ToString());
                                    relCmd.Parameters.AddWithValue("@qty", qty);
                                    await relCmd.ExecuteNonQueryAsync();
                                }

                                if (qty != 0)
                                {
                                    await InsertItemTransaction(conn, (MySqlTransaction)transaction, qty, model.Date.Date, rate, tenderId.ToString(), itemIdOf.ToString(), model.WarehouseId.ToString());

                                    await InsertItemJournal(conn, (MySqlTransaction)transaction, qty, model.Date.Date, nextItemCode.ToString(), rate, 0, tenderId.ToString());
                                }

                                nextItemCode++;
                                subId++;

                            }
                        }

                        await transaction.CommitAsync();

                        return Ok(new { status = true, message = model.Id == 0 ? "Project Tender created successfully" : "Project Tender updated successfully", id = tenderId });
                    }
                    catch (Exception exInner)
                    {
                        await transaction.RollbackAsync();
                        return StatusCode(500, new { status = false, message = "An unexpected error occurred (inner): " + exInner.Message });
                    }
                }
                catch (Exception ex)
                {
                    return Json( new { status = false, message = "An unexpected error occurred: " + ex.Message });
                }
            }
            catch(Exception ex)
            {
                return Json(new { status = false, message = "An unexpected error occurred: " + ex.Message });
            }
           
        }

        private async Task InsertItemTransaction(MySqlConnection conn, MySqlTransaction trx,
            decimal qty, DateTime dated, decimal cost, string pId, string itemId, string warehouseId)
        {
            // Insert into tbl_item_transaction
            string insert = @"
            INSERT INTO tbl_item_transaction 
            (date, type, reference, item_id, cost_price, qty_in, sales_price, qty_out, qty_inc, description, warehouse_id) 
            VALUES (@date, @type, @reference, @itemId, @costPrice, @qtyIn, @sales_price, @qtyOut, @qtyInc, @description, @warehouseId);";
            await using (var cmd = new MySqlCommand(insert, conn, trx))
            {
                cmd.Parameters.AddWithValue("@date", dated);
                cmd.Parameters.AddWithValue("@type", "Project Tender");
                cmd.Parameters.AddWithValue("@reference", pId);
                cmd.Parameters.AddWithValue("@itemId", itemId);
                cmd.Parameters.AddWithValue("@costPrice", cost.ToString());
                cmd.Parameters.AddWithValue("@sales_price", "0");
                cmd.Parameters.AddWithValue("@qtyIn", qty.ToString());
                cmd.Parameters.AddWithValue("@qtyOut", "0");
                cmd.Parameters.AddWithValue("@qtyInc", qty.ToString());
                cmd.Parameters.AddWithValue("@description", "Project Opening Balance");
                cmd.Parameters.AddWithValue("@warehouseId", warehouseId);
                await cmd.ExecuteNonQueryAsync();
            }

            await UpdateOnHandItem(conn, trx, itemId);
            await AddItemCardDetails(conn, trx, dated, "Project Tender", pId, itemId, cost.ToString(), qty.ToString(), "0", "0", qty.ToString(), "Project Opening Balance", warehouseId);
        }

        private async Task UpdateOnHandItem(MySqlConnection conn, MySqlTransaction trx, string itemId)
        {
            string update = @"UPDATE tbl_items SET on_hand = (SELECT IFNULL(SUM(qty_in - qty_out),0) FROM tbl_item_transaction WHERE item_id = @itemId) WHERE id = @itemId;";
            await using var cmd = new MySqlCommand(update, conn, trx);
            cmd.Parameters.AddWithValue("@itemId", itemId);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task AddItemCardDetails(MySqlConnection conn, MySqlTransaction trx,
            DateTime date, string type, string reference, string itemId, string costPrice, string qtyIn, string salesPrice, string qtyOut, string qtyInc, string description, string warehouseId)
        {
            decimal debit = 0, credit = 0, _qtyIn = 0, _qtyOut = 0;
            decimal.TryParse(costPrice, out decimal price);
            if (!string.IsNullOrEmpty(qtyIn) && decimal.Parse(qtyIn) > 0)
            {
                debit = decimal.Parse(qtyIn) * price;
                _qtyIn = decimal.Parse(qtyIn);
            }
            if (!string.IsNullOrEmpty(qtyOut) && decimal.Parse(qtyOut) > 0)
            {
                credit = decimal.Parse(qtyOut) * price;
                _qtyOut = decimal.Parse(qtyOut);
            }

            // compute existing balances from tbl_item_card_details
            string sqlQtyBalance = "SELECT IFNULL(SUM(qty_in - qty_out),0) FROM tbl_item_card_details WHERE itemId = @id";
            string sqlBalance = "SELECT IFNULL(SUM(debit - credit),0) FROM tbl_item_card_details WHERE itemId = @id";
            decimal existingQtyBalance = 0, existingBalance = 0;

            await using (var cmd = new MySqlCommand(sqlQtyBalance, conn, trx))
            {
                cmd.Parameters.AddWithValue("@id", itemId);
                var qtyObj = await cmd.ExecuteScalarAsync();
                existingQtyBalance = qtyObj != null && qtyObj != DBNull.Value ? Convert.ToDecimal(qtyObj) : 0;
            }
            await using (var cmd2 = new MySqlCommand(sqlBalance, conn, trx))
            {
                cmd2.Parameters.AddWithValue("@id", itemId);
                var balObj = await cmd2.ExecuteScalarAsync();
                existingBalance = balObj != null && balObj != DBNull.Value ? Convert.ToDecimal(balObj) : 0;
            }

            decimal qtyBalance = existingQtyBalance + (_qtyIn - _qtyOut);
            decimal balance = existingBalance + (debit - credit);
            decimal fifoQty = 0, fifoCost = 0;

            string invoiceNo = "INV-" + reference;
            string transNo = reference;
            string transType = type;

            string insertCard = @"
            INSERT INTO tbl_item_card_details (
                itemId, date, wharehouse_id, inv_no, trans_no, trans_type, description,
                price, qty_in, qty_out, qty_balance, debit, credit, balance, fifo_qty, fifo_cost
            ) VALUES (
                @itemId, @date, @wharehouse_id, @inv_no, @trans_no, @trans_type, @description,
                @price, @qty_in, @qty_out, @qty_balance, @debit, @credit, @balance, @fifo_qty, @fifo_cost
            );";
            await using (var cmd3 = new MySqlCommand(insertCard, conn, trx))
            {
                cmd3.Parameters.AddWithValue("@itemId", itemId);
                cmd3.Parameters.AddWithValue("@date", date);
                cmd3.Parameters.AddWithValue("@wharehouse_id", warehouseId);
                cmd3.Parameters.AddWithValue("@inv_no", invoiceNo);
                cmd3.Parameters.AddWithValue("@trans_no", transNo);
                cmd3.Parameters.AddWithValue("@trans_type", transType);
                cmd3.Parameters.AddWithValue("@description", description);
                cmd3.Parameters.AddWithValue("@price", price);
                cmd3.Parameters.AddWithValue("@qty_in", qtyIn);
                cmd3.Parameters.AddWithValue("@qty_out", qtyOut);
                cmd3.Parameters.AddWithValue("@qty_balance", qtyBalance);
                cmd3.Parameters.AddWithValue("@debit", debit);
                cmd3.Parameters.AddWithValue("@credit", credit);
                cmd3.Parameters.AddWithValue("@balance", balance);
                cmd3.Parameters.AddWithValue("@fifo_qty", fifoQty);
                cmd3.Parameters.AddWithValue("@fifo_cost", fifoCost);
                await cmd3.ExecuteNonQueryAsync();
            }
        }

        private async Task InsertItemJournal(MySqlConnection conn, MySqlTransaction trx,
      decimal qty, DateTime dated, string itemCode, decimal cost, decimal totalValue, string pId)
        {
            totalValue = qty * cost;

            string inventoryAccount = await SelectDefaultLevelAccount(conn, trx, "Inventory");
            string openingEquityAccount = await SelectDefaultLevelAccount(conn, trx, "Opening Balance Equity");

            await InsertTransactionEntry(conn, trx, dated.Date, inventoryAccount, totalValue.ToString(), "0", pId, pId, "Project Tender",
                $"Project Opening Balance - Item Code - {itemCode}", (int)(HttpContext.Session.GetInt32("UserId") ?? 0), DateTime.Now.Date);

            await InsertTransactionEntry(conn, trx, dated.Date, openingEquityAccount, "0", totalValue.ToString(), pId, "0", "Project Tender",
                $"Project Opening Balance Equity - Item Code - {itemCode}", (int)(HttpContext.Session.GetInt32("UserId") ?? 0), DateTime.Now.Date);
        }
        private async Task<string> SelectDefaultLevelAccount(MySqlConnection conn, MySqlTransaction trx, string accountName)
        {
            string sql = "SELECT Id FROM tbl_coa_level_4 WHERE name = @name LIMIT 1";
            using (var cmd = new MySqlCommand(sql, conn, trx))
            {
                cmd.Parameters.AddWithValue("@name", accountName);
                object result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? "0";
            }
        }

        private async Task InsertTransactionEntry(MySqlConnection conn, MySqlTransaction trx,
            DateTime date, string accountId, string debit, string credit, string transactionId, string humId, string type, string description, int createdBy, DateTime createdDate)
        {
            string insert = @"
            INSERT INTO tbl_transaction 
            (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state) 
            VALUES (@date, @accountId, @debit, @credit, @transactionId, @humId, @tType, @type, @description, @createdBy, @createdDate, 0);";
            await using (var cmd = new MySqlCommand(insert, conn, trx))
            {
                cmd.Parameters.AddWithValue("@date", date);
                cmd.Parameters.AddWithValue("@accountId", accountId);
                cmd.Parameters.AddWithValue("@debit", debit);
                cmd.Parameters.AddWithValue("@credit", credit);
                cmd.Parameters.AddWithValue("@transactionId", transactionId);
                cmd.Parameters.AddWithValue("@humId", humId);
                cmd.Parameters.AddWithValue("@tType", "");
                cmd.Parameters.AddWithValue("@type", type.Trim());
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@createdBy", createdBy);
                cmd.Parameters.AddWithValue("@createdDate", createdDate);
                await cmd.ExecuteNonQueryAsync();
            }
        }



        #endregion

        #region Project Planning

        public IActionResult ProjectPlanning()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectPlanning(
           int? projectId = null,
           string? projectStatus = null,
           string? projectType = null)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY p.date) AS SN,
                p.date AS Date,
                p.id,
                p.id AS `P NO`,
                CONCAT(pr.code, ' - ', pr.name) AS `Project Name`,
                p.start_date AS `Start Date`,
                p.end_date AS `End Date`,
                p.status AS `Status`,
                p.project_type AS `Project Type`,
                p.estimated_budget AS `Est Budget`,
                p.progress AS `Progress`,
                p.fund_account_id As `Fund_Account_Id`,
                p.project_id As `Project_Id`,
                p.site As `Site`,
                p.tender_name_id As `Tender_Name_Id` 
            FROM tbl_project_planning p
            INNER JOIN tbl_projects pr ON p.project_id = pr.id
            WHERE p.state = 0";

                var parameters = new List<MySqlParameter>();

                // --- Apply filters only if values are provided ---
                if (projectId.HasValue)
                {
                    query += " AND p.project_id = @projectId";
                    parameters.Add(new MySqlParameter("@projectId", projectId.Value));
                }

                if (!string.IsNullOrEmpty(projectStatus))
                {
                    query += " AND p.status = @status";
                    parameters.Add(new MySqlParameter("@status", projectStatus));
                }

                if (!string.IsNullOrEmpty(projectType))
                {
                    query += " AND p.project_type = @type";
                    parameters.Add(new MySqlParameter("@type", projectType));
                }

                query += " GROUP BY p.id, p.date, p.estimated_budget;";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                var projects = new List<object>();
                int sn = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    projects.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("Id"),
                        Date = reader["Date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd")
                            : null,
                        ProjectNo = reader["P NO"].ToString(),
                        ProjectName = reader["Project Name"].ToString(),
                        StartDate = reader["Start Date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["Start Date"]).ToString("yyyy-MM-dd")
                            : null,
                        EndDate = reader["End Date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["End Date"]).ToString("yyyy-MM-dd")
                            : null,
                        Status = reader["Status"].ToString(),
                        ProjectType = reader["Project Type"].ToString(),
                        EstBudget = reader["Est Budget"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Est Budget"])
                            : 0,
                        Progress = reader["Progress"].ToString(),
                        Fund_Account_Id = reader.GetInt32("Fund_Account_Id"),
                        Project_Id = reader.GetInt32("Project_Id"),
                        Site = reader.GetInt32("Site"),
                        Tender_Name_Id = reader.GetInt32("Tender_Name_Id"),
                    });
                }

                return Ok(new { status = true, message = "Success", data = projects });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateProjectPlanning([FromBody] ProjectPlanningRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            if (model.ProjectId <= 0)
                return Json(new { status = false, message = "Select Project First." });

            if (model.EstimatedBudget <= 0)
                return Json(new { status = false, message = "Budget Must Be Bigger Than Zero" });

            if (model.TenderId <= 0)
                return Json(new { status = false, message = "Tender is not initiated for this project" });

            if (model.TenderNameId <= 0)
                return Json(new { status = false, message = "Select Tender Name First." });

            if (model.SiteId <= 0)
                return Json(new { status = false, message = "Select Site Name First." });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Json(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();
                await using var transaction = await conn.BeginTransactionAsync();

                try
                {
                    int recordId = 0;

                    if (model.Id > 0)
                    {
                        // Update existing project planning
                        string updateSql = @"
                    UPDATE tbl_project_planning
                    SET modified_by=@modifiedBy, modified_date=@modifiedDate,
                        date=@date, project_id=@projectId, location=@location,
                        site=@site, plot_number=@plotNumber, start_date=@startDate,
                        end_date=@endDate, status=@status, estimated_budget=@estimatedBudget,
                        project_type=@projectType, description=@description,
                        fund_account_id=@accountId, fund_period=@fundPeriod,
                        assigned_team=@assignedTeam, progress=@progress
                    WHERE id=@id;";

                        await using var cmd = new MySqlCommand(updateSql, conn, (MySqlTransaction)transaction);
                        cmd.Parameters.AddWithValue("@id", model.Id);
                        cmd.Parameters.AddWithValue("@modifiedBy", userId);
                        cmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@date", model.Date.Date);
                        cmd.Parameters.AddWithValue("@projectId", model.ProjectId);
                        cmd.Parameters.AddWithValue("@location", "0");
                        cmd.Parameters.AddWithValue("@site", model.SiteId);
                        cmd.Parameters.AddWithValue("@plotNumber", "");
                        cmd.Parameters.AddWithValue("@startDate", model.StartDate.Date);
                        cmd.Parameters.AddWithValue("@endDate", model.EndDate.Date);
                        cmd.Parameters.AddWithValue("@status", model.Status ?? "");
                        cmd.Parameters.AddWithValue("@projectType", model.ProjectType ?? "");
                        cmd.Parameters.AddWithValue("@estimatedBudget", model.EstimatedBudget);
                        cmd.Parameters.AddWithValue("@description", "");
                        cmd.Parameters.AddWithValue("@accountId", model.AccountId);
                        cmd.Parameters.AddWithValue("@fundPeriod", "");
                        cmd.Parameters.AddWithValue("@assignedTeam", "");
                        cmd.Parameters.AddWithValue("@progress", 0);

                        int affected = await cmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                        {
                            await transaction.RollbackAsync();
                            return NotFound(new { status = false, message = "Project Planning not found" });
                        }

                        recordId = model.Id;

                        // Delete previous transactions
                        string deleteTransactions = @"DELETE FROM tbl_transaction WHERE t_type='Project Planning' AND transaction_id=@id;";
                        await using var delCmd = new MySqlCommand(deleteTransactions, conn, (MySqlTransaction)transaction);
                        delCmd.Parameters.AddWithValue("@id", recordId);
                        await delCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        // Insert new project planning
                        string insertSql = @"
                    INSERT INTO tbl_project_planning
                    (date, project_id, location, site, plot_number, start_date, end_date, status, estimated_budget, project_type,
                     description, fund_account_id, fund_period, assigned_team, progress, tender_id, created_by, created_date, tender_name_id)
                    VALUES
                    (@date, @projectId, @location, @site, @plotNumber, @startDate, @endDate, @status, @estimatedBudget, @projectType,
                     @description, @accountId, @fundPeriod, @assignedTeam, @progress, @tenderId, @created_by, @created_date, @tenderNameId);
                    SELECT LAST_INSERT_ID();";

                        await using var cmd = new MySqlCommand(insertSql, conn, (MySqlTransaction)transaction);
                        cmd.Parameters.AddWithValue("@date", model.Date.Date);
                        cmd.Parameters.AddWithValue("@projectId", model.ProjectId);
                        cmd.Parameters.AddWithValue("@location", "0");
                        cmd.Parameters.AddWithValue("@site", model.SiteId);
                        cmd.Parameters.AddWithValue("@plotNumber", "");
                        cmd.Parameters.AddWithValue("@startDate", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@endDate", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@status", model.Status ?? "");
                        cmd.Parameters.AddWithValue("@projectType", model.ProjectType ?? "");
                        cmd.Parameters.AddWithValue("@estimatedBudget", model.EstimatedBudget);
                        cmd.Parameters.AddWithValue("@description", "");
                        cmd.Parameters.AddWithValue("@accountId", model.AccountId);
                        cmd.Parameters.AddWithValue("@fundPeriod", "");
                        cmd.Parameters.AddWithValue("@assignedTeam", "");
                        cmd.Parameters.AddWithValue("@progress", 0);
                        cmd.Parameters.AddWithValue("@tenderId", model.TenderId);
                        cmd.Parameters.AddWithValue("@tenderNameId", model.TenderNameId);
                        cmd.Parameters.AddWithValue("@created_by", userId);
                        cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);

                        var resultObj = await cmd.ExecuteScalarAsync();
                        recordId = resultObj != null ? Convert.ToInt32(resultObj) : 0;
                    }

                    // Add transactions (mirroring WinForms transactions method)
                    if (model.EstimatedBudget > 0)
                    {
                        await InsertTransaction(conn, (MySqlTransaction)transaction, model.Date, model.AccountId.ToString(), model.EstimatedBudget, 0, recordId);
                        await InsertTransaction(conn, (MySqlTransaction)transaction, model.Date, model.CashAccountId.ToString(), 0, model.EstimatedBudget, recordId);
                    }

                  
                    await transaction.CommitAsync();

                    return Ok(new { status = true, message = model.Id > 0 ? "Project Planning updated successfully" : "Project Planning created successfully", id = recordId });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { status = false, message = "Error: " + ex.Message });
                }
            }
            catch (Exception exOuter)
            {
                return StatusCode(500, new { status = false, message = "Error: " + exOuter.Message });
            }
        }

        private async Task InsertTransaction(MySqlConnection conn, MySqlTransaction trx, DateTime date, string accountId, decimal debit, decimal credit, int transactionId)
        {
            string insert = @"
        INSERT INTO tbl_transaction (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state)
        VALUES (@date, @accountId, @debit, @credit, @transactionId, 0, 'Project Planning', 'PROJECT PLANNING', 'Project Planning Invoice NO.', @createdBy, @createdDate, 0);";

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            await using var cmd = new MySqlCommand(insert, conn, trx);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transactionId", transactionId);
            cmd.Parameters.AddWithValue("@createdBy", userId);
            cmd.Parameters.AddWithValue("@createdDate", DateTime.Now.Date);
            await cmd.ExecuteNonQueryAsync();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectTenderDetails(int tenderId)
        {
            try
            {
                if (tenderId == 0)
                    return BadRequest(new { status = false, message = "Tender ID is required" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                ptd.id,
                ptd.sr,
                ib.name,
                ib.id AS item_id,
                ib.type,
                ib.unit_name,
                ptd.start_date,
                ptd.end_date,
                ptd.progress,
                ptd.assigned,
                ptd.tender_id
            FROM tbl_project_tender_details ptd
            INNER JOIN tbl_items_boq ib 
                ON ptd.tender_id = @tenderId 
                AND ptd.item_id = ib.id";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tenderId", tenderId);

                var items = new List<object>();
                int count = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    // Format dates
                    string startDate = reader["start_date"] != DBNull.Value
                        ? Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd")
                        : DateTime.Now.ToString("yyyy-MM-dd");

                    string endDate = reader["end_date"] != DBNull.Value
                        ? Convert.ToDateTime(reader["end_date"]).ToString("yyyy-MM-dd")
                        : DateTime.Now.ToString("yyyy-MM-dd");

                    // Assigned employee
                    string empId = reader["assigned"] != DBNull.Value ? reader["assigned"].ToString() : "0";

                    // Progress
                    string progress = reader["progress"] != DBNull.Value
                        ? decimal.Parse(reader["progress"].ToString()).ToString("#.##")
                        : "0";

                    items.Add(new
                    {
                        SNo = count++,
                        Id = reader["id"].ToString(),
                        SR = reader["sr"].ToString(),
                        Name = reader["name"].ToString(),
                        StartDate = startDate,
                        EndDate = endDate,
                        Progress = progress,
                        Assigned = empId,
                        ItemId = reader["item_id"].ToString(),
                        Type = reader["type"].ToString(),
                        UnitName = reader["unit_name"].ToString(),
                        Tender_Id = reader.GetInt32("tender_id")
                    });
                }

                return Ok(new { status = true, message = "Success", data = items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #region Tab Get API

        [HttpGet]
        public async Task<IActionResult> GetRequestedMaterial(int planningId)
        {
            try
            {
                if (planningId == 0)
                    return BadRequest(new { status = false, message = "Planning ID is required" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                rm.id,
                rm.RequestedQty AS qty,
                rm.RequestedDate AS date,
                rm.unit,
                boq.sr,
                boq.name,
                CASE 
                    WHEN rm.ReceivedQty > 0 THEN 'Received' 
                    WHEN rm.IssuedQty > 0 THEN 'Issued' 
                    ELSE 'Requested' 
                END AS status
            FROM tbl_project_material_requests rm
            INNER JOIN tbl_items_boq boq 
                ON rm.tender_id = boq.ref_id AND rm.itemId = boq.id
            WHERE rm.planning_id = @planningId";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@planningId", planningId);

                var materialRequests = new List<object>();
                int count = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string requestedDate = reader["date"] != DBNull.Value
                        ? Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd")
                        : "";

                    materialRequests.Add(new
                    {
                        SNo = count++,
                        Id = reader["id"].ToString(),
                        PlanningId = planningId,
                        Date = requestedDate,
                        Name = $"{reader["sr"]} - {reader["name"]}",
                        Unit = reader["unit"].ToString(),
                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]).ToString("N2") : "0.00",
                        Status = reader["status"].ToString()
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = materialRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetIssuedMaterial(int planningId)
        {
            try
            {
                if (planningId == 0)
                    return BadRequest(new { status = false, message = "Planning ID is required" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
        SELECT 
            rm.id,
            rm.IssuedQty AS qty,
            rm.IssuedDate AS date,
            rm.unit,
            boq.sr,
            boq.name,
            CASE 
                WHEN rm.ReceivedQty > 0 THEN 'Received' 
                WHEN rm.IssuedQty > 0 THEN 'Issued' 
                ELSE 'Requested' 
            END AS status
        FROM tbl_project_material_requests rm
        INNER JOIN tbl_items_boq boq 
            ON rm.tender_id = boq.ref_id AND rm.itemId = boq.id
        WHERE rm.IssuedQty > 0 AND rm.planning_id = @planningId";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@planningId", planningId);

                var issuedItems = new List<object>();
                int count = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string issuedDate = reader["date"] != DBNull.Value
                        ? Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd")
                        : "";

                    issuedItems.Add(new
                    {
                        SNo = count++,
                        Id = reader["id"].ToString(),
                        PlanningId = planningId,
                        Date = issuedDate,
                        Name = $"{reader["sr"]} - {reader["name"]}",
                        Unit = reader["unit"].ToString(),
                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]).ToString("N2") : "0.00",
                        Status = reader["status"].ToString()
                    });
                }

                return Ok(new { status = true, message = "Success", data = issuedItems });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReceivedMaterial(int planningId)
        {
            try
            {
                if (planningId == 0)
                    return BadRequest(new { status = false, message = "Planning ID is required" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
        SELECT 
            rm.id,
            rm.ReceivedQty AS qty,
            rm.IssuedDate AS date,
            rm.unit,
            boq.sr,
            boq.name,
            CASE 
                WHEN rm.ReceivedQty > 0 THEN 'Received' 
                WHEN rm.IssuedQty > 0 THEN 'Issued' 
                ELSE 'Requested' 
            END AS status
        FROM tbl_project_material_requests rm
        INNER JOIN tbl_items_boq boq 
            ON rm.tender_id = boq.ref_id AND rm.itemId = boq.id
        WHERE rm.ReceivedQty > 0 AND rm.planning_id = @planningId";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@planningId", planningId);

                var receivedItems = new List<object>();
                int count = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string receivedDate = reader["date"] != DBNull.Value
                        ? Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd")
                        : "";

                    receivedItems.Add(new
                    {
                        SNo = count++,
                        Id = reader["id"].ToString(),
                        PlanningId = planningId,
                        Date = receivedDate,
                        Name = $"{reader["sr"]} - {reader["name"]}",
                        Unit = reader["unit"].ToString(),
                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]).ToString("N2") : "0.00",
                        Status = reader["status"].ToString()
                    });
                }

                return Ok(new { status = true, message = "Success", data = receivedItems });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetResourceData(int planningId)
        {
            try
            {
                if (planningId == 0)
                    return BadRequest(new { status = false, message = "Planning ID is required" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
   SELECT 
    pr.id,
    pr.code,
    pr.name,
    r.name AS roleName,
    pr.type,
    pr.price_unit,
    pr.unit_time,
    pr.max_unit_time,
    p.id AS PlanningId
FROM tbl_project_resource pr
INNER JOIN tbl_project_role r ON r.id = pr.role
INNER JOIN tbl_project_planning p 
    ON p.id = @planningId
    AND FIND_IN_SET(pr.id, p.assigned_team) > 0;";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@planningId", planningId);

                var resources = new List<object>();
                int count = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    resources.Add(new
                    {
                        SNo = count++,
                        Id = reader["id"].ToString(),
                        Code = reader["code"].ToString(),
                        PlanningId = reader.GetInt32("PlanningId"),
                        ResourceName = reader["name"].ToString(),
                        ResourceType = reader["type"].ToString(),
                        PrimaryRole = reader["roleName"].ToString(),
                        DefaultUnitsTime = reader["price_unit"] != DBNull.Value ? Convert.ToDecimal(reader["price_unit"]).ToString("N2") : "0.00"
                    });
                }

                return Ok(new { status = true, message = "Success", data = resources });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateMaterialRequest([FromBody] MaterialRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (model.TenderId <= 0)
                return BadRequest(new { status = false, message = "Please select a Tender" });

            if (model.PlanningId <= 0)
                return BadRequest(new { status = false, message = "Please select a Planning ID" });

            if (model.Items == null || model.Items.Count == 0)
                return BadRequest(new { status = false, message = "Please add at least one item" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 If Id == 0 => INSERT all
                if (model.Id == 0)
                {
                    foreach (var item in model.Items)
                    {
                        if (item.ItemId == null || item.ItemId <= 0)
                            continue;

                        string insertQuery = @"
                    INSERT INTO tbl_project_material_requests
                        (tender_id, planning_id, RequestedDate, itemId, unit, RequestedQty, IssuedQty, ReceivedQty)
                    VALUES
                        (@tenderId, @planningId, @requestedDate, @itemId, @unit, @qty, 0, 0);";

                        await using var insertCmd = new MySqlCommand(insertQuery, conn);
                        insertCmd.Parameters.AddWithValue("@tenderId", model.TenderId);
                        insertCmd.Parameters.AddWithValue("@planningId", model.PlanningId);
                        insertCmd.Parameters.AddWithValue("@requestedDate", model.RequestedDate);
                        insertCmd.Parameters.AddWithValue("@itemId", item.ItemId);
                        insertCmd.Parameters.AddWithValue("@unit", item.Unit ?? "");
                        insertCmd.Parameters.AddWithValue("@qty", item.RequestedQty);

                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new { status = true, message = "Material requests inserted successfully" });
                }
                else
                {
                    // 🔹 If Id > 0 => UPDATE all
                    foreach (var item in model.Items)
                    {
                        if (item.ItemId == null || item.ItemId <= 0)
                            continue;

                        // Get the record ID if passed in (optional per item)
                        int rowId = item.Id ?? 0;

                        if (rowId > 0)
                        {
                            // Update by row ID
                            string updateQuery = @"
                        UPDATE tbl_project_material_requests 
                        SET RequestedDate = @requestedDate,
                            itemId = @itemId,
                            unit = @unit,
                            RequestedQty = @qty
                        WHERE id = @id;";

                            await using var updateCmd = new MySqlCommand(updateQuery, conn);
                            updateCmd.Parameters.AddWithValue("@requestedDate", model.RequestedDate);
                            updateCmd.Parameters.AddWithValue("@itemId", item.ItemId);
                            updateCmd.Parameters.AddWithValue("@unit", item.Unit ?? "");
                            updateCmd.Parameters.AddWithValue("@qty", item.RequestedQty);
                            updateCmd.Parameters.AddWithValue("@id", rowId);

                            await updateCmd.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            // If no ID for this row, insert as new
                            string insertQuery = @"
                        INSERT INTO tbl_project_material_requests
                            (tender_id, planning_id, RequestedDate, itemId, unit, RequestedQty, IssuedQty, ReceivedQty)
                        VALUES
                            (@tenderId, @planningId, @requestedDate, @itemId, @unit, @qty, 0, 0);";

                            await using var insertCmd = new MySqlCommand(insertQuery, conn);
                            insertCmd.Parameters.AddWithValue("@tenderId", model.TenderId);
                            insertCmd.Parameters.AddWithValue("@planningId", model.PlanningId);
                            insertCmd.Parameters.AddWithValue("@requestedDate", model.RequestedDate);
                            insertCmd.Parameters.AddWithValue("@itemId", item.ItemId);
                            insertCmd.Parameters.AddWithValue("@unit", item.Unit ?? "");
                            insertCmd.Parameters.AddWithValue("@qty", item.RequestedQty);

                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }

                    return Ok(new { status = true, message = "Material requests updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = "An unexpected error occurred: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetItemsByTenderId(int tenderId)
        {
            if (tenderId <= 0)
            {
                return BadRequest(new { status = false, message = "Invalid Tender ID" });
            }

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Query to fetch items associated with the tenderId
                string query = @"
            SELECT 
                CONCAT(tbl_items_boq.sr, ' - ', tbl_items_boq.name) AS name,
                tbl_items_boq.id,
                tbl_items_boq.qty,
                tbl_items_boq.unit_name 
            FROM tbl_project_tender_details 
            INNER JOIN tbl_items_boq 
                ON tbl_project_tender_details.tender_id = ref_id 
                AND tbl_project_tender_details.item_id = tbl_items_boq.id
            WHERE tbl_project_tender_details.tender_id = @tenderId";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tenderId", tenderId);

                var dt = new DataTable();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    dt.Load(reader);
                }

                var items = dt.AsEnumerable()
                    .Select(row => new
                    {
                        Name = row["name"].ToString(),
                        Id = Convert.ToInt32(row["id"]),
                        Qty = Convert.ToDecimal(row["qty"]).ToString("F2"),
                        Unit = row["unit_name"].ToString()
                    })
                    .ToList();

                return Ok(new { status = true, data = items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = "An unexpected error occurred: " + ex.Message });
            }
        }

        #region Add Dropdown API

        [HttpGet]
        public async Task<IActionResult> GetRequestedData(int planningId)
        {
            if (planningId <= 0)
                return BadRequest(new { status = false, message = "Planning ID is required" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                rm.id,
                rm.RequestedQty AS qty,
                rm.RequestedDate AS date,
                rm.unit,
                rm.itemId As ItemId,
                boq.sr,
                boq.name,
                'Requested' AS status
            FROM tbl_project_material_requests rm
            INNER JOIN tbl_items_boq boq 
                ON rm.tender_id = boq.ref_id AND rm.itemId = boq.id
            WHERE rm.planning_id = @planningId
              AND rm.RequestedDate IS NOT NULL
              AND rm.IssuedDate IS NULL
              AND rm.ReceivedDate IS NULL;";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@planningId", planningId);

                var materialRequests = new List<object>();
                int count = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    string requestedDate = reader["date"] != DBNull.Value
                        ? Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd")
                        : "";

                    materialRequests.Add(new
                    {
                        SNo = count++,
                        Id = reader["id"].ToString(),
                        PlanningId = planningId,
                        Date = requestedDate,
                        Name = $"{reader["sr"]} - {reader["name"]}",
                        Unit = reader["unit"].ToString(),
                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]).ToString("N2") : "0.00",
                        Status = "Requested",
                        ItemId = reader.GetInt32("ItemId")
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = materialRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateIssueMaterial([FromBody] IssueMaterialModel model)
        {
            if (model == null || model.PlanningId <= 0 || model.TenderId <= 0 || model.Items == null || model.Items.Count == 0)
                return BadRequest(new { status = false, message = "Invalid data provided" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            UPDATE tbl_project_material_requests 
            SET IssuedDate = @IssuedDate, IssuedQty = @IssuedQty
            WHERE planning_id = @PlanningId 
              AND tender_id = @TenderId 
              AND itemId = @ItemId;";

                foreach (var item in model.Items)
                {
                    await using var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                    cmd.Parameters.AddWithValue("@PlanningId", model.PlanningId);
                    cmd.Parameters.AddWithValue("@TenderId", model.TenderId);
                    cmd.Parameters.AddWithValue("@IssuedDate", model.IssueDate);
                    cmd.Parameters.AddWithValue("@IssuedQty", item.IssuedQty);
                    await cmd.ExecuteNonQueryAsync();

                }

                return Ok(new { status = true, message = "Issue Material updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPendingReceivedMaterials(int planningId)
        {
            if (planningId <= 0)
                return BadRequest(new { status = false, message = "Planning ID is required" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                rm.id,
                rm.RequestedQty AS qty,
                rm.RequestedDate,
                rm.IssuedDate,
                rm.ReceivedDate,
                rm.unit,
                rm.itemId AS ItemId,
                boq.sr,
                boq.name,
                'Issued' AS status
            FROM tbl_project_material_requests rm
            INNER JOIN tbl_items_boq boq 
                ON rm.tender_id = boq.ref_id AND rm.itemId = boq.id
            WHERE rm.planning_id = @planningId
              AND rm.ReceivedDate IS NULL;";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@planningId", planningId);

                var materialRequests = new List<object>();
                int count = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    materialRequests.Add(new
                    {
                        SNo = count++,
                        Id = reader["id"].ToString(),
                        PlanningId = planningId,
                        RequestedDate = reader["RequestedDate"] != DBNull.Value
                            ? Convert.ToDateTime(reader["RequestedDate"]).ToString("yyyy-MM-dd")
                            : null,
                        IssuedDate = reader["IssuedDate"] != DBNull.Value
                            ? Convert.ToDateTime(reader["IssuedDate"]).ToString("yyyy-MM-dd")
                            : null,
                        ReceivedDate = reader["ReceivedDate"] != DBNull.Value
                            ? Convert.ToDateTime(reader["ReceivedDate"]).ToString("yyyy-MM-dd")
                            : null,
                        Name = $"{reader["sr"]} - {reader["name"]}",
                        Unit = reader["unit"].ToString(),
                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]).ToString("N2") : "0.00",
                        Status = reader["status"].ToString(),
                        ItemId = reader.GetInt32("ItemId")
                    });
                }

                return Ok(new
                {
                    status = true,
                    message = "Success",
                    data = materialRequests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateReceiveMaterial([FromBody] ReceiveMaterialModel model)
        {
            if (model == null || model.PlanningId <= 0 || model.TenderId <= 0 || model.Items == null || model.Items.Count == 0)
                return BadRequest(new { status = false, message = "Invalid data provided" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Ensure we always use today if no date provided
                var receiveDate = model.ReceiveDate == default ? DateTime.Today : model.ReceiveDate;

                string query = @"
            UPDATE tbl_project_material_requests 
            SET ReceivedDate = @ReceivedDate, ReceivedQty = @ReceivedQty
            WHERE planning_id = @PlanningId 
              AND tender_id = @TenderId 
              AND itemId = @ItemId;";

                foreach (var item in model.Items)
                {
                    await using var cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@ItemId", item.ItemId);
                    cmd.Parameters.AddWithValue("@PlanningId", model.PlanningId);
                    cmd.Parameters.AddWithValue("@TenderId", model.TenderId);
                    cmd.Parameters.AddWithValue("@ReceivedDate", receiveDate);
                    cmd.Parameters.AddWithValue("@ReceivedQty", item.ReceivedQty);

                    int affected = await cmd.ExecuteNonQueryAsync();

                    if (affected == 0)
                    {
                        Console.WriteLine($"No rows updated for ItemId={item.ItemId}, PlanningId={model.PlanningId}, TenderId={model.TenderId}");
                    }
                }

                return Ok(new { status = true, message = "Received material updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        [HttpGet]
        public async Task<IActionResult> GetProjectResources(int? planningId = null)
        {
            try
            {
                // Validate session
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                // Build connection dynamically
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Base query to load all resources
                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY pr.id) AS SN,
                pr.id,
                pr.code,
                pr.date,
                pr.name,
                r.name AS roleName,
                pr.phone,
                pr.type,
                pr.price_unit,
                pr.unit_time,
                pr.max_unit_time
            FROM tbl_project_resource pr
            INNER JOIN tbl_project_role r ON r.id = pr.role;
        ";

                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var resources = new List<dynamic>();
                while (await reader.ReadAsync())
                {
                    resources.Add(new
                    {
                        sn = reader.GetInt32("SN"),
                        id = reader.GetInt32("id"),
                        code = reader["code"].ToString(),
                        name = reader["name"].ToString(),
                        type = reader["type"].ToString(),
                        roleName = reader["roleName"].ToString(),
                        unitTime = reader["unit_time"].ToString(),
                        priceUnit = reader["price_unit"].ToString(),
                        maxUnitTime = reader["max_unit_time"].ToString(),
                        phone = reader["phone"].ToString(),
                        date = Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd"),
                        selected = false // will be updated later if planningId is provided
                    });
                }

                await reader.CloseAsync();

                // If planningId is provided, mark assigned resources as selected
                if (planningId.HasValue)
                {
                    string assignedQuery = @"
                SELECT id 
                FROM tbl_project_resource 
                WHERE EXISTS (
                    SELECT 1 FROM tbl_project_planning p 
                    WHERE p.id = @planningId 
                    AND FIND_IN_SET(tbl_project_resource.id, p.assigned_team) > 0
                );
            ";

                    await using var assignedCmd = new MySqlCommand(assignedQuery, conn);
                    assignedCmd.Parameters.AddWithValue("@planningId", planningId.Value);

                    var assignedIds = new List<int>();
                    await using var assignedReader = await assignedCmd.ExecuteReaderAsync();
                    while (await assignedReader.ReadAsync())
                        assignedIds.Add(assignedReader.GetInt32("id"));

                    foreach (var res in resources)
                    {
                        if (assignedIds.Contains((int)res.id))
                            res.selected = true;
                    }
                }

                return Ok(new { status = true, data = resources });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateRole([FromBody] ProjectRoleRequest model)
        {

            try
            {
                if (model == null)
                    return Json(new { status = false, message = "Invalid request" });

                if (string.IsNullOrWhiteSpace(model.Name))
                    return Json(new { status = false, message = "Please enter Role Name" });

                try
                {
                    int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    if (userId <= 0)
                        return Unauthorized(new { status = false, message = "User not logged in" });

                    var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                    {
                        Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                    };

                    await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                    await conn.OpenAsync();

                    // 1️⃣ Check if role name already exists
                    string checkQuery = "SELECT id FROM tbl_project_role WHERE name=@name";
                    await using (var checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                        var existingId = await checkCmd.ExecuteScalarAsync();

                        if (existingId != null && (model.Id == 0 || model.Id != Convert.ToInt32(existingId)))
                        {
                            return Json(new { status = false, message = "Role name already in use" });
                        }
                    }

                    // 2️⃣ Generate role code automatically (R1, R2, R3...)
                    string roleCode = model.Id == 0 ? await GenerateNextRoleCode(conn) : model.Code;

                    if (model.Id == 0)
                    {
                        // 3️⃣ Insert new role
                        string insertQuery = @"
                INSERT INTO tbl_project_role (code, name)
                VALUES (@code, @name);
                SELECT LAST_INSERT_ID();";

                        await using var cmd = new MySqlCommand(insertQuery, conn);
                        cmd.Parameters.AddWithValue("@code", roleCode);
                        cmd.Parameters.AddWithValue("@name", model.Name.Trim());

                        int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                      

                        return Ok(new
                        {
                            status = true,
                            message = "Role inserted successfully",
                            id = newId,
                            code = roleCode
                        });
                    }
                    else
                    {
                        // 4️⃣ Update existing role
                        string updateQuery = "UPDATE tbl_project_role SET name=@name, code=@code WHERE id=@id";
                        await using var cmd = new MySqlCommand(updateQuery, conn);
                        cmd.Parameters.AddWithValue("@id", model.Id);
                        cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                        cmd.Parameters.AddWithValue("@code", roleCode);

                        int affected = await cmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                            return NotFound(new { status = false, message = "Role not found" });

                        // Optional: Audit log
                        string audit = "INSERT INTO tbl_audit_log (user_id, action, module, ref_id, description) VALUES (@user_id, @action, @module, @ref_id, @desc)";
                        await using (var auditCmd = new MySqlCommand(audit, conn))
                        {
                            auditCmd.Parameters.AddWithValue("@user_id", userId);
                            auditCmd.Parameters.AddWithValue("@action", "Update Project Role");
                            auditCmd.Parameters.AddWithValue("@module", "Project Role");
                            auditCmd.Parameters.AddWithValue("@ref_id", model.Id);
                            auditCmd.Parameters.AddWithValue("@desc", "Updated Project Role: " + model.Name);
                            await auditCmd.ExecuteNonQueryAsync();
                        }

                        return Ok(new
                        {
                            status = true,
                            message = "Role updated successfully",
                            id = model.Id,
                            code = roleCode
                        });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { status = false, message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "An unexpected error occurred: " + ex.Message });
            }
        }

        private async Task<string> GenerateNextRoleCode(MySqlConnection conn)
        {
            try
            {
                string query = "SELECT code FROM tbl_project_role ORDER BY id DESC LIMIT 1";
                await using var cmd = new MySqlCommand(query, conn);
                var lastCodeObj = await cmd.ExecuteScalarAsync();

                if (lastCodeObj == null || string.IsNullOrWhiteSpace(lastCodeObj.ToString()))
                    return "R1";

                string lastCode = lastCodeObj.ToString();
                if (int.TryParse(lastCode.Replace("R", ""), out int num))
                    return $"R{num + 1}";

                return "R1";
            }
            catch(Exception ex)
            {
                throw ex;
            }
          
        }

        [HttpGet]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = "SELECT id, code, name FROM tbl_project_role ORDER BY id ASC";
                var roles = new List<object>();

                await using (var cmd = new MySqlCommand(query, conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        roles.Add(new
                        {
                            id = reader.GetInt32("id"),
                            code = reader.GetString("code"),
                            name = reader.GetString("name")
                        });
                    }
                }

                return Ok(new { status = true, data = roles });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateProjectResource([FromBody] ProjectResourceRequest model)
        {

            try
            {
                if (model == null)
                    return Json(new { status = false, message = "Invalid request" });

                if (string.IsNullOrWhiteSpace(model.Role))
                    return Json(new { status = false, message = "Role can't be empty" });

                if (string.IsNullOrWhiteSpace(model.Name))
                    return Json(new { status = false, message = "Name can't be empty" });

                if (model.Type == "Labour" && (model.EmployeeId == null || model.EmployeeId <= 0))
                    return Json(new { status = false, message = "Employee name can't be empty" });

                try
                {
                    int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    if (userId <= 0)
                        return Json(new { status = false, message = "User not logged in" });

                    var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                    {
                        Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                    };

                    await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                    await conn.OpenAsync();

                    // 🔹 Auto-generate code if empty
                    string resourceCode = string.IsNullOrEmpty(model.Code)
                        ? await GenerateNextResourceCode(conn)
                        : model.Code;

                    if (model.Id == 0)
                    {
                        // 🟩 INSERT new resource
                        string insertQuery = @"
                INSERT INTO tbl_project_resource
                (code, date, name, role, phone, type, price_unit, unit_time, max_unit_time, employee_id)
                VALUES (@code, @date, @name, @role, @phone, @type, @priceUnit, @unitTime, @maxUnitTime, @empId);
                SELECT LAST_INSERT_ID();";

                        await using var cmd = new MySqlCommand(insertQuery, conn);
                        cmd.Parameters.AddWithValue("@code", resourceCode);
                        cmd.Parameters.AddWithValue("@date", model.Date);
                        cmd.Parameters.AddWithValue("@name", model.Name);
                        cmd.Parameters.AddWithValue("@role", model.Role);
                        cmd.Parameters.AddWithValue("@phone", model.Phone ?? "");
                        cmd.Parameters.AddWithValue("@type", model.Type ?? "Non");
                        cmd.Parameters.AddWithValue("@priceUnit", model.PriceUnit ?? 0);
                        cmd.Parameters.AddWithValue("@unitTime", model.UnitTime ?? 8);
                        cmd.Parameters.AddWithValue("@maxUnitTime", model.MaxUnitTime ?? 8);
                        cmd.Parameters.AddWithValue("@empId", model.EmployeeId ?? 0);

                        int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                        return Ok(new
                        {
                            status = true,
                            message = "Resource inserted successfully",
                            id = newId,
                            code = resourceCode
                        });
                    }
                    else
                    {
                        // 🟦 UPDATE existing resource
                        string updateQuery = @"
                UPDATE tbl_project_resource
                SET date = @date, name = @name, role = @role, phone = @phone, type = @type,
                    code = @code, price_unit = @priceUnit, unit_time = @unitTime,
                    max_unit_time = @maxUnitTime, employee_id = @empId
                WHERE id = @id";

                        await using var cmd = new MySqlCommand(updateQuery, conn);
                        cmd.Parameters.AddWithValue("@id", model.Id);
                        cmd.Parameters.AddWithValue("@code", resourceCode);
                        cmd.Parameters.AddWithValue("@date", model.Date);
                        cmd.Parameters.AddWithValue("@name", model.Name);
                        cmd.Parameters.AddWithValue("@role", model.Role);
                        cmd.Parameters.AddWithValue("@phone", model.Phone ?? "");
                        cmd.Parameters.AddWithValue("@type", model.Type ?? "Non");
                        cmd.Parameters.AddWithValue("@priceUnit", model.PriceUnit ?? 0);
                        cmd.Parameters.AddWithValue("@unitTime", model.UnitTime ?? 8);
                        cmd.Parameters.AddWithValue("@maxUnitTime", model.MaxUnitTime ?? 8);
                        cmd.Parameters.AddWithValue("@empId", model.EmployeeId ?? 0);

                        int affected = await cmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                            return NotFound(new { status = false, message = "Resource not found" });

                        return Ok(new
                        {
                            status = true,
                            message = "Resource updated successfully",
                            id = model.Id,
                            code = resourceCode
                        });
                    }
                }
                catch (Exception ex)
                {
                    return Json(500, new { status = false, message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "An unexpected error occurred: " + ex.Message });
            }

        }

        private async Task<string> GenerateNextResourceCode(MySqlConnection conn)
        {
            try
            {
                string query = "SELECT code FROM tbl_project_resource ORDER BY id DESC LIMIT 1";
                await using var cmd = new MySqlCommand(query, conn);
                var lastCodeObj = await cmd.ExecuteScalarAsync();

                if (lastCodeObj == null)
                    return "RS1";

                string lastCode = lastCodeObj.ToString();
                if (int.TryParse(lastCode.Replace("RS", ""), out int num))
                    return $"RS{num + 1}";
                return "RS1";
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        [HttpPost]
        public async Task<IActionResult> AssignResources([FromBody] AssignResourcesRequest model)
        {
            try
            {
                if (model == null || model.PlanningId <= 0)
                    return Json(new { status = false, message = "Invalid request" });

                try
                {
                    int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                    if (userId <= 0)
                        return Json(new { status = false, message = "User not logged in" });

                    // Convert list of ResourceIds to comma-separated string
                    string assignedTeam = model.ResourceIds != null && model.ResourceIds.Count > 0
                        ? string.Join(",", model.ResourceIds)
                        : "";

                    // Build connection string
                    var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                    {
                        Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                    };

                    await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                    await conn.OpenAsync();

                    // Update assigned_team
                    string updateQuery = "UPDATE tbl_project_planning SET assigned_team=@assignedTeam WHERE id=@id";
                    await using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@assignedTeam", assignedTeam);
                    cmd.Parameters.AddWithValue("@id", model.PlanningId);

                    int affected = await cmd.ExecuteNonQueryAsync();

                    if (affected > 0)
                    {
                        return Ok(new { status = true, message = "Resources assigned successfully", assignedTeam });
                    }
                    else
                    {
                        return Json(new { status = false, message = "Planning record not found" });
                    }
                }
                catch (Exception ex)
                {
                    return Json(500, new { status = false, message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "An unexpected error occurred: " + ex.Message });
            }

        }
        [HttpGet]
        public async Task<IActionResult> GetAssignedResources(int planningId)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                pr.id, 
                pr.code, 
                pr.date, 
                pr.name, 
                r.name AS roleName, 
                pr.phone, 
                pr.type, 
                pr.price_unit, 
                pr.unit_time, 
                pr.max_unit_time
            FROM tbl_project_resource pr
            JOIN tbl_project_role r ON r.id = pr.role
            WHERE EXISTS (
                SELECT 1 
                FROM tbl_project_planning p 
                WHERE p.id = @planningId 
                  AND FIND_IN_SET(pr.id, p.assigned_team) > 0
            );";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@planningId", planningId);

                await using var reader = await cmd.ExecuteReaderAsync();

                var resourceList = new List<object>();
                int sn = 1;

                while (await reader.ReadAsync())
                {
                    resourceList.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        Code = reader["code"]?.ToString(),
                        Name = reader["name"]?.ToString(),
                    });
                }

                return Ok(new { status = true, data = resourceList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateProjectActivity([FromBody] ProjectActivityRequest model)
        {
            try
            {
                if (model == null)
                    return Json(new { status = false, message = "Invalid request data." });

                if (model.PlanningId <= 0)
                    return Json(new { status = false, message = "Planning ID is required." });

                if (model.TenderId <= 0)
                    return Json(new { status = false, message = "Tender ID is required." });

                if (model.ItemId <= 0)
                    return Json(new { status = false, message = "Item ID is required." });

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Json(new { status = false, message = "User not logged in." });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                int progress = model.Progress ?? 0;
                DateTime startDate = model.StartDate ?? DateTime.Today;
                DateTime endDate = model.EndDate ?? DateTime.Today;
                string status = progress == 100 ? "Completed" :
                                (DateTime.Today >= startDate ? "In Progress" : "Not Started");

                int activityId;

                // -------------------------
                // Insert or Update Activity
                // -------------------------
                if (model.Id == 0)
                {
                    string insertQuery = @"
                INSERT INTO tbl_project_activity 
                    (planning_id, code, name, start_date, end_date, progress, status)
                VALUES (@planningId, @code, @name, @startDate, @endDate, @progress, @status);
                SELECT LAST_INSERT_ID();";

                    await using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@planningId", model.PlanningId);
                    cmd.Parameters.AddWithValue("@code", model.ItemId);
                    cmd.Parameters.AddWithValue("@name", model.ItemId);
                    cmd.Parameters.AddWithValue("@startDate", startDate);
                    cmd.Parameters.AddWithValue("@endDate", endDate);
                    cmd.Parameters.AddWithValue("@progress", progress);
                    cmd.Parameters.AddWithValue("@status", status);

                    activityId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                else
                {
                    string updateQuery = @"
                UPDATE tbl_project_activity 
                SET planning_id = @planningId, code = @code, name = @name,
                    start_date = @startDate, end_date = @endDate,
                    progress = @progress, status = @status
                WHERE id = @id;";

                    await using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@planningId", model.PlanningId);
                    cmd.Parameters.AddWithValue("@code", model.ItemId);
                    cmd.Parameters.AddWithValue("@name", model.ItemId);
                    cmd.Parameters.AddWithValue("@startDate", startDate);
                    cmd.Parameters.AddWithValue("@endDate", endDate);
                    cmd.Parameters.AddWithValue("@progress", progress);
                    cmd.Parameters.AddWithValue("@status", status);

                    await cmd.ExecuteNonQueryAsync();
                    activityId = model.Id;

                    // Remove previous resource assignments
                    string deleteAssignments = "DELETE FROM tbl_project_activity_assignment WHERE activity_id = @id";
                    await using var delCmd = new MySqlCommand(deleteAssignments, conn);
                    delCmd.Parameters.AddWithValue("@id", activityId);
                    await delCmd.ExecuteNonQueryAsync();
                }

                // -------------------------
                // Assign Resources
                // -------------------------
                if (model.AssignedResources != null && model.AssignedResources.Any())
                {
                    foreach (var resId in model.AssignedResources)
                    {
                        string insertAssignment = @"
                    INSERT INTO tbl_project_activity_assignment (activity_id, resource_id)
                    VALUES (@activityId, @resourceId);";

                        await using var assignCmd = new MySqlCommand(insertAssignment, conn);
                        assignCmd.Parameters.AddWithValue("@activityId", activityId);
                        assignCmd.Parameters.AddWithValue("@resourceId", resId);
                        await assignCmd.ExecuteNonQueryAsync();
                    }
                }

                // -------------------------
                // Update Tender Details
                // -------------------------
                string updateTender = @"
            UPDATE tbl_project_tender_details 
            SET start_date=@startDate, end_date=@endDate, progress=@progress
            WHERE item_id=@itemId AND tender_id=@tenderId;";

                await using (var tenderCmd = new MySqlCommand(updateTender, conn))
                {
                    tenderCmd.Parameters.AddWithValue("@itemId", model.ItemId);
                    tenderCmd.Parameters.AddWithValue("@tenderId", model.TenderId);
                    tenderCmd.Parameters.AddWithValue("@startDate", startDate);
                    tenderCmd.Parameters.AddWithValue("@endDate", endDate);
                    tenderCmd.Parameters.AddWithValue("@progress", progress);
                    await tenderCmd.ExecuteNonQueryAsync();

                    var affectedRows = await tenderCmd.ExecuteNonQueryAsync();
                    if (affectedRows == 0)
                        Console.WriteLine("No tender row updated! Check ItemId/TenderId combination.");

                }

                // -------------------------
                // Update Project Planning
                // -------------------------
                string updatePlanning = @"
            UPDATE tbl_project_planning
            SET modified_by = @modifiedBy,
                modified_date = @modifiedDate,
                progress = @progress
            WHERE id = @planningId;";

                await using (var planningCmd = new MySqlCommand(updatePlanning, conn))
                {
                    planningCmd.Parameters.AddWithValue("@modifiedBy", userId);
                    planningCmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now.Date);
                    planningCmd.Parameters.AddWithValue("@progress", 0); // default progress
                    planningCmd.Parameters.AddWithValue("@planningId", model.PlanningId);
                    await planningCmd.ExecuteNonQueryAsync();
                }

                return Ok(new
                {
                    status = true,
                    message = model.Id == 0 ? "Activity inserted successfully" : "Activity updated successfully",
                    id = activityId,
                    progress = progress,
                    statusText = status
                });
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Project Work Done

        public IActionResult ProjectWorkDone()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectWorkDone(
    int? projectId = null,
    int? tenderId = null)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY pwd.date) AS Sn,
                pwd.id AS VNo,
                pwd.id,
                pwd.date AS Date,
                CONCAT(p.code,' - ', p.name) AS ProjectName,
                CONCAT(t.code,' - ', t.name) AS TenderName,
                (SELECT SUM(qty_total) FROM tbl_project_work_done_details WHERE ref_id = pwd.id) AS TotalQtyP,
                (SELECT SUM(qty_used) FROM tbl_project_work_done_details WHERE ref_id = pwd.id) AS TotalQtyWD
            FROM tbl_project_work_done pwd
            INNER JOIN tbl_project_planning pp ON pwd.planning_id = pp.id
            INNER JOIN tbl_projects p ON pp.project_id = p.id
            INNER JOIN tbl_tender_names t ON pp.tender_name_id = t.id
            WHERE pwd.state = 0";

                var parameters = new List<MySqlParameter>();

                if (projectId.HasValue)
                {
                    query += " AND pp.project_id = @projectId";
                    parameters.Add(new MySqlParameter("@projectId", projectId.Value));
                }

                if (tenderId.HasValue)
                {
                    query += " AND pp.tender_name_id = @tenderId";
                    parameters.Add(new MySqlParameter("@tenderId", tenderId.Value));
                }

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                var workDoneList = new List<object>();
                int sn = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    workDoneList.Add(new
                    {
                        Sn = sn++,
                        Id = reader.GetInt32("Id"),
                        VNo = reader.GetInt32("VNo"),
                        Date = reader["Date"] != DBNull.Value ? Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd") : null,
                        ProjectName = reader["ProjectName"].ToString(),
                        TenderName = reader["TenderName"].ToString(),
                        TotalQtyP = reader["TotalQtyP"] != DBNull.Value ? Convert.ToDecimal(reader["TotalQtyP"]) : 0,
                        TotalQtyWD = reader["TotalQtyWD"] != DBNull.Value ? Convert.ToDecimal(reader["TotalQtyWD"]) : 0
                    });
                }

                return Ok(new { status = true, data = workDoneList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectWorkDoneItems(int id)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if there are any items
                string checkQuery = "SELECT COUNT(*) FROM tbl_project_work_done_details WHERE ref_id = @id";
                await using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);
                    int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                    if (count == 0)
                    {
                        return Ok(new { status = true, data = new List<object>(), message = "No items found" });
                    }
                }

                // Fetch item details
                string query = @"
            SELECT 
                CONCAT(ti.sr, ' - ', ti.name) AS ItemName,
                pwd.qty_used AS Qty,
                ti.price AS Rate,
                pwd.unit AS Unit,
                (pwd.qty_used * ti.price) AS Amount
            FROM tbl_project_work_done_details pwd
            INNER JOIN tbl_items_boq ti ON pwd.main_id = ti.id
            WHERE pwd.ref_id = @id";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                var items = new List<object>();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new
                    {
                        ItemName = reader["ItemName"].ToString(),
                        Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                        Rate = reader["Rate"] != DBNull.Value ? Convert.ToDecimal(reader["Rate"]) : 0,
                        Unit = reader["Unit"].ToString(),
                        Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0
                    });
                }

                return Ok(new { status = true, data = items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetProjectWorkDoneDetails(int projectId, int tenderId, int siteId)
        {
            try
            {
                // Build database connection
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Validate parameters
                if (projectId <= 0 || tenderId <= 0 || siteId <= 0)
                {
                    return Ok(new { status = false, data = new List<object>(), message = "Invalid input" });
                }

                // ✅ Fixed parameter name here
                string query = @"
            SELECT Id, Date 
            FROM tbl_project_planning 
            WHERE project_id = @project 
              AND tender_name_id = @tenderId 
              AND site = @siteId";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@project", projectId);
                cmd.Parameters.AddWithValue("@tenderId", tenderId); // ✅ match query
                cmd.Parameters.AddWithValue("@siteId", siteId);

                var planningList = new List<object>();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    planningList.Add(new
                    {
                        Id = reader["Id"].ToString(),
                        Date = Convert.ToDateTime(reader["Date"]).ToString("dd/MM/yyyy")
                    });
                }

                return Ok(new
                {
                    status = true,
                    data = planningList,
                    message = planningList.Count > 0 ? "Planning dates loaded successfully" : "No data found"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBoqItems(int planningId)
        {
            try
            {
                // Build connection dynamically
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
           SELECT tbl_items_boq.id,tbl_project_tender_details.sr,tbl_project_tender_details.qty,tbl_project_tender_details.unit_id,tbl_project_tender_details.item_id, tbl_items_boq.id as code,tbl_items_boq.name,tbl_items_boq.type,tbl_items_boq.unit_name as unit_name FROM tbl_project_tender_details 
                            INNER JOIN tbl_items_boq ON tbl_project_tender_details.tender_id = ref_id AND tbl_project_tender_details.item_id = tbl_items_boq.id
                            WHERE tbl_project_tender_details.tender_id = (SELECT tender_id FROM tbl_project_planning WHERE id=@planningId)";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@planningId", planningId);

                var boqItems = new List<object>();
                await using var reader = await cmd.ExecuteReaderAsync();

                int count = 1;
                while (await reader.ReadAsync())
                {
                    boqItems.Add(new
                    {
                        Id = reader["id"].ToString(),
                        SrNo = count++,
                        Sr = reader["sr"].ToString(),
                        Name = reader["name"].ToString(),
                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]) : 0,
                        Unit = reader["unit_name"].ToString()
                    });
                }

                return Ok(new
                {
                    status = true,
                    data = boqItems,
                    message = boqItems.Count > 0 ? "BOQ items loaded successfully" : "No items found"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAssemblyData(int itemId)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT ti.code,
                   ti.id,
                   ti.on_hand AS qty,
                   ti.cost_price AS rate,
                   ti.name,
                   ti.ref_id,
                   ti.unit_id,
                   (SELECT unit_name FROM tbl_items_boq WHERE id = ti.ref_id) AS unit_name
            FROM tbl_item_assembly_bos ta
            INNER JOIN tbl_items_boq_details ti 
                ON ta.item_id = ti.id AND ti.ref_id = ta.assembly_id
            WHERE ta.assembly_id = @itemId;";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@itemId", itemId);

                var assemblyDataList = new List<object>();

                // Read all main assembly data first
                var mainRows = new List<dynamic>();
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        mainRows.Add(new
                        {
                            Id = reader["id"].ToString(),
                            Code = reader["ref_id"].ToString(),
                            RefId = reader["ref_id"].ToString(),
                            Name = reader["name"].ToString(),
                            BoqQty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]) : 0,
                            Unit = reader["unit_name"].ToString()
                        });
                    }
                }

                // For each main row, fetch material data in separate connection
                foreach (var row in mainRows)
                {
                    await using var conn2 = new MySqlConnection(connStrBuilder.ConnectionString);
                    await conn2.OpenAsync();

                    string materialQuery = @"
                SELECT RequestedDate,
                       IssuedDate,
                       ReceivedDate,
                       RequestedQty,
                       IssuedQty,
                       ReceivedQty
                FROM tbl_project_material_requests
                WHERE itemId = @refId;";

                    await using var materialCmd = new MySqlCommand(materialQuery, conn2);
                    materialCmd.Parameters.AddWithValue("@refId", row.RefId);

                    var materialData = new
                    {
                        RequestedDate = (DateTime?)null,
                        IssuedDate = (DateTime?)null,
                        ReceivedDate = (DateTime?)null,
                        RequestedQty = 0m,
                        IssuedQty = 0m,
                        ReceivedQty = 0m
                    };

                    await using (var reader2 = await materialCmd.ExecuteReaderAsync())
                    {
                        if (await reader2.ReadAsync())
                        {
                            materialData = new
                            {
                                RequestedDate = reader2["RequestedDate"] != DBNull.Value ? Convert.ToDateTime(reader2["RequestedDate"]) : (DateTime?)null,
                                IssuedDate = reader2["IssuedDate"] != DBNull.Value ? Convert.ToDateTime(reader2["IssuedDate"]) : (DateTime?)null,
                                ReceivedDate = reader2["ReceivedDate"] != DBNull.Value ? Convert.ToDateTime(reader2["ReceivedDate"]) : (DateTime?)null,
                                RequestedQty = reader2["RequestedQty"] != DBNull.Value ? Convert.ToDecimal(reader2["RequestedQty"]) : 0,
                                IssuedQty = reader2["IssuedQty"] != DBNull.Value ? Convert.ToDecimal(reader2["IssuedQty"]) : 0,
                                ReceivedQty = reader2["ReceivedQty"] != DBNull.Value ? Convert.ToDecimal(reader2["ReceivedQty"]) : 0
                            };
                        }
                    }

                    assemblyDataList.Add(new
                    {
                        row.Id,
                        row.Code,
                        row.RefId,
                        row.Name,
                        row.BoqQty,
                        row.Unit,
                        RequestedQty = materialData.RequestedQty,
                        IssuedQty = materialData.IssuedQty,
                        ReceivedQty = materialData.ReceivedQty,
                        UsedQty = materialData.ReceivedQty
                    });
                }

                return Ok(new
                {
                    status = true,
                    data = assemblyDataList,
                    message = assemblyDataList.Count > 0 ? "Assembly data loaded successfully" : "No data found"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateProjectWorkDone([FromBody] ProjectWorkDoneRequest model)
        {
            try
            {
                if (model == null)
                    return Json(new { status = false, message = "Invalid request data." });

                if (model.PlanningId <= 0)
                    return Json(new { status = false, message = "Planning ID is required." });

                if (model.AccountId <= 0)
                    return Json(new { status = false, message = "Account ID is required." });

                if (model.WarehouseId <= 0)
                    return Json(new { status = false, message = "Warehouse ID is required." });

                if (model.Items == null || !model.Items.Any())
                    return Json(new { status = false, message = "At least one work done item is required." });

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Json(new { status = false, message = "User not logged in." });
                DateTime? workDate = model.Date;
                // Build dynamic connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                int workDoneId;

                // Check if main work done record exists
                string checkMainQuery = @"
    SELECT id 
    FROM tbl_project_work_done 
    WHERE planning_id = @planningId 
      AND warehouse_id = @warehouseId
    LIMIT 1;";

                await using var checkMainCmd = new MySqlCommand(checkMainQuery, conn);
                checkMainCmd.Parameters.AddWithValue("@planningId", model.PlanningId);
                checkMainCmd.Parameters.AddWithValue("@accountId", model.AccountId);
                checkMainCmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);

                var existingWorkDoneIdObj = await checkMainCmd.ExecuteScalarAsync();
                int? existingWorkDoneId = existingWorkDoneIdObj != null ? Convert.ToInt32(existingWorkDoneIdObj) : (int?)null;

                // Check if any item already exists in details
                if (existingWorkDoneId.HasValue)
                {
                    // Check if any item already exists in details (item_id only)
                    foreach (var item in model.Items)
                    {
                        string checkDetailQuery = @"
SELECT 1
FROM tbl_project_work_done_details
WHERE item_id = @itemId
LIMIT 1;";

                        await using var checkDetailCmd = new MySqlCommand(checkDetailQuery, conn);
                        checkDetailCmd.Parameters.AddWithValue("@itemId", item.ItemId);

                        var exists = await checkDetailCmd.ExecuteScalarAsync();
                        if (exists != null)
                        {
                            return Json(new
                            {
                                status = false,
                                message = $"Item {item.ItemId} already exists in work done details."
                            });
                        }
                    }

                }


                if (model.Id == 0)
                {
                    // --------------------------
                    // INSERT MAIN RECORD
                    // --------------------------
                    string insertQuery = @"
                INSERT INTO tbl_project_work_done 
                    (date, planning_id, account_id, warehouse_id, created_by, created_date, state)
                VALUES
                    (@date, @planningId, @accountId, @warehouseId, @createdBy, @createdDate, 0);
                SELECT LAST_INSERT_ID();";

                    await using var insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@date", model.Date ?? DateTime.Now.Date);
                    insertCmd.Parameters.AddWithValue("@planningId", model.PlanningId);
                    insertCmd.Parameters.AddWithValue("@accountId", model.AccountId);
                    insertCmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                    insertCmd.Parameters.AddWithValue("@createdBy", userId);
                    insertCmd.Parameters.AddWithValue("@createdDate", DateTime.Now);

                    workDoneId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                }
                else
                {
                    // --------------------------
                    // UPDATE MAIN RECORD
                    // --------------------------
                    string updateQuery = @"
                UPDATE tbl_project_work_done
                SET modified_by = @modifiedBy,
                    modified_date = @modifiedDate,
                    date = @date,
                    planning_id = @planningId,
                    warehouse_id = @warehouseId,
                    account_id = @accountId
                WHERE id = @id;";

                    await using var updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);
                    updateCmd.Parameters.AddWithValue("@date", model.Date ?? DateTime.Now.Date);
                    updateCmd.Parameters.AddWithValue("@planningId", model.PlanningId);
                    updateCmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                    updateCmd.Parameters.AddWithValue("@accountId", model.AccountId);
                    updateCmd.Parameters.AddWithValue("@modifiedBy", userId);
                    updateCmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now);

                    await updateCmd.ExecuteNonQueryAsync();

                    workDoneId = model.Id;

                    // --------------------------
                    // DELETE EXISTING DETAILS
                    // --------------------------
                    string deleteDetails = "DELETE FROM tbl_project_work_done_details WHERE ref_id = @id;";
                    await using var delCmd = new MySqlCommand(deleteDetails, conn);
                    delCmd.Parameters.AddWithValue("@id", workDoneId);
                    await delCmd.ExecuteNonQueryAsync();
                }

                // --------------------------
                // INSERT ITEM DETAILS
                // --------------------------
                foreach (var item in model.Items)
                {
                    string insertDetailQuery = @"
                INSERT INTO tbl_project_work_done_details
                    (ref_id, item_id, main_id, code, qty_total, unit, qty_used)
                VALUES
                    (@refId, @itemId, @mainItemId, @code, @qtyTotal, @unit, @qtyUsed);";

                    await using var detailCmd = new MySqlCommand(insertDetailQuery, conn);
                    detailCmd.Parameters.AddWithValue("@refId", workDoneId);
                    detailCmd.Parameters.AddWithValue("@itemId", item.ItemId);
                    detailCmd.Parameters.AddWithValue("@mainItemId", item.Code ?? "");
                    detailCmd.Parameters.AddWithValue("@code", item.Code ?? "");
                    detailCmd.Parameters.AddWithValue("@qtyTotal", item.QtyTotal);
                    detailCmd.Parameters.AddWithValue("@unit", item.Unit ?? "");
                    detailCmd.Parameters.AddWithValue("@qtyUsed", item.QtyUsed);
                    await detailCmd.ExecuteNonQueryAsync();
                }


                return Ok(new
                {
                    status = true,
                    message = model.Id == 0
                        ? "Project Work Done inserted successfully."
                        : "Project Work Done updated successfully.",
                    id = workDoneId
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }



        #endregion

        #region Project Summary

        public IActionResult ProjectSummary()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectSummary(
    int? projectId = null,
    string projectStatus = null,
    string projectType = null)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
        SELECT 
            ROW_NUMBER() OVER (ORDER BY tbl_project_planning.date) AS SN,
            tbl_project_planning.date AS DATE,
            tbl_project_planning.id AS ProjectNo,
            CONCAT(tbl_projects.code, ' - ', tbl_projects.name) AS ProjectName,
            tbl_project_planning.start_date AS StartDate,
            tbl_project_planning.end_date AS EndDate,
            tbl_project_planning.Status,
            tbl_project_planning.project_type AS ProjectType,
            tbl_project_planning.estimated_budget AS EstBudget,
            COALESCE(tbl_project_management.id, 0) AS ManagementId,
            COALESCE(tbl_project_management.budget, '') AS Budget,
            COALESCE(tbl_project_management.actual_cost, '') AS ActualCost,
            COALESCE(tbl_project_management.remaining_budget, '') AS RemainingBudget
        FROM tbl_project_planning
        INNER JOIN tbl_projects ON tbl_project_planning.project_id = tbl_projects.id
        LEFT JOIN tbl_project_management 
            ON tbl_project_management.project_planning_id = tbl_project_planning.id 
            AND tbl_project_management.project_id = tbl_project_planning.project_id
        WHERE tbl_project_planning.state = 0";

                var parameters = new List<MySqlParameter>();

                if (projectId.HasValue)
                {
                    query += " AND tbl_project_planning.project_id = @projectId";
                    parameters.Add(new MySqlParameter("@projectId", projectId.Value));
                }

                if (!string.IsNullOrEmpty(projectStatus))
                {
                    query += " AND tbl_project_planning.status = @status";
                    parameters.Add(new MySqlParameter("@status", projectStatus));
                }

                if (!string.IsNullOrEmpty(projectType))
                {
                    query += " AND tbl_project_planning.project_type = @type";
                    parameters.Add(new MySqlParameter("@type", projectType));
                }


                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();
                var list = new List<object>();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        SN = reader.GetInt32("SN"),
                        Date = reader["DATE"] != DBNull.Value ? Convert.ToDateTime(reader["DATE"]).ToString("yyyy-MM-dd") : null,
                        ProjectNo = reader.GetInt32("ProjectNo"),
                        ProjectName = reader["ProjectName"].ToString(),
                        StartDate = reader["StartDate"] != DBNull.Value ? Convert.ToDateTime(reader["StartDate"]).ToString("yyyy-MM-dd") : null,
                        EndDate = reader["EndDate"] != DBNull.Value ? Convert.ToDateTime(reader["EndDate"]).ToString("yyyy-MM-dd") : null,
                        Status = reader["Status"].ToString(),
                        ProjectType = reader["ProjectType"].ToString(),
                        EstBudget = reader["EstBudget"].ToString(),
                        ManagementId = reader.GetInt32("ManagementId"),
                        Budget = reader["Budget"].ToString(),
                        ActualCost = reader["ActualCost"].ToString(),
                        RemainingBudget = reader["RemainingBudget"].ToString()
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

