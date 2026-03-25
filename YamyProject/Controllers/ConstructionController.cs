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


        #region ProjectManagement

        public IActionResult ProjectManagement()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetProjectManagement()
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
            p.id,
            p.code,
            p.name AS name_en,
            p.name_ar,
            p.category,
            p.description,
            p.emirate,
            p.status,
            p.type,
            p.start_date,
            p.end_date,
            p.extend_delay,
            p.execution_period_months,
            p.customer,
            p.contractor,
            p.consultant,
            p.location,
            p.details,
            p.contract_value,
            p.additional_value,
            p.deduction_value,
            p.total_value,
            p.billed_to_date,
            p.expenses,
            p.balance,
            p.contracting_date,
            p.building_licensing_date,
            p.contact_person,
            p.contact_person_number,
            p.area,
            p.plot_number,
            p.block,
            p.total_values,
            p.billed_to_dates,
            p.balances,
            p.country,
            p.city,
            c.name AS customer_name
        FROM tbl_projects p
        LEFT JOIN tbl_customer c ON p.customer = c.id
        ORDER BY p.id;";

                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var projectList = new List<ProjectResponse>();
                int sn = 1;

                while (await reader.ReadAsync()) 
                {
                    projectList.Add(new ProjectResponse
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        Code = reader["code"].ToString(),
                        Name = reader["name_en"].ToString(),
                        NameArabic = reader["name_ar"].ToString(),
                        Category = reader["category"].ToString(),
                        Description = reader["description"].ToString(),
                        Emirate = reader["emirate"].ToString(),
                        Status = reader["status"].ToString(),
                        Type = reader["type"].ToString(),
                        StartDate = reader["start_date"] != DBNull.Value ? Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd") : null,
                        EndDate = reader["end_date"] != DBNull.Value ? Convert.ToDateTime(reader["end_date"]).ToString("yyyy-MM-dd") : null,
                        ExtendDelay = reader["extend_delay"] != DBNull.Value ? Convert.ToDateTime(reader["extend_delay"]).ToString("yyyy-MM-dd") : null,
                        ExecutionPeriodMonths = reader["execution_period_months"] != DBNull.Value ? Convert.ToInt32(reader["execution_period_months"]) : (int?)null,
                        Customer = reader["customer"].ToString(),
                        CustomerName = reader["customer_name"].ToString(),
                        Contractor = reader["contractor"].ToString(),
                        Consultant = reader["consultant"].ToString(),
                        Location = reader["location"].ToString(),
                        Details = reader["details"].ToString(),
                        ContractValue = reader["contract_value"] != DBNull.Value ? Convert.ToDecimal(reader["contract_value"]) : 0,
                        AdditionalValue = reader["additional_value"] != DBNull.Value ? Convert.ToDecimal(reader["additional_value"]) : 0,
                        DeductionValue = reader["deduction_value"] != DBNull.Value ? Convert.ToDecimal(reader["deduction_value"]) : 0,
                        TotalValue = reader["total_value"] != DBNull.Value ? Convert.ToDecimal(reader["total_value"]) : 0,
                        BilledToDate = reader["billed_to_date"] != DBNull.Value ? Convert.ToDecimal(reader["billed_to_date"]) : 0,
                        Expenses = reader["expenses"] != DBNull.Value ? Convert.ToDecimal(reader["expenses"]) : 0,
                        Balance = reader["balance"] != DBNull.Value ? Convert.ToDecimal(reader["balance"]) : 0,


                        ContractingDate = reader["contracting_date"] != DBNull.Value
    ? Convert.ToDateTime(reader["contracting_date"]).ToString("yyyy-MM-dd") : null,
                        BuildingLicensingDate = reader["building_licensing_date"] != DBNull.Value
    ? Convert.ToDateTime(reader["building_licensing_date"]).ToString("yyyy-MM-dd") : null,
                        ContactPerson = reader["contact_person"].ToString(),
                        ContactPersonNumber = reader["contact_person_number"].ToString(),
                        Area = reader["area"].ToString(),
                        PlotNumber = reader["plot_number"].ToString(),
                        Block = reader["block"].ToString(),
                        TotalValues = reader["total_values"] != DBNull.Value
    ? Convert.ToDecimal(reader["total_values"]) : 0,
                        BilledToDates = reader["billed_to_dates"] != DBNull.Value
    ? Convert.ToDecimal(reader["billed_to_dates"]) : 0,
                        Balances = reader["balances"] != DBNull.Value
    ? Convert.ToDecimal(reader["balances"]) : 0,
                        Country = reader["country"].ToString(),
                        City = reader["city"].ToString(),


                        Accounts = new List<ProjectAccountItem>() // initialize empty list


                    });
                }

                await reader.CloseAsync();

                // 🔹 Get Accounts
                string accQuery = @"SELECT project_id, name, account_id, checkbox_id 
                            FROM tbl_projects_accounts";

                await using var accCmd = new MySqlCommand(accQuery, conn);
                await using var accReader = await accCmd.ExecuteReaderAsync();

                var accountList = new List<dynamic>();

                while (await accReader.ReadAsync())
                {
                    accountList.Add(new
                    {
                        ProjectId = Convert.ToInt32(accReader["project_id"]),
                        Name = accReader["name"].ToString(),
                        AccountId = Convert.ToInt32(accReader["account_id"]),
                        CheckboxId = Convert.ToInt32(accReader["checkbox_id"]),
                     //   Code = accReader["code"] != DBNull.Value ? accReader["code"].ToString() : ""
                    });
                }

                // 🔹 Attach accounts to projects
                foreach (var project in projectList)
                {
                    project.Accounts = accountList
                        .Where(a => a.ProjectId == project.Id)
                        .Select(a => new ProjectAccountItem
                        {
                            Name = a.Name,
                            AccountId = a.AccountId,
                         //   Code = a.Code,
                            CheckboxId = a.CheckboxId != 0
                        })
                        .ToList();
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
            try
            {
                if (model == null) return BadRequest(new { status = false, message = "Invalid request" });
                if (string.IsNullOrWhiteSpace(model.Name)) return BadRequest(new { status = false, message = "Please enter Project Name" });
                if (model.StartDate.HasValue && model.EndDate.HasValue && model.StartDate >= model.EndDate)
                {
                    return BadRequest(new { status = false, message = "Start Date must be before End Date" });
                }

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0) return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                { Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase") };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check duplicate name
                string checkQuery = "SELECT id FROM tbl_projects WHERE name=@name";
                try
                {
                    await using (var checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                        var existingId = await checkCmd.ExecuteScalarAsync();
                        if (existingId != null && (model.Id == 0 || model.Id != Convert.ToInt32(existingId)))
                            return BadRequest(new { status = false, message = "Project Name already in use" });
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                // Generate code if new
                string projectCode = model.Id == 0 ? await GenerateNextProjectCode(conn) : model.Code;

                if (model.Id == 0)
                {
                    string insertQuery = @"
        INSERT INTO tbl_projects
        (code, name, name_ar, category, description, emirate, status, type,
         start_date, end_date, extend_delay, execution_period_months,
         customer, contractor, consultant, location, details,
         contract_value, additional_value, deduction_value, total_value,
         billed_to_date, expenses, balance, contracting_date, building_licensing_date,
contact_person, contact_person_number,
area, plot_number, block, country, city,
total_values, billed_to_dates, balances)
        VALUES
        (@code, @name, @name_ar, @category, @description, @emirate, @status, @type,
         @start_date, @end_date, @extend_delay, @execution_period_months,
         @customer, @contractor, @consultant, @location, @details,
         @contract_value, @additional_value, @deduction_value, @total_value,
         @billed_to_date, @expenses, @balance,@contracting_date, @building_licensing_date,
@contact_person, @contact_person_number,
@area, @plot_number, @block, @country, @city,
@total_values, @billed_to_dates, @balances);
        SELECT LAST_INSERT_ID();";

                    await using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@code", projectCode);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@name_ar", model.NameArabic?.Trim());
                    cmd.Parameters.AddWithValue("@category", model.Category);
                    cmd.Parameters.AddWithValue("@description", model.Description);
                    cmd.Parameters.AddWithValue("@emirate", model.Emirate);
                    cmd.Parameters.AddWithValue("@status", model.Status);
                    cmd.Parameters.AddWithValue("@type", model.Type);
                    cmd.Parameters.AddWithValue("@start_date", model.StartDate);
                    cmd.Parameters.AddWithValue("@end_date", model.EndDate);
                    cmd.Parameters.AddWithValue("@extend_delay", model.ExtendDelay);
                    cmd.Parameters.AddWithValue("@execution_period_months", model.ExecutionPeriodMonths);
                    cmd.Parameters.AddWithValue("@customer", model.Customer);
                    cmd.Parameters.AddWithValue("@contractor", model.Contractor);
                    cmd.Parameters.AddWithValue("@consultant", model.Consultant);
                    cmd.Parameters.AddWithValue("@location", model.Location);
                    cmd.Parameters.AddWithValue("@details", model.Details);
                    cmd.Parameters.AddWithValue("@contract_value", model.ContractValue);
                    cmd.Parameters.AddWithValue("@additional_value", model.AdditionalValue);
                    cmd.Parameters.AddWithValue("@deduction_value", model.DeductionValue);
                    cmd.Parameters.AddWithValue("@total_value", model.TotalValue);
                    cmd.Parameters.AddWithValue("@billed_to_date", model.BilledToDate);
                    cmd.Parameters.AddWithValue("@expenses", model.Expenses);
                    cmd.Parameters.AddWithValue("@balance", model.Balance);


                    cmd.Parameters.AddWithValue("@contracting_date", model.ContractingDate);
                    cmd.Parameters.AddWithValue("@building_licensing_date", model.BuildingLicensingDate);
                    cmd.Parameters.AddWithValue("@contact_person", model.ContactPerson);
                    cmd.Parameters.AddWithValue("@contact_person_number", model.ContactPersonNumber);
                    cmd.Parameters.AddWithValue("@area", model.Area);
                    cmd.Parameters.AddWithValue("@plot_number", model.PlotNumber);
                    cmd.Parameters.AddWithValue("@block", model.Block);
                    cmd.Parameters.AddWithValue("@country", model.Country);
                    cmd.Parameters.AddWithValue("@city", model.City);
                    cmd.Parameters.AddWithValue("@total_values", model.TotalValues);
                    cmd.Parameters.AddWithValue("@billed_to_dates", model.BilledToDates);
                    cmd.Parameters.AddWithValue("@balances", model.Balances);

                    int projectId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    return Ok(new { status = true, message = "Project created successfully", id = projectId, code = projectCode });
                }
                else
                {
                    // Update existing project
                    string updateQuery = @"
        UPDATE tbl_projects
        SET code=@code, name=@name, name_ar=@name_ar, category=@category, description=@description,
            emirate=@emirate, status=@status, type=@type,
            start_date=@start_date, end_date=@end_date, extend_delay=@extend_delay,
            execution_period_months=@execution_period_months,
            customer=@customer, contractor=@contractor, consultant=@consultant,
            location=@location, details=@details,
            contract_value=@contract_value, additional_value=@additional_value,
            deduction_value=@deduction_value, total_value=@total_value,
            billed_to_date=@billed_to_date, expenses=@expenses, balance=@balance,
            contracting_date=@contracting_date,
            building_licensing_date=@building_licensing_date,
            contact_person=@contact_person,
            contact_person_number=@contact_person_number,
            area=@area,
            plot_number=@plot_number,
            block=@block,
            country=@country,
            city=@city,
            total_values=@total_values,
            billed_to_dates=@billed_to_dates,
            balances=@balances
        WHERE id=@id;";

                    await using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@code", projectCode);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@name_ar", model.NameArabic?.Trim());
                    cmd.Parameters.AddWithValue("@category", model.Category);
                    cmd.Parameters.AddWithValue("@description", model.Description);
                    cmd.Parameters.AddWithValue("@emirate", model.Emirate);
                    cmd.Parameters.AddWithValue("@status", model.Status);
                    cmd.Parameters.AddWithValue("@type", model.Type);
                    cmd.Parameters.AddWithValue("@start_date", model.StartDate);
                    cmd.Parameters.AddWithValue("@end_date", model.EndDate);
                    cmd.Parameters.AddWithValue("@extend_delay", model.ExtendDelay);
                    cmd.Parameters.AddWithValue("@execution_period_months", model.ExecutionPeriodMonths);
                    cmd.Parameters.AddWithValue("@customer", model.Customer);
                    cmd.Parameters.AddWithValue("@contractor", model.Contractor);
                    cmd.Parameters.AddWithValue("@consultant", model.Consultant);
                    cmd.Parameters.AddWithValue("@location", model.Location);
                    cmd.Parameters.AddWithValue("@details", model.Details);
                    cmd.Parameters.AddWithValue("@contract_value", model.ContractValue);
                    cmd.Parameters.AddWithValue("@additional_value", model.AdditionalValue);
                    cmd.Parameters.AddWithValue("@deduction_value", model.DeductionValue);
                    cmd.Parameters.AddWithValue("@total_value", model.TotalValue);
                    cmd.Parameters.AddWithValue("@billed_to_date", model.BilledToDate);
                    cmd.Parameters.AddWithValue("@expenses", model.Expenses);
                    cmd.Parameters.AddWithValue("@balance", model.Balance);

                    cmd.Parameters.AddWithValue("@contracting_date", model.ContractingDate);
                    cmd.Parameters.AddWithValue("@building_licensing_date", model.BuildingLicensingDate);
                    cmd.Parameters.AddWithValue("@contact_person", model.ContactPerson);
                    cmd.Parameters.AddWithValue("@contact_person_number", model.ContactPersonNumber);
                    cmd.Parameters.AddWithValue("@area", model.Area);
                    cmd.Parameters.AddWithValue("@plot_number", model.PlotNumber);
                    cmd.Parameters.AddWithValue("@block", model.Block);
                    cmd.Parameters.AddWithValue("@country", model.Country);
                    cmd.Parameters.AddWithValue("@city", model.City);
                    cmd.Parameters.AddWithValue("@total_values", model.TotalValues);
                    cmd.Parameters.AddWithValue("@billed_to_dates", model.BilledToDates);
                    cmd.Parameters.AddWithValue("@balances", model.Balances);

                    int affected = await cmd.ExecuteNonQueryAsync();
                    if (affected == 0) return NotFound(new { status = false, message = "Project not found" });
                    return Ok(new { status = true, message = "Project updated successfully", id = model.Id, code = projectCode });
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private async Task<string> GenerateNextProjectCode(MySqlConnection conn)
        {
            string query = "SELECT MAX(CAST(code AS UNSIGNED)) FROM tbl_projects";
            await using var cmd = new MySqlCommand(query, conn);
            object result = await cmd.ExecuteScalarAsync();
            int next = 1;
            if (result != DBNull.Value && result != null) next = Convert.ToInt32(result) + 1;
            return next.ToString("D3");
        }

        private async Task<int> GetDefaultAccountId(string category)
        {
            int accountId = 0;

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            var query = "SELECT account_id FROM tbl_coa_config WHERE category = @category LIMIT 1";
            await using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@category", category);

            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
                accountId = Convert.ToInt32(result);

            return accountId;
        }
        [HttpGet]
        public async Task<IActionResult> GetPaymentMethodInfo([FromQuery] string method)
        {
            try
            {
                int accountId = 0;
                bool paymentTermsEnabled = false;

                if (method == "IncomeAccount")
                {
                    // Get default Cash account from database
                    accountId = await GetDefaultAccountId("Income");
                    paymentTermsEnabled = false;
                }
                else if (method == "ExpensesAccount")
                {
                    // Get default Customer account from database
                    accountId = await GetDefaultAccountId("Expenses");
                    paymentTermsEnabled = true;
                }
                else if (method == "RetentionAccount")
                {
                    // Get default Customer account from database
                    accountId = await GetDefaultAccountId("Retention");
                    paymentTermsEnabled = true;
                }
                else if (method == "DownpaymentAccount")
                {
                    // Get default Customer account from database
                    accountId = await GetDefaultAccountId("Downpayment");
                    paymentTermsEnabled = true;
                }

                return Ok(new
                {
                    status = true,
                    accountId,
                    paymentTermsEnabled
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #region Project Accounts

        [HttpPost]
        public async Task<IActionResult> SaveProjectAccounts([FromBody] ProjectAccountRequest model)
        {
            try
            {
                if (model == null || model.ProjectId <= 0)
                    return BadRequest(new { status = false, message = "Invalid request" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                await using var transaction = await conn.BeginTransactionAsync();

                // 🔹 Delete existing accounts for edit
                string deleteQuery = "DELETE FROM tbl_projects_accounts WHERE project_id=@project_id";
                await using (var deleteCmd = new MySqlCommand(deleteQuery, conn, (MySqlTransaction)transaction))
                {
                    deleteCmd.Parameters.AddWithValue("@project_id", model.ProjectId);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                // 🔹 Insert accounts
                string insertQuery = @"INSERT INTO tbl_projects_accounts 
                               (name, project_id, account_id, checkbox_id)
                               VALUES (@name, @project_id, @account_id, @checkbox_id)";

                foreach (var acc in model.Accounts)
                {
                    await using var cmd = new MySqlCommand(insertQuery, conn, (MySqlTransaction)transaction);

                    cmd.Parameters.AddWithValue("@name", acc.Name);
                    cmd.Parameters.AddWithValue("@project_id", model.ProjectId);
                    cmd.Parameters.AddWithValue("@account_id", acc.AccountId);
                    cmd.Parameters.AddWithValue("@checkbox_id", acc.CheckboxId ? 1 : 0);

                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return Ok(new { status = true, message = "Accounts saved successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Project Attachements

        [HttpGet]
        public async Task<IActionResult> GetProjectAttachments(int? projectId)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Fetch projects
                string projectQuery = @"SELECT id, name FROM tbl_projects";
                if (projectId.HasValue)
                {
                    projectQuery += " WHERE id = @projectId";
                }
                projectQuery += " ORDER BY id";

                var projectList = new List<ProjectResponse>();
                await using (var cmd = new MySqlCommand(projectQuery, conn))
                {
                    if (projectId.HasValue)
                        cmd.Parameters.AddWithValue("@projectId", projectId.Value);

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        projectList.Add(new ProjectResponse
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader["name"].ToString(),
                            Attachments = new List<ProjectAttachmentItem>()
                        });
                    }
                }

                // 🔹 Fetch attachments (only for selected project if id passed)
                string attachQuery = @"SELECT id, project_id, file_name, file_path, description
                               FROM tbl_project_attachments";
                if (projectId.HasValue)
                {
                    attachQuery += " WHERE project_id = @projectId";
                }

                var attachmentList = new List<ProjectAttachmentItem>();
                await using (var attachCmd = new MySqlCommand(attachQuery, conn))
                {
                    if (projectId.HasValue)
                        attachCmd.Parameters.AddWithValue("@projectId", projectId.Value);

                    await using var attachReader = await attachCmd.ExecuteReaderAsync();
                    while (await attachReader.ReadAsync())
                    {
                        attachmentList.Add(new ProjectAttachmentItem
                        {
                            Id = attachReader.GetInt32("id"),
                            ProjectId = attachReader.GetInt32("project_id"),
                            FileName = attachReader["file_name"].ToString(),
                            FilePath = attachReader["file_path"].ToString(),
                            Description = attachReader["description"].ToString()
                        });
                    }
                }

                // 🔹 Map attachments to projects
                foreach (var project in projectList)
                {
                    project.Attachments = attachmentList
                        .Where(a => a.ProjectId == project.Id)
                        .ToList();
                }

                return Ok(new { status = true, data = projectList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> SaveProjectAttachment([FromForm] ProjectAttachmentRequest model)
        {
            try
            {
                if (model == null || model.ProjectId <= 0)
                    return BadRequest(new { status = false, message = "Invalid request" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string fileName = "";
                string filePath = "";

                if (model.File != null)
                {
                    fileName = Guid.NewGuid() + Path.GetExtension(model.File.FileName);
                    filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.File.CopyToAsync(stream);
                    }
                }

                if (model.Id == 0)
                {
                    // 🔹 INSERT
                    string insertQuery = @"INSERT INTO tbl_project_attachments
                          (project_id, file_name, file_path, description)
                          VALUES (@project_id, @file_name, @file_path, @description)";

                    await using var cmd = new MySqlCommand(insertQuery, conn);

                    cmd.Parameters.AddWithValue("@project_id", model.ProjectId);
                    cmd.Parameters.AddWithValue("@file_name", model.File.FileName);
                    cmd.Parameters.AddWithValue("@file_path", "/uploads/" + fileName);
                    cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                    await cmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // 🔹 UPDATE
                    string updateQuery;

                    if (model.File != null)
                    {
                        updateQuery = @"UPDATE tbl_project_attachments 
                        SET file_name=@file_name,
                            file_path=@file_path,
                            description=@description
                        WHERE id=@id";
                    }
                    else
                    {
                        updateQuery = @"UPDATE tbl_project_attachments 
                        SET description=@description
                        WHERE id=@id";
                    }

                    await using var cmd = new MySqlCommand(updateQuery, conn);

                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                    if (model.File != null)
                    {
                        cmd.Parameters.AddWithValue("@file_name", model.File.FileName);
                        cmd.Parameters.AddWithValue("@file_path", "/uploads/" + fileName);
                    }

                    await cmd.ExecuteNonQueryAsync();
                }

                return Ok(new { status = true, message = "Attachment saved successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteProjectAttachment(int attachmentId)
        {
            if (attachmentId <= 0)
                return BadRequest(new { status = false, message = "Invalid attachment ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Get file path before deleting (so we can remove physical file)
                string selectQuery = "SELECT file_path FROM tbl_project_attachments WHERE id=@id";
                string filePath = null;
                await using (var selectCmd = new MySqlCommand(selectQuery, conn))
                {
                    selectCmd.Parameters.AddWithValue("@id", attachmentId);
                    var result = await selectCmd.ExecuteScalarAsync();
                    filePath = result?.ToString();
                }

                if (filePath == null)
                    return NotFound(new { status = false, message = "Attachment not found" });

                // 🔹 Delete attachment from database
                string deleteQuery = "DELETE FROM tbl_project_attachments WHERE id=@id";
                await using (var deleteCmd = new MySqlCommand(deleteQuery, conn))
                {
                    deleteCmd.Parameters.AddWithValue("@id", attachmentId);
                    int affected = await deleteCmd.ExecuteNonQueryAsync();
                    if (affected == 0)
                        return NotFound(new { status = false, message = "Attachment not found" });
                }

                // 🔹 Delete physical file if exists
                try
                {
                    var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString()));
                    if (System.IO.File.Exists(physicalPath))
                    {
                        System.IO.File.Delete(physicalPath);
                    }
                }
                catch
                {
                    // ignore file deletion errors
                }

                return Ok(new { status = true, message = "Attachment deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Project Subcontractors

        [HttpGet]
        public async Task<IActionResult> GetProjectSubcontractors(int? projectId)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Fetch Projects
                string projectQuery = @"SELECT id, name FROM tbl_projects";

                if (projectId.HasValue)
                    projectQuery += " WHERE id = @projectId";

                projectQuery += " ORDER BY id";

                var projectList = new List<ProjectSubcontractorResponse>();

                await using (var cmd = new MySqlCommand(projectQuery, conn))
                {
                    if (projectId.HasValue)
                        cmd.Parameters.AddWithValue("@projectId", projectId.Value);

                    await using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        projectList.Add(new ProjectSubcontractorResponse
                        {
                            Id = reader.GetInt32("id"),
                            Name = reader["name"].ToString(),
                            Subcontractors = new List<ProjectSubcontractorItem>()
                        });
                    }
                }

                // 🔹 Fetch Subcontractors
                string subQuery = @"SELECT 
                            id,
                            project_id,
                            code_id,
                            subcontractor_id,
                            contract_no,
                            contract_value,
                            contract_date,
                            contract_period,
                            works,
                            works_en
                        FROM tbl_project_subcontractors";

                if (projectId.HasValue)
                    subQuery += " WHERE project_id = @projectId";

                var subcontractorList = new List<ProjectSubcontractorItem>();

                await using (var cmd = new MySqlCommand(subQuery, conn))
                {
                    if (projectId.HasValue)
                        cmd.Parameters.AddWithValue("@projectId", projectId.Value);

                    await using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        subcontractorList.Add(new ProjectSubcontractorItem
                        {
                            Id = reader.GetInt32("id"),
                            ProjectId = reader.GetInt32("project_id"),
                            CodeId = reader.GetInt32("code_id"),
                            SubcontractorId = reader.GetInt32("subcontractor_id"),
                            ContractNo = reader["contract_no"].ToString(),
                            ContractValue = Convert.ToDecimal(reader["contract_value"]),
                            ContractDate = reader["contract_date"]?.ToString(),
                            ContractPeriod = reader["contract_period"].ToString(),
                            Works = reader["works"].ToString(),
                            WorksEn = reader["works_en"].ToString()
                        });
                    }
                }

                // 🔹 Map Subcontractors to Projects
                foreach (var project in projectList)
                {
                    project.Subcontractors = subcontractorList
                        .Where(a => a.ProjectId == project.Id)
                        .ToList();
                }

                return Ok(new { status = true, data = projectList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveProjectSubcontractors([FromBody] List<ProjectSubcontractorRequest> model)
        {
            try
            {
                if (model == null || !model.Any())
                    return BadRequest(new { status = false, message = "No data found" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                foreach (var item in model)
                {
                    if (item.Id == 0)
                    {
                        string insertQuery = @"INSERT INTO tbl_project_subcontractors
                    (project_id, code_id, subcontractor_id, contract_no, contract_value,
                     contract_date, contract_period, works, works_en)
                    VALUES
                    (@project_id, @code_id, @subcontractor_id, @contract_no, @contract_value,
                     @contract_date, @contract_period, @works, @works_en)";

                        await using var cmd = new MySqlCommand(insertQuery, conn);

                        cmd.Parameters.AddWithValue("@project_id", item.ProjectId);
                        cmd.Parameters.AddWithValue("@code_id", item.CodeId);
                        cmd.Parameters.AddWithValue("@subcontractor_id", item.SubcontractorId);
                        cmd.Parameters.AddWithValue("@contract_no", item.ContractNo ?? "");
                        cmd.Parameters.AddWithValue("@contract_value", item.ContractValue);
                        cmd.Parameters.AddWithValue("@contract_date", item.ContractDate);
                        cmd.Parameters.AddWithValue("@contract_period", item.ContractPeriod ?? "");
                        cmd.Parameters.AddWithValue("@works", item.Works ?? "");
                        cmd.Parameters.AddWithValue("@works_en", item.WorksEn ?? "");

                        await cmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        string updateQuery = @"UPDATE tbl_project_subcontractors SET
                        code_id=@code_id,
                        subcontractor_id=@subcontractor_id,
                        contract_no=@contract_no,
                        contract_value=@contract_value,
                        contract_date=@contract_date,
                        contract_period=@contract_period,
                        works=@works,
                        works_en=@works_en
                        WHERE id=@id";

                        await using var cmd = new MySqlCommand(updateQuery, conn);

                        cmd.Parameters.AddWithValue("@id", item.Id);
                        cmd.Parameters.AddWithValue("@code_id", item.CodeId);
                        cmd.Parameters.AddWithValue("@subcontractor_id", item.SubcontractorId);
                        cmd.Parameters.AddWithValue("@contract_no", item.ContractNo ?? "");
                        cmd.Parameters.AddWithValue("@contract_value", item.ContractValue);
                        cmd.Parameters.AddWithValue("@contract_date", item.ContractDate);
                        cmd.Parameters.AddWithValue("@contract_period", item.ContractPeriod ?? "");
                        cmd.Parameters.AddWithValue("@works", item.Works ?? "");
                        cmd.Parameters.AddWithValue("@works_en", item.WorksEn ?? "");

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Ok(new { status = true, message = "Subcontractors saved successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { status = false, message = ex.Message });
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteProjectSubcontractor(int subcontractorId)
        {
            if (subcontractorId <= 0)
                return BadRequest(new { status = false, message = "Invalid subcontractor ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string deleteQuery = "DELETE FROM tbl_project_subcontractors WHERE id=@id";

                await using var cmd = new MySqlCommand(deleteQuery, conn);
                cmd.Parameters.AddWithValue("@id", subcontractorId);

                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected == 0)
                    return NotFound(new { status = false, message = "Subcontractor not found" });

                return Ok(new { status = true, message = "Subcontractor deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Project Engineers

        [HttpGet]
        public async Task<IActionResult> GetProjectEngineers(int? projectId)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Fetch engineers
                string query = @"SELECT id, project_id, engineer_code, engineer_name, phone, contract_date 
                         FROM tbl_project_engineers";

                if (projectId.HasValue)
                    query += " WHERE project_id = @projectId";

                var engineerList = new List<ProjectEngineerRequest>();

                await using (var cmd = new MySqlCommand(query, conn))
                {
                    if (projectId.HasValue)
                        cmd.Parameters.AddWithValue("@projectId", projectId.Value);

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        engineerList.Add(new ProjectEngineerRequest
                        {
                            Id = reader.GetInt32("id"),
                            ProjectId = reader.GetInt32("project_id"),
                            EngineerCode = reader["engineer_code"].ToString(),
                            EngineerName = reader["engineer_name"].ToString(),
                            Phone = reader["phone"].ToString(),
                            ContractDate = reader["contract_date"] != DBNull.Value
                                            ? Convert.ToDateTime(reader["contract_date"])
                                            : (DateTime?)null
                        });
                    }
                }

                return Ok(new { status = true, data = engineerList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveProjectEngineers([FromBody] List<ProjectEngineerRequest> engineers)
        {
            if (engineers == null || engineers.Count == 0)
                return BadRequest(new { status = false, message = "No engineer data received" });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                foreach (var engineer in engineers)
                {
                    if (engineer.Id == 0)
                    {
                        if (string.IsNullOrWhiteSpace(engineer.EngineerCode))
                        {
                            engineer.EngineerCode = await GenerateNextEngineerCode(conn, transaction);
                        }

                        string insertQuery = @"
                    INSERT INTO tbl_project_engineers
                    (project_id, engineer_code, engineer_name, phone, contract_date)
                    VALUES (@project_id, @engineer_code, @engineer_name, @phone, @contract_date)";

                        await using var insertCmd = new MySqlCommand(insertQuery, conn, transaction);
                        insertCmd.Parameters.AddWithValue("@project_id", engineer.ProjectId);
                        insertCmd.Parameters.AddWithValue("@engineer_code", engineer.EngineerCode);
                        insertCmd.Parameters.AddWithValue("@engineer_name", engineer.EngineerName ?? "");
                        insertCmd.Parameters.AddWithValue("@phone", engineer.Phone ?? "");
                        insertCmd.Parameters.AddWithValue("@contract_date", engineer.ContractDate.HasValue ? engineer.ContractDate.Value.Date : (object)DBNull.Value);

                        await insertCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        // Update existing engineer
                        string updateQuery = @"
                    UPDATE tbl_project_engineers
                    SET engineer_code=@engineer_code,
                        engineer_name=@engineer_name,
                        phone=@phone,
                        contract_date=@contract_date
                    WHERE id=@id AND project_id=@project_id";

                        await using var updateCmd = new MySqlCommand(updateQuery, conn, transaction);
                        updateCmd.Parameters.AddWithValue("@id", engineer.Id);
                        updateCmd.Parameters.AddWithValue("@project_id", engineer.ProjectId);
                        updateCmd.Parameters.AddWithValue("@engineer_code", engineer.EngineerCode ?? "");
                        updateCmd.Parameters.AddWithValue("@engineer_name", engineer.EngineerName ?? "");
                        updateCmd.Parameters.AddWithValue("@phone", engineer.Phone ?? "");
                        updateCmd.Parameters.AddWithValue("@contract_date", engineer.ContractDate.HasValue ? engineer.ContractDate.Value.Date : (object)DBNull.Value);

                        int affected = await updateCmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                        {
                            return NotFound(new { status = false, message = $"Engineer with ID {engineer.Id} not found" });
                        }
                    }
                }

                await transaction.CommitAsync();
                return Ok(new { status = true, message = "Engineers saved successfully" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateNextEngineerCode(MySqlConnection conn, MySqlTransaction transaction)
        {
            try
            {
                string query = "SELECT MAX(CAST(engineer_code AS UNSIGNED)) FROM tbl_project_engineers";
                await using var cmd = new MySqlCommand(query, conn, transaction);  // Assign transaction here
                object result = await cmd.ExecuteScalarAsync();

                int next = 1;
                if (result != DBNull.Value && result != null) next = Convert.ToInt32(result) + 1;

                return next.ToString("D3");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteProjectEngineer(int engineerId)
        {
            if (engineerId <= 0)
                return BadRequest(new { status = false, message = "Invalid engineer ID" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string deleteQuery = "DELETE FROM tbl_project_engineers WHERE id=@id";

                await using var cmd = new MySqlCommand(deleteQuery, conn);
                cmd.Parameters.AddWithValue("@id", engineerId);

                int affected = await cmd.ExecuteNonQueryAsync();

                if (affected == 0)
                    return NotFound(new { status = false, message = "Engineer not found" });

                return Ok(new { status = true, message = "Engineer deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Project Location

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
                    INSERT INTO tbl_project_sites 
                    (project_id, code, name, location_id, plot_number, address)
                    VALUES 
                    (@project_id, @code, @name, @location_id, @plot_number, @address);
                    SELECT LAST_INSERT_ID();";
                    
                    await using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@code", code);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@location_id", model.LocationId ?? 0);
                    cmd.Parameters.AddWithValue("@plot_number", model.PlotNumber ?? "");
                    cmd.Parameters.AddWithValue("@address", model.Address ?? "");
                    cmd.Parameters.AddWithValue("@project_id", model.ProjectId);

                    int siteId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    return Ok(new { status = true, message = "Site added successfully", id = siteId, code = code });
                }
                else
                {
                    // ✏️ Update existing site
                    string updateQuery = @"
                        UPDATE tbl_project_sites 
                        SET project_id=@project_id,
                            name=@name,
                            location_id=@location_id,
                            plot_number=@plot_number,
                            address=@address
                        WHERE id=@id;";

                    await using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    cmd.Parameters.AddWithValue("@location_id", model.LocationId ?? 0);
                    cmd.Parameters.AddWithValue("@plot_number", model.PlotNumber ?? "");
                    cmd.Parameters.AddWithValue("@address", model.Address ?? "");
                    cmd.Parameters.AddWithValue("@project_id", model.ProjectId);

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

        [HttpGet]
        public async Task<IActionResult> GetProjectSites(int? projectId)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
        SELECT 
            ROW_NUMBER() OVER (ORDER BY ps.id) AS SN,
            ps.id,
            ps.project_id,
            ps.code,
            ps.name,
            COALESCE(c.name,'Unknown') AS location,
            ps.plot_number,
            ps.address,
            c.id AS LocationId
        FROM tbl_project_sites ps
        LEFT JOIN tbl_city c ON c.id = ps.location_id
        ";

                if (projectId.HasValue)
                {
                    query += " WHERE ps.project_id = @projectId";
                }

                query += " ORDER BY ps.id";

                await using var cmd = new MySqlCommand(query, conn);

                if (projectId.HasValue)
                    cmd.Parameters.AddWithValue("@projectId", projectId.Value);

                await using var reader = await cmd.ExecuteReaderAsync();

                var list = new List<object>();

                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        sn = reader.GetInt32("SN"),
                        id = reader.GetInt32("id"),
                        projectId = reader.GetInt32("project_id"),
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

        #endregion

        #endregion

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
                emirate AS CountryId,
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
                        CountryId = reader.GetInt32("emirate"),
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

       // [HttpPost]
       // public async Task<IActionResult> SaveProject([FromBody] ProjectRequest model)
       // {
       //     if (model == null)
       //         return BadRequest(new { status = false, message = "Invalid request" });

       //     if (string.IsNullOrWhiteSpace(model.Name))
       //         return BadRequest(new { status = false, message = "Please enter Project Name" });

       //     if (model.StartDate >= model.EndDate)
       //         return BadRequest(new { status = false, message = "Start Date must be before End Date" });


       //     try
       //     {
       //         int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
       //         if (userId <= 0)
       //             return Unauthorized(new { status = false, message = "User not logged in" });

       //         var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
       //         {
       //             Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
       //         };

       //         await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
       //         await conn.OpenAsync();

       //         // 1️⃣ Check if project name already exists
       //         string checkQuery = "SELECT id FROM tbl_projects WHERE name=@name";
       //         await using (var checkCmd = new MySqlCommand(checkQuery, conn))
       //         {
       //             checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
       //             var existingId = await checkCmd.ExecuteScalarAsync();
       //             if (existingId != null && (model.Id == 0 || model.Id != Convert.ToInt32(existingId)))
       //             {
       //                 return BadRequest(new { status = false, message = "Project Name already in use" });
       //             }
       //         }

       //         // 2️⃣ Generate project code for new projects
       //         string projectCode = model.Id == 0 ? await GenerateNextProjectCode(conn) : model.Code;

       //         if (model.Id == 0)
       //         {
       //             // Insert new project
       //             string insertQuery = @"
       //         INSERT INTO tbl_projects
       //         (code, name, category, description, start_date, end_date, country_id, city_id)
       //         VALUES (@code, @name, @category, @description, @start_date, @end_date, @country_id, @city_id);
       //         SELECT LAST_INSERT_ID();";

       //             await using var cmd = new MySqlCommand(insertQuery, conn);
       //             cmd.Parameters.AddWithValue("@code", projectCode);
       //             cmd.Parameters.AddWithValue("@name", model.Name.Trim());
       //             cmd.Parameters.AddWithValue("@category", model.Category);
       //             cmd.Parameters.AddWithValue("@description", model.Description);
       //             cmd.Parameters.AddWithValue("@start_date", model.StartDate);
       //             cmd.Parameters.AddWithValue("@end_date", model.EndDate);
       //             cmd.Parameters.AddWithValue("@country_id", model.CountryId ?? 0);
       //             cmd.Parameters.AddWithValue("@city_id", model.CityId ?? 0);

       //             int projectId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

       //             // Optional: Insert cost center if enabled
       //             //if (model.IsProjectOption)
       //             //{


       //             // 🔹 Generate Cost Center Main Code
       //             int newMainCode = 101;
       //             string getMaxMainCode = "SELECT MAX(code) FROM tbl_cost_center";
       //             await using (var mainCodeCmd = new MySqlCommand(getMaxMainCode, conn))
       //             {
       //                 var result = await mainCodeCmd.ExecuteScalarAsync();
       //                 if (result != DBNull.Value && result != null)
       //                     newMainCode = Convert.ToInt32(result) + 1;
       //             }

       //             int mainId = 0;
       //                 string mainInsert = @"
       //             INSERT INTO tbl_cost_center (name, code, project_id)
       //             VALUES (@name, @code, @project_id);
       //             SELECT LAST_INSERT_ID();";

       //                 await using (var mainCmd = new MySqlCommand(mainInsert, conn))
       //                 {
       //                     mainCmd.Parameters.AddWithValue("@name", model.Name);
       //                     mainCmd.Parameters.AddWithValue("@code", newMainCode);
       //                     mainCmd.Parameters.AddWithValue("@project_id", projectId);
       //                     mainId = Convert.ToInt32(await mainCmd.ExecuteScalarAsync());
       //                 }

       //             // 🔹 Generate Sub Cost Center Code
       //             int newSubCode = 1001;
       //             string getMaxSubCode = "SELECT MAX(code) FROM tbl_sub_cost_center";
       //             await using (var subCodeCmd = new MySqlCommand(getMaxSubCode, conn))
       //             {
       //                 var result = await subCodeCmd.ExecuteScalarAsync();
       //                 if (result != DBNull.Value && result != null)
       //                     newSubCode = Convert.ToInt32(result) + 1;
       //             }

       //             string subInsert = @"
       //             INSERT INTO tbl_sub_cost_center (code, name, main_id, project_id)
       //             VALUES (@code, @name, @main_id, @project_id);";

       //                 await using (var subCmd = new MySqlCommand(subInsert, conn))
       //                 {
       //                     subCmd.Parameters.AddWithValue("@code", newSubCode);
       //                     subCmd.Parameters.AddWithValue("@name", model.Name);
       //                     subCmd.Parameters.AddWithValue("@main_id", mainId);
       //                     subCmd.Parameters.AddWithValue("@project_id", projectId);
       //                     await subCmd.ExecuteNonQueryAsync();
       //                 }
       //             //}

       //             return Ok(new
       //             {
       //                 status = true,
       //                 message = "Project created successfully",
       //                 id = projectId,
       //                 code = projectCode
       //             });
       //         }
       //         else
       //         {
                   
       //             // 🔹 Get existing project code (to avoid overwriting with null)
       //             string existingCodeQuery = "SELECT code FROM tbl_projects WHERE id=@id";
       //             string existingCode = "";
       //             await using (var codeCmd = new MySqlCommand(existingCodeQuery, conn))
       //             {
       //                 codeCmd.Parameters.AddWithValue("@id", model.Id);
       //                 var codeResult = await codeCmd.ExecuteScalarAsync();
       //                 if (codeResult != DBNull.Value && codeResult != null)
       //                     existingCode = codeResult.ToString();
       //             }

       //             string finalProjectCode = string.IsNullOrWhiteSpace(model.Code)
       //? existingCode
       //: model.Code;


       //             // 🔹 Update existing project
       //             string updateQuery = @"
       // UPDATE tbl_projects
       // SET code=@code, name=@name, category=@category, description=@description,
       //     start_date=@start_date, end_date=@end_date, country_id=@country_id, city_id=@city_id
       // WHERE id=@id;";

       //             await using var cmd = new MySqlCommand(updateQuery, conn);
       //             cmd.Parameters.AddWithValue("@id", model.Id);
       //             cmd.Parameters.AddWithValue("@code", finalProjectCode);
       //             cmd.Parameters.AddWithValue("@name", model.Name.Trim());
       //             cmd.Parameters.AddWithValue("@category", model.Category);
       //             cmd.Parameters.AddWithValue("@description", model.Description);
       //             cmd.Parameters.AddWithValue("@start_date", model.StartDate);
       //             cmd.Parameters.AddWithValue("@end_date", model.EndDate);
       //             cmd.Parameters.AddWithValue("@country_id", model.CountryId ?? 0);
       //             cmd.Parameters.AddWithValue("@city_id", model.CityId ?? 0);

       //             int affected = await cmd.ExecuteNonQueryAsync();
       //             if (affected == 0)
       //                 return NotFound(new { status = false, message = "Project not found" });

       //             // 🔹 Update cost center and sub cost center names
       //             string updateMain = @"
       // UPDATE tbl_cost_center 
       // SET name=@name 
       // WHERE project_id=@project_id;";
       //             await using (var mainCmd = new MySqlCommand(updateMain, conn))
       //             {
       //                 mainCmd.Parameters.AddWithValue("@name", model.Name.Trim());
       //                 mainCmd.Parameters.AddWithValue("@project_id", model.Id);
       //                 await mainCmd.ExecuteNonQueryAsync();
       //             }

       //             string updateSub = @"
       // UPDATE tbl_sub_cost_center 
       // SET name=@name 
       // WHERE project_id=@project_id;";
       //             await using (var subCmd = new MySqlCommand(updateSub, conn))
       //             {
       //                 subCmd.Parameters.AddWithValue("@name", model.Name.Trim());
       //                 subCmd.Parameters.AddWithValue("@project_id", model.Id);
       //                 await subCmd.ExecuteNonQueryAsync();
       //             }

       //             return Ok(new
       //             {
       //                 status = true,
       //                 message = "Project updated successfully",
       //                 id = model.Id,
       //                 code = projectCode
       //             });
       //         }

       //     }
       //     catch (Exception ex)
       //     {
       //         return StatusCode(500, new { status = false, message = ex.Message });
       //     }
       // }

       // private async Task<string> GenerateNextProjectCode(MySqlConnection conn)
       // {
       //     string query = "SELECT MAX(CAST(code AS UNSIGNED)) FROM tbl_projects";
       //     await using var cmd = new MySqlCommand(query, conn);
       //     object result = await cmd.ExecuteScalarAsync();

       //     int next = 1;
       //     if (result != DBNull.Value && result != null)
       //         next = Convert.ToInt32(result) + 1;

       //     return next.ToString("D3"); // Format: 001, 002, 003
       // }

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

        //[HttpGet]
        //public async Task<IActionResult> GetProjectSites()
        //{
        //    try
        //    {
        //        int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        //        if (userId <= 0)
        //            return Unauthorized(new { status = false, message = "User not logged in" });

        //        var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
        //        {
        //            Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
        //        };

        //        await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
        //        await conn.OpenAsync();

        //        string query = @"
        //    SELECT 
        //        ROW_NUMBER() OVER (ORDER BY ps.id) AS SN, 
        //        ps.id, 
        //        ps.code, 
        //        ps.name, 
        //        COALESCE(c.name, 'Unknown') AS location, 
        //        ps.plot_number, 
        //        ps.address,
        //        c.id AS LocationId
        //    FROM tbl_project_sites ps
        //    LEFT JOIN tbl_city c ON c.id = ps.location_id;
        //";

        //        await using var cmd = new MySqlCommand(query, conn);
        //        await using var reader = await cmd.ExecuteReaderAsync();

        //        var list = new List<object>();
        //        while (await reader.ReadAsync())
        //        {
        //            list.Add(new
        //            {
        //                sn = reader.GetInt32("SN"),
        //                id = reader.GetInt32("id"),
        //                code = reader["code"].ToString(),
        //                name = reader["name"].ToString(),
        //                location = reader["location"].ToString(),
        //                plotNumber = reader["plot_number"].ToString(),
        //                address = reader["address"].ToString(),
        //                locationId = reader.GetInt32("LocationId")
        //            });
        //        }

        //        return Ok(new { status = true, data = list });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = false, message = ex.Message });
        //    }
        //}

        //[HttpPost]
        //public async Task<IActionResult> SaveSite([FromBody] SiteRequest model)
        //{
        //    if (model == null)
        //        return BadRequest(new { status = false, message = "Invalid request" });

        //    if (string.IsNullOrWhiteSpace(model.Name))
        //        return BadRequest(new { status = false, message = "Please enter Site Name" });

        //    if (model.LocationId == null || model.LocationId <= 0)
        //        return BadRequest(new { status = false, message = "Please select a project location" });

        //    try
        //    {
        //        int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        //        if (userId <= 0)
        //            return Unauthorized(new { status = false, message = "User not logged in" });

        //        var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
        //        {
        //            Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
        //        };

        //        await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
        //        await conn.OpenAsync();

        //        // 🔍 Check if site name already exists
        //        string checkQuery = "SELECT id FROM tbl_project_sites WHERE name=@name";
        //        await using (var checkCmd = new MySqlCommand(checkQuery, conn))
        //        {
        //            checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
        //            var existingId = await checkCmd.ExecuteScalarAsync();
        //            if (existingId != null && (model.Id == 0 || model.Id != Convert.ToInt32(existingId)))
        //                return BadRequest(new { status = false, message = "Site Name already in use" });
        //        }

        //        if (model.Id == 0)
        //        {
        //            // 🆕 Generate next site code
        //            string code = await GenerateNextSiteCode(conn);

        //            // Insert new site
        //            string insertQuery = @"
        //        INSERT INTO tbl_project_sites (code, name, location_id, plot_number, address)
        //        VALUES (@code, @name, @location_id, @plot_number, @address);
        //        SELECT LAST_INSERT_ID();";

        //            await using var cmd = new MySqlCommand(insertQuery, conn);
        //            cmd.Parameters.AddWithValue("@code", code);
        //            cmd.Parameters.AddWithValue("@name", model.Name.Trim());
        //            cmd.Parameters.AddWithValue("@location_id", model.LocationId ?? 0);
        //            cmd.Parameters.AddWithValue("@plot_number", model.PlotNumber ?? "");
        //            cmd.Parameters.AddWithValue("@address", model.Address ?? "");

        //            int siteId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        //            return Ok(new { status = true, message = "Site added successfully", id = siteId, code = code });
        //        }
        //        else
        //        {
        //            // ✏️ Update existing site
        //            string updateQuery = @"
        //        UPDATE tbl_project_sites 
        //        SET name=@name, location_id=@location_id, plot_number=@plot_number, address=@address 
        //        WHERE id=@id;";

        //            await using var cmd = new MySqlCommand(updateQuery, conn);
        //            cmd.Parameters.AddWithValue("@id", model.Id);
        //            cmd.Parameters.AddWithValue("@name", model.Name.Trim());
        //            cmd.Parameters.AddWithValue("@location_id", model.LocationId ?? 0);
        //            cmd.Parameters.AddWithValue("@plot_number", model.PlotNumber ?? "");
        //            cmd.Parameters.AddWithValue("@address", model.Address ?? "");

        //            int affected = await cmd.ExecuteNonQueryAsync();
        //            if (affected == 0)
        //                return NotFound(new { status = false, message = "Site not found" });

        //            return Ok(new { status = true, message = "Site updated successfully", id = model.Id });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = false, message = ex.Message });
        //    }
        //}

        //private async Task<string> GenerateNextSiteCode(MySqlConnection conn)
        //{
        //    string query = "SELECT MAX(CAST(code AS UNSIGNED)) FROM tbl_project_sites";
        //    await using var cmd = new MySqlCommand(query, conn);
        //    object result = await cmd.ExecuteScalarAsync();

        //    int next = 1;
        //    if (result != DBNull.Value && result != null)
        //        next = Convert.ToInt32(result) + 1;

        //    return next.ToString("D3"); // e.g., 001, 002, 003
        //}

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


        #region Project Site Management

        public IActionResult ProjectSiteManagement()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectPlanning(
       int? projectId = null,
       string? projectStatus = null)
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
    p.budget AS `Est Budget`,

    IFNULL((
        SELECT AVG(a.progress)
        FROM tbl_project_activity a
        WHERE a.planning_id = p.id
    ), 0) AS `Progress`,

    p.subcontractor_id,
    IFNULL(sc.name, '') AS subcontractor_name,

    p.retention_percentage,
    p.retention AS retention, 

    p.tender_id AS Tender_Id,
    IFNULL(t.name, '') AS tender_name,

  p.project_id AS Project_Id,
    p.site AS Site,
    IFNULL(ts.name, '') AS site_name

FROM tbl_projects_site_setup p

INNER JOIN tbl_projects pr 
    ON p.project_id = pr.id

LEFT JOIN tbl_tender_names t 
    ON p.tender_id = t.id

LEFT JOIN tbl_vendor sc 
    ON p.subcontractor_id = sc.id 
    AND sc.type = 'subcontractor'

 LEFT JOIN tbl_project_sites ts 
    ON p.site = ts.id 

 ";

                var parameters = new List<MySqlParameter>();

                // ✅ Filters
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

                query += " ORDER BY p.date DESC;";

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
                        Id = reader.GetInt32("id"),

                        Date = reader["Date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd")
                            : null,

                        ProjectNo = reader["P NO"]?.ToString(),
                        ProjectName = reader["Project Name"]?.ToString(),

                        // ✅ SAFE NULL HANDLING
                        SubcontractorName = reader["subcontractor_name"]?.ToString(),
                        TenderName = reader["tender_name"]?.ToString(),

                        StartDate = reader["Start Date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["Start Date"]).ToString("yyyy-MM-dd")
                            : null,

                        EndDate = reader["End Date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["End Date"]).ToString("yyyy-MM-dd")
                            : null,

                        Status = reader["Status"]?.ToString(),

                        EstBudget = reader["Est Budget"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Est Budget"])
                            : 0,

                        Progress = reader["Progress"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Progress"])
                            : 0,

                        SubcontractorId = reader["subcontractor_id"] != DBNull.Value
                            ? Convert.ToInt32(reader["subcontractor_id"])
                            : 0,

                        RetentionPercentage = reader["retention_percentage"] != DBNull.Value
                            ? Convert.ToDecimal(reader["retention_percentage"])
                            : 0,

                        // ✅ FIXED alias
                        Retention = reader["retention"] != DBNull.Value
                            ? Convert.ToDecimal(reader["retention"])
                            : 0,

                        Project_Id = reader.GetInt32("Project_Id"),

                        Site = reader["Site"]?.ToString(),
                        SiteName = reader["site_name"]?.ToString(),

                        Tender_Id = reader["Tender_Id"] != DBNull.Value
                            ? Convert.ToInt32(reader["Tender_Id"])
                            : 0
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

            if (model.SiteId <= 0)
                return Json(new { status = false, message = "Select Site First." });

            if (model.TenderId <= 0)
                return Json(new { status = false, message = "Select Tender First." });

            if (model.SubcontractorId <= 0)
                return Json(new { status = false, message = "Select Subcontractor." });

            if (model.EstimatedBudget <= 0)
                return Json(new { status = false, message = "Budget must be greater than zero." });


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

                    decimal retentionAmount = (model.EstimatedBudget * model.RetentionPercent) / 100;

                    if (model.Id > 0)
                    {
                        // ✅ UPDATE
                        string updateSql = @"
                UPDATE tbl_projects_site_setup
                SET 
                    date=@date,
                    project_id=@projectId,
                    site=@site,
                    tender_id=@tenderId,
                    tender_name_id=@tenderNameId,
                    start_date=@startDate,
                    end_date=@endDate,
                    budget=@budget,
                    subcontractor_id=@subcontractorId,
                    retention_percentage=@retentionPercentage,
                    retention=@retention,
                    status=@status,
                    modified_by=@modifiedBy,
                    updated_at=NOW()
                WHERE id=@id;";

                        await using var cmd = new MySqlCommand(updateSql, conn, (MySqlTransaction)transaction);

                        cmd.Parameters.AddWithValue("@id", model.Id);
                        cmd.Parameters.AddWithValue("@date", model.Date);
                        cmd.Parameters.AddWithValue("@projectId", model.ProjectId);
                        cmd.Parameters.AddWithValue("@site", model.SiteId ); 
                        cmd.Parameters.AddWithValue("@tenderId", model.TenderId);
                        cmd.Parameters.AddWithValue("@tenderNameId", model.TenderNameId);
                        cmd.Parameters.AddWithValue("@startDate", model.StartDate);
                        cmd.Parameters.AddWithValue("@endDate", model.EndDate);
                        cmd.Parameters.AddWithValue("@budget", model.EstimatedBudget);
                        cmd.Parameters.AddWithValue("@subcontractorId", model.SubcontractorId);
                        cmd.Parameters.AddWithValue("@retentionPercentage", model.RetentionPercent);
                        cmd.Parameters.AddWithValue("@retention", retentionAmount);
                        cmd.Parameters.AddWithValue("@status", model.Status ?? "");
                        cmd.Parameters.AddWithValue("@modifiedBy", userId);

                        int affected = await cmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                        {
                            await transaction.RollbackAsync();
                            return NotFound(new { status = false, message = "Record not found" });
                        }

                        recordId = model.Id;
                    }
                    else
                    {
                        // ✅ INSERT
                        string insertSql = @"
                INSERT INTO tbl_projects_site_setup
                (date, project_id, site, tender_id, tender_name_id,
                 start_date, end_date, budget,
                 subcontractor_id, retention_percentage, retention,
                 status, created_by)
                VALUES
                (@date, @projectId, @site, @tenderId, @tenderNameId,
                 @startDate, @endDate, @budget,
                 @subcontractorId, @retentionPercentage, @retention,
                 @status, @createdBy);
                SELECT LAST_INSERT_ID();";

                        await using var cmd = new MySqlCommand(insertSql, conn, (MySqlTransaction)transaction);

                        cmd.Parameters.AddWithValue("@date", model.Date);
                        cmd.Parameters.AddWithValue("@projectId", model.ProjectId);
                        cmd.Parameters.AddWithValue("@site", model.SiteId ); 
                        cmd.Parameters.AddWithValue("@tenderId", model.TenderId);
                        cmd.Parameters.AddWithValue("@tenderNameId", model.TenderNameId);
                        cmd.Parameters.AddWithValue("@startDate", model.StartDate);
                        cmd.Parameters.AddWithValue("@endDate", model.EndDate);
                        cmd.Parameters.AddWithValue("@budget", model.EstimatedBudget);
                        cmd.Parameters.AddWithValue("@subcontractorId", model.SubcontractorId);
                        cmd.Parameters.AddWithValue("@retentionPercentage", model.RetentionPercent);
                        cmd.Parameters.AddWithValue("@retention", retentionAmount);
                        cmd.Parameters.AddWithValue("@status", model.Status ?? "");
                        cmd.Parameters.AddWithValue("@createdBy", userId);

                        var result = await cmd.ExecuteScalarAsync();
                        recordId = result != null ? Convert.ToInt32(result) : 0;

                    }


                    //    if (model.EstimatedBudget > 0)
                    //{
                    //    await InsertTransaction(conn, (MySqlTransaction)transaction, model.Date, model.AccountId.ToString(), model.EstimatedBudget, 0, recordId);
                    //    await InsertTransaction(conn, (MySqlTransaction)transaction, model.Date, model.AccountId.ToString(), 0, model.EstimatedBudget, recordId);
                    //}


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
            catch (Exception ex)
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
            catch (Exception ex)
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


                int activityId = 0;

                //if (model.Id <= 0)
                //{
                // INSERT
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
                //}


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

        #region Project Site Work

        public IActionResult ProjectSiteWork()
        {
            return View();
        }
        #endregion

        #region Project Tender

        public IActionResult ProjectTender()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectTenders(int? projectId = null, int? tenderId = null)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
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
                pt.project_id AS Project_Id,
                pt.account_id AS Account_Id,
                pt.warehouse_id AS Warehouse_Id,
                pt.tender_name_id AS Tender_Name_Id,
                pt.description AS Description,
                p.code AS Code
            FROM tbl_project_tender pt
            INNER JOIN tbl_projects p ON pt.project_id = p.id
            INNER JOIN tbl_tender_names t ON pt.tender_name_id = t.id
            WHERE pt.state = 0
        ";

                var parameters = new List<MySqlParameter>();

                // Add filters only when selected
                if (projectId.HasValue && projectId.Value > 0)
                {
                    query += " AND pt.project_id = @projectId";
                    parameters.Add(new MySqlParameter("@projectId", projectId.Value));
                }

                if (tenderId.HasValue && tenderId.Value > 0)
                {
                    query += " AND pt.tender_name_id = @tenderId";
                    parameters.Add(new MySqlParameter("@tenderId", tenderId.Value));
                }

                query += " ORDER BY pt.date DESC";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                var list = new List<object>();
                int sn = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new
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

                return Ok(new { status = true, data = list });
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
                       ts.total As Total,
                       SUM(ts.amount) OVER() AS TotalAmount
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
                        Total = reader["Total"] != DBNull.Value ? Convert.ToDecimal(reader["Total"]) : 0,
                        TotalAmount = reader["TotalAmount"] != DBNull.Value ? Convert.ToDecimal(reader["TotalAmount"]) : 0
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
        public async Task<IActionResult> SaveOrUpdateEstimation([FromBody] GenerateEstimateRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            // 🔹 Validation: Required fields
            if (model.Id <= 0)
                return BadRequest(new { status = false, message = "Tender ID is required" });

            if (model.ProjectId <= 0)
                return BadRequest(new { status = false, message = "Please select a Project" });

            if (model.TenderNameId <= 0)
                return BadRequest(new { status = false, message = "Please select a Tender" });

            if (model.WarehouseId <= 0)
                return BadRequest(new { status = false, message = "Please select a Warehouse" });

            if (model.Items == null || model.Items.Count == 0)
                return BadRequest(new { status = false, message = "Please add at least one item" });

            if (string.IsNullOrEmpty(model.TotalAmount) || decimal.Parse(model.TotalAmount) == 0)
                return BadRequest(new { status = false, message = "Total must be bigger than zero" });

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

                    string updateTenderQuery = @"
                UPDATE tbl_project_tender 
                SET modified_by = @modifiedBy, 
                    modified_date = @modifiedDate,
                    date = @date, 
                    project_id = @projectId, 
                    submission_date = @submissionDate, 
                    description = @description,
                    fees = @fees,
                    amount = @amount,
                    warehouse_id = @warehouseId,
                    account_id = @accountId,
                    tender_name_id = @tenderNameId,
                    estimate_status = 1 
                WHERE id = @id;";

                    await using var updateCmd = new MySqlCommand(updateTenderQuery, conn, transaction);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);
                    updateCmd.Parameters.AddWithValue("@date", model.Date);
                    updateCmd.Parameters.AddWithValue("@submissionDate", model.SubmissionDate);
                    updateCmd.Parameters.AddWithValue("@projectId", model.ProjectId);
                    updateCmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                    updateCmd.Parameters.AddWithValue("@accountId", model.AccountId);
                    updateCmd.Parameters.AddWithValue("@tenderNameId", model.TenderNameId);
                    updateCmd.Parameters.AddWithValue("@fees", model.Fees ?? "0");
                    updateCmd.Parameters.AddWithValue("@amount", model.TotalAmount);
                    updateCmd.Parameters.AddWithValue("@description", model.Description ?? "");
                    updateCmd.Parameters.AddWithValue("@modifiedBy", userId);
                    updateCmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now.Date);

                    await updateCmd.ExecuteNonQueryAsync();

                    // 🔹 Delete existing related records
                    string deleteQuery = @"
                DELETE FROM tbl_project_tender_details WHERE tender_id = @tenderId;
                DELETE FROM tbl_items_boq_details WHERE ref_id IN (SELECT id FROM tbl_items_boq WHERE ref_id = @tenderId);
                DELETE FROM tbl_item_assembly_bos WHERE assembly_id IN (SELECT id FROM tbl_items_boq WHERE ref_id = @tenderId);
                DELETE FROM tbl_items_boq WHERE ref_id = @tenderId;
                DELETE FROM tbl_item_transaction WHERE type = 'Project Tender' AND item_id = @tenderId;
                DELETE FROM tbl_transaction WHERE type = 'Project Tender' AND transaction_id = @tenderId;
                DELETE FROM tbl_item_card_details WHERE trans_type = 'Project Tender' AND trans_no = @tenderId;";

                    await using var deleteCmd = new MySqlCommand(deleteQuery, conn, transaction);
                    deleteCmd.Parameters.AddWithValue("@tenderId", model.Id);
                    await deleteCmd.ExecuteNonQueryAsync();

                    // 🔹 Get next item code
                    string getNextCodeQuery = "SELECT IFNULL(MAX(CAST(code AS UNSIGNED)), 0) + 1 AS next_code FROM tbl_items;";
                    await using var codeCmd = new MySqlCommand(getNextCodeQuery, conn, transaction);
                    int itemCode = Convert.ToInt32(await codeCmd.ExecuteScalarAsync() ?? 0);

                    string refSr = "";
                    int assemblyItemId = 0;

                    // 🔹 Process each item
                    foreach (var item in model.Items)
                    {
                        // Validate and set defaults
                        string sr = item.Sr ?? "";
                        string description = item.Name ?? "";
                        string rate = string.IsNullOrEmpty(item.Rate) ? "0" : item.Rate;
                        string qty = string.IsNullOrEmpty(item.Qty) ? "0" : item.Qty;
                        string amount = string.IsNullOrEmpty(item.Amount) ? "0" : item.Amount;
                        string marginAmount = string.IsNullOrEmpty(item.MarginAmount) ? "0" : item.MarginAmount;
                        string marginPercentage = string.IsNullOrEmpty(item.MarginPercentage) ? "0" : item.MarginPercentage;
                        string length = string.IsNullOrEmpty(item.Length) ? "0" : item.Length;
                        string width = string.IsNullOrEmpty(item.Width) ? "0" : item.Width;
                        string thick = string.IsNullOrEmpty(item.Thick) ? "0" : item.Thick;
                        string note = item.Note ?? " ";
                        string unit = item.Unit ?? " ";

                        if (string.IsNullOrEmpty(sr) || string.IsNullOrEmpty(description))
                            continue;

                        // 🔹 Insert into tbl_items_boq
                        string insertBoqQuery = @"
                    INSERT INTO tbl_items_boq(sr, ref_id, type, name, unit_name, qty, price, amount, length, width, thickness, note)
                    VALUES(@sr, @tenderId, @type, @itemName, @unit, @qty, @rate, @amount, @length, @width, @thickness, @note); 
                    SELECT LAST_INSERT_ID();";

                        await using var boqCmd = new MySqlCommand(insertBoqQuery, conn, transaction);
                        boqCmd.Parameters.AddWithValue("@sr", sr);
                        boqCmd.Parameters.AddWithValue("@tenderId", model.Id);
                        boqCmd.Parameters.AddWithValue("@type", "BOQ");
                        boqCmd.Parameters.AddWithValue("@itemName", description);
                        boqCmd.Parameters.AddWithValue("@qty", qty);
                        boqCmd.Parameters.AddWithValue("@unit", unit);
                        boqCmd.Parameters.AddWithValue("@rate", rate);
                        boqCmd.Parameters.AddWithValue("@amount", amount);
                        boqCmd.Parameters.AddWithValue("@length", length);
                        boqCmd.Parameters.AddWithValue("@width", width);
                        boqCmd.Parameters.AddWithValue("@thickness", thick);
                        boqCmd.Parameters.AddWithValue("@note", note);

                        int boqItemId = Convert.ToInt32(await boqCmd.ExecuteScalarAsync());

                        // 🔹 Insert into tbl_project_tender_details
                        string insertDetailsQuery = @"
                    INSERT INTO tbl_project_tender_details 
                    (sr, tender_id, item_id, qty, unit_id, rate, amount, length, width, thickness, note, margin_percentage, margin_amount)
                    VALUES (@sr, @tenderId, @itemId, @qty, @unitId, @rate, @amount, @length, @width, @thickness, @note, @marginPercentage, @marginAmount);";

                        await using var detailsCmd = new MySqlCommand(insertDetailsQuery, conn, transaction);
                        detailsCmd.Parameters.AddWithValue("@sr", sr);
                        detailsCmd.Parameters.AddWithValue("@tenderId", model.Id);
                        detailsCmd.Parameters.AddWithValue("@itemId", boqItemId);
                        detailsCmd.Parameters.AddWithValue("@qty", qty);
                        detailsCmd.Parameters.AddWithValue("@unitId", "0");
                        detailsCmd.Parameters.AddWithValue("@rate", rate);
                        detailsCmd.Parameters.AddWithValue("@amount", amount);
                        detailsCmd.Parameters.AddWithValue("@length", length);
                        detailsCmd.Parameters.AddWithValue("@width", width);
                        detailsCmd.Parameters.AddWithValue("@thickness", thick);
                        detailsCmd.Parameters.AddWithValue("@note", note);
                        detailsCmd.Parameters.AddWithValue("@marginPercentage", marginPercentage);
                        detailsCmd.Parameters.AddWithValue("@marginAmount", marginAmount);

                        await detailsCmd.ExecuteNonQueryAsync();

                        // 🔹 Check if SR is alphabetic (Assembly Item)
                        if (!string.IsNullOrEmpty(sr) && System.Text.RegularExpressions.Regex.IsMatch(sr, @"^[A-Za-z]+$"))
                        {
                            refSr = sr;

                            // 🔹 Insert Assembly Item into tbl_items
                            string insertAssemblyItemQuery = @"
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
                        ); 
                        SELECT LAST_INSERT_ID();";

                            await using var assemblyCmd = new MySqlCommand(insertAssemblyItemQuery, conn, transaction);
                            assemblyCmd.Parameters.AddWithValue("@code", itemCode.ToString());
                            assemblyCmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                            assemblyCmd.Parameters.AddWithValue("@type", "13 - Inventory Assembly");
                            assemblyCmd.Parameters.AddWithValue("@category", 0);
                            assemblyCmd.Parameters.AddWithValue("@name", description);
                            assemblyCmd.Parameters.AddWithValue("@unit_id", "0");
                            assemblyCmd.Parameters.AddWithValue("@barcode", "");
                            assemblyCmd.Parameters.AddWithValue("@cost_price", rate);
                            assemblyCmd.Parameters.AddWithValue("@cogs_account_id", model.COGSAccountId);
                            assemblyCmd.Parameters.AddWithValue("@vendor_id", 0);
                            assemblyCmd.Parameters.AddWithValue("@sales_price", "0");
                            assemblyCmd.Parameters.AddWithValue("@income_account_id", model.IncomeAccountId);
                            assemblyCmd.Parameters.AddWithValue("@asset_account_id", model.AssetAccountId);
                            assemblyCmd.Parameters.AddWithValue("@min_amount", 0);
                            assemblyCmd.Parameters.AddWithValue("@max_amount", 0);
                            assemblyCmd.Parameters.AddWithValue("@on_hand", qty);
                            assemblyCmd.Parameters.AddWithValue("@method", "fifo");
                            assemblyCmd.Parameters.AddWithValue("@total_value", 0);
                            assemblyCmd.Parameters.AddWithValue("@date", model.Date);
                            assemblyCmd.Parameters.AddWithValue("@img", "");
                            assemblyCmd.Parameters.AddWithValue("@active", 0);
                            assemblyCmd.Parameters.AddWithValue("@state", 0);
                            assemblyCmd.Parameters.AddWithValue("@created_By", userId);
                            assemblyCmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                            assemblyCmd.Parameters.AddWithValue("@Item_type", "Inventory");

                            assemblyItemId = Convert.ToInt32(await assemblyCmd.ExecuteScalarAsync());
                            itemCode++;

                        }
                        else
                        {
                            // 🔹 Sub-item with Assembly Items (WinForms ELSE block)
                            if (!string.IsNullOrEmpty(model.AssemblyJson))
                            {
                                try
                                {
                                    var assemblyData = JsonConvert.DeserializeObject<Dictionary<string, List<AssemblyItemRequest>>>(model.AssemblyJson);

                                    // 🔹 Loop through ALL assembly data, not just for current description
                                    if (assemblyData != null && assemblyData.Count > 0)
                                    {
                                        // Process each assembly group
                                        foreach (var assemblyGroup in assemblyData)
                                        {
                                            string assemblyName = assemblyGroup.Key;  
                                            List<AssemblyItemRequest> assemblyItems = assemblyGroup.Value;

                                            if (assemblyItems != null && assemblyItems.Count > 0)
                                            {
                                                int subId = 0;
                                                foreach (var assemblyItem in assemblyItems)
                                                {
                                                    // 🔹 Create AssemblyItemModel using CURRENT ROW data
                                                    var assemblyModel = new
                                                    {
                                                        Code = refSr + (subId + 1),
                                                        Name = description,           
                                                        Cost = decimal.Parse(rate),   
                                                        Qty = decimal.Parse(qty),     
                                                        Total = decimal.Parse(amount), 
                                                        AssetAccountId = assemblyItem.AssetAccountId,
                                                        COGSAccountId = assemblyItem.COGSAccountId,
                                                        IncomeAccountId = assemblyItem.IncomeAccountId,
                                                        VendorAccountId = assemblyItem.VendorAccountId
                                                    };

                                                    // 🔹 Insert into tbl_items_boq_details
                                                    string insertBoqDetailsQuery = @"
                                INSERT INTO tbl_items_boq_details(
                                    code, warehouse_id, type, category_id, name, unit_id, barcode, cost_price, 
                                    cogs_account_id, vendor_id, sales_price, income_account_id, asset_account_id, 
                                    min_amount, max_amount, on_hand, method, total_value, date, img, active, state, 
                                    created_By, created_date, ref_id) 
                                VALUES (
                                    @code, @warehouse_id, @type, @category, @name, @unit_id, @barcode, @cost_price, 
                                    @cogs_account_id, @vendor_id, @sales_price, @income_account_id, @asset_account_id, 
                                    @min_amount, @max_amount, @on_hand, @method, @total_value, @date, @img, @active, @state, 
                                    @created_By, @created_date, @refId); 
                                SELECT LAST_INSERT_ID();";

                                                    await using var boqDetailsCmd = new MySqlCommand(insertBoqDetailsQuery, conn, transaction);
                                                    boqDetailsCmd.Parameters.AddWithValue("@code", assemblyModel.Code);
                                                    boqDetailsCmd.Parameters.AddWithValue("@warehouse_id", 0);
                                                    boqDetailsCmd.Parameters.AddWithValue("@type", "13 - Inventory Assembly");
                                                    boqDetailsCmd.Parameters.AddWithValue("@category", 0);
                                                    boqDetailsCmd.Parameters.AddWithValue("@name", assemblyModel.Name);
                                                    boqDetailsCmd.Parameters.AddWithValue("@unit_id", 0);
                                                    boqDetailsCmd.Parameters.AddWithValue("@barcode", "");
                                                    boqDetailsCmd.Parameters.AddWithValue("@cost_price", assemblyModel.Cost);
                                                    boqDetailsCmd.Parameters.AddWithValue("@cogs_account_id", assemblyModel.COGSAccountId);
                                                    boqDetailsCmd.Parameters.AddWithValue("@vendor_id", assemblyModel.VendorAccountId);
                                                    boqDetailsCmd.Parameters.AddWithValue("@sales_price", 0);
                                                    boqDetailsCmd.Parameters.AddWithValue("@income_account_id", assemblyModel.IncomeAccountId);
                                                    boqDetailsCmd.Parameters.AddWithValue("@asset_account_id", assemblyModel.AssetAccountId);
                                                    boqDetailsCmd.Parameters.AddWithValue("@min_amount", 0);
                                                    boqDetailsCmd.Parameters.AddWithValue("@max_amount", 0);
                                                    boqDetailsCmd.Parameters.AddWithValue("@on_hand", assemblyModel.Qty);
                                                    boqDetailsCmd.Parameters.AddWithValue("@method", "fifo");
                                                    boqDetailsCmd.Parameters.AddWithValue("@total_value", 0);
                                                    boqDetailsCmd.Parameters.AddWithValue("@date", model.Date);
                                                    boqDetailsCmd.Parameters.AddWithValue("@img", "");
                                                    boqDetailsCmd.Parameters.AddWithValue("@active", 0);
                                                    boqDetailsCmd.Parameters.AddWithValue("@state", 0);
                                                    boqDetailsCmd.Parameters.AddWithValue("@created_By", userId);
                                                    boqDetailsCmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                                                    boqDetailsCmd.Parameters.AddWithValue("@refId", boqItemId.ToString());

                                                    int assemblyId = Convert.ToInt32(await boqDetailsCmd.ExecuteScalarAsync());

                                                    // 🔹 Insert into tbl_items (Inventory Part)
                                                    string insertInventoryPartQuery = @"
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
                                ); 
                                SELECT LAST_INSERT_ID();";

                                                    await using var inventoryPartCmd = new MySqlCommand(insertInventoryPartQuery, conn, transaction);
                                                    inventoryPartCmd.Parameters.AddWithValue("@code", itemCode);
                                                    inventoryPartCmd.Parameters.AddWithValue("@warehouseId", model.WarehouseId);
                                                    inventoryPartCmd.Parameters.AddWithValue("@type", "11 - Inventory Part");
                                                    inventoryPartCmd.Parameters.AddWithValue("@category", 0);
                                                    inventoryPartCmd.Parameters.AddWithValue("@name", assemblyModel.Name);
                                                    inventoryPartCmd.Parameters.AddWithValue("@unit_id", 0);
                                                    inventoryPartCmd.Parameters.AddWithValue("@barcode", "");
                                                    inventoryPartCmd.Parameters.AddWithValue("@cost_price", assemblyModel.Cost);
                                                    inventoryPartCmd.Parameters.AddWithValue("@cogs_account_id", assemblyModel.COGSAccountId);
                                                    inventoryPartCmd.Parameters.AddWithValue("@vendor_id", assemblyModel.VendorAccountId);
                                                    inventoryPartCmd.Parameters.AddWithValue("@sales_price", "0");
                                                    inventoryPartCmd.Parameters.AddWithValue("@income_account_id", assemblyModel.IncomeAccountId);
                                                    inventoryPartCmd.Parameters.AddWithValue("@asset_account_id", assemblyModel.AssetAccountId);
                                                    inventoryPartCmd.Parameters.AddWithValue("@min_amount", 0);
                                                    inventoryPartCmd.Parameters.AddWithValue("@max_amount", 0);
                                                    inventoryPartCmd.Parameters.AddWithValue("@on_hand", assemblyModel.Qty);
                                                    inventoryPartCmd.Parameters.AddWithValue("@method", "fifo");
                                                    inventoryPartCmd.Parameters.AddWithValue("@total_value", 0);
                                                    inventoryPartCmd.Parameters.AddWithValue("@date", model.Date);
                                                    inventoryPartCmd.Parameters.AddWithValue("@img", "");
                                                    inventoryPartCmd.Parameters.AddWithValue("@active", 0);
                                                    inventoryPartCmd.Parameters.AddWithValue("@state", 0);
                                                    inventoryPartCmd.Parameters.AddWithValue("@created_By", userId);
                                                    inventoryPartCmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                                                    inventoryPartCmd.Parameters.AddWithValue("@Item_type", "Inventory");

                                                    var invResult = await inventoryPartCmd.ExecuteScalarAsync();
                                                    int itemIdOf = 0;
                                                    if (invResult != null && invResult != DBNull.Value && Convert.ToInt32(invResult) > 0)
                                                    {
                                                        itemIdOf = Convert.ToInt32(invResult);
                                                    }

                                                    // 🔹 Insert assembly relationships
                                                    string insertAssemblyQuery = @"
                                INSERT INTO tbl_item_assembly_bos(assembly_id, item_id, qty) 
                                VALUES (@assembly_id, @item_id, @qty);
                                INSERT INTO tbl_item_assembly(assembly_id, item_id, qty) 
                                VALUES (@assembly_item_id, @itemId, @qty);";

                                                    await using var assemblyRelCmd = new MySqlCommand(insertAssemblyQuery, conn, transaction);
                                                    assemblyRelCmd.Parameters.AddWithValue("@assembly_id", boqItemId.ToString());
                                                    assemblyRelCmd.Parameters.AddWithValue("@assembly_item_id", itemIdOf.ToString());
                                                    assemblyRelCmd.Parameters.AddWithValue("@item_id", assemblyId.ToString());
                                                    assemblyRelCmd.Parameters.AddWithValue("@itemId", assemblyItemId.ToString());
                                                    assemblyRelCmd.Parameters.AddWithValue("@qty", assemblyModel.Qty);

                                                    await assemblyRelCmd.ExecuteNonQueryAsync();

                                                    // 🔹 Insert item transaction if qty > 0
                                                    if (assemblyModel.Qty > 0)
                                                    {
                                                        await InsertItemTransactionAsync(conn, transaction, assemblyModel.Qty, model.Date,
                                                            assemblyModel.Cost, model.Id.ToString(), itemIdOf.ToString());

                                                        await InsertItemJournalAsync(conn, transaction, assemblyModel.Qty, model.Date,
                                                            itemCode.ToString(), assemblyModel.Cost, model.Id.ToString(), userId);
                                                    }

                                                    itemCode++;
                                                    subId++;
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error parsing AssemblyJson: {ex.Message}");
                                }
                            }
                        }
                    }


                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        status = true,
                        message = "Estimate generated successfully",
                        id = model.Id
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
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

        // 🔹 Helper Methods

        private async Task InsertItemTransactionAsync(MySqlConnection conn, MySqlTransaction transaction,
            decimal qty, DateTime date, decimal cost, string tenderId, string itemId)
        {
            string insertTransQuery = @"
        INSERT INTO tbl_item_transaction 
        (date, type, reference, item_id, cost_price, qty_in, sales_price, qty_out, qty_inc, description, warehouse_id) 
        VALUES (@date, @type, @reference, @itemId, @costPrice, @qtyIn, @salesPrice, @qtyOut, @qtyInc, @description, @warehouseId);";

            await using var transCmd = new MySqlCommand(insertTransQuery, conn, transaction);
            transCmd.Parameters.AddWithValue("@date", date);
            transCmd.Parameters.AddWithValue("@type", "Project Tender");
            transCmd.Parameters.AddWithValue("@reference", tenderId);
            transCmd.Parameters.AddWithValue("@itemId", itemId);
            transCmd.Parameters.AddWithValue("@costPrice", cost);
            transCmd.Parameters.AddWithValue("@qtyIn", qty);
            transCmd.Parameters.AddWithValue("@salesPrice", "0");
            transCmd.Parameters.AddWithValue("@qtyOut", "0");
            transCmd.Parameters.AddWithValue("@qtyInc", qty);
            transCmd.Parameters.AddWithValue("@description", "Project Opening Balance");
            transCmd.Parameters.AddWithValue("@warehouseId", "0");

            await transCmd.ExecuteNonQueryAsync();

            await UpdateOnHandItemAsync(conn, transaction, itemId);
            await AddItemCardDetailsAsync(conn, transaction, date, "Project Tender", tenderId, itemId,
                cost.ToString(), qty.ToString(), "0", "0", qty.ToString(), "Project Opening Balance", "0");
        }

        private async Task UpdateOnHandItemAsync(MySqlConnection conn, MySqlTransaction transaction, string itemId)
        {
            string updateQuery = @"
        UPDATE tbl_items 
        SET on_hand = (SELECT SUM(qty_in - qty_out) FROM tbl_item_transaction WHERE item_id = @itemId) 
        WHERE id = @itemId";

            await using var cmd = new MySqlCommand(updateQuery, conn, transaction);
            cmd.Parameters.AddWithValue("@itemId", itemId);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task AddItemCardDetailsAsync(MySqlConnection conn, MySqlTransaction transaction,
            DateTime date, string type, string reference, string itemId, string costPrice, string qtyIn,
            string salesPrice, string qtyOut, string qtyInc, string description, string warehouseId)
        {
            string invoiceNo = "INV-" + reference;
            string transNo = reference;
            string transType = type;
            decimal qtyBalance = 0;
            decimal debit = 0;
            decimal credit = 0;
            decimal price = decimal.Parse(costPrice);
            decimal balance = 0;
            decimal _qtyIn = 0;
            decimal _qtyOut = 0;

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
            string getBalanceQuery = @"
        SELECT IFNULL(SUM(qty_in-qty_out), 0) as QtyBalance,
               IFNULL(SUM(debit-credit), 0) as Balance
        FROM tbl_item_card_details 
        WHERE itemId = @itemId";

            await using var balCmd = new MySqlCommand(getBalanceQuery, conn, transaction);
            balCmd.Parameters.AddWithValue("@itemId", itemId);

            await using var reader = await balCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                decimal _QtyBalance = reader.GetDecimal("QtyBalance");
                decimal _Balance = reader.GetDecimal("Balance");
                qtyBalance = _QtyBalance + (_qtyIn - _qtyOut);
                balance = _Balance + (debit - credit);
            }
            await reader.CloseAsync();

            // Insert card details
            string insertCardQuery = @"
        INSERT INTO tbl_item_card_details (
            itemId, date, wharehouse_id, inv_no, trans_no, trans_type, description,
            price, qty_in, qty_out, qty_balance, debit, credit, balance, fifo_qty, fifo_cost
        ) VALUES (
            @itemId, @date, @wharehouse_id, @inv_no, @trans_no, @trans_type, @description,
            @price, @qty_in, @qty_out, @qty_balance, @debit, @credit, @balance, @fifo_qty, @fifo_cost
        );";

            await using var cardCmd = new MySqlCommand(insertCardQuery, conn, transaction);
            cardCmd.Parameters.AddWithValue("@itemId", itemId);
            cardCmd.Parameters.AddWithValue("@date", date);
            cardCmd.Parameters.AddWithValue("@wharehouse_id", warehouseId);
            cardCmd.Parameters.AddWithValue("@inv_no", invoiceNo);
            cardCmd.Parameters.AddWithValue("@trans_no", transNo);
            cardCmd.Parameters.AddWithValue("@trans_type", transType);
            cardCmd.Parameters.AddWithValue("@description", description);
            cardCmd.Parameters.AddWithValue("@price", price);
            cardCmd.Parameters.AddWithValue("@qty_in", qtyIn);
            cardCmd.Parameters.AddWithValue("@qty_out", qtyOut);
            cardCmd.Parameters.AddWithValue("@qty_balance", qtyBalance);
            cardCmd.Parameters.AddWithValue("@debit", debit);
            cardCmd.Parameters.AddWithValue("@credit", credit);
            cardCmd.Parameters.AddWithValue("@balance", balance);
            cardCmd.Parameters.AddWithValue("@fifo_qty", 0);
            cardCmd.Parameters.AddWithValue("@fifo_cost", 0);

            await cardCmd.ExecuteNonQueryAsync();
        }

        private async Task InsertItemJournalAsync(MySqlConnection conn, MySqlTransaction transaction,
     decimal qty, DateTime date, string itemCode, decimal cost, string tenderId, int userId)
        {
            try
            {
                decimal totalValue = qty * cost;

                // Get default accounts from tbl_coa_config
                int inventoryAccountId = await SelectDefaultLevelAccountAsync(conn, transaction, "Inventory");
                int equityAccountId = await SelectDefaultLevelAccountAsync(conn, transaction, "Opening Balance Equity");

                // Insert Inventory Account Entry
                await InsertTransactionEntryAsync(conn, transaction, date, inventoryAccountId.ToString(),
                    totalValue.ToString(), "0", tenderId, tenderId, "Project Tender",
                    $"Project Opening Balance - Item Code - {itemCode}", userId, DateTime.Now.Date);

                // Insert Opening Balance Equity Entry
                await InsertTransactionEntryAsync(conn, transaction, date, equityAccountId.ToString(),
                    "0", totalValue.ToString(), tenderId, "0", "Project Tender",
                    $"Project Opening Balance Equity - Item Code - {itemCode}", userId, DateTime.Now.Date);
            }
            catch(Exception ex)
            {
                throw ex;
            }
            
        }
        private async Task<int> SelectDefaultLevelAccountAsync(MySqlConnection conn, MySqlTransaction transaction, string category)
        {
            string query = "SELECT category, account_id FROM tbl_coa_config WHERE category = @category";

            await using var cmd = new MySqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@category", category);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return Convert.ToInt32(reader["account_id"]);
            }

            return 0;
        }
        private async Task InsertTransactionEntryAsync(MySqlConnection conn, MySqlTransaction transaction,
            DateTime date, string accountId, string debit, string credit, string transactionId,
            string humId, string type, string description, int createdBy, DateTime createdDate)
        {
            string insertQuery = @"
        INSERT INTO tbl_transaction 
        (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state) 
        VALUES (@date, @accountId, @debit, @credit, @transactionId, @hum_id, @tType, @type, @description, @createdBy, @createdDate, 0);";

            await using var cmd = new MySqlCommand(insertQuery, conn, transaction);
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

            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        //#region Project Assembly

        //[ApiController]
        //[Route("api/assembly")]
        //public class AssemblyController : ControllerBase
        //{
        //    [HttpPost("save")]
        //    public IActionResult SaveAssembly([FromBody] AssemblySaveRequest model)
        //    {
        //        if (model == null)
        //            return Ok(new { status = false, message = "Invalid request" });

        //        if (string.IsNullOrWhiteSpace(model.Name))
        //            return Ok(new { status = false, message = "Enter Item Name First." });

        //        if (model.Items == null || !model.Items.Any())
        //            return Ok(new { status = false, message = "Item Assembly can't be empty." });

        //        for (int i = 0; i < model.Items.Count; i++)
        //        {
        //            var item = model.Items[i];

        //            if (string.IsNullOrWhiteSpace(item.Code) ||
        //                string.IsNullOrWhiteSpace(item.Name) ||
        //                item.Qty <= 0)
        //            {
        //                return Ok(new
        //                {
        //                    status = false,
        //                    message = $"Item Assembly can't be 0 or empty (Row {i + 1})"
        //                });
        //            }
        //        }

        //        // === INSERT ===
        //        if (model.Id == 0)
        //        {
        //            AssemblyItemManager.RemoveItemByRefId(model.RefId);

        //            int count = 1;
        //            foreach (var row in model.Items)
        //            {
        //                AssemblyItemManager.AddItem(new AssemblyItemModel
        //                {
        //                    ItemId = count++,
        //                    RefId = model.RefId,
        //                    No = "",
        //                    Code = row.Code,
        //                    Name = row.Name,
        //                    Cost = row.Cost,
        //                    Qty = row.Qty,
        //                    Total = row.Total,
        //                    AssetAccountId = model.AssetAccountId,
        //                    IncomeAccountId = model.IncomeAccountId,
        //                    VendorAccountId = model.VendorAccountId,
        //                    COGSAccountId = model.COGSAccountId
        //                });
        //            }

        //            return Ok(new
        //            {
        //                status = true,
        //                message = "Assembly Items saved successfully"
        //            });
        //        }
        //        // === UPDATE ===
        //        else
        //        {
        //            AssemblyItemManager.RemoveItemByRefId(model.RefId);

        //            int count = 1;
        //            foreach (var row in model.Items)
        //            {
        //                AssemblyItemManager.AddItem(new AssemblyItemModel
        //                {
        //                    ItemId = count++,
        //                    RefId = model.RefId,
        //                    No = "",
        //                    Code = row.Code,
        //                    Name = row.Name,
        //                    Cost = row.Cost,
        //                    Qty = row.Qty,
        //                    Total = row.Total,
        //                    AssetAccountId = model.AssetAccountId,
        //                    IncomeAccountId = model.IncomeAccountId,
        //                    VendorAccountId = model.VendorAccountId,
        //                    COGSAccountId = model.COGSAccountId
        //                });
        //            }

        //            return Ok(new
        //            {
        //                status = true,
        //                message = "Assembly Items updated successfully"
        //            });
        //        }
        //    }
        //}
        //public static class AssemblyItemManager
        //{
        //    private static List<AssemblyItemModel> _itemsList = new();

        //    public static List<AssemblyItemModel> GetItemsList()
        //        => _itemsList;

        //    public static List<AssemblyItemModel> GetItemsListWhere(string refId)
        //        => _itemsList.Where(x => x.RefId == refId).ToList();

        //    public static void AddItem(AssemblyItemModel item)
        //        => _itemsList.Add(item);

        //    public static void RemoveItemByRefId(string refId)
        //        => _itemsList.RemoveAll(x => x.RefId == refId);

        //    public static void ClearItemsList()
        //        => _itemsList.Clear();
        //}


        //#endregion

//        #region Project Planning

//        public IActionResult ProjectPlanning()
//        {
//            return View();
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetProjectPlanning(
//           int? projectId = null,
//           string? projectStatus = null,
//           string? projectType = null)
//        {
//            try
//            {
//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                string query = @"
//            SELECT 
//                ROW_NUMBER() OVER (ORDER BY p.date) AS SN,
//                p.date AS Date,
//                p.id,
//                p.id AS `P NO`,
//                CONCAT(pr.code, ' - ', pr.name) AS `Project Name`,
//                p.start_date AS `Start Date`,
//                p.end_date AS `End Date`,
//                p.status AS `Status`,
//                p.project_type AS `Project Type`,
//                p.estimated_budget AS `Est Budget`,
//                IFNULL((
//    SELECT AVG(a.progress)
//    FROM tbl_project_activity a
//    WHERE a.planning_id = p.id
//), 0) AS `Progress`,
//                p.fund_account_id As `Fund_Account_Id`,
//                p.project_id As `Project_Id`,
//                p.site As `Site`,
//                p.tender_name_id As `Tender_Name_Id` 
//            FROM tbl_project_planning p
//            INNER JOIN tbl_projects pr ON p.project_id = pr.id
//            WHERE p.state = 0";

//                var parameters = new List<MySqlParameter>();

//                // --- Apply filters only if values are provided ---
//                if (projectId.HasValue)
//                {
//                    query += " AND p.project_id = @projectId";
//                    parameters.Add(new MySqlParameter("@projectId", projectId.Value));
//                }

//                if (!string.IsNullOrEmpty(projectStatus))
//                {
//                    query += " AND p.status = @status";
//                    parameters.Add(new MySqlParameter("@status", projectStatus));
//                }

//                if (!string.IsNullOrEmpty(projectType))
//                {
//                    query += " AND p.project_type = @type";
//                    parameters.Add(new MySqlParameter("@type", projectType));
//                }

//                query += " GROUP BY p.id, p.date, p.estimated_budget;";

//                await using var cmd = new MySqlCommand(query, conn);
//                cmd.Parameters.AddRange(parameters.ToArray());

//                var projects = new List<object>();
//                int sn = 1;

//                await using var reader = await cmd.ExecuteReaderAsync();
//                while (await reader.ReadAsync())
//                {
//                    projects.Add(new
//                    {
//                        SN = sn++,
//                        Id = reader.GetInt32("Id"),
//                        Date = reader["Date"] != DBNull.Value
//                            ? Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd")
//                            : null,
//                        ProjectNo = reader["P NO"].ToString(),
//                        ProjectName = reader["Project Name"].ToString(),
//                        StartDate = reader["Start Date"] != DBNull.Value
//                            ? Convert.ToDateTime(reader["Start Date"]).ToString("yyyy-MM-dd")
//                            : null,
//                        EndDate = reader["End Date"] != DBNull.Value
//                            ? Convert.ToDateTime(reader["End Date"]).ToString("yyyy-MM-dd")
//                            : null,
//                        Status = reader["Status"].ToString(),
//                        ProjectType = reader["Project Type"].ToString(),
//                        EstBudget = reader["Est Budget"] != DBNull.Value
//                            ? Convert.ToDecimal(reader["Est Budget"])
//                            : 0,
//                        Progress = reader["Progress"].ToString(),
//                        Fund_Account_Id = reader.GetInt32("Fund_Account_Id"),
//                        Project_Id = reader.GetInt32("Project_Id"),
//                        Site = reader.GetInt32("Site"),
//                        Tender_Name_Id = reader.GetInt32("Tender_Name_Id"),
//                    });
//                }

//                return Ok(new { status = true, message = "Success", data = projects });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public async Task<IActionResult> SaveOrUpdateProjectPlanning([FromBody] ProjectPlanningRequest model)
//        {
//            if (model == null)
//                return Json(new { status = false, message = "Invalid request" });

//            if (model.ProjectId <= 0)
//                return Json(new { status = false, message = "Select Project First." });

//            if (model.EstimatedBudget <= 0)
//                return Json(new { status = false, message = "Budget Must Be Bigger Than Zero" });

//            if (model.TenderId <= 0)
//                return Json(new { status = false, message = "Tender is not initiated for this project" });

//            if (model.TenderNameId <= 0)
//                return Json(new { status = false, message = "Select Tender Name First." });

//            if (model.SiteId <= 0)
//                return Json(new { status = false, message = "Select Site Name First." });

//            try
//            {
//                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
//                if (userId <= 0)
//                    return Json(new { status = false, message = "User not logged in" });

//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();
//                await using var transaction = await conn.BeginTransactionAsync();

//                try
//                {
//                    int recordId = 0;

//                    if (model.Id > 0)
//                    {
//                        // Update existing project planning
//                        string updateSql = @"
//                    UPDATE tbl_project_planning
//                    SET modified_by=@modifiedBy, modified_date=@modifiedDate,
//                        date=@date, project_id=@projectId, location=@location,
//                        site=@site, plot_number=@plotNumber, start_date=@startDate,
//                        end_date=@endDate, status=@status, estimated_budget=@estimatedBudget,
//                        project_type=@projectType, description=@description,
//                        fund_account_id=@accountId, fund_period=@fundPeriod,
//                        assigned_team=@assignedTeam, progress=@progress
//                    WHERE id=@id;";

//                        await using var cmd = new MySqlCommand(updateSql, conn, (MySqlTransaction)transaction);
//                        cmd.Parameters.AddWithValue("@id", model.Id);
//                        cmd.Parameters.AddWithValue("@modifiedBy", userId);
//                        cmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now.Date);
//                        cmd.Parameters.AddWithValue("@date", model.Date.Date);
//                        cmd.Parameters.AddWithValue("@projectId", model.ProjectId);
//                        cmd.Parameters.AddWithValue("@location", "0");
//                        cmd.Parameters.AddWithValue("@site", model.SiteId);
//                        cmd.Parameters.AddWithValue("@plotNumber", "");
//                        cmd.Parameters.AddWithValue("@startDate", model.StartDate.Date);
//                        cmd.Parameters.AddWithValue("@endDate", model.EndDate.Date);
//                        cmd.Parameters.AddWithValue("@status", model.Status ?? "");
//                        cmd.Parameters.AddWithValue("@projectType", model.ProjectType ?? "");
//                        cmd.Parameters.AddWithValue("@estimatedBudget", model.EstimatedBudget);
//                        cmd.Parameters.AddWithValue("@description", "");
//                        cmd.Parameters.AddWithValue("@accountId", model.AccountId);
//                        cmd.Parameters.AddWithValue("@fundPeriod", "");
//                        cmd.Parameters.AddWithValue("@assignedTeam", "");
//                        cmd.Parameters.AddWithValue("@progress", 0);

//                        int affected = await cmd.ExecuteNonQueryAsync();
//                        if (affected == 0)
//                        {
//                            await transaction.RollbackAsync();
//                            return NotFound(new { status = false, message = "Project Planning not found" });
//                        }

//                        recordId = model.Id;

//                        // Delete previous transactions
//                        string deleteTransactions = @"DELETE FROM tbl_transaction WHERE t_type='Project Planning' AND transaction_id=@id;";
//                        await using var delCmd = new MySqlCommand(deleteTransactions, conn, (MySqlTransaction)transaction);
//                        delCmd.Parameters.AddWithValue("@id", recordId);
//                        await delCmd.ExecuteNonQueryAsync();
//                    }
//                    else
//                    {
//                        // Insert new project planning
//                        string insertSql = @"
//                    INSERT INTO tbl_project_planning
//                    (date, project_id, location, site, plot_number, start_date, end_date, status, estimated_budget, project_type,
//                     description, fund_account_id, fund_period, assigned_team, progress, tender_id, created_by, created_date, tender_name_id)
//                    VALUES
//                    (@date, @projectId, @location, @site, @plotNumber, @startDate, @endDate, @status, @estimatedBudget, @projectType,
//                     @description, @accountId, @fundPeriod, @assignedTeam, @progress, @tenderId, @created_by, @created_date, @tenderNameId);
//                    SELECT LAST_INSERT_ID();";

//                        await using var cmd = new MySqlCommand(insertSql, conn, (MySqlTransaction)transaction);
//                        cmd.Parameters.AddWithValue("@date", model.Date.Date);
//                        cmd.Parameters.AddWithValue("@projectId", model.ProjectId);
//                        cmd.Parameters.AddWithValue("@location", "0");
//                        cmd.Parameters.AddWithValue("@site", model.SiteId);
//                        cmd.Parameters.AddWithValue("@plotNumber", "");
//                        cmd.Parameters.AddWithValue("@startDate", DateTime.Now.Date);
//                        cmd.Parameters.AddWithValue("@endDate", DateTime.Now.Date);
//                        cmd.Parameters.AddWithValue("@status", model.Status ?? "");
//                        cmd.Parameters.AddWithValue("@projectType", model.ProjectType ?? "");
//                        cmd.Parameters.AddWithValue("@estimatedBudget", model.EstimatedBudget);
//                        cmd.Parameters.AddWithValue("@description", "");
//                        cmd.Parameters.AddWithValue("@accountId", model.AccountId);
//                        cmd.Parameters.AddWithValue("@fundPeriod", "");
//                        cmd.Parameters.AddWithValue("@assignedTeam", "");
//                        cmd.Parameters.AddWithValue("@progress", 0);
//                        cmd.Parameters.AddWithValue("@tenderId", model.TenderId);
//                        cmd.Parameters.AddWithValue("@tenderNameId", model.TenderNameId);
//                        cmd.Parameters.AddWithValue("@created_by", userId);
//                        cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);

//                        var resultObj = await cmd.ExecuteScalarAsync();
//                        recordId = resultObj != null ? Convert.ToInt32(resultObj) : 0;
//                    }

//                    // Add transactions (mirroring WinForms transactions method)
//                    if (model.EstimatedBudget > 0)
//                    {
//                        await InsertTransaction(conn, (MySqlTransaction)transaction, model.Date, model.AccountId.ToString(), model.EstimatedBudget, 0, recordId);
//                        await InsertTransaction(conn, (MySqlTransaction)transaction, model.Date, model.AccountId.ToString(), 0, model.EstimatedBudget, recordId);
//                    }

                  
//                    await transaction.CommitAsync();

//                    return Ok(new { status = true, message = model.Id > 0 ? "Project Planning updated successfully" : "Project Planning created successfully", id = recordId });
//                }
//                catch (Exception ex)
//                {
//                    await transaction.RollbackAsync();
//                    return StatusCode(500, new { status = false, message = "Error: " + ex.Message });
//                }
//            }
//            catch (Exception exOuter)
//            {
//                return StatusCode(500, new { status = false, message = "Error: " + exOuter.Message });
//            }
//        }

//        private async Task InsertTransaction(MySqlConnection conn, MySqlTransaction trx, DateTime date, string accountId, decimal debit, decimal credit, int transactionId)
//        {
//            string insert = @"
//        INSERT INTO tbl_transaction (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state)
//        VALUES (@date, @accountId, @debit, @credit, @transactionId, 0, 'Project Planning', 'PROJECT PLANNING', 'Project Planning Invoice NO.', @createdBy, @createdDate, 0);";

//            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

//            await using var cmd = new MySqlCommand(insert, conn, trx);
//            cmd.Parameters.AddWithValue("@date", date);
//            cmd.Parameters.AddWithValue("@accountId", accountId);
//            cmd.Parameters.AddWithValue("@debit", debit);
//            cmd.Parameters.AddWithValue("@credit", credit);
//            cmd.Parameters.AddWithValue("@transactionId", transactionId);
//            cmd.Parameters.AddWithValue("@createdBy", userId);
//            cmd.Parameters.AddWithValue("@createdDate", DateTime.Now.Date);
//            await cmd.ExecuteNonQueryAsync();
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetProjectTenderDetails(int tenderId)
//        {
//            try
//            {
//                if (tenderId == 0)
//                    return BadRequest(new { status = false, message = "Tender ID is required" });

//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                string query = @"
//            SELECT 
//                ptd.id,
//                ptd.sr,
//                ib.name,
//                ib.id AS item_id,
//                ib.type,
//                ib.unit_name,
//                ptd.start_date,
//                ptd.end_date,
//                ptd.progress,
//                ptd.assigned,
//                ptd.tender_id
//            FROM tbl_project_tender_details ptd
//            INNER JOIN tbl_items_boq ib 
//                ON ptd.tender_id = @tenderId 
//                AND ptd.item_id = ib.id";

//                await using var cmd = new MySqlCommand(query, conn);
//                cmd.Parameters.AddWithValue("@tenderId", tenderId);

//                var items = new List<object>();
//                int count = 1;

//                await using var reader = await cmd.ExecuteReaderAsync();
//                while (await reader.ReadAsync())
//                {
//                    // Format dates
//                    string startDate = reader["start_date"] != DBNull.Value
//                        ? Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd")
//                        : DateTime.Now.ToString("yyyy-MM-dd");

//                    string endDate = reader["end_date"] != DBNull.Value
//                        ? Convert.ToDateTime(reader["end_date"]).ToString("yyyy-MM-dd")
//                        : DateTime.Now.ToString("yyyy-MM-dd");

//                    // Assigned employee
//                    string empId = reader["assigned"] != DBNull.Value ? reader["assigned"].ToString() : "0";

//                    // Progress
//                    string progress = reader["progress"] != DBNull.Value
//                        ? decimal.Parse(reader["progress"].ToString()).ToString("#.##")
//                        : "0";

//                    items.Add(new
//                    {
//                        SNo = count++,
//                        Id = reader["id"].ToString(),
//                        SR = reader["sr"].ToString(),
//                        Name = reader["name"].ToString(),
//                        StartDate = startDate,
//                        EndDate = endDate,
//                        Progress = progress,
//                        Assigned = empId,
//                        ItemId = reader["item_id"].ToString(),
//                        Type = reader["type"].ToString(),
//                        UnitName = reader["unit_name"].ToString(),
//                        Tender_Id = reader.GetInt32("tender_id")
//                    });
//                }

//                return Ok(new { status = true, message = "Success", data = items });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        #region Tab Get API

//        [HttpGet]
//        public async Task<IActionResult> GetRequestedMaterial(int planningId)
//        {
//            try
//            {
//                if (planningId == 0)
//                    return BadRequest(new { status = false, message = "Planning ID is required" });

//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                string query = @"
//            SELECT 
//                rm.id,
//                rm.RequestedQty AS qty,
//                rm.RequestedDate AS date,
//                rm.unit,
//                boq.sr,
//                boq.name,
//                CASE 
//                    WHEN rm.ReceivedQty > 0 THEN 'Received' 
//                    WHEN rm.IssuedQty > 0 THEN 'Issued' 
//                    ELSE 'Requested' 
//                END AS status
//            FROM tbl_project_material_requests rm
//            INNER JOIN tbl_items_boq boq 
//                ON rm.tender_id = boq.ref_id AND rm.itemId = boq.id
//            WHERE rm.planning_id = @planningId";

//                await using var cmd = new MySqlCommand(query, conn);
//                cmd.Parameters.AddWithValue("@planningId", planningId);

//                var materialRequests = new List<object>();
//                int count = 1;

//                await using var reader = await cmd.ExecuteReaderAsync();
//                while (await reader.ReadAsync())
//                {
//                    string requestedDate = reader["date"] != DBNull.Value
//                        ? Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd")
//                        : "";

//                    materialRequests.Add(new
//                    {
//                        SNo = count++,
//                        Id = reader["id"].ToString(),
//                        PlanningId = planningId,
//                        Date = requestedDate,
//                        Name = $"{reader["sr"]} - {reader["name"]}",
//                        Unit = reader["unit"].ToString(),
//                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]).ToString("N2") : "0.00",
//                        Status = reader["status"].ToString()
//                    });
//                }

//                return Ok(new
//                {
//                    status = true,
//                    message = "Success",
//                    data = materialRequests
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetIssuedMaterial(int planningId)
//        {
//            try
//            {
//                if (planningId == 0)
//                    return BadRequest(new { status = false, message = "Planning ID is required" });

//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                string query = @"
//        SELECT 
//            rm.id,
//            rm.IssuedQty AS qty,
//            rm.IssuedDate AS date,
//            rm.unit,
//            boq.sr,
//            boq.name,
//            CASE 
//                WHEN rm.ReceivedQty > 0 THEN 'Received' 
//                WHEN rm.IssuedQty > 0 THEN 'Issued' 
//                ELSE 'Requested' 
//            END AS status
//        FROM tbl_project_material_requests rm
//        INNER JOIN tbl_items_boq boq 
//            ON rm.tender_id = boq.ref_id AND rm.itemId = boq.id
//        WHERE rm.IssuedQty > 0 AND rm.planning_id = @planningId";

//                await using var cmd = new MySqlCommand(query, conn);
//                cmd.Parameters.AddWithValue("@planningId", planningId);

//                var issuedItems = new List<object>();
//                int count = 1;

//                await using var reader = await cmd.ExecuteReaderAsync();
//                while (await reader.ReadAsync())
//                {
//                    string issuedDate = reader["date"] != DBNull.Value
//                        ? Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd")
//                        : "";

//                    issuedItems.Add(new
//                    {
//                        SNo = count++,
//                        Id = reader["id"].ToString(),
//                        PlanningId = planningId,
//                        Date = issuedDate,
//                        Name = $"{reader["sr"]} - {reader["name"]}",
//                        Unit = reader["unit"].ToString(),
//                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]).ToString("N2") : "0.00",
//                        Status = reader["status"].ToString()
//                    });
//                }

//                return Ok(new { status = true, message = "Success", data = issuedItems });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetReceivedMaterial(int planningId)
//        {
//            try
//            {
//                if (planningId == 0)
//                    return BadRequest(new { status = false, message = "Planning ID is required" });

//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                string query = @"
//        SELECT 
//            rm.id,
//            rm.ReceivedQty AS qty,
//            rm.IssuedDate AS date,
//            rm.unit,
//            boq.sr,
//            boq.name,
//            CASE 
//                WHEN rm.ReceivedQty > 0 THEN 'Received' 
//                WHEN rm.IssuedQty > 0 THEN 'Issued' 
//                ELSE 'Requested' 
//            END AS status
//        FROM tbl_project_material_requests rm
//        INNER JOIN tbl_items_boq boq 
//            ON rm.tender_id = boq.ref_id AND rm.itemId = boq.id
//        WHERE rm.ReceivedQty > 0 AND rm.planning_id = @planningId";

//                await using var cmd = new MySqlCommand(query, conn);
//                cmd.Parameters.AddWithValue("@planningId", planningId);

//                var receivedItems = new List<object>();
//                int count = 1;

//                await using var reader = await cmd.ExecuteReaderAsync();
//                while (await reader.ReadAsync())
//                {
//                    string receivedDate = reader["date"] != DBNull.Value
//                        ? Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd")
//                        : "";

//                    receivedItems.Add(new
//                    {
//                        SNo = count++,
//                        Id = reader["id"].ToString(),
//                        PlanningId = planningId,
//                        Date = receivedDate,
//                        Name = $"{reader["sr"]} - {reader["name"]}",
//                        Unit = reader["unit"].ToString(),
//                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]).ToString("N2") : "0.00",
//                        Status = reader["status"].ToString()
//                    });
//                }

//                return Ok(new { status = true, message = "Success", data = receivedItems });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetResourceData(int planningId)
//        {
//            try
//            {
//                if (planningId == 0)
//                    return BadRequest(new { status = false, message = "Planning ID is required" });

//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                string query = @"
//   SELECT 
//    pr.id,
//    pr.code,
//    pr.name,
//    r.name AS roleName,
//    pr.type,
//    pr.price_unit,
//    pr.unit_time,
//    pr.max_unit_time,
//    p.id AS PlanningId
//FROM tbl_project_resource pr
//INNER JOIN tbl_project_role r ON r.id = pr.role
//INNER JOIN tbl_project_planning p 
//    ON p.id = @planningId
//    AND FIND_IN_SET(pr.id, p.assigned_team) > 0;";

//                await using var cmd = new MySqlCommand(query, conn);
//                cmd.Parameters.AddWithValue("@planningId", planningId);

//                var resources = new List<object>();
//                int count = 1;

//                await using var reader = await cmd.ExecuteReaderAsync();
//                while (await reader.ReadAsync())
//                {
//                    resources.Add(new
//                    {
//                        SNo = count++,
//                        Id = reader["id"].ToString(),
//                        Code = reader["code"].ToString(),
//                        PlanningId = reader.GetInt32("PlanningId"),
//                        ResourceName = reader["name"].ToString(),
//                        ResourceType = reader["type"].ToString(),
//                        PrimaryRole = reader["roleName"].ToString(),
//                        DefaultUnitsTime = reader["price_unit"] != DBNull.Value ? Convert.ToDecimal(reader["price_unit"]).ToString("N2") : "0.00"
//                    });
//                }

//                return Ok(new { status = true, message = "Success", data = resources });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        #endregion

//        [HttpPost]
//        public async Task<IActionResult> SaveOrUpdateMaterialRequest([FromBody] MaterialRequest model)
//        {
//            if (model == null)
//                return BadRequest(new { status = false, message = "Invalid request" });

//            if (model.TenderId <= 0)
//                return BadRequest(new { status = false, message = "Please select a Tender" });

//            if (model.PlanningId <= 0)
//                return BadRequest(new { status = false, message = "Please select a Planning ID" });

//            if (model.Items == null || model.Items.Count == 0)
//                return BadRequest(new { status = false, message = "Please add at least one item" });

//            try
//            {
//                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
//                if (userId <= 0)
//                    return Unauthorized(new { status = false, message = "User not logged in" });

//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                // 🔹 If Id == 0 => INSERT all
//                if (model.Id == 0)
//                {
//                    foreach (var item in model.Items)
//                    {
//                        if (item.ItemId == null || item.ItemId <= 0)
//                            continue;

//                        string insertQuery = @"
//                    INSERT INTO tbl_project_material_requests
//                        (tender_id, planning_id, RequestedDate, itemId, unit, RequestedQty, IssuedQty, ReceivedQty)
//                    VALUES
//                        (@tenderId, @planningId, @requestedDate, @itemId, @unit, @qty, 0, 0);";

//                        await using var insertCmd = new MySqlCommand(insertQuery, conn);
//                        insertCmd.Parameters.AddWithValue("@tenderId", model.TenderId);
//                        insertCmd.Parameters.AddWithValue("@planningId", model.PlanningId);
//                        insertCmd.Parameters.AddWithValue("@requestedDate", model.RequestedDate);
//                        insertCmd.Parameters.AddWithValue("@itemId", item.ItemId);
//                        insertCmd.Parameters.AddWithValue("@unit", item.Unit ?? "");
//                        insertCmd.Parameters.AddWithValue("@qty", item.RequestedQty);

//                        await insertCmd.ExecuteNonQueryAsync();
//                    }

//                    return Ok(new { status = true, message = "Material requests inserted successfully" });
//                }
//                else
//                {
//                    // 🔹 If Id > 0 => UPDATE all
//                    foreach (var item in model.Items)
//                    {
//                        if (item.ItemId == null || item.ItemId <= 0)
//                            continue;

//                        // Get the record ID if passed in (optional per item)
//                        int rowId = item.Id ?? 0;

//                        if (rowId > 0)
//                        {
//                            // Update by row ID
//                            string updateQuery = @"
//                        UPDATE tbl_project_material_requests 
//                        SET RequestedDate = @requestedDate,
//                            itemId = @itemId,
//                            unit = @unit,
//                            RequestedQty = @qty
//                        WHERE id = @id;";

//                            await using var updateCmd = new MySqlCommand(updateQuery, conn);
//                            updateCmd.Parameters.AddWithValue("@requestedDate", model.RequestedDate);
//                            updateCmd.Parameters.AddWithValue("@itemId", item.ItemId);
//                            updateCmd.Parameters.AddWithValue("@unit", item.Unit ?? "");
//                            updateCmd.Parameters.AddWithValue("@qty", item.RequestedQty);
//                            updateCmd.Parameters.AddWithValue("@id", rowId);

//                            await updateCmd.ExecuteNonQueryAsync();
//                        }
//                        else
//                        {
//                            // If no ID for this row, insert as new
//                            string insertQuery = @"
//                        INSERT INTO tbl_project_material_requests
//                            (tender_id, planning_id, RequestedDate, itemId, unit, RequestedQty, IssuedQty, ReceivedQty)
//                        VALUES
//                            (@tenderId, @planningId, @requestedDate, @itemId, @unit, @qty, 0, 0);";

//                            await using var insertCmd = new MySqlCommand(insertQuery, conn);
//                            insertCmd.Parameters.AddWithValue("@tenderId", model.TenderId);
//                            insertCmd.Parameters.AddWithValue("@planningId", model.PlanningId);
//                            insertCmd.Parameters.AddWithValue("@requestedDate", model.RequestedDate);
//                            insertCmd.Parameters.AddWithValue("@itemId", item.ItemId);
//                            insertCmd.Parameters.AddWithValue("@unit", item.Unit ?? "");
//                            insertCmd.Parameters.AddWithValue("@qty", item.RequestedQty);

//                            await insertCmd.ExecuteNonQueryAsync();
//                        }
//                    }

//                    return Ok(new { status = true, message = "Material requests updated successfully" });
//                }
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = "An unexpected error occurred: " + ex.Message });
//            }
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetItemsByTenderId(int tenderId)
//        {
//            if (tenderId <= 0)
//            {
//                return BadRequest(new { status = false, message = "Invalid Tender ID" });
//            }

//            try
//            {
//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                // Query to fetch items associated with the tenderId
//                string query = @"
//            SELECT 
//                CONCAT(tbl_items_boq.sr, ' - ', tbl_items_boq.name) AS name,
//                tbl_items_boq.id,
//                tbl_items_boq.qty,
//                tbl_items_boq.unit_name 
//            FROM tbl_project_tender_details 
//            INNER JOIN tbl_items_boq 
//                ON tbl_project_tender_details.tender_id = ref_id 
//                AND tbl_project_tender_details.item_id = tbl_items_boq.id
//            WHERE tbl_project_tender_details.tender_id = @tenderId";

//                await using var cmd = new MySqlCommand(query, conn);
//                cmd.Parameters.AddWithValue("@tenderId", tenderId);

//                var dt = new DataTable();
//                using (var reader = await cmd.ExecuteReaderAsync())
//                {
//                    dt.Load(reader);
//                }

//                var items = dt.AsEnumerable()
//                    .Select(row => new
//                    {
//                        Name = row["name"].ToString(),
//                        Id = Convert.ToInt32(row["id"]),
//                        Qty = Convert.ToDecimal(row["qty"]).ToString("F2"),
//                        Unit = row["unit_name"].ToString()
//                    })
//                    .ToList();

//                return Ok(new { status = true, data = items });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = "An unexpected error occurred: " + ex.Message });
//            }
//        }

//        #region Add Dropdown API

//        [HttpGet]
//        public async Task<IActionResult> GetRequestedData(int planningId)
//        {
//            if (planningId <= 0)
//                return BadRequest(new { status = false, message = "Planning ID is required" });

//            try
//            {
//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                string query = @"
//            SELECT 
//                rm.id,
//                rm.RequestedQty AS qty,
//                rm.RequestedDate AS date,
//                rm.unit,
//                rm.itemId As ItemId,
//                boq.sr,
//                boq.name,
//                'Requested' AS status
//            FROM tbl_project_material_requests rm
//            INNER JOIN tbl_items_boq boq 
//                ON rm.tender_id = boq.ref_id AND rm.itemId = boq.id
//            WHERE rm.planning_id = @planningId
//              AND rm.RequestedDate IS NOT NULL
//              AND rm.IssuedDate IS NULL
//              AND rm.ReceivedDate IS NULL;";

//                await using var cmd = new MySqlCommand(query, conn);
//                cmd.Parameters.AddWithValue("@planningId", planningId);

//                var materialRequests = new List<object>();
//                int count = 1;

//                await using var reader = await cmd.ExecuteReaderAsync();
//                while (await reader.ReadAsync())
//                {
//                    string requestedDate = reader["date"] != DBNull.Value
//                        ? Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd")
//                        : "";

//                    materialRequests.Add(new
//                    {
//                        SNo = count++,
//                        Id = reader["id"].ToString(),
//                        PlanningId = planningId,
//                        Date = requestedDate,
//                        Name = $"{reader["sr"]} - {reader["name"]}",
//                        Unit = reader["unit"].ToString(),
//                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]).ToString("N2") : "0.00",
//                        Status = "Requested",
//                        ItemId = reader.GetInt32("ItemId")
//                    });
//                }

//                return Ok(new
//                {
//                    status = true,
//                    message = "Success",
//                    data = materialRequests
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public async Task<IActionResult> SaveOrUpdateIssueMaterial([FromBody] IssueMaterialModel model)
//        {
//            if (model == null || model.PlanningId <= 0 || model.TenderId <= 0 || model.Items == null || model.Items.Count == 0)
//                return BadRequest(new { status = false, message = "Invalid data provided" });

//            try
//            {
//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                string query = @"
//            UPDATE tbl_project_material_requests 
//            SET IssuedDate = @IssuedDate, IssuedQty = @IssuedQty
//            WHERE planning_id = @PlanningId 
//              AND tender_id = @TenderId 
//              AND itemId = @ItemId;";

//                foreach (var item in model.Items)
//                {
//                    await using var cmd = new MySqlCommand(query, conn);
//                    cmd.Parameters.AddWithValue("@ItemId", item.ItemId);
//                    cmd.Parameters.AddWithValue("@PlanningId", model.PlanningId);
//                    cmd.Parameters.AddWithValue("@TenderId", model.TenderId);
//                    cmd.Parameters.AddWithValue("@IssuedDate", model.IssueDate);
//                    cmd.Parameters.AddWithValue("@IssuedQty", item.IssuedQty);
//                    await cmd.ExecuteNonQueryAsync();

//                }

//                return Ok(new { status = true, message = "Issue Material updated successfully" });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetPendingReceivedMaterials(int planningId)
//        {
//            if (planningId <= 0)
//                return BadRequest(new { status = false, message = "Planning ID is required" });

//            try
//            {
//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                string query = @"
//            SELECT 
//                rm.id,
//                rm.RequestedQty AS qty,
//                rm.RequestedDate,
//                rm.IssuedDate,
//                rm.ReceivedDate,
//                rm.unit,
//                rm.itemId AS ItemId,
//                boq.sr,
//                boq.name,
//                'Issued' AS status
//            FROM tbl_project_material_requests rm
//            INNER JOIN tbl_items_boq boq 
//                ON rm.tender_id = boq.ref_id AND rm.itemId = boq.id
//            WHERE rm.planning_id = @planningId
//              AND rm.ReceivedDate IS NULL;";

//                await using var cmd = new MySqlCommand(query, conn);
//                cmd.Parameters.AddWithValue("@planningId", planningId);

//                var materialRequests = new List<object>();
//                int count = 1;

//                await using var reader = await cmd.ExecuteReaderAsync();
//                while (await reader.ReadAsync())
//                {
//                    materialRequests.Add(new
//                    {
//                        SNo = count++,
//                        Id = reader["id"].ToString(),
//                        PlanningId = planningId,
//                        RequestedDate = reader["RequestedDate"] != DBNull.Value
//                            ? Convert.ToDateTime(reader["RequestedDate"]).ToString("yyyy-MM-dd")
//                            : null,
//                        IssuedDate = reader["IssuedDate"] != DBNull.Value
//                            ? Convert.ToDateTime(reader["IssuedDate"]).ToString("yyyy-MM-dd")
//                            : null,
//                        ReceivedDate = reader["ReceivedDate"] != DBNull.Value
//                            ? Convert.ToDateTime(reader["ReceivedDate"]).ToString("yyyy-MM-dd")
//                            : null,
//                        Name = $"{reader["sr"]} - {reader["name"]}",
//                        Unit = reader["unit"].ToString(),
//                        Qty = reader["qty"] != DBNull.Value ? Convert.ToDecimal(reader["qty"]).ToString("N2") : "0.00",
//                        Status = reader["status"].ToString(),
//                        ItemId = reader.GetInt32("ItemId")
//                    });
//                }

//                return Ok(new
//                {
//                    status = true,
//                    message = "Success",
//                    data = materialRequests
//                });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public async Task<IActionResult> SaveOrUpdateReceiveMaterial([FromBody] ReceiveMaterialModel model)
//        {
//            if (model == null || model.PlanningId <= 0 || model.TenderId <= 0 || model.Items == null || model.Items.Count == 0)
//                return BadRequest(new { status = false, message = "Invalid data provided" });

//            try
//            {
//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                // Ensure we always use today if no date provided
//                var receiveDate = model.ReceiveDate == default ? DateTime.Today : model.ReceiveDate;

//                string query = @"
//            UPDATE tbl_project_material_requests 
//            SET ReceivedDate = @ReceivedDate, ReceivedQty = @ReceivedQty
//            WHERE planning_id = @PlanningId 
//              AND tender_id = @TenderId 
//              AND itemId = @ItemId;";

//                foreach (var item in model.Items)
//                {
//                    await using var cmd = new MySqlCommand(query, conn);
//                    cmd.Parameters.AddWithValue("@ItemId", item.ItemId);
//                    cmd.Parameters.AddWithValue("@PlanningId", model.PlanningId);
//                    cmd.Parameters.AddWithValue("@TenderId", model.TenderId);
//                    cmd.Parameters.AddWithValue("@ReceivedDate", receiveDate);
//                    cmd.Parameters.AddWithValue("@ReceivedQty", item.ReceivedQty);

//                    int affected = await cmd.ExecuteNonQueryAsync();

//                    if (affected == 0)
//                    {
//                        Console.WriteLine($"No rows updated for ItemId={item.ItemId}, PlanningId={model.PlanningId}, TenderId={model.TenderId}");
//                    }
//                }

//                return Ok(new { status = true, message = "Received material updated successfully" });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }


//        #endregion

//        [HttpGet]
//        public async Task<IActionResult> GetProjectResources(int? planningId = null)
//        {
//            try
//            {
//                // Validate session
//                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
//                if (userId <= 0)
//                    return Unauthorized(new { status = false, message = "User not logged in" });

//                // Build connection dynamically
//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                // Base query to load all resources
//                string query = @"
//            SELECT 
//                ROW_NUMBER() OVER (ORDER BY pr.id) AS SN,
//                pr.id,
//                pr.code,
//                pr.date,
//                pr.name,
//                r.name AS roleName,
//                pr.phone,
//                pr.type,
//                pr.price_unit,
//                pr.unit_time,
//                pr.max_unit_time
//            FROM tbl_project_resource pr
//            INNER JOIN tbl_project_role r ON r.id = pr.role;
//        ";

//                await using var cmd = new MySqlCommand(query, conn);
//                await using var reader = await cmd.ExecuteReaderAsync();

//                var resources = new List<dynamic>();
//                while (await reader.ReadAsync())
//                {
//                    resources.Add(new
//                    {
//                        sn = reader.GetInt32("SN"),
//                        id = reader.GetInt32("id"),
//                        code = reader["code"].ToString(),
//                        name = reader["name"].ToString(),
//                        type = reader["type"].ToString(),
//                        roleName = reader["roleName"].ToString(),
//                        unitTime = reader["unit_time"].ToString(),
//                        priceUnit = reader["price_unit"].ToString(),
//                        maxUnitTime = reader["max_unit_time"].ToString(),
//                        phone = reader["phone"].ToString(),
//                        date = Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd"),
//                        selected = false // will be updated later if planningId is provided
//                    });
//                }

//                await reader.CloseAsync();

//                // If planningId is provided, mark assigned resources as selected
//                if (planningId.HasValue)
//                {
//                    string assignedQuery = @"
//                SELECT id 
//                FROM tbl_project_resource 
//                WHERE EXISTS (
//                    SELECT 1 FROM tbl_project_planning p 
//                    WHERE p.id = @planningId 
//                    AND FIND_IN_SET(tbl_project_resource.id, p.assigned_team) > 0
//                );
//            ";

//                    await using var assignedCmd = new MySqlCommand(assignedQuery, conn);
//                    assignedCmd.Parameters.AddWithValue("@planningId", planningId.Value);

//                    var assignedIds = new List<int>();
//                    await using var assignedReader = await assignedCmd.ExecuteReaderAsync();
//                    while (await assignedReader.ReadAsync())
//                        assignedIds.Add(assignedReader.GetInt32("id"));

//                    foreach (var res in resources)
//                    {
//                        if (assignedIds.Contains((int)res.id))
//                            res.selected = true;
//                    }
//                }

//                return Ok(new { status = true, data = resources });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public async Task<IActionResult> SaveOrUpdateRole([FromBody] ProjectRoleRequest model)
//        {

//            try
//            {
//                if (model == null)
//                    return Json(new { status = false, message = "Invalid request" });

//                if (string.IsNullOrWhiteSpace(model.Name))
//                    return Json(new { status = false, message = "Please enter Role Name" });

//                try
//                {
//                    int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
//                    if (userId <= 0)
//                        return Unauthorized(new { status = false, message = "User not logged in" });

//                    var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                    {
//                        Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                    };

//                    await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                    await conn.OpenAsync();

//                    // 1️⃣ Check if role name already exists
//                    string checkQuery = "SELECT id FROM tbl_project_role WHERE name=@name";
//                    await using (var checkCmd = new MySqlCommand(checkQuery, conn))
//                    {
//                        checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
//                        var existingId = await checkCmd.ExecuteScalarAsync();

//                        if (existingId != null && (model.Id == 0 || model.Id != Convert.ToInt32(existingId)))
//                        {
//                            return Json(new { status = false, message = "Role name already in use" });
//                        }
//                    }

//                    // 2️⃣ Generate role code automatically (R1, R2, R3...)
//                    string roleCode = model.Id == 0 ? await GenerateNextRoleCode(conn) : model.Code;

//                    if (model.Id == 0)
//                    {
//                        // 3️⃣ Insert new role
//                        string insertQuery = @"
//                INSERT INTO tbl_project_role (code, name)
//                VALUES (@code, @name);
//                SELECT LAST_INSERT_ID();";

//                        await using var cmd = new MySqlCommand(insertQuery, conn);
//                        cmd.Parameters.AddWithValue("@code", roleCode);
//                        cmd.Parameters.AddWithValue("@name", model.Name.Trim());

//                        int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                      

//                        return Ok(new
//                        {
//                            status = true,
//                            message = "Role inserted successfully",
//                            id = newId,
//                            code = roleCode
//                        });
//                    }
//                    else
//                    {
//                        // 4️⃣ Update existing role
//                        string updateQuery = "UPDATE tbl_project_role SET name=@name, code=@code WHERE id=@id";
//                        await using var cmd = new MySqlCommand(updateQuery, conn);
//                        cmd.Parameters.AddWithValue("@id", model.Id);
//                        cmd.Parameters.AddWithValue("@name", model.Name.Trim());
//                        cmd.Parameters.AddWithValue("@code", roleCode);

//                        int affected = await cmd.ExecuteNonQueryAsync();
//                        if (affected == 0)
//                            return NotFound(new { status = false, message = "Role not found" });

//                        // Optional: Audit log
//                        string audit = "INSERT INTO tbl_audit_log (user_id, action, module, ref_id, description) VALUES (@user_id, @action, @module, @ref_id, @desc)";
//                        await using (var auditCmd = new MySqlCommand(audit, conn))
//                        {
//                            auditCmd.Parameters.AddWithValue("@user_id", userId);
//                            auditCmd.Parameters.AddWithValue("@action", "Update Project Role");
//                            auditCmd.Parameters.AddWithValue("@module", "Project Role");
//                            auditCmd.Parameters.AddWithValue("@ref_id", model.Id);
//                            auditCmd.Parameters.AddWithValue("@desc", "Updated Project Role: " + model.Name);
//                            await auditCmd.ExecuteNonQueryAsync();
//                        }

//                        return Ok(new
//                        {
//                            status = true,
//                            message = "Role updated successfully",
//                            id = model.Id,
//                            code = roleCode
//                        });
//                    }
//                }
//                catch (Exception ex)
//                {
//                    return StatusCode(500, new { status = false, message = ex.Message });
//                }
//            }
//            catch (Exception ex)
//            {
//                return Json(new { status = false, message = "An unexpected error occurred: " + ex.Message });
//            }
//        }

//        private async Task<string> GenerateNextRoleCode(MySqlConnection conn)
//        {
//            try
//            {
//                string query = "SELECT code FROM tbl_project_role ORDER BY id DESC LIMIT 1";
//                await using var cmd = new MySqlCommand(query, conn);
//                var lastCodeObj = await cmd.ExecuteScalarAsync();

//                if (lastCodeObj == null || string.IsNullOrWhiteSpace(lastCodeObj.ToString()))
//                    return "R1";

//                string lastCode = lastCodeObj.ToString();
//                if (int.TryParse(lastCode.Replace("R", ""), out int num))
//                    return $"R{num + 1}";

//                return "R1";
//            }
//            catch(Exception ex)
//            {
//                throw ex;
//            }
          
//        }
//        [HttpGet]
//        public async Task<IActionResult> GetRoles()
//        {
//            try
//            {
//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                string query = "SELECT id, code, name FROM tbl_project_role ORDER BY id ASC";
//                var roles = new List<object>();

//                await using (var cmd = new MySqlCommand(query, conn))
//                await using (var reader = await cmd.ExecuteReaderAsync())
//                {
//                    while (await reader.ReadAsync())
//                    {
//                        roles.Add(new
//                        {
//                            id = reader.GetInt32("id"),
//                            code = reader.GetString("code"),
//                            name = reader.GetString("name")
//                        });
//                    }
//                }

//                return Ok(new { status = true, data = roles });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public async Task<IActionResult> SaveOrUpdateProjectResource([FromBody] ProjectResourceRequest model)
//        {

//            try
//            {
//                if (model == null)
//                    return Json(new { status = false, message = "Invalid request" });

//                if (string.IsNullOrWhiteSpace(model.Role))
//                    return Json(new { status = false, message = "Role can't be empty" });

//                if (string.IsNullOrWhiteSpace(model.Name))
//                    return Json(new { status = false, message = "Name can't be empty" });

//                if (model.Type == "Labour" && (model.EmployeeId == null || model.EmployeeId <= 0))
//                    return Json(new { status = false, message = "Employee name can't be empty" });

//                try
//                {
//                    int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
//                    if (userId <= 0)
//                        return Json(new { status = false, message = "User not logged in" });

//                    var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                    {
//                        Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                    };

//                    await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                    await conn.OpenAsync();

//                    // 🔹 Auto-generate code if empty
//                    string resourceCode = string.IsNullOrEmpty(model.Code)
//                        ? await GenerateNextResourceCode(conn)
//                        : model.Code;

//                    if (model.Id == 0)
//                    {
//                        // 🟩 INSERT new resource
//                        string insertQuery = @"
//                INSERT INTO tbl_project_resource
//                (code, date, name, role, phone, type, price_unit, unit_time, max_unit_time, employee_id)
//                VALUES (@code, @date, @name, @role, @phone, @type, @priceUnit, @unitTime, @maxUnitTime, @empId);
//                SELECT LAST_INSERT_ID();";

//                        await using var cmd = new MySqlCommand(insertQuery, conn);
//                        cmd.Parameters.AddWithValue("@code", resourceCode);
//                        cmd.Parameters.AddWithValue("@date", model.Date);
//                        cmd.Parameters.AddWithValue("@name", model.Name);
//                        cmd.Parameters.AddWithValue("@role", model.Role);
//                        cmd.Parameters.AddWithValue("@phone", model.Phone ?? "");
//                        cmd.Parameters.AddWithValue("@type", model.Type ?? "Non");
//                        cmd.Parameters.AddWithValue("@priceUnit", model.PriceUnit ?? 0);
//                        cmd.Parameters.AddWithValue("@unitTime", model.UnitTime ?? 8);
//                        cmd.Parameters.AddWithValue("@maxUnitTime", model.MaxUnitTime ?? 8);
//                        cmd.Parameters.AddWithValue("@empId", model.EmployeeId ?? 0);

//                        int newId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

//                        return Ok(new
//                        {
//                            status = true,
//                            message = "Resource inserted successfully",
//                            id = newId,
//                            code = resourceCode
//                        });
//                    }
//                    else
//                    {
//                        // 🟦 UPDATE existing resource
//                        string updateQuery = @"
//                UPDATE tbl_project_resource
//                SET date = @date, name = @name, role = @role, phone = @phone, type = @type,
//                    code = @code, price_unit = @priceUnit, unit_time = @unitTime,
//                    max_unit_time = @maxUnitTime, employee_id = @empId
//                WHERE id = @id";

//                        await using var cmd = new MySqlCommand(updateQuery, conn);
//                        cmd.Parameters.AddWithValue("@id", model.Id);
//                        cmd.Parameters.AddWithValue("@code", resourceCode);
//                        cmd.Parameters.AddWithValue("@date", model.Date);
//                        cmd.Parameters.AddWithValue("@name", model.Name);
//                        cmd.Parameters.AddWithValue("@role", model.Role);
//                        cmd.Parameters.AddWithValue("@phone", model.Phone ?? "");
//                        cmd.Parameters.AddWithValue("@type", model.Type ?? "Non");
//                        cmd.Parameters.AddWithValue("@priceUnit", model.PriceUnit ?? 0);
//                        cmd.Parameters.AddWithValue("@unitTime", model.UnitTime ?? 8);
//                        cmd.Parameters.AddWithValue("@maxUnitTime", model.MaxUnitTime ?? 8);
//                        cmd.Parameters.AddWithValue("@empId", model.EmployeeId ?? 0);

//                        int affected = await cmd.ExecuteNonQueryAsync();
//                        if (affected == 0)
//                            return NotFound(new { status = false, message = "Resource not found" });

//                        return Ok(new
//                        {
//                            status = true,
//                            message = "Resource updated successfully",
//                            id = model.Id,
//                            code = resourceCode
//                        });
//                    }
//                }
//                catch (Exception ex)
//                {
//                    return Json(500, new { status = false, message = ex.Message });
//                }
//            }
//            catch (Exception ex)
//            {
//                return Json(new { status = false, message = "An unexpected error occurred: " + ex.Message });
//            }

//        }

//        private async Task<string> GenerateNextResourceCode(MySqlConnection conn)
//        {
//            try
//            {
//                string query = "SELECT code FROM tbl_project_resource ORDER BY id DESC LIMIT 1";
//                await using var cmd = new MySqlCommand(query, conn);
//                var lastCodeObj = await cmd.ExecuteScalarAsync();

//                if (lastCodeObj == null)
//                    return "RS1";

//                string lastCode = lastCodeObj.ToString();
//                if (int.TryParse(lastCode.Replace("RS", ""), out int num))
//                    return $"RS{num + 1}";
//                return "RS1";
//            }
//            catch(Exception ex)
//            {
//                throw ex;
//            }
//        }

//        [HttpPost]
//        public async Task<IActionResult> AssignResources([FromBody] AssignResourcesRequest model)
//        {
//            try
//            {
//                if (model == null || model.PlanningId <= 0)
//                    return Json(new { status = false, message = "Invalid request" });

//                try
//                {
//                    int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
//                    if (userId <= 0)
//                        return Json(new { status = false, message = "User not logged in" });

//                    // Convert list of ResourceIds to comma-separated string
//                    string assignedTeam = model.ResourceIds != null && model.ResourceIds.Count > 0
//                        ? string.Join(",", model.ResourceIds)
//                        : "";

//                    // Build connection string
//                    var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                    {
//                        Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                    };

//                    await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                    await conn.OpenAsync();

//                    // Update assigned_team
//                    string updateQuery = "UPDATE tbl_project_planning SET assigned_team=@assignedTeam WHERE id=@id";
//                    await using var cmd = new MySqlCommand(updateQuery, conn);
//                    cmd.Parameters.AddWithValue("@assignedTeam", assignedTeam);
//                    cmd.Parameters.AddWithValue("@id", model.PlanningId);

//                    int affected = await cmd.ExecuteNonQueryAsync();

//                    if (affected > 0)
//                    {
//                        return Ok(new { status = true, message = "Resources assigned successfully", assignedTeam });
//                    }
//                    else
//                    {
//                        return Json(new { status = false, message = "Planning record not found" });
//                    }
//                }
//                catch (Exception ex)
//                {
//                    return Json(500, new { status = false, message = ex.Message });
//                }
//            }
//            catch (Exception ex)
//            {
//                return Json(new { status = false, message = "An unexpected error occurred: " + ex.Message });
//            }

//        }
//        [HttpGet]
//        public async Task<IActionResult> GetAssignedResources(int planningId)
//        {
//            try
//            {
//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                string query = @"
//            SELECT 
//                pr.id, 
//                pr.code, 
//                pr.date, 
//                pr.name, 
//                r.name AS roleName, 
//                pr.phone, 
//                pr.type, 
//                pr.price_unit, 
//                pr.unit_time, 
//                pr.max_unit_time
//            FROM tbl_project_resource pr
//            JOIN tbl_project_role r ON r.id = pr.role
//            WHERE EXISTS (
//                SELECT 1 
//                FROM tbl_project_planning p 
//                WHERE p.id = @planningId 
//                  AND FIND_IN_SET(pr.id, p.assigned_team) > 0
//            );";

//                await using var cmd = new MySqlCommand(query, conn);
//                cmd.Parameters.AddWithValue("@planningId", planningId);

//                await using var reader = await cmd.ExecuteReaderAsync();

//                var resourceList = new List<object>();
//                int sn = 1;

//                while (await reader.ReadAsync())
//                {
//                    resourceList.Add(new
//                    {
//                        SN = sn++,
//                        Id = reader.GetInt32("id"),
//                        Code = reader["code"]?.ToString(),
//                        Name = reader["name"]?.ToString(),
//                    });
//                }

//                return Ok(new { status = true, data = resourceList });
//            }
//            catch (Exception ex)
//            {
//                return StatusCode(500, new { status = false, message = ex.Message });
//            }
//        }

//        [HttpPost]
//        public async Task<IActionResult> SaveOrUpdateProjectActivity([FromBody] ProjectActivityRequest model)
//        {
//            try
//            {
//                if (model == null)
//                    return Json(new { status = false, message = "Invalid request data." });

//                if (model.PlanningId <= 0)
//                    return Json(new { status = false, message = "Planning ID is required." });

//                if (model.TenderId <= 0)
//                    return Json(new { status = false, message = "Tender ID is required." });

//                if (model.ItemId <= 0)
//                    return Json(new { status = false, message = "Item ID is required." });

//                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
//                if (userId <= 0)
//                    return Json(new { status = false, message = "User not logged in." });

//                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
//                {
//                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
//                };

//                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
//                await conn.OpenAsync();

//                int progress = model.Progress ?? 0;
//                DateTime startDate = model.StartDate ?? DateTime.Today;
//                DateTime endDate = model.EndDate ?? DateTime.Today;
//                string status = progress == 100 ? "Completed" :
//                                (DateTime.Today >= startDate ? "In Progress" : "Not Started");

                
//                int activityId = 0; 

//                //if (model.Id <= 0)
//                //{
//                    // INSERT
//                    string insertQuery = @"
//        INSERT INTO tbl_project_activity 
//            (planning_id, code, name, start_date, end_date, progress, status)
//        VALUES (@planningId, @code, @name, @startDate, @endDate, @progress, @status);
//        SELECT LAST_INSERT_ID();";

//                    await using var cmd = new MySqlCommand(insertQuery, conn);
//                    cmd.Parameters.AddWithValue("@planningId", model.PlanningId);
//                    cmd.Parameters.AddWithValue("@code", model.ItemId);
//                    cmd.Parameters.AddWithValue("@name", model.ItemId);
//                    cmd.Parameters.AddWithValue("@startDate", startDate);
//                    cmd.Parameters.AddWithValue("@endDate", endDate);
//                    cmd.Parameters.AddWithValue("@progress", progress);
//                    cmd.Parameters.AddWithValue("@status", status);

//                    activityId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
//                //}


//                // -------------------------
//                // Assign Resources
//                // -------------------------
//                if (model.AssignedResources != null && model.AssignedResources.Any())
//                {
//                    foreach (var resId in model.AssignedResources)
//                    {
//                        string insertAssignment = @"
//                    INSERT INTO tbl_project_activity_assignment (activity_id, resource_id)
//                    VALUES (@activityId, @resourceId);";

//                        await using var assignCmd = new MySqlCommand(insertAssignment, conn);
//                        assignCmd.Parameters.AddWithValue("@activityId", activityId);
//                        assignCmd.Parameters.AddWithValue("@resourceId", resId);
//                        await assignCmd.ExecuteNonQueryAsync();
//                    }
//                }

//                // -------------------------
//                // Update Tender Details
//                // -------------------------
//                string updateTender = @"
//            UPDATE tbl_project_tender_details 
//            SET start_date=@startDate, end_date=@endDate, progress=@progress
//            WHERE item_id=@itemId AND tender_id=@tenderId;";

//                await using (var tenderCmd = new MySqlCommand(updateTender, conn))
//                {
//                    tenderCmd.Parameters.AddWithValue("@itemId", model.ItemId);
//                    tenderCmd.Parameters.AddWithValue("@tenderId", model.TenderId);
//                    tenderCmd.Parameters.AddWithValue("@startDate", startDate);
//                    tenderCmd.Parameters.AddWithValue("@endDate", endDate);
//                    tenderCmd.Parameters.AddWithValue("@progress", progress);
//                    await tenderCmd.ExecuteNonQueryAsync();

//                    var affectedRows = await tenderCmd.ExecuteNonQueryAsync();
//                    if (affectedRows == 0)
//                        Console.WriteLine("No tender row updated! Check ItemId/TenderId combination.");

//                }

//                // -------------------------
//                // Update Project Planning
//                // -------------------------
//                string updatePlanning = @"
//            UPDATE tbl_project_planning
//            SET modified_by = @modifiedBy,
//                modified_date = @modifiedDate,
//                progress = @progress
//            WHERE id = @planningId;";

//                await using (var planningCmd = new MySqlCommand(updatePlanning, conn))
//                {
//                    planningCmd.Parameters.AddWithValue("@modifiedBy", userId);
//                    planningCmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now.Date);
//                    planningCmd.Parameters.AddWithValue("@progress", 0); // default progress
//                    planningCmd.Parameters.AddWithValue("@planningId", model.PlanningId);
//                    await planningCmd.ExecuteNonQueryAsync();
//                }

//                return Ok(new
//                {
//                    status = true,
//                    message = model.Id == 0 ? "Activity inserted successfully" : "Activity updated successfully",
//                    id = activityId,
//                    progress = progress,
//                    statusText = status
//                });
//            }
//            catch (Exception ex)
//            {
//                return Json(500, new { status = false, message = ex.Message });
//            }
//        }


//        #endregion

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


        [HttpGet]
        public async Task<IActionResult> GetProjectPlanningSummary(int id)
        { 
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 1. Get project planning + project + tender
                string planningQuery = @"
            SELECT pl.*, p.name AS project_name, p.code AS project_code, 
                   (SELECT name FROM tbl_tender_names WHERE id = pl.tender_id) AS tender_name
            FROM tbl_project_planning pl
            JOIN tbl_projects p ON pl.project_id = p.id
            WHERE pl.id = @id;";
                await using var cmd = new MySqlCommand(planningQuery, conn);
                cmd.Parameters.AddWithValue("@id", id);

                await using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return NotFound(new { status = false, message = "Project planning not found." });

                var projectPlanningId = reader["id"].ToString();
                var projectData = new
                {
                    Id = reader["id"],
                    ProjectId = reader["project_id"],
                    ProjectCode = reader["project_code"].ToString(),
                    ProjectName = reader["project_name"].ToString(),
                    TenderId = reader["tender_id"],
                    TenderName = reader["tender_name"].ToString(),
                    StartDate = reader["start_date"],
                    EndDate = reader["end_date"],
                    Status = reader["status"].ToString(),
                    EstimatedBudget = reader["estimated_budget"],
                    ProjectType = reader["project_type"].ToString(),
                    FundAccountId = reader["fund_account_id"]
                };
                await reader.CloseAsync();



                // 1a. Get budget info from tbl_project_management
                object budgetData = null;
                string budgetQuery = "SELECT * FROM tbl_project_management WHERE project_planning_id = @planId LIMIT 1;";
                await using var budgetCmd = new MySqlCommand(budgetQuery, conn);
                budgetCmd.Parameters.AddWithValue("@planId", projectPlanningId);

                await using var budgetReader = await budgetCmd.ExecuteReaderAsync();
                if (await budgetReader.ReadAsync())
                {
                    budgetData = new
                    {
                        Budget = budgetReader["budget"] != DBNull.Value ? Convert.ToDecimal(budgetReader["budget"]) : 0,
                        ActualCost = budgetReader["actual_cost"] != DBNull.Value ? Convert.ToDecimal(budgetReader["actual_cost"]) : 0,
                        RemainingBudget = budgetReader["remaining_budget"] != DBNull.Value ? Convert.ToDecimal(budgetReader["remaining_budget"]) : 0,
                        Date = budgetReader["date"] != DBNull.Value ? Convert.ToDateTime(budgetReader["date"]) : (DateTime?)null,
                        RecordId = budgetReader["id"]
                    };
                }
                await budgetReader.CloseAsync();

                // 2. Site info
                string siteQuery = @"
            SELECT a.id, a.code, a.name AS site, a.plot_number, a.address, b.name AS location
            FROM tbl_project_sites a
            JOIN tbl_city b ON a.location_id = b.id
            WHERE a.id = (SELECT site FROM tbl_project_planning WHERE id = @id);";

                await using var siteCmd = new MySqlCommand(siteQuery, conn);
                siteCmd.Parameters.AddWithValue("@id", id);
                await using var siteReader = await siteCmd.ExecuteReaderAsync();

                object siteData = null;
                if (await siteReader.ReadAsync())
                {
                    siteData = new
                    {
                        Id = siteReader["id"],
                        SiteCode = siteReader["code"].ToString(),
                        SiteName = siteReader["site"].ToString(),
                        PlotNumber = siteReader["plot_number"].ToString(),
                        Address = siteReader["address"].ToString(),
                        Location = siteReader["location"].ToString()
                    };
                }
                await siteReader.CloseAsync();

                // 3. Assigned team
                string teamQuery = @"
            SELECT name
            FROM tbl_project_resource
            WHERE employee_id > 0 
              AND FIND_IN_SET(id, (SELECT assigned_team FROM tbl_project_planning WHERE id = @planId)) > 0;";
                await using var teamCmd = new MySqlCommand(teamQuery, conn);
                teamCmd.Parameters.AddWithValue("@planId", projectPlanningId);

                var assignedTeam = new List<string>();
                await using var teamReader = await teamCmd.ExecuteReaderAsync();
                while (await teamReader.ReadAsync())
                    assignedTeam.Add(teamReader["name"].ToString());
                await teamReader.CloseAsync();

                // 4. Activity list
                string activityQuery = @"
            SELECT CONCAT(a.code, '-', b.name) AS description,
                   b.qty AS QtyNeeded,
                   IFNULL(c.ReceivedQty, 0) AS QtyUsed,
                   CASE 
                     WHEN IFNULL(c.ReceivedQty, 0) > 0 THEN 'Received'
                     WHEN IFNULL(c.IssuedQty, 0) > 0 THEN 'Issued'
                     ELSE 'Requested'
                   END AS status
            FROM tbl_project_activity a
            LEFT JOIN tbl_items_boq b ON b.id = a.code
            LEFT JOIN tbl_project_material_requests c ON c.itemId = a.code AND a.planning_id = c.planning_id
            WHERE a.planning_id = @planId;";

                await using var activityCmd = new MySqlCommand(activityQuery, conn);
                activityCmd.Parameters.AddWithValue("@planId", projectPlanningId);

                var activityList = new List<object>();
                await using var activityReader = await activityCmd.ExecuteReaderAsync();
                while (await activityReader.ReadAsync())
                {
                    activityList.Add(new
                    {
                        Description = activityReader["description"].ToString(),
                        QtyNeeded = activityReader["QtyNeeded"],
                        QtyUsed = activityReader["QtyUsed"],
                        Status = activityReader["status"].ToString()
                    });
                }
                await activityReader.CloseAsync();

                // 5. Assignment details
                string assignQuery = @"
            SELECT b.name, b.date, COUNT(a.resource_id) AS task, c.status
            FROM tbl_project_activity_assignment a
            JOIN tbl_project_activity c ON a.activity_id = c.id
            LEFT JOIN tbl_project_resource b ON a.resource_id = b.id
            WHERE c.planning_id = @planId
            GROUP BY b.name, b.date, c.status;";

                await using var assignCmd = new MySqlCommand(assignQuery, conn);
                assignCmd.Parameters.AddWithValue("@planId", projectPlanningId);

                var assignmentList = new List<object>();
                await using var assignReader = await assignCmd.ExecuteReaderAsync();
                while (await assignReader.ReadAsync())
                {
                    assignmentList.Add(new
                    {
                        Name = assignReader["name"].ToString(),
                        Date = assignReader["date"] == DBNull.Value ? "" : Convert.ToDateTime(assignReader["date"]).ToString("dd/MM/yyyy"),
                        TaskCount = assignReader["task"],
                        Status = assignReader["status"].ToString()
                    });
                }
                await assignReader.CloseAsync();

                return Ok(new
                {
                    status = true,
                    project = projectData,
                    site = siteData,
                    assignedTeam,
                    activities = activityList,
                    assignments = assignmentList,
                    budgetInfo = budgetData
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        //[HttpPost]
        //public async Task<IActionResult> SaveOrUpdateProjectManagement([FromBody] ProjectManagementRequest model)
        //{
        //    try
        //    {
        //        if (model == null)
        //            return Json(new { status = false, message = "Invalid request data." });

        //        if (model.ProjectPlanningId <= 0)
        //            return Json(new { status = false, message = "Project Planning ID is required." });

        //        if (model.ProjectId <= 0)
        //            return Json(new { status = false, message = "Project ID is required." });

        //        int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        //        if (userId <= 0)
        //            return Json(new { status = false, message = "User not logged in." });

        //        var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
        //        {
        //            Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
        //        };

        //        await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
        //        await conn.OpenAsync();

        //        int recordId;

        //        if (model.Id == 0)
        //        {
        //            string insertQuery = @"
        //        INSERT INTO tbl_project_management
        //            (date, project_planning_id, project_id, budget, actual_cost, remaining_budget, created_by, created_date, state)
        //        VALUES
        //            (@date, @projectPlanningId, @projectId, @budget, @actualCost, @remainingBudget, @createdBy, @createdDate, 0);
        //        SELECT LAST_INSERT_ID();";

        //            await using var insertCmd = new MySqlCommand(insertQuery, conn);
        //            insertCmd.Parameters.AddWithValue("@date", model.Date ?? DateTime.Now.Date);
        //            insertCmd.Parameters.AddWithValue("@projectPlanningId", model.ProjectPlanningId);
        //            insertCmd.Parameters.AddWithValue("@projectId", model.ProjectId);
        //            insertCmd.Parameters.AddWithValue("@budget", model.Budget);
        //            insertCmd.Parameters.AddWithValue("@actualCost", model.ActualCost);
        //            insertCmd.Parameters.AddWithValue("@remainingBudget", model.RemainingBudget);
        //            insertCmd.Parameters.AddWithValue("@createdBy", userId);
        //            insertCmd.Parameters.AddWithValue("@createdDate", DateTime.Now);

        //            recordId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

        //        }
        //        else
        //        {
        //            string updateQuery = @"
        //        UPDATE tbl_project_management
        //        SET date = @date,
        //            project_planning_id = @projectPlanningId,
        //            project_id = @projectId,
        //            budget = @budget,
        //            actual_cost = @actualCost,
        //            remaining_budget = @remainingBudget,
        //            modified_by = @modifiedBy,
        //            modified_date = @modifiedDate
        //        WHERE id = @id;";

        //            await using var updateCmd = new MySqlCommand(updateQuery, conn);
        //            updateCmd.Parameters.AddWithValue("@id", model.Id);
        //            updateCmd.Parameters.AddWithValue("@date", model.Date ?? DateTime.Now.Date);
        //            updateCmd.Parameters.AddWithValue("@projectPlanningId", model.ProjectPlanningId);
        //            updateCmd.Parameters.AddWithValue("@projectId", model.ProjectId);
        //            updateCmd.Parameters.AddWithValue("@budget", model.Budget);
        //            updateCmd.Parameters.AddWithValue("@actualCost", model.ActualCost);
        //            updateCmd.Parameters.AddWithValue("@remainingBudget", model.RemainingBudget);
        //            updateCmd.Parameters.AddWithValue("@modifiedBy", userId);
        //            updateCmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now);

        //            await updateCmd.ExecuteNonQueryAsync();
        //            recordId = model.Id;

        //        }


        //        string updateProgressQuery = @"
        //    UPDATE tbl_project_planning
        //    SET progress = @progress
        //    WHERE id = @id;";

        //        await using var progressCmd = new MySqlCommand(updateProgressQuery, conn);
        //        progressCmd.Parameters.AddWithValue("@id", model.ProjectPlanningId);
        //        progressCmd.Parameters.AddWithValue("@progress", model.Progress);
        //        await progressCmd.ExecuteNonQueryAsync();


        //        return Json(new
        //        {
        //            status = true,
        //            message = model.Id == 0 ? "Project Management inserted successfully." : "Project Management updated successfully.",
        //            id = recordId
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { status = false, message = ex.Message });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> SaveOrUpdateProjectManagement([FromBody] ProjectManagementRequest model)
        {
            try
            {
                if (model == null)
                    return Json(new { status = false, message = "Invalid request data." });

                if (model.ProjectPlanningId <= 0)
                    return Json(new { status = false, message = "Project Planning ID is required." });

                if (model.ProjectId <= 0)
                    return Json(new { status = false, message = "Project ID is required." });

                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Json(new { status = false, message = "User not logged in." });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                await using var transaction = await conn.BeginTransactionAsync();

                try
                {
                    int recordId;
                    bool isInsert = false;

                    // Check if record exists with this ProjectPlanningId
                    string checkQuery = @"
                SELECT id 
                FROM tbl_project_management 
                WHERE project_planning_id = @projectPlanningId 
                AND state = 0 
                LIMIT 1;";

                    await using var checkCmd = new MySqlCommand(checkQuery, conn, transaction);
                    checkCmd.Parameters.AddWithValue("@projectPlanningId", model.ProjectPlanningId);

                    var existingIdObj = await checkCmd.ExecuteScalarAsync();
                    int existingId = existingIdObj != null ? Convert.ToInt32(existingIdObj) : 0;

                    if (existingId == 0)
                    {
                        // INSERT new record
                        isInsert = true;
                        string insertQuery = @"
                    INSERT INTO tbl_project_management
                        (date, project_planning_id, project_id, budget, actual_cost, remaining_budget, created_by, created_date, state)
                    VALUES
                        (@date, @projectPlanningId, @projectId, @budget, @actualCost, @remainingBudget, @createdBy, @createdDate, 0);
                    SELECT LAST_INSERT_ID();";

                        await using var insertCmd = new MySqlCommand(insertQuery, conn, transaction);
                        insertCmd.Parameters.AddWithValue("@date", model.Date ?? DateTime.Now.Date);
                        insertCmd.Parameters.AddWithValue("@projectPlanningId", model.ProjectPlanningId);
                        insertCmd.Parameters.AddWithValue("@projectId", model.ProjectId);
                        insertCmd.Parameters.AddWithValue("@budget", model.Budget);
                        insertCmd.Parameters.AddWithValue("@actualCost", model.ActualCost);
                        insertCmd.Parameters.AddWithValue("@remainingBudget", model.RemainingBudget);
                        insertCmd.Parameters.AddWithValue("@createdBy", userId);
                        insertCmd.Parameters.AddWithValue("@createdDate", DateTime.Now);

                        recordId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                    }
                    else
                    {
                        // UPDATE existing record
                        string updateQuery = @"
                    UPDATE tbl_project_management
                    SET date = @date,
                        project_id = @projectId,
                        budget = @budget,
                        actual_cost = @actualCost,
                        remaining_budget = @remainingBudget,
                        modified_by = @modifiedBy,
                        modified_date = @modifiedDate
                    WHERE id = @id AND state = 0;";

                        await using var updateCmd = new MySqlCommand(updateQuery, conn, transaction);
                        updateCmd.Parameters.AddWithValue("@id", existingId);
                        updateCmd.Parameters.AddWithValue("@date", model.Date ?? DateTime.Now.Date);
                        updateCmd.Parameters.AddWithValue("@projectId", model.ProjectId);
                        updateCmd.Parameters.AddWithValue("@budget", model.Budget);
                        updateCmd.Parameters.AddWithValue("@actualCost", model.ActualCost);
                        updateCmd.Parameters.AddWithValue("@remainingBudget", model.RemainingBudget);
                        updateCmd.Parameters.AddWithValue("@modifiedBy", userId);
                        updateCmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now);

                        int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            await transaction.RollbackAsync();
                            return Json(new { status = false, message = "Failed to update record." });
                        }

                        recordId = existingId;
                    }

                    // Update progress in project planning
                    string updateProgressQuery = @"
                UPDATE tbl_project_planning
                SET progress = @progress
                WHERE id = @id;";

                    await using var progressCmd = new MySqlCommand(updateProgressQuery, conn, transaction);
                    progressCmd.Parameters.AddWithValue("@id", model.ProjectPlanningId);
                    progressCmd.Parameters.AddWithValue("@progress", model.Progress);
                    await progressCmd.ExecuteNonQueryAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    return Json(new
                    {
                        status = true,
                        message = isInsert ? "Project Management inserted successfully." : "Project Management updated successfully.",
                        id = recordId
                    });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Project Activity Report

        public IActionResult ActivitySummary()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetActivitySummary(bool showAll = true, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"
            SELECT 
                id,
                (SELECT name FROM tbl_items_boq WHERE id = tpa.code LIMIT 1) AS Name,
                code,
                start_date AS StartDate,
                end_date AS EndDate,
                progress,
                status
            FROM tbl_project_activity tpa
            WHERE id > 0";

                var parameters = new List<MySqlParameter>();

                // Apply date filter only when showAll is false
                if (!showAll && startDate.HasValue && endDate.HasValue)
                {
                    query += @" AND planning_id IN (
                            SELECT id FROM tbl_project_planning 
                            WHERE date >= @dateFrom AND date <= @dateTo
                        )";
                    parameters.Add(new MySqlParameter("@dateFrom", startDate.Value));
                    parameters.Add(new MySqlParameter("@dateTo", endDate.Value));
                }

                query += " ORDER BY id;";

                await using var cmd = new MySqlCommand(query, conn);
                if (parameters.Any())
                    cmd.Parameters.AddRange(parameters.ToArray());

                var reader = await cmd.ExecuteReaderAsync();
                var activityList = new List<object>();
                int sn = 1;

                while (await reader.ReadAsync())
                {
                    activityList.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        Name = reader["Name"]?.ToString(),
                        Code = reader["code"]?.ToString(),
                        StartDate = reader["StartDate"] != DBNull.Value
                            ? Convert.ToDateTime(reader["StartDate"]).ToString("yyyy-MM-dd")
                            : null,
                        EndDate = reader["EndDate"] != DBNull.Value
                            ? Convert.ToDateTime(reader["EndDate"]).ToString("yyyy-MM-dd")
                            : null,
                        Progress = reader["progress"] != DBNull.Value ? Convert.ToDecimal(reader["progress"]) : 0,
                        Status = reader["status"]?.ToString()
                    });
                }

                await reader.CloseAsync();
                await conn.CloseAsync();

                return Ok(new { status = true, data = activityList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Project Assign Report

        public IActionResult ProjectAssignReport()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetActivityAssignmentSummary(bool showAll = true, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Build connection string dynamically (supports multi-tenant setups)
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Base query
                var query = @"
            SELECT 
                a.id,
                c.code AS Code,
                c.date AS Date,
                c.name AS Name,
                c.type AS Type,
                (SELECT name FROM tbl_project_role WHERE id = role) AS Role,
                price_unit AS `PricePerUnit`,
                unit_time AS `DefaultUnitTime`,
                max_unit_time AS `MaxUnitTime`
            FROM tbl_project_activity_assignment a
            LEFT JOIN tbl_project_activity b ON a.activity_id = b.id
            INNER JOIN tbl_project_resource c ON c.id = a.resource_id
            WHERE c.employee_id > 0";

                var parameters = new List<MySqlParameter>();

                // Apply date filter only when showAll = false
                if (!showAll && startDate.HasValue && endDate.HasValue)
                {
                    query += @" AND b.planning_id IN (
                            SELECT id FROM tbl_project_planning 
                            WHERE date >= @dateFrom AND date <= @dateTo
                        )";

                    parameters.Add(new MySqlParameter("@dateFrom", startDate.Value));
                    parameters.Add(new MySqlParameter("@dateTo", endDate.Value));
                }

                query += " ORDER BY a.id;";

                await using var cmd = new MySqlCommand(query, conn);
                if (parameters.Any())
                    cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();

                var activityAssignments = new List<object>();
                int sn = 1;

                while (await reader.ReadAsync())
                {
                    activityAssignments.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        Code = reader["Code"]?.ToString(),
                        Date = reader["Date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd")
                            : null,
                        Name = reader["Name"]?.ToString(),
                        Type = reader["Type"]?.ToString(),
                        Role = reader["Role"]?.ToString(),
                        PricePerUnit = reader["PricePerUnit"] != DBNull.Value
                            ? Convert.ToDecimal(reader["PricePerUnit"]).ToString("N2")
                            : "0.00",
                        DefaultUnitTime = reader["DefaultUnitTime"]?.ToString(),
                        MaxUnitTime = reader["MaxUnitTime"]?.ToString()
                    });
                }

                await reader.CloseAsync();
                await conn.CloseAsync();

                return Ok(new { status = true, data = activityAssignments });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Project Resource Report

        public IActionResult ProjectResourceReport()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectAssignmentSummary(bool showAll = true, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            try
            {
                // ✅ Connection setup with dynamic DB selection (as per your pattern)
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Base query
                var query = @"
            SELECT a.id,c.code Code,c.Date,c.name Name,c.type Type,(SELECT NAME FROM tbl_project_role WHERE id = role) Role,IFNULL((SELECT NAME FROM tbl_employee WHERE id = employee_id),'') Employee,
                       price_unit AS PricePerUnit,
    unit_time AS DefaultUnitTime,
    max_unit_time AS MaxUnitTime
                        from tbl_project_activity_assignment a LEFT JOIN tbl_project_activity b ON a.activity_id = b.id LEFT JOIN tbl_project_resource c ON c.id = a.resource_id";

                var cmd = new MySqlCommand();
                cmd.Connection = conn;

                // ✅ Optional date filter
                if (!showAll && dateFrom.HasValue && dateTo.HasValue)
                {
                    query += " AND b.planning_id IN (SELECT id FROM tbl_project_planning WHERE date >= @dateFrom AND date <= @dateTo)";
                    cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value.Date);
                    cmd.Parameters.AddWithValue("@dateTo", dateTo.Value.Date);
                }

                cmd.CommandText = query;

                var list = new List<object>();
                int sn = 1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {


                    list.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        Code = reader["Code"]?.ToString() ?? "",
                        Date = reader["Date"] != DBNull.Value ? Convert.ToDateTime(reader["Date"]).ToString("yyyy-MM-dd") : "",
                        Name = reader["Name"]?.ToString() ?? "",
                        Type = reader["Type"]?.ToString() ?? "",
                        Role = reader["Role"]?.ToString() ?? "",
                        Employee = reader["Employee"]?.ToString() ?? "",
                        PricePerUnit = reader["PricePerUnit"]?.ToString()??"",
                        DefaultUnitTime = reader["DefaultUnitTime"]?.ToString() ?? "",
                        MaxUnitTime = reader["MaxUnitTime"]?.ToString() ?? ""
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

        #region  Project Progress Summary

        public IActionResult ProjectProgressSummary()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectActivitySummary()
        {
            try
            {
                // Build connection string dynamically
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // SQL query to summarize activities
                var query = @"
            SELECT 
                planning_id, 
                COUNT(*) AS total_activities,
                SUM(CASE WHEN progress = 100 THEN 1 ELSE 0 END) AS completed,
                CONCAT(ROUND(SUM(progress) / COUNT(*), 2), '%') AS avg_progress
            FROM tbl_project_activity
            GROUP BY planning_id;";

                await using var cmd = new MySqlCommand(query, conn);

                var list = new List<object>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        PlanningId = reader.GetInt32("planning_id"),
                        TotalActivities = reader.GetInt32("total_activities"),
                        Completed = reader.GetInt32("completed"),
                        AvgProgress = reader["avg_progress"]?.ToString() ?? "0%"
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

        #region Project Work Progress Summary

        public IActionResult ProjectWorkProgressSummary()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetProjectWorkDoneDetailss(int? projectId = null)
        {
            try
            {
                // Build MySQL connection string dynamically
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Base query
                var query = @"
            SELECT 
                (SELECT name FROM tbl_items_boq WHERE id = code) AS Description,
                SUM(qty_total) AS TotalDoneQty
            FROM tbl_project_work_done_details
            WHERE id > 0";

                // Add project filter if provided
                if (projectId.HasValue)
                {
                    query += " AND ref_id = @projectId";
                }

                query += " GROUP BY code;";

                await using var cmd = new MySqlCommand(query, conn);

                if (projectId.HasValue)
                {
                    cmd.Parameters.AddWithValue("@projectId", projectId.Value);
                }

                var list = new List<object>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(new
                    {
                        Description = reader["Description"]?.ToString() ?? "",
                        TotalDoneQty = reader["TotalDoneQty"] != DBNull.Value ? Convert.ToDecimal(reader["TotalDoneQty"]) : 0
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

        #region Project Dashboard

        public IActionResult ProjectDashboard()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetProjectDashboard(int? projectId = null)
        {
            try
            {
                // Connection with dynamic DB
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Base queries
                string queryProjects = @"
            SELECT a.id, a.date, a.project_id, a.start_date, a.end_date, a.status, a.project_type, a.estimated_budget, 
                   b.name AS location, a.fund_account_id, a.created_by, a.description, a.fund_period
            FROM tbl_project_planning a
            LEFT JOIN tbl_project_sites b ON b.id = a.site
            WHERE a.state = 0";

                string queryEstimates = @"SELECT id, '' AS description, project_id FROM tbl_project_tender WHERE state = 0 AND estimate_status > 0";
                string queryTenders = @"
            SELECT a.id, b.name AS tender_name, b.code, a.amount AS bid_amount, a.submission_date,
                   (SELECT CONCAT(ROUND(SUM(progress)/100, 2), '% / ', COUNT(*)) FROM tbl_project_tender_details WHERE tender_id = a.id) AS status,
                   a.DESCRIPTION, a.project_id
            FROM tbl_project_tender a
            LEFT JOIN tbl_tender_names b ON a.tender_name_id = b.id
            WHERE a.state = 0";

                // Filter by project if projectId is provided
                if (projectId.HasValue)
                {
                    queryProjects += " AND project_id = @projectId";
                    queryEstimates += " AND project_id = @projectId";
                    queryTenders += " AND project_id = @projectId";
                }

                // Execute queries
                var projects = new List<object>();
                await using (var cmd = new MySqlCommand(queryProjects, conn))
                {
                    if (projectId.HasValue) cmd.Parameters.AddWithValue("@projectId", projectId.Value);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    int sn = 1;
                    while (await reader.ReadAsync())
                    {
                        projects.Add(new
                        {
                            SN = sn++,
                            Id = reader.GetInt32("id"),
                            ProjectId = reader.GetInt32("project_id"),
                            Date = reader["date"] != DBNull.Value ? Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd") : null,
                            StartDate = reader["start_date"] != DBNull.Value ? Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd") : null,
                            EndDate = reader["end_date"] != DBNull.Value ? Convert.ToDateTime(reader["end_date"]).ToString("yyyy-MM-dd") : null,
                            Status = reader["status"]?.ToString(),
                            ProjectType = reader["project_type"]?.ToString(),
                            EstimatedBudget = reader["estimated_budget"] != DBNull.Value ? Convert.ToDecimal(reader["estimated_budget"]) : 0,
                            Location = reader["location"]?.ToString(),
                            FundAccountId = reader["fund_account_id"] != DBNull.Value ? Convert.ToInt32(reader["fund_account_id"]) : 0,
                            CreatedBy = reader["created_by"]?.ToString(),
                            Description = reader["description"]?.ToString(),
                            FundPeriod = reader["fund_period"]?.ToString()
                        });
                    }
                }

                var estimates = new List<object>();
                await using (var cmd = new MySqlCommand(queryEstimates, conn))
                {
                    if (projectId.HasValue) cmd.Parameters.AddWithValue("@projectId", projectId.Value);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        estimates.Add(new
                        {
                            Id = reader.GetInt32("id"),
                            Description = reader["description"]?.ToString(),
                            ProjectId = reader.GetInt32("project_id")
                        });
                    }
                }

                var tenders = new List<object>();
                await using (var cmd = new MySqlCommand(queryTenders, conn))
                {
                    if (projectId.HasValue) cmd.Parameters.AddWithValue("@projectId", projectId.Value);
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        tenders.Add(new
                        {
                            Id = reader.GetInt32("id"),
                            TenderName = reader["tender_name"]?.ToString(),
                            Code = reader["code"]?.ToString(),
                            BidAmount = reader["bid_amount"] != DBNull.Value ? Convert.ToDecimal(reader["bid_amount"]) : 0,
                            SubmissionDate = reader["submission_date"] != DBNull.Value ? Convert.ToDateTime(reader["submission_date"]).ToString("yyyy-MM-dd") : null,
                            Status = reader["status"]?.ToString(),
                            Description = reader["DESCRIPTION"]?.ToString(),
                            ProjectId = reader["project_id"] != DBNull.Value ? Convert.ToInt32(reader["project_id"]) : 0
                        });
                    }
                }

                return Ok(new { status = true, projects, estimates, tenders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Project TimeLine

        public IActionResult ProjectTimeLine()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectTimelineSummary()
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
                plan.id,
                IFNULL(ted.name, CONCAT('Task ', plan.id)) AS task_name,
                plan.start_date,
                plan.end_date,
                plan.progress AS planning_progress,
                plan.description,
                plan.date
            FROM tbl_project_planning plan
            LEFT JOIN tbl_tender_names ted ON plan.tender_id = ted.id;
        ";

                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var timelineList = new List<object>();
                int sn = 1;

                while (await reader.ReadAsync())
                {
                    timelineList.Add(new
                    {
                        SN = sn++,
                        Id = reader.GetInt32("id"),
                        TaskName = reader["task_name"].ToString(),
                        StartDate = reader["start_date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["start_date"]).ToString("yyyy-MM-dd")
                            : null,
                        EndDate = reader["end_date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["end_date"]).ToString("yyyy-MM-dd")
                            : null,
                        Progress = reader["planning_progress"] != DBNull.Value
                            ? Convert.ToDecimal(reader["planning_progress"])
                            : 0,
                        Description = reader["description"]?.ToString() ?? "",
                        Date = reader["date"] != DBNull.Value
                            ? Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd")
                            : null
                    });
                }

                return Ok(new { status = true, data = timelineList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Project TimeLine(Gantt chart)

        public IActionResult ProjectTimeLineChart()
        {
            return View();  
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectTimelineFull()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ??
                               _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // -------------------------------
                // 1. MAIN PROJECT PLANNING QUERY
                // -------------------------------
                string mainQuery = @"
            SELECT 
                plan.id,
                IFNULL(ted.name, CONCAT('Task ', plan.id)) AS task_name,
                plan.description,
                plan.date,
                plan.start_date,
                plan.end_date
            FROM tbl_project_planning plan
            LEFT JOIN tbl_tender_names ted ON plan.tender_id = ted.id;
        ";

                await using var cmdMain = new MySqlCommand(mainQuery, conn);
                await using var readerMain = await cmdMain.ExecuteReaderAsync();

                var planningList = new List<object>();

                while (await readerMain.ReadAsync())
                {
                    int planId = readerMain.GetInt32("id");

                    var item = new
                    {
                        PlanId = planId,
                        TaskName = readerMain["task_name"].ToString(),
                        Description = readerMain["description"]?.ToString(),
                        Date = readerMain["date"] != DBNull.Value
                               ? Convert.ToDateTime(readerMain["date"]).ToString("yyyy-MM-dd")
                               : null,
                        StartDate = readerMain["start_date"] != DBNull.Value
                               ? Convert.ToDateTime(readerMain["start_date"]).ToString("yyyy-MM-dd")
                               : null,
                        EndDate = readerMain["end_date"] != DBNull.Value
                               ? Convert.ToDateTime(readerMain["end_date"]).ToString("yyyy-MM-dd")
                               : null,

                        // Placeholder for nested activity list
                        Activities = new List<object>()
                    };

                    planningList.Add(item);
                }

                readerMain.Close(); // Close first reader before next command

                // ----------------------------------
                // 2. LOOP PLANS → LOAD ACTIVITIES
                // ----------------------------------
                foreach (dynamic plan in planningList)
                {
                    string activityQuery = @"
                SELECT 
                    act.id AS activity_id,
                    boq.name AS task_name,
                    boq.sr AS task_code,
                    act.start_date,
                    act.end_date,
                    plan.description AS planning_description,
                    plan.progress AS planning_progress,
                    act.progress AS progress,
                    plan.project_id,
                    GROUP_CONCAT(DISTINCT res.name ORDER BY res.name SEPARATOR ', ') AS assigned_team_names,
                    SUM(COALESCE(res.unit_time, 0)) AS completed_work,
                    SUM(COALESCE(res.max_unit_time, 0)) AS planned_work,
                    GROUP_CONCAT(DISTINCT res.unit_time ORDER BY res.name SEPARATOR ', ') AS assigned_time,
                    GROUP_CONCAT(DISTINCT res.max_unit_time ORDER BY res.name SEPARATOR ', ') AS assigned_max_time
                FROM tbl_project_activity act
                JOIN tbl_project_planning plan ON plan.id = act.planning_id
                LEFT JOIN tbl_project_resource res 
                    ON FIND_IN_SET(res.id, plan.assigned_team) > 0 
                   AND res.TYPE = 'Labour'
                LEFT JOIN tbl_items_boq boq ON act.name = boq.id
                WHERE plan.id = @planId
                GROUP BY act.id, boq.name, boq.sr, act.start_date, act.end_date, 
                         plan.description, plan.progress, plan.project_id, boq.id
                ORDER BY boq.id;
            ";

                    await using var cmdAct = new MySqlCommand(activityQuery, conn);
                    cmdAct.Parameters.AddWithValue("@planId", plan.PlanId);

                    await using var readerAct = await cmdAct.ExecuteReaderAsync();

                    var activityList = new List<object>();

                    while (await readerAct.ReadAsync())
                    {
                        activityList.Add(new
                        {
                            ActivityId = readerAct.GetInt32("activity_id"),
                            TaskName = readerAct["task_name"].ToString(),
                            TaskCode = readerAct["task_code"].ToString(),
                            StartDate = readerAct["start_date"] != DBNull.Value
                                ? Convert.ToDateTime(readerAct["start_date"]).ToString("yyyy-MM-dd")
                                : null,
                            EndDate = readerAct["end_date"] != DBNull.Value
                                ? Convert.ToDateTime(readerAct["end_date"]).ToString("yyyy-MM-dd")
                                : null,

                            Description = readerAct["planning_description"].ToString(),
                            Progress = readerAct["progress"] != DBNull.Value
                                ? Convert.ToDecimal(readerAct["progress"])
                                : 0,

                            AssignedTeam = readerAct["assigned_team_names"]?.ToString() ?? "Unassigned",
                            CompletedWork = readerAct["completed_work"] != DBNull.Value
                                ? Convert.ToDecimal(readerAct["completed_work"])
                                : 0,
                            PlannedWork = readerAct["planned_work"] != DBNull.Value
                                ? Convert.ToDecimal(readerAct["planned_work"])
                                : 0,

                            AssignedTime = readerAct["assigned_time"]?.ToString(),
                            AssignedMaxTime = readerAct["assigned_max_time"]?.ToString()
                        });
                    }

                    readerAct.Close();

                    // Attach activities to main record
                    ((List<object>)plan.Activities).AddRange(activityList);
                }

                return Ok(new { status = true, data = planningList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion




    }


}

