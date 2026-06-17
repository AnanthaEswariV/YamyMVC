using Microsoft.AspNetCore.Mvc;

namespace YamyProject.Controllers
{
    public class SubcontractorController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public SubcontractorController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        #region Subcontractor CRUD operation

        public IActionResult Subcontractor()
        {             
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSubContractors(string state = "All")
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
        SELECT 
            c.id,
            CONCAT(c.code, ' - ', c.name) AS Name,
            c.Cat_id,
            c.date,
            c.work_phone,
            c.main_phone,
            tc.name AS Category,
            c.region,
            c.email,
            c.trn,
            c.mobile,
            c.ccemail,
            c.website,
            c.country,
            c.city,
            c.region,
            c.building_name,
            c.account_id,
            c.facilty_name,
            c.project_id,
            c.project_site,
            c.active,
            COALESCE(t.Amount, 0) AS Amount
        FROM tbl_vendor c
        LEFT JOIN (
            SELECT 
                hum_id,
                SUM(credit - debit) AS Amount
            FROM tbl_transaction
            WHERE 
                state = 0 AND
                type IN (
                    'Subcontractor Payment',
                    'Petty Cash',
                    'Purchase Invoice',
                    'Subcontractor Opening Balance',
                    'Check Cancel (Subcontractor)',
                    'Purchase Return Invoice',
                    'Debit Note',
                    'PDC Payable'
                )
            GROUP BY hum_id
        ) t ON t.hum_id = c.id
        LEFT JOIN tbl_vendor_category tc ON c.Cat_id = tc.id
        WHERE c.type = 'Subcontractor'";

                //// State filter
                //if (state == "Active Subcontractor")
                //    query += " AND c.active = 0";
                //else if (state == "Inactive Subcontractor")
                //    query += " AND c.active != 0";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var subcontractors = new List<object>();

                while (await reader.ReadAsync())
                {
                    string nameAndCode = reader["Name"]?.ToString() ?? "-";
                    string code = "-";
                    string name = "-";

                    if (nameAndCode.Contains("-"))
                    {
                        var parts = nameAndCode.Split('-');
                        code = parts[0].Trim();
                        name = string.Join(" - ", parts.Skip(1).Select(p => p.Trim()));
                    }

                    subcontractors.Add(new
                    {
                        Id = reader.GetInt32("id"),
                        Code = code,
                        Name = name,
                        WorkPhone = reader["work_phone"]?.ToString() ?? "-",
                        MainPhone = reader["main_phone"]?.ToString() ?? "-",
                        Category = reader["Category"]?.ToString() ?? "-",
                        Region = reader["region"]?.ToString() ?? "-",
                        Email = reader["email"]?.ToString() ?? "-",
                        TRN = reader["trn"]?.ToString() ?? "-",
                        Amount = Convert.ToDecimal(reader["Amount"]),
                        Cat_id = reader["Cat_id"] != DBNull.Value ? Convert.ToInt32(reader["Cat_id"]) : 0,
                        Mobile = reader["mobile"]?.ToString() ?? "-",
                        CCMail = reader["ccemail"]?.ToString() ?? "-",
                        Website = reader["website"]?.ToString() ?? "-",
                        Country = reader["country"]?.ToString() ?? "-",
                        City = reader["city"]?.ToString() ?? "-",
                        BuildingName = reader["building_name"]?.ToString() ?? "-",
                        AccountId = reader["account_id"] != DBNull.Value ? Convert.ToInt32(reader["account_id"]) : 0,
                        FaciltyName = reader["facilty_name"]?.ToString() ?? "-",
                        ProjectId = reader["project_id"] != DBNull.Value ? Convert.ToInt32(reader["project_id"]) : 0,
                        ProjectSite = reader["project_site"]?.ToString() ?? "-",
                        //Debit = Convert.ToDecimal(reader["debit"]),
                        //Credit = Convert.ToDecimal(reader["credit"]),
                        Date = reader["date"] != DBNull.Value ? Convert.ToDateTime(reader["date"]) : (DateTime?)null,
                        Active = reader["active"] != DBNull.Value ? Convert.ToInt32(reader["active"]) : 0,
                    });
                }

                return Ok(new
                {
                    status = true,
                    data = subcontractors
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSubContractorInvoices(int id)
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

                string query = @"
        SELECT 
            ROW_NUMBER() OVER (ORDER BY t.id) AS SN,
            t.id,
            t.transaction_id AS InvoiceId,
            t.voucher_no AS VoucherNo,
            t.date,
            t.type,
            ta.name AS Description,
            t.debit,
            t.credit,
            SUM(t.credit - t.debit) OVER (
                ORDER BY t.id 
                ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
            ) AS Balance
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 ta ON t.account_id = ta.id
        WHERE
            t.hum_id = @id
            AND t.state = 0
            AND t.type IN (
                'Subcontractor Payment',
                'Petty Cash',
                'Purchase Invoice',
                'Purchase Invoice Cash',
                'Subcontractor Opening Balance',
                'Check Cancel (Subcontractor)',
                'Purchase Return Invoice',
                'Debit Note',
                'PDC Payable'
            )
        ORDER BY t.id;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.Add(new MySqlParameter("@id", id));

                using var reader = await cmd.ExecuteReaderAsync();

                var transactions = new List<object>();

                while (await reader.ReadAsync())
                {
                    int invoiceId = reader.IsDBNull("InvoiceId") ? 0 : reader.GetInt32("InvoiceId");

                    transactions.Add(new
                    {
                        SN = reader.GetInt32("SN"),
                        Id = reader.GetInt32("id"),
                        InvoiceId = invoiceId,
                        Date = reader.GetDateTime("date").ToString("yyyy-MM-dd"),
                        VoucherNo = string.IsNullOrEmpty(reader["VoucherNo"]?.ToString())
                                    ? $"GV-00{invoiceId}"
                                    : reader["VoucherNo"].ToString(),
                        Type = reader["type"]?.ToString() ?? "",
                        Description = reader["Description"]?.ToString() ?? "",
                        Debit = reader.GetDecimal("debit").ToString("N2"),
                        Credit = reader.GetDecimal("credit").ToString("N2"),
                        Balance = reader.GetDecimal("Balance").ToString("N2")
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
        public async Task<IActionResult> GetSubContractorCashInvoice(int id)
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

                List<string> includeTypes = new() { "%Purchase Invoice Cash%" };
                List<string> excludeTypes = new();

                string query = @"
        SELECT 
            t.id,
            t.transaction_id AS InvoiceId,
            ROW_NUMBER() OVER (ORDER BY t.date, t.id) AS SN,
            t.voucher_no AS VoucherNo,
            t.date,
            CONCAT(ta.code, ' - ', ta.name) AS AccountName,
            t.type,
            CASE 
                WHEN t.type = 'Purchase Invoice Cash' 
                    THEN IF(t.debit = 0, t.credit, t.debit)
                ELSE NULL
            END AS Amount
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 ta ON t.account_id = ta.id
        WHERE t.hum_id = @id AND t.state = 0";

                if (includeTypes.Any())
                    query += " AND (" + string.Join(" OR ",
                        includeTypes.Select((t, i) => $"t.type LIKE @include{i}")) + ")";

                if (excludeTypes.Any())
                    query += " AND (" + string.Join(" AND ",
                        excludeTypes.Select((t, i) => $"t.type NOT LIKE @exclude{i}")) + ")";

                query += " ORDER BY t.date, t.id;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.Add(new MySqlParameter("@id", id));

                for (int i = 0; i < includeTypes.Count; i++)
                    cmd.Parameters.Add(new MySqlParameter($"@include{i}", includeTypes[i]));

                for (int i = 0; i < excludeTypes.Count; i++)
                    cmd.Parameters.Add(new MySqlParameter($"@exclude{i}", excludeTypes[i]));

                using var reader = await cmd.ExecuteReaderAsync();

                var invoices = new List<object>();
                decimal totalAmount = 0;

                while (await reader.ReadAsync())
                {
                    decimal amount = reader.IsDBNull("Amount") ? 0 : reader.GetDecimal("Amount");
                    totalAmount += amount;

                    invoices.Add(new
                    {
                        SN = reader.GetInt32("SN"),
                        InvoiceId = reader.GetInt32("InvoiceId"),
                        VoucherNo = reader["VoucherNo"]?.ToString() ?? "",
                        Date = reader.GetDateTime("date").ToString("yyyy-MM-dd"),
                        AccountName = reader["AccountName"]?.ToString() ?? "",
                        Type = reader["type"]?.ToString() ?? "",
                        Amount = amount.ToString("F3")
                    });
                }

                // Total row (same as WinForms)
                invoices.Add(new
                {
                    SN = "Total",
                    InvoiceId = "",
                    VoucherNo = "",
                    Date = "",
                    AccountName = "Total",
                    Type = "",
                    Amount = totalAmount.ToString("F3")
                });

                return Ok(new { status = true, data = invoices });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveSubContract([FromBody] SubContractRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            if (!string.IsNullOrWhiteSpace(model.TRN))
            {
                if (model.TRN.Length < 3 || model.TRN.Length > 15)
                    return Json(new { status = false, message = "TRN must be between 3 and 15 characters." });
            }

            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { status = false, message = "Enter SubContract Name First." });

            if (model.AccountId == null || model.AccountId <= 0)
                return Json(new { status = false, message = "Account must be set for the SubContract." });

            if (model.OpeningBalanceDate.HasValue &&
                model.OpeningBalanceDate.Value.Date > DateTime.Now.Date)
                return Json(new { status = false, message = "Date value must be less or equal to today." });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in" });

            decimal debit = model.Debit ?? 0;
            decimal credit = model.Credit ?? 0;
            decimal balance = credit - debit;   // ✔ SAME AS WINFORMS

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
                using var tx = await conn.BeginTransactionAsync();

                // ===== DUPLICATE CHECK =====
                var dupSql = "SELECT id FROM tbl_vendor WHERE name=@name";
                using (var dupCmd = new MySqlCommand(dupSql, conn, (MySqlTransaction)tx))
                {
                    dupCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    var existingId = await dupCmd.ExecuteScalarAsync();

                    if (existingId != null &&
                        (model.Id == 0 || Convert.ToInt32(existingId) != model.Id))
                    {
                        return Json(new { status = false, message = "SubContract already exists. Enter another name." });
                    }
                }

                string formattedCode = model.Code;
                var projectSite = string.Join(",", model.ProjectSites);

                // ================= INSERT =================
                if (model.Id == 0)
                {
                    // Generate next SubContract code (starts from 20001)
                    int lastCode = 20000;
                    using (var codeCmd = new MySqlCommand(
                        "SELECT MAX(CAST(code AS UNSIGNED)) FROM tbl_vendor", conn, (MySqlTransaction)tx))
                    {
                        var result = await codeCmd.ExecuteScalarAsync();
                        if (result != DBNull.Value && result != null)
                            lastCode = Convert.ToInt32(result);
                    }
                    formattedCode = (lastCode + 1).ToString("D5");

                    var insertSql = @"
            INSERT INTO tbl_vendor
            (code, NAME, Cat_id, Balance, DATE, main_phone, work_phone, mobile,
             email, ccemail, website, country, city, region, building_name,
             account_id, trn, facilty_name, active, created_by, created_date,
             state, type, project_id, project_site)
            VALUES
            (@code,@name,@cat_id,@balance,@date,@main_phone,@work_phone,@mobile,
             @email,@ccemail,@website,@country,@city,@region,@building_name,
             @account_id,@trn,@facilty_name,@active,@created_by,@created_date,
             0,'Subcontractor',@project_id,@project_site);
            SELECT LAST_INSERT_ID();";

                    using var insertCmd = new MySqlCommand(insertSql, conn, (MySqlTransaction)tx);
                    insertCmd.Parameters.AddWithValue("@code", formattedCode);
                    insertCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    insertCmd.Parameters.AddWithValue("@cat_id", model.CategoryId ?? 0);
                    insertCmd.Parameters.AddWithValue("@balance", balance);
                    insertCmd.Parameters.AddWithValue("@date", model.OpeningBalanceDate);
                    insertCmd.Parameters.AddWithValue("@main_phone", model.MainPhone ?? "");
                    insertCmd.Parameters.AddWithValue("@work_phone", model.WorkPhone ?? "");
                    insertCmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
                    insertCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                    insertCmd.Parameters.AddWithValue("@ccemail", model.CCEmail ?? "");
                    insertCmd.Parameters.AddWithValue("@website", model.Website ?? "");
                    insertCmd.Parameters.AddWithValue("@country", model.CountryId ?? 0);
                    insertCmd.Parameters.AddWithValue("@city", model.CityId ?? 0);
                    insertCmd.Parameters.AddWithValue("@region", model.Region ?? "");
                    insertCmd.Parameters.AddWithValue("@building_name", model.BuildingName ?? "");
                    insertCmd.Parameters.AddWithValue("@account_id", model.AccountId ?? 0);
                    insertCmd.Parameters.AddWithValue("@trn", model.TRN ?? "");
                    insertCmd.Parameters.AddWithValue("@facilty_name", model.FacilityName ?? "");
                    insertCmd.Parameters.AddWithValue("@active", model.Active ? 0 : -1);
                    insertCmd.Parameters.AddWithValue("@created_by", userId);
                    insertCmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    //insertCmd.Parameters.AddWithValue("@project_id", projectSite);
                    //insertCmd.Parameters.AddWithValue("@project_site", projectSite);
                    insertCmd.Parameters.Add("@project_id", MySqlDbType.Int32).Value =
                                    string.IsNullOrWhiteSpace(projectSite) ? 0 : int.Parse(projectSite);
                    insertCmd.Parameters.AddWithValue("@project_site", projectSite ?? "");

                    int subContractId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                    await ProcessSubContractOpeningBalanceAsync(
                        conn, tx, subContractId, formattedCode, model, userId);

                    await tx.CommitAsync();

                    return Json(new { status = true, message = "SubContract added successfully", code = formattedCode });
                }

                // ================= UPDATE =================
                else
                {
                    
                    var updateSql = @"
            UPDATE tbl_vendor SET
                code=@code, NAME=@name, Cat_id=@cat_id, DATE=@date,
                main_phone=@main_phone, work_phone=@work_phone, mobile=@mobile,
                email=@email, ccemail=@ccemail, website=@website,
                country=@country, city=@city, region=@region,
                project_id=@project_id, project_site=@project_site,
                building_name=@building_name, account_id=@account_id,
                trn=@trn, facilty_name=@facilty_name,
                active=@active, balance=@balance, type='Subcontractor'
            WHERE id=@id";

                    using var updateCmd = new MySqlCommand(updateSql, conn, (MySqlTransaction)tx);
                    updateCmd.Parameters.AddWithValue("@code", model.Code);
                    updateCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    updateCmd.Parameters.AddWithValue("@cat_id", model.CategoryId ?? 0);
                    updateCmd.Parameters.AddWithValue("@date", model.OpeningBalanceDate);
                    updateCmd.Parameters.AddWithValue("@main_phone", model.MainPhone ?? "");
                    updateCmd.Parameters.AddWithValue("@work_phone", model.WorkPhone ?? "");
                    updateCmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
                    updateCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                    updateCmd.Parameters.AddWithValue("@ccemail", model.CCEmail ?? "");
                    updateCmd.Parameters.AddWithValue("@website", model.Website ?? "");
                    updateCmd.Parameters.AddWithValue("@country", model.CountryId ?? 0);
                    updateCmd.Parameters.AddWithValue("@city", model.CityId ?? 0);
                    updateCmd.Parameters.AddWithValue("@region", model.Region ?? "");
                    updateCmd.Parameters.AddWithValue("@building_name", model.BuildingName ?? "");
                    updateCmd.Parameters.AddWithValue("@account_id", model.AccountId ?? 0);
                    updateCmd.Parameters.AddWithValue("@trn", model.TRN ?? "");
                    updateCmd.Parameters.AddWithValue("@facilty_name", model.FacilityName ?? "");
                    updateCmd.Parameters.AddWithValue("@active", model.Active ? 0 : -1);
                    updateCmd.Parameters.AddWithValue("@balance", balance);
                    updateCmd.Parameters.AddWithValue("@project_id", projectSite);
                    updateCmd.Parameters.AddWithValue("@project_site", projectSite);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);

                    await updateCmd.ExecuteNonQueryAsync();

                    // Remove old opening balance entries
                    var delCmd = new MySqlCommand(
                        "DELETE FROM tbl_transaction WHERE transaction_id=@id AND type='Subcontractor Opening Balance'",
                        conn, (MySqlTransaction)tx);
                    delCmd.Parameters.AddWithValue("@id", model.Id);
                    await delCmd.ExecuteNonQueryAsync();

                    await ProcessSubContractOpeningBalanceAsync(
                        conn, tx, model.Id, model.Code, model, userId);

                    await tx.CommitAsync();

                    return Json(new { status = true, message = "SubContract updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        private async Task ProcessSubContractOpeningBalanceAsync(
    MySqlConnection conn,
    IDbTransaction tx,
    int subContractId,
    string code,
    SubContractRequest model,
    int userId)
        {
            decimal credit = model.Credit ?? 0;
            decimal debit = model.Debit ?? 0;

            int openingEquityId = 0;
            using (var cmd = new MySqlCommand(
                "SELECT id FROM tbl_coa_level_4 WHERE name='Opening Balance Equity'",
                conn, (MySqlTransaction)tx))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    openingEquityId = Convert.ToInt32(result);
            }

            if (openingEquityId <= 0)
                throw new Exception("Cannot make opening balance without opening balance equity account");

            if (credit != 0)
            {
                await AddTransactionAsync(conn, tx, model.OpeningBalanceDate, openingEquityId,
                    credit, 0, subContractId, 0,
                    "Subcontractor Opening Balance",
                    "OPENING BALANCE",
                    $"Opening Balance Equity - Subcontractor Code - {code}",
                    userId);

                await AddTransactionAsync(conn, tx, model.OpeningBalanceDate, model.AccountId ?? 0,
                    0, credit, subContractId, subContractId,
                    "Subcontractor Opening Balance",
                    "OPENING BALANCE",
                    $"Account Payable - Subcontractor Code - {code}",
                    userId);
            }

            if (debit != 0)
            {
                await AddTransactionAsync(conn, tx, model.OpeningBalanceDate, openingEquityId,
                    0, debit, subContractId, 0,
                    "Subcontractor Opening Balance",
                    "OPENING BALANCE",
                    $"Opening Balance Equity - Subcontractor Code - {code}",
                    userId);

                await AddTransactionAsync(conn, tx, model.OpeningBalanceDate, model.AccountId ?? 0,
                    debit, 0, subContractId, subContractId,
                    "Subcontractor Opening Balance",
                    "OPENING BALANCE",
                    $"Account Payable - Subcontractor Code - {code}",
                    userId);
            }
        }

        private async Task AddTransactionAsync(
    MySqlConnection conn,
    IDbTransaction tx,
    DateTime? date,
    int accountId,
    decimal debit,
    decimal credit,
    int transactionId,
    int humId,
    string type,
    string voucherName,
    string description,
    int createdBy)
        {
            var sql = @"
    INSERT INTO tbl_transaction
    (date, account_id, debit, credit, transaction_id, hum_id,
     t_type, type, description, created_by, created_date, state, voucher_no)
    VALUES
    (@date,@account_id,@debit,@credit,@transaction_id,@hum_id,
     @t_type,@type,@description,@created_by,@created_date,0,'')";

            using var cmd = new MySqlCommand(sql, conn, (MySqlTransaction)tx);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@account_id", accountId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transaction_id", transactionId);
            cmd.Parameters.AddWithValue("@hum_id", humId);
            cmd.Parameters.AddWithValue("@t_type", voucherName);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@created_by", createdBy);
            cmd.Parameters.AddWithValue("@created_date", DateTime.Now);

            await cmd.ExecuteNonQueryAsync();
        }


        [HttpPost]
        public async Task<IActionResult> SaveVendorCategory([FromBody] VendorCategoryRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            string categoryName = model.Name?.Trim();

            // Validation: name required
            if (string.IsNullOrEmpty(categoryName))
                return Json(new { status = false, message = "Please enter a category name first." });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in." });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            try
            {
                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check duplicate category
                var dupQuery = "SELECT id FROM tbl_vendor_category WHERE name=@name";
                using (var dupCmd = new MySqlCommand(dupQuery, conn))
                {
                    dupCmd.Parameters.AddWithValue("@name", categoryName);
                    var existingIdObj = await dupCmd.ExecuteScalarAsync();
                    if (existingIdObj != null)
                    {
                        int existingId = Convert.ToInt32(existingIdObj);
                        if (model.Id == 0 || model.Id != existingId)
                        {
                            return Json(new { status = false, message = "Category already exists. Please enter another name." });
                        }
                    }
                }

                if (model.Id == 0) // Insert
                {
                    var insertQuery = "INSERT INTO tbl_vendor_category (name) VALUES (@name); SELECT LAST_INSERT_ID();";
                    using var insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@name", categoryName);
                    int newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                    return Json(new { status = true, message = "Category added successfully", id = newId, name= categoryName });
                }
                else // Update
                {
                    var updateQuery = "UPDATE tbl_vendor_category SET name=@name WHERE id=@id";
                    using var updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@name", categoryName);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);

                    int affected = await updateCmd.ExecuteNonQueryAsync();

                    return Json(new { status = true, message = "Category updated successfully", id=model.Id, name = categoryName });
                }
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetVendorCategories()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in." });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            try
            {
                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = "SELECT * FROM tbl_vendor_category";
                using var cmd = new MySqlCommand(query, conn);

                using var reader = await cmd.ExecuteReaderAsync();
                var categories = new List<object>();

                while (await reader.ReadAsync())
                {
                    categories.Add(new
                    {
                        Id = reader.GetInt32("id"),
                        Name = reader["name"].ToString()
                    });
                }

                if (categories.Count == 0)
                    return Json(new { status = false, message = "No categories available." });

                return Json(new { status = true, data = categories });
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }


        [HttpGet]
        public async Task<IActionResult> GetCustomerDetails(int id)
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

                // Customer details query
                string query = @"
        SELECT 
            c.id,
            c.code,
            c.name,
            c.work_phone,
            c.main_phone,
            c.email,
            c.mobile,
            c.ccemail,
            c.website,
            c.country,
            c.city,
            c.region,
            c.building_name,
            c.account_id,
            c.facilty_name,
            c.project_id,
            c.project_site,
            c.active,
            c.date,
            tc.name AS Category,
            c.trn
        FROM tbl_vendor c
        LEFT JOIN tbl_vendor_category tc ON c.Cat_id = tc.id
        WHERE c.id = @id";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.Add(new MySqlParameter("@id", id));

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return NotFound(new { status = false, message = "Customer not found" });
                }

                // Fetch the customer data
                var customer = new
                {
                    Id = reader.GetInt32("id"),
                    Code = reader.GetInt32("code"),
                    Name = reader.GetString("name"),
                    WorkPhone = reader["work_phone"]?.ToString() ?? "-",
                    MainPhone = reader["main_phone"]?.ToString() ?? "-",
                    Email = reader["email"]?.ToString() ?? "-",
                    Mobile = reader["mobile"]?.ToString() ?? "-",
                };

                return Ok(new
                {
                    status = true,
                    data = new
                    {
                        Customer = customer,
                    }
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetVendorInvoices(int id, string startDate = null, string endDate = null)
        {
            try
            {
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
            t.transaction_id AS InvoiceId,
            t.voucher_no AS VoucherNo,
            t.date,
            t.type,
            ta.name AS Description,
            t.debit,
            t.credit
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 ta ON t.account_id = ta.id
        WHERE t.hum_id = @id 
          AND t.state = 0
          AND t.type IN (
              'Subcontractor Payment',
              'Petty Cash', 
              'Purchase Invoice', 
              'Purchase Invoice Cash', 
              'Subcontractor Opening Balance', 
              'Check Cancel (Subcontractor)', 
              'Purchase Return Invoice', 
              'Debit Note', 
              'PDC Payable'
          )";

                // Add date filter if provided
                if (!string.IsNullOrEmpty(startDate))
                {
                    query += " AND t.date >= @startDate";
                }

                if (!string.IsNullOrEmpty(endDate))
                {
                    query += " AND t.date <= @endDate";
                }

                query += " ORDER BY t.id;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                if (!string.IsNullOrEmpty(startDate))
                {
                    cmd.Parameters.Add(new MySqlParameter("@startDate", DateTime.Parse(startDate)));
                }

                if (!string.IsNullOrEmpty(endDate))
                {
                    cmd.Parameters.Add(new MySqlParameter("@endDate", DateTime.Parse(endDate)));
                }

                using var reader = await cmd.ExecuteReaderAsync();

                var transactions = new List<object>();
                decimal runningBalance = 0;
                decimal totalDebit = 0;
                decimal totalCredit = 0;
                int displaySN = 1;

                while (await reader.ReadAsync())
                {
                    string type = reader["type"]?.ToString() ?? "";

                    decimal originalDebit = reader.IsDBNull(reader.GetOrdinal("debit")) ? 0 : reader.GetDecimal("debit");
                    decimal originalCredit = reader.IsDBNull(reader.GetOrdinal("credit")) ? 0 : reader.GetDecimal("credit");

                    string invoiceIdStr = reader["InvoiceId"]?.ToString() ?? "0";
                    int invoiceId = 0;
                    int.TryParse(invoiceIdStr, out invoiceId);

                    string voucherNo = string.IsNullOrEmpty(reader["VoucherNo"]?.ToString()) ? $"PV-00{invoiceId}" : reader["VoucherNo"].ToString();
                    string description = reader["Description"]?.ToString() ?? "";
                    string dateStr = reader.GetDateTime("date").ToString("yyyy-MM-dd");
                    int transactionId = Convert.ToInt32(reader["id"]);

                    if (type.Equals("Purchase Invoice Cash", StringComparison.OrdinalIgnoreCase))
                    {
                        // Split Purchase Invoice Cash into TWO rows
                        decimal amount = originalCredit > 0 ? originalCredit : originalDebit;

                        // Row 1: Credit (Purchase)
                        runningBalance += amount;
                        totalCredit += amount;

                        transactions.Add(new
                        {
                            SN = displaySN++,
                            Id = transactionId,
                            InvoiceId = invoiceId,
                            Date = dateStr,
                            VoucherNo = voucherNo,
                            Type = type,
                            Description = description,
                            Debit = "0.00",
                            Credit = amount.ToString("N2"),
                            Balance = runningBalance.ToString("N2")
                        });

                        // Row 2: Debit (Payment)
                        runningBalance -= amount;
                        totalDebit += amount;

                        transactions.Add(new
                        {
                            SN = displaySN++,
                            Id = transactionId,
                            InvoiceId = invoiceId,
                            Date = dateStr,
                            VoucherNo = voucherNo,
                            Type = "Cash Payment",
                            Description = description,
                            Debit = amount.ToString("N2"),
                            Credit = "0.00",
                            Balance = runningBalance.ToString("N2")
                        });
                    }
                    else if (type.Equals("Subcontractor Payment", StringComparison.OrdinalIgnoreCase))
                    {
                        // Subcontractor Payment: Show credit amount as debit only, reduce balance
                        decimal amount = originalCredit > 0 ? originalCredit : originalDebit;
                        runningBalance -= amount;
                        totalDebit += amount;

                        transactions.Add(new
                        {
                            SN = displaySN++,
                            Id = transactionId,
                            InvoiceId = invoiceId,
                            Date = dateStr,
                            VoucherNo = voucherNo,
                            Type = type,
                            Description = description,
                            Debit = amount.ToString("N2"),
                            Credit = "0.00",
                            Balance = runningBalance.ToString("N2")
                        });
                    }
                    else
                    {
                        // Purchase Invoice transactions on credit: Normal display, calculate running balance
                        runningBalance += originalCredit - originalDebit;
                        totalDebit += originalDebit;
                        totalCredit += originalCredit;

                        transactions.Add(new
                        {
                            SN = displaySN++,
                            Id = transactionId,
                            InvoiceId = invoiceId,
                            Date = dateStr,
                            VoucherNo = voucherNo,
                            Type = type,
                            Description = description,
                            Debit = originalDebit.ToString("N2"),
                            Credit = originalCredit.ToString("N2"),
                            Balance = runningBalance.ToString("N2")
                        });
                    }
                }

                return Ok(new { status = true, data = transactions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetVendorCashInvoices(int id, string startDate = null, string endDate = null)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                List<string> includeTypes = new() { "%Purchase Invoice Cash%" };

                string query = @"
        SELECT 
            t.id,
            t.transaction_id AS InvoiceId,
            t.voucher_no AS VoucherNo,
            t.date,
            CONCAT(ta.code, ' - ', ta.name) AS AccountName,
            t.type,
            CASE 
                WHEN t.type LIKE 'Purchase Invoice Cash%' THEN IF(t.debit = 0, t.credit, t.debit)
                ELSE 0
            END AS Amount
        FROM tbl_transaction t
        INNER JOIN tbl_coa_level_4 ta ON t.account_id = ta.id
        WHERE t.hum_id = @id AND t.state = 0";

                if (includeTypes.Any())
                    query += " AND (" + string.Join(" OR ", includeTypes.Select((t, i) => $"t.type LIKE @include{i}")) + ")";

                // Add date filter if provided
                if (!string.IsNullOrEmpty(startDate))
                {
                    query += " AND t.date >= @startDate";
                }

                if (!string.IsNullOrEmpty(endDate))
                {
                    query += " AND t.date <= @endDate";
                }

                query += " ORDER BY t.date, t.id;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                for (int i = 0; i < includeTypes.Count; i++)
                    cmd.Parameters.AddWithValue($"@include{i}", includeTypes[i]);

                if (!string.IsNullOrEmpty(startDate))
                {
                    cmd.Parameters.Add(new MySqlParameter("@startDate", DateTime.Parse(startDate)));
                }

                if (!string.IsNullOrEmpty(endDate))
                {
                    cmd.Parameters.Add(new MySqlParameter("@endDate", DateTime.Parse(endDate)));
                }

                using var reader = await cmd.ExecuteReaderAsync();
                var invoices = new List<object>();
                decimal totalAmount = 0;
                int snCounter = 1;

                while (await reader.ReadAsync())
                {
                    string invoiceIdStr = reader["InvoiceId"]?.ToString() ?? "0";
                    int invoiceId = 0;
                    int.TryParse(invoiceIdStr, out invoiceId);

                    decimal amount = reader.IsDBNull(reader.GetOrdinal("Amount")) ? 0 : reader.GetDecimal("Amount");
                    totalAmount += amount;

                    invoices.Add(new
                    {
                        SN = snCounter++,
                        InvoiceId = invoiceId,
                        VoucherNo = string.IsNullOrEmpty(reader["VoucherNo"]?.ToString()) ? $"GV-00{invoiceId}" : reader["VoucherNo"].ToString(),
                        Date = reader.GetDateTime("date").ToString("yyyy-MM-dd"),
                        AccountName = reader["AccountName"]?.ToString() ?? "",
                        Type = reader["type"]?.ToString() ?? "",
                        Amount = amount.ToString("N2")
                    });
                }

                // Total row
                invoices.Add(new
                {
                    SN = "Total",
                    InvoiceId = "",
                    VoucherNo = "",
                    Date = "",
                    AccountName = "Total",
                    Type = "",
                    Amount = totalAmount.ToString("N2")
                });

                return Ok(new { status = true, data = invoices });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }



        #endregion

        #region Purchase Center

        public IActionResult PurchaseCenter()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetPurchases(
    DateTime? dateFrom,
    DateTime? dateTo,
    int? customerId,
    string paymentMethod,
    string selectionMethod = "Default",
    int page = 1,
    int pageSize = 20)
        {
            return await GetPurchasesByVendorType(
                "Subcontractor", dateFrom, dateTo, customerId, paymentMethod, selectionMethod, page, pageSize);
        }
        private async Task<IActionResult> GetPurchasesByVendorType(
    string vendorType,
    DateTime? dateFrom,
    DateTime? dateTo,
    int? customerId,
    string paymentMethod,
    string selectionMethod,
    int page,
    int pageSize)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = selectionMethod == "Default"
                    ? GetPurchaseDefaultQuery()
                    : GetPurchaseDetailedQuery();

                var parameters = new List<MySqlParameter>();

                // Vendor type filter — now active
                query += " AND v.type = @vendorType ";
                parameters.Add(new MySqlParameter("@vendorType", vendorType));

                if (customerId.HasValue)
                {
                    query += " AND p.vendor_id = @vendorId ";
                    parameters.Add(new MySqlParameter("@vendorId", customerId.Value));
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query += " AND LOWER(p.payment_method) = LOWER(@payment) ";
                    parameters.Add(new MySqlParameter("@payment", paymentMethod.Trim()));
                }

                if (dateFrom.HasValue)
                {
                    query += " AND p.date >= @dateFrom ";
                    parameters.Add(new MySqlParameter("@dateFrom", dateFrom.Value.Date));
                }

                if (dateTo.HasValue)
                {
                    query += " AND p.date <= @dateTo ";
                    parameters.Add(new MySqlParameter("@dateTo", dateTo.Value.Date));
                }

                var purchases = new List<PurchaseDto>();

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();

                PurchaseDto currentPurchase = null;
                int lastPurchaseId = -1;

                while (await reader.ReadAsync())
                {
                    int purchaseId = reader.GetInt32("Id");

                    if (purchaseId != lastPurchaseId)
                    {
                        currentPurchase = new PurchaseDto
                        {
                            Id = purchaseId,
                            Date = reader.GetDateTime("Date"),
                            InvoiceNo = reader["InvoiceNo"]?.ToString(),
                            VendorId = reader["VendorId"] != DBNull.Value ? Convert.ToInt32(reader["VendorId"]) : 0,
                            VendorName = reader["VendorName"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),
                            Total = reader["Total"] != DBNull.Value ? Convert.ToDecimal(reader["Total"]) : 0,
                            Vat = reader["Vat"] != DBNull.Value ? Convert.ToDecimal(reader["Vat"]) : 0,
                            Net = reader["Net"] != DBNull.Value ? Convert.ToDecimal(reader["Net"]) : 0,
                            RetentionPercentage = reader["RetentionPercentage"] != DBNull.Value ? Convert.ToDecimal(reader["RetentionPercentage"]) : 0,
                            RetentionAmount = reader["RetentionAmount"] != DBNull.Value ? Convert.ToDecimal(reader["RetentionAmount"]) : 0,
                            WarehouseId = reader["Warehouse_Id"] != DBNull.Value ? reader.GetInt32("Warehouse_Id") : (int?)null,
                            PO_Num = reader["PO_Num"]?.ToString(),
                            BillTo = reader["Bill_To"]?.ToString(),
                            PurchaseType = reader["PurchaseType"]?.ToString(),
                            FixedAssetCategoryId = reader["FixedAssetCategoryId"] != DBNull.Value ? reader.GetInt32("FixedAssetCategoryId") : (int?)null,
                            City = reader["City"]?.ToString(),
                            Ship_Date = reader["Ship_Date"] != DBNull.Value ? reader.GetDateTime("Ship_Date") : (DateTime?)null,
                            Ship_Via = reader["Ship_Via"]?.ToString(),
                            Ship_To = reader["Ship_To"]?.ToString(),
                            Account_Cash_Id = reader["Account_Cash_Id"] != DBNull.Value ? reader.GetInt32("Account_Cash_Id") : (int?)null,
                            Payment_Terms = reader["Payment_Terms"]?.ToString(),
                            Payment_Date = reader["Payment_Date"] != DBNull.Value ? reader.GetDateTime("Payment_Date") : (DateTime?)null,
                            Description = reader["Description"]?.ToString(),
                            SalesMan = reader["Sales_Man"]?.ToString(),
                            Pay = reader["Pay"] != DBNull.Value ? reader.GetDecimal("Pay") : 0,
                            Items = new List<PurchaseItemDto>()
                        };

                        purchases.Add(currentPurchase);
                        lastPurchaseId = purchaseId;
                    }

                    if (reader["ItemId"] != DBNull.Value)
                    {
                        currentPurchase.Items.Add(new PurchaseItemDto
                        {
                            ItemId = reader["ItemId"] != DBNull.Value ? Convert.ToInt32(reader["ItemId"]) : 0,
                            ItemCode = reader["ItemCode"]?.ToString(),
                            ItemName = reader["ItemName"]?.ToString(),
                            Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                            CostPrice = reader["CostPrice"] != DBNull.Value ? Convert.ToDecimal(reader["CostPrice"]) : 0,
                            Discount = reader["Discount"] != DBNull.Value ? Convert.ToDecimal(reader["Discount"]) : 0,
                            Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0,
                            Vat = reader["ItemVat"] != DBNull.Value ? Convert.ToDecimal(reader["ItemVat"]) : 0,
                            VatP = reader["VatP"] != DBNull.Value ? Convert.ToDecimal(reader["VatP"]) : 0,
                            Total = reader["ItemTotal"] != DBNull.Value ? Convert.ToDecimal(reader["ItemTotal"]) : 0,
                            Cost_Center_Id = reader["Cost_Center_Id"] != DBNull.Value ? Convert.ToInt32(reader["Cost_Center_Id"]) : (int?)null
                        });
                    }
                }

                return Ok(new
                {
                    status = true,
                    page,
                    pageSize,
                    data = purchases
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        private string GetPurchaseDefaultQuery()
        {
            return @"
      SELECT 
    p.id AS Id,
    p.date AS Date,
    p.invoice_id AS InvoiceNo,
    v.id AS VendorId,
    CONCAT(v.code,' - ',v.name) AS VendorName,
    p.payment_method AS PaymentMethod,
    p.total AS Total,
    p.vat AS Vat,
    p.net AS Net,
    i.code AS ItemCode,
    i.name AS ItemName,
    pd.qty AS Qty,
    pd.cost_price AS CostPrice,
    pd.price As Price,
    pd.vat AS ItemVat,
    pd.vatp AS VatP,
    pd.discount AS Discount,
    pd.total AS ItemTotal,
    p.warehouse_id AS Warehouse_Id,
    p.po_num AS PO_Num,
    p.bill_to AS Bill_To,
    p.sales_man AS Sales_Man,
    p.purchase_type AS PurchaseType,
    p.fixed_asset_category_id As FixedAssetCategoryId,
    p.city AS City,
    p.ship_date AS Ship_Date,
    p.ship_via AS Ship_Via,
    p.ship_to AS Ship_To,
    p.account_cash_id AS Account_Cash_Id,
    p.payment_terms AS Payment_Terms,
    p.payment_date AS Payment_Date,
    p.description AS Description,
    p.pay AS Pay,
    p.retention_percentage AS RetentionPercentage,
    p.retention_amount AS RetentionAmount,
    i.id AS ItemId,
    pd.cost_center_id AS Cost_Center_Id
FROM tbl_purchase p
INNER JOIN tbl_vendor v ON p.vendor_id = v.id
LEFT JOIN tbl_purchase_details pd ON p.id = pd.purchase_id
LEFT JOIN tbl_items i ON pd.item_id = i.id
WHERE p.state = 0
    ";
        }

        private string GetPurchaseDetailedQuery()
        {
            return @"
      SELECT
    p.id AS Id,
    p.date AS Date,
    p.invoice_id AS InvoiceNo,
    CONCAT(v.code,' - ',v.name) AS VendorName,
    p.payment_method AS PaymentMethod,
    p.total AS Total,
    p.vat AS Vat,
    d.vatp AS VatP,
    p.net AS Net,
    v.id AS VendorId,
    CONCAT(i.code,' - ',i.name) AS ItemName,
    d.qty AS Qty,
    d.cost_price AS CostPrice,
    d.discount AS Discount,
    d.vat AS ItemVat,
      i.code AS ItemCode,
    d.total AS ItemTotal,
    p.warehouse_id AS Warehouse_Id,
    p.po_num AS PO_Num,
    p.bill_to AS Bill_To,
    p.purchase_type AS PurchaseType,
    p.fixed_asset_category_id As FixedAssetCategoryId,
    p.city AS City,
     p.retention_percentage AS RetentionPercentage,
    p.retention_amount AS RetentionAmount,
  p.sales_man AS Sales_Man,
    p.ship_date AS Ship_Date,
    p.ship_via AS Ship_Via,
    p.ship_to AS Ship_To,
    p.account_cash_id AS Account_Cash_Id,
    p.payment_terms AS Payment_Terms,
    p.payment_date AS Payment_Date,
    p.description AS Description,
    p.pay AS Pay,
    i.id As ItemId,
    d.price As Price,
    d.cost_center_id AS Cost_Center_Id
FROM tbl_purchase p
INNER JOIN tbl_purchase_details d ON p.id = d.purchase_id
INNER JOIN tbl_items i ON d.item_id = i.id
INNER JOIN tbl_vendor v ON p.vendor_id = v.id
WHERE p.state = 0

    ";
        }


        #endregion

    }
}
