using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using System.Xml.Linq;

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
                       ti.thickness As Thickness
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
                        Thickness = reader["Thickness"] != DBNull.Value ? Convert.ToDecimal(reader["Thickness"]) : 0
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

        #endregion

















    }
}
