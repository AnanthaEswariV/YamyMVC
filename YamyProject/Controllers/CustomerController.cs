using Microsoft.AspNetCore.Mvc;

namespace YamyProject.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public CustomerController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }


        #region Customer CRUD Operation

        public IActionResult Customer()
        {
            return View();  
        }

        [HttpPost]
        public async Task<IActionResult> SaveCustomer([FromBody] CustomerRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            // Basic validations
            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { status = false, message = "Please enter customer name" });

            if (model.CategoryId == null || model.CategoryId <= 0)
                return Json(new { status = false, message = "Please select customer category" });

            if (model.CountryId == null || model.CountryId <= 0)
                return Json(new { status = false, message = "Please select country" });

            if (model.OpeningBalanceDate.HasValue &&
    model.OpeningBalanceDate.Value.Date > DateTime.Now.Date)
            {
                return Json(new { status = false, message = "Opening balance date must be today or earlier" });
            }

            if (!string.IsNullOrWhiteSpace(model.TRN))
            {
                if (model.TRN.Length < 3 || model.TRN.Length > 15)
                {
                    return Json(new { status = false, message = "TRN must be between 3 and 15 characters" });
                }
            }


            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in" });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            try
            {
                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check duplicate customer
                var dupQuery = "SELECT id FROM tbl_customer WHERE name=@name";
                using (var dupCmd = new MySqlCommand(dupQuery, conn))
                {
                    dupCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    var existingId = await dupCmd.ExecuteScalarAsync();
                    if (existingId != null && (model.Id == 0 || Convert.ToInt32(existingId) != model.Id))
                    {
                        return Json(new { status = false, message = "Customer already exists. Enter another name." });
                    }
                }

                string formattedCode = model.Code;
                if (model.Id == 0) // Insert
                {
                    // Generate next customer code
                    int lastCode = 0;
                    var codeQuery = "SELECT MAX(CAST(code AS UNSIGNED)) FROM tbl_customer";
                    using (var codeCmd = new MySqlCommand(codeQuery, conn))
                    {
                        var result = await codeCmd.ExecuteScalarAsync();
                        if (result != DBNull.Value && result != null)
                            lastCode = Convert.ToInt32(result);
                    }
                    formattedCode = (lastCode + 1).ToString("D5");

                    // Insert customer
                    var projectSite = string.Join(",", model.ProjectSites);
                    var insertQuery = @"INSERT INTO tbl_customer 
        (code, NAME, Cat_id, Balance, DATE, main_phone, work_phone, mobile, email, ccemail, website, 
        country, city, region, building_name, account_id, trn, facilty_name, active, created_by, created_date, state, project_site)
        VALUES(@code,@name,@cat_id,@balance,@date,@main_phone,@work_phone,@mobile,@email,@ccemail,@website,
        @country,@city,@region,@building_name,@account_id,@trn,@facilty_name,@active,@created_by,@created_date,0,@project_site);
        SELECT LAST_INSERT_ID();";

                    using var insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@code", formattedCode);
                    insertCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    insertCmd.Parameters.AddWithValue("@cat_id", model.CategoryId);
                    decimal balance = (model.Debit ?? 0) - (model.Credit ?? 0);
                    insertCmd.Parameters.AddWithValue("@balance", balance);
                    insertCmd.Parameters.AddWithValue("@date", model.OpeningBalanceDate);
                    insertCmd.Parameters.AddWithValue("@main_phone", model.MainPhone ?? "");
                    insertCmd.Parameters.AddWithValue("@work_phone", model.WorkPhone ?? "");
                    insertCmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
                    insertCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                    insertCmd.Parameters.AddWithValue("@ccemail", model.CCEmail ?? "");
                    insertCmd.Parameters.AddWithValue("@website", model.Website ?? "");
                    insertCmd.Parameters.AddWithValue("@country", model.CountryId);
                    insertCmd.Parameters.AddWithValue("@city", model.CityId ?? 0);
                    insertCmd.Parameters.AddWithValue("@region", model.Region ?? "");
                    insertCmd.Parameters.AddWithValue("@building_name", model.BuildingName ?? "");
                    insertCmd.Parameters.AddWithValue("@account_id", model.AccountId ?? 0);
                    insertCmd.Parameters.AddWithValue("@trn", model.TRN ?? "");
                    insertCmd.Parameters.AddWithValue("@facilty_name", model.FacilityName ?? "");
                    insertCmd.Parameters.AddWithValue("@active", model.Active ? 0 : -1);
                    insertCmd.Parameters.AddWithValue("@created_by", userId);
                    insertCmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    insertCmd.Parameters.AddWithValue("@project_site", projectSite);

                    var customerId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                    // Process opening balance
                    await ProcessOpeningBalanceAsync(conn, customerId, formattedCode, userId, model);

                    return Json(new { status = true, message = "Customer added successfully", code = formattedCode });
                }
                else // Update
                {

                    var projectSite = string.Join(",", model.ProjectSites);
                    var updateQuery = @"UPDATE tbl_customer SET 
                            code=@code, NAME=@name, Cat_id=@cat_id, DATE=@date, main_phone=@main_phone,
                            work_phone=@work_phone, mobile=@mobile, email=@email, ccemail=@ccemail, website=@website,
                            country=@country, city=@city, region=@region,  project_site=@project_site,
                            building_name=@building_name, account_id=@account_id, trn=@trn, facilty_name=@facilty_name,
                            active=@active, Balance=@balance
                        WHERE id=@id";

                    using var updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@code", model.Code);
                    updateCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    updateCmd.Parameters.AddWithValue("@cat_id", model.CategoryId);
                    updateCmd.Parameters.AddWithValue("@date", model.OpeningBalanceDate);
                    updateCmd.Parameters.AddWithValue("@main_phone", model.MainPhone ?? "");
                    updateCmd.Parameters.AddWithValue("@work_phone", model.WorkPhone ?? "");
                    updateCmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
                    updateCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                    updateCmd.Parameters.AddWithValue("@ccemail", model.CCEmail ?? "");
                    updateCmd.Parameters.AddWithValue("@website", model.Website ?? "");
                    updateCmd.Parameters.AddWithValue("@country", model.CountryId);
                    updateCmd.Parameters.AddWithValue("@city", model.CityId ?? 0);
                    updateCmd.Parameters.AddWithValue("@region", model.Region ?? "");
                    updateCmd.Parameters.AddWithValue("@building_name", model.BuildingName ?? "");
                    updateCmd.Parameters.AddWithValue("@account_id", model.AccountId ?? 0);
                    updateCmd.Parameters.AddWithValue("@trn", model.TRN ?? "");
                    updateCmd.Parameters.AddWithValue("@facilty_name", model.FacilityName ?? "");
                    updateCmd.Parameters.AddWithValue("@active", model.Active ? 0 : -1);
                    decimal balance = (model.Debit ?? 0) - (model.Credit ?? 0);
                    updateCmd.Parameters.AddWithValue("@balance", balance);
                    updateCmd.Parameters.AddWithValue("@project_site", projectSite);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);

                    int affected = await updateCmd.ExecuteNonQueryAsync();

                    return Json(new { status = true, message = "Customer updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }


        // Helper method to process opening balance
        private async Task ProcessOpeningBalanceAsync(MySqlConnection conn, int customerId, string formattedCode, int userId,CustomerRequest model)
        {
            decimal debitAmount = model.Debit ?? 0;
            decimal creditAmount = model.Credit ?? 0;
            string accountId = model.AccountId?.ToString() ?? "0";

            // Get Opening Balance Equity account
            string openingBalanceEquity = await SelectDefaultLevelAccountAsync(conn, "Opening Balance Equity");
            if (string.IsNullOrWhiteSpace(openingBalanceEquity) || openingBalanceEquity == "0")
            {
                var cmd = new MySqlCommand("SELECT id FROM tbl_coa_level_4 WHERE name='Opening Balance Equity'", conn);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    openingBalanceEquity = result.ToString();
            }

            if (string.IsNullOrWhiteSpace(openingBalanceEquity) || openingBalanceEquity == "0")
                throw new Exception("Cannot make opening balance without Opening Balance Equity account");

            // Insert transactions
            if (creditAmount != 0)
            {
                await AddTransactionEntryAsync(conn, model.OpeningBalanceDate ?? DateTime.Now, openingBalanceEquity, "0", creditAmount.ToString(),
                    customerId.ToString(), "0", "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance Equity - Customer Code - {formattedCode}", userId, DateTime.Now, "");

                await AddTransactionEntryAsync(conn, model.OpeningBalanceDate ?? DateTime.Now, accountId, "0", creditAmount.ToString(),
                    customerId.ToString(), customerId.ToString(), "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance - Customer Code - {formattedCode}", userId, DateTime.Now, "");
            }

            if (debitAmount != 0)
            {
                await AddTransactionEntryAsync(conn, model.OpeningBalanceDate ?? DateTime.Now, openingBalanceEquity, debitAmount.ToString(), "0",
                    customerId.ToString(), "0", "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance Equity - Customer Code - {formattedCode}", userId, DateTime.Now, "");

                await AddTransactionEntryAsync(conn, model.OpeningBalanceDate ?? DateTime.Now, accountId, debitAmount.ToString(), "0",
                    customerId.ToString(), customerId.ToString(), "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance - Customer Code - {formattedCode}", userId, DateTime.Now, "");
            }
        }


        public static async Task AddTransactionEntryAsync(MySqlConnection conn, DateTime date, string accountId, string debit, string credit,
               string transactionId, string humId, string type, string voucherName, string description,
               int createdBy, DateTime createdDate, string voucherNo)
        {
            var query = @"INSERT INTO tbl_transaction 
            (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no) 
            VALUES (@date, @accountId, @debit, @credit, @transactionId, @hum_id, @tType, @type, @description, @createdBy, @createdDate, 0, @voucher_no)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transactionId", transactionId);
            cmd.Parameters.AddWithValue("@hum_id", humId);
            cmd.Parameters.AddWithValue("@tType", voucherName);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@createdBy", createdBy);
            cmd.Parameters.AddWithValue("@createdDate", createdDate);
            cmd.Parameters.AddWithValue("@voucher_no", voucherNo);

            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<string> SelectDefaultLevelAccountAsync(MySqlConnection conn, string accountName)
            {
                var cmd = new MySqlCommand("SELECT id FROM tbl_coa_level_4 WHERE name=@name LIMIT 1", conn);
                cmd.Parameters.AddWithValue("@name", accountName);
                var result = await cmd.ExecuteScalarAsync();
                return result?.ToString() ?? "0";
            }

        [HttpPost]
        public async Task<IActionResult> AddCategory([FromBody] CategoryRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Category name is required" });

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

                // Check for duplicate name
                var checkQuery = "SELECT COUNT(*) FROM tbl_customer_category WHERE name = @name";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

                if (exists)
                    return BadRequest(new { status = false, message = "Category name already exists" });

                // Insert new category
                var insertQuery = @"INSERT INTO tbl_customer_category (name) 
                            VALUES (@name);
                            SELECT LAST_INSERT_ID();";
                using var insertCmd = new MySqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@name", model.Name.Trim());

                var newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                return Ok(new { status = true, id = newId, name = model.Name.Trim() });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerCategories()
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var categories = new List<object>();

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = "SELECT id, name FROM tbl_customer_category ORDER BY name";
                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    categories.Add(new
                    {
                        id = reader.GetInt32("id"),
                        name = reader.GetString("name")
                    });
                }

                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers(string state = "All")
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

                // Base query
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
        FROM tbl_customer c
        LEFT JOIN (
            SELECT 
                hum_id,
                SUM(debit - credit) AS Amount
            FROM tbl_transaction
            WHERE 
                state = 0 AND
                type IN (
                    'Customer Receipt',
                    'Sales Invoice',
                    'Customer Opening Balance',
                    'Customer Advance Payment',
                    'Check Cancel (Customer)',
                    'SalesReturn Invoice',
                    'Credit Note',
                    'PDC Receivable'
                )
            GROUP BY hum_id
        ) t ON t.hum_id = c.id
        LEFT JOIN tbl_customer_category tc ON c.Cat_id = tc.id
        WHERE 1 = 1";

                // Filter based on state
                if (state == "Active Customer")
                    query += " AND c.active = 0";
                else if (state == "Inactive Customer")
                    query += " AND c.active != 0";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var customers = new List<object>();
                while (await reader.ReadAsync())
                {
                    string nameAndCode = reader["Name"].ToString();
                    string code = "-";
                    string name = "-";
                    if (!string.IsNullOrWhiteSpace(nameAndCode) && nameAndCode.Contains("-"))
                    {
                        var parts = nameAndCode.Split('-');
                        code = parts[0].Trim();
                        name = string.Join(" - ", parts.Skip(1).Select(p => p.Trim()));
                    }

                    customers.Add(new
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
                        Cat_id = reader.GetInt32("Cat_id"),
                        Mobile = reader["mobile"]?.ToString() ?? "-",
                        CCMail = reader["ccemail"]?.ToString() ?? "-",
                        Website = reader["website"]?.ToString() ?? "-",
                        Country = reader["country"]?.ToString() ?? "-",
                        City = reader["city"]?.ToString() ?? "-",
                        BuildingName = reader["building_name"]?.ToString() ?? "-",
                        AccountId = reader.GetInt32("account_id"),
                        FaciltyName = reader["facilty_name"]?.ToString() ?? "-",
                        ProjectId = reader.GetInt32("project_id"),
                        ProjectSite = reader["project_site"]?.ToString() ?? "-",
                        //Debit = Convert.ToDecimal(reader["debit"]),
                        //Credit = Convert.ToDecimal(reader["credit"]),
                        Date = reader["date"] != DBNull.Value ? Convert.ToDateTime(reader["date"]) : (DateTime?)null,
                        Active = reader.GetInt32("active")

                    });
                }

                return Ok(new { status = true, data = customers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
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
        FROM tbl_customer c
        LEFT JOIN tbl_customer_category tc ON c.Cat_id = tc.id
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
        public async Task<IActionResult> GetInvoices(int id)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase"),
                   
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
        SELECT
            ROW_NUMBER() OVER (ORDER BY t.id) AS SN,
            t.id,
            t.transaction_id AS InvoiceId,
             t.voucher_no AS 'V - No',
            t.date,
            ta.name AS AccountName,
            t.type,
            t.debit,
            t.credit,
           SUM(t.debit - t.credit) OVER (ORDER BY t.id ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS 'Balance'
        FROM 
            tbl_transaction t
        INNER JOIN 
            tbl_coa_level_4 ta ON t.account_id = ta.id
        WHERE
            t.hum_id = @id 
            AND t.state = 0 
            AND t.type IN (
                'Customer Receipt', 
                'Sales Invoice', 
                'Sales Invoice Cash', 
                'Customer Opening Balance', 
                'Customer Advance Payment',
                'Check Cancel (Customer)', 
                'SalesReturn Invoice', 
                'Credit Note', 
                'PDC Receivable'
            )";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.Add(new MySqlParameter("@id", id));

                using var reader = await cmd.ExecuteReaderAsync();

                var transactions = new List<object>();
                decimal totalAmount = 0, totalPaidAmount = 0, amount = 0, paidAmount = 0;
                string _type = "";

                while (await reader.ReadAsync())
                {
                    _type = reader.IsDBNull(reader.GetOrdinal("type")) ? "" : reader.GetString("type");

                    amount = reader.IsDBNull(reader.GetOrdinal("debit")) ? 0 : reader.GetDecimal("debit");
                    paidAmount = reader.IsDBNull(reader.GetOrdinal("credit")) ? 0 : reader.GetDecimal("credit");
                    decimal balance = reader.IsDBNull(reader.GetOrdinal("Balance")) ? 0 : reader.GetDecimal("Balance");

                    totalAmount += amount - paidAmount;
                    totalPaidAmount += paidAmount;



                    string invoiceIdStr = reader.IsDBNull(reader.GetOrdinal("InvoiceId")) ? null : reader.GetString("InvoiceId");
                    int invoiceId = 0;
                    if (!string.IsNullOrEmpty(invoiceIdStr))
                    {
                        int.TryParse(invoiceIdStr, out invoiceId); 
                    }

                    transactions.Add(new
                    {
                        SN = reader.GetInt32("SN"),
                        Id = reader.GetInt32("id"),
                        InvoiceId = invoiceId, // Store as an integer (or 0 if parse failed)
                        Date = reader.GetDateTime("date").ToString("yyyy-MM-dd"), // Format the date
                        VoucherNo = string.IsNullOrEmpty(reader.GetString("V - No")) ? $"GV-00{invoiceId}" : reader.GetString("V - No"),
                        Type = _type,
                        AccountName = reader.GetString("AccountName"),
                        Debit = amount.ToString("N2"),
                        Credit = paidAmount.ToString("N2"),
                        Balance = balance.ToString("N2")
                    });

                }

                // Add total row
                transactions.Add(new
                {
                    SN = "Total",
                    Id = "",
                    InvoiceId = "",
                    Date = "",
                    VoucherNo = "",
                    Type = "",
                    AccountName = "Total",
                    Debit = totalAmount.ToString("N2"),
                    Credit = totalPaidAmount.ToString("N2"),
                    Balance = totalAmount.ToString("N2")
                });

                return Ok(new { status = true, data = transactions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCashInvoice(int id)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                List<string> includeTypes = new List<string> { "%Sales Invoice Cash%" };
                List<string> excludeTypes = new List<string>();

                // Use aliases without spaces
                string query = @"
       SELECT 
                t.id, 
                t.transaction_id AS 'InvoiceId',
                ROW_NUMBER() OVER (ORDER BY t.date, t.id) AS SN,
                t.voucher_no AS 'V - No',
                t.date AS 'Date', 
                CONCAT(ta.code, ' - ', ta.name) AS 'Account Name',
                t.type, 
                CASE 
                    WHEN t.type = 'Customer Receipt' THEN -IF(t.debit = 0, t.credit, t.debit)
                    WHEN t.type = 'Customer Advance Payment' THEN -IF(t.debit = 0, t.credit, t.debit)
                    WHEN t.type = 'Sales Invoice Cash' THEN IF(t.debit = 0, t.credit, t.debit)
                    WHEN t.type = 'Customer Opening Balance' AND t.credit > 0 THEN -t.credit
                    WHEN t.type = 'Check Cancel (Customer)' THEN t.debit
                    WHEN t.type LIKE 'Customer%' OR t.type LIKE 'Sales%' THEN 
                        IF(t.debit = 0, t.credit, t.debit)
                    ELSE NULL 
                END AS 'Amount',
                '' AS Balance 
            FROM tbl_transaction t
            INNER JOIN tbl_coa_level_4 ta ON t.account_id = ta.id
            WHERE t.hum_id = @id AND t.state = 0";

                if (includeTypes.Count > 0)
                    query += " AND (" + string.Join(" OR ", includeTypes.Select((t, i) => $"t.type LIKE @include{i}")) + ")";
                if (excludeTypes.Count > 0)
                    query += " AND (" + string.Join(" AND ", excludeTypes.Select((t, i) => $"t.type NOT LIKE @exclude{i}")) + ")";

                query += " ORDER BY t.date, t.id;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.Add(new MySqlParameter("@id", id));
                for (int i = 0; i < includeTypes.Count; i++)
                    cmd.Parameters.Add(new MySqlParameter($"@include{i}", includeTypes[i]));
                for (int i = 0; i < excludeTypes.Count; i++)
                    cmd.Parameters.Add(new MySqlParameter($"@exclude{i}", excludeTypes[i]));

                using var reader = await cmd.ExecuteReaderAsync();

                var invoices = new List<object>();
                decimal totalAmount = 0, amount = 0;

                while (await reader.ReadAsync())
                {
                    amount = reader.IsDBNull(reader.GetOrdinal("Amount")) ? 0 : reader.GetDecimal("Amount");

                    string invoiceIdStr = reader.IsDBNull(reader.GetOrdinal("InvoiceId")) ? null : reader.GetString("InvoiceId");
                   
                    invoices.Add(new
                    {
                        Sn = reader.GetInt32("SN"),
                        InvoiceId = reader.GetInt32("InvoiceId"),
                        VoucherNo = reader.GetString("V - No"),
                        Date = reader.GetDateTime("Date").ToString("yyyy-MM-dd"), // Format date
                        AccountName = reader.GetString("Account Name"),
                        Type = reader.GetString("type"),
                        Amount = amount.ToString("N2")
                    });
                }

                // Add total row
                invoices.Add(new
                {
                    Sn = "Total",
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



        #endregion






    }
}
