using Microsoft.AspNetCore.Mvc;

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
                pt.fees AS Fees
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
                        Fees = reader["Fees"] != DBNull.Value ? Convert.ToDecimal(reader["Fees"]) : 0
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
                       ts.amount AS Amount
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

        #endregion


















    }
}
