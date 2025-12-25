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
        private async Task ProcessOpeningBalanceAsync(MySqlConnection conn, int customerId, string formattedCode, int userId, CustomerRequest model)
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

        #region Sales Center

        public IActionResult SalesCenter()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetSalesReport(DateTime? dateFrom, DateTime? dateTo, int? customerId)
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

                // 🔹 BASE QUERY (NO GROUP BY – IMPORTANT)
                string query = @"
SELECT
    s.id AS Id,
    s.date AS Date,
    s.invoice_id AS InvoiceNo,

    c.id AS CustomerId,
    CONCAT(c.code, ' - ', c.name) AS CustomerName,

    s.payment_method AS PaymentMethod,
    s.total AS Total,
    s.vat AS Vat,
    s.net AS Net,

    CONCAT(
        '000',
        (
            SELECT MAX(t2.transaction_id)
            FROM tbl_transaction t2
            WHERE t2.transaction_id = s.id
        )
    ) AS JvNo,

    i.id AS ItemId,
    i.code AS ItemCode,
    i.name AS ItemName,

    sd.sales_id AS Sales_Id,
    sd.qty AS Qty,
    sd.price AS Price,
    sd.discount AS Discount,
    sd.vat AS ItemVat,
    sd.vatp AS VatP,
    sd.total AS ItemTotal,
    sd.cost_price AS Cost_Price,
    sd.cost_center_id AS Cost_Center_Id,
    sd.project_id AS Project_Id,

    s.warehouse_id AS Warehouse_Id,
    s.po_num AS PO_Num,
    s.bill_to AS Bill_To,
    s.city AS City,
    s.sales_man AS Sales_Man,
    s.ship_date AS Ship_Date,
    s.ship_via AS Ship_Via,
    s.ship_to AS Ship_To,
    s.account_cash_id AS Account_Cash_Id,
    s.payment_terms AS Payment_Terms,
    s.payment_date AS Payment_Date,
    s.pay AS Pay

FROM tbl_sales s
INNER JOIN tbl_customer c ON s.customer_id = c.id
LEFT JOIN tbl_sales_details sd ON s.id = sd.sales_id
LEFT JOIN tbl_items i ON sd.item_id = i.id
WHERE s.state = 0
";

                // 🔹 FILTERS
                var parameters = new List<MySqlParameter>();

                if (dateFrom.HasValue)
                {
                    query += " AND s.date >= @dateFrom ";
                    parameters.Add(new MySqlParameter("@dateFrom", dateFrom.Value.Date));
                }

                if (dateTo.HasValue)
                {
                    query += " AND s.date <= @dateTo ";
                    parameters.Add(new MySqlParameter("@dateTo", dateTo.Value.Date));
                }

                if (customerId.HasValue)
                {
                    query += " AND s.customer_id = @customerId ";
                    parameters.Add(new MySqlParameter("@customerId", customerId.Value));
                }

                query += " ORDER BY s.date DESC, s.id ";

                var sales = new List<SaleDto>();

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();

                SaleDto currentSale = null;
                int lastSaleId = -1;

                while (await reader.ReadAsync())
                {
                    int saleId = reader.GetInt32("Id");

                    // 🔹 NEW SALE HEADER
                    if (saleId != lastSaleId)
                    {
                        currentSale = new SaleDto
                        {
                            Id = saleId,
                            Date = reader.GetDateTime("Date"),
                            InvoiceNo = reader.GetString("InvoiceNo"),
                            CustomerId = reader.GetInt32("CustomerId"),
                            CustomerName = reader.GetString("CustomerName"),
                            PaymentMethod = reader.GetString("PaymentMethod"),
                            Total = reader.GetDecimal("Total"),
                            Vat = reader.GetDecimal("Vat"),
                            Net = reader.GetDecimal("Net"),
                            JvNo = reader.GetString("JvNo"),

                            Warehouse_Id = reader.GetInt32("Warehouse_Id"),
                            PO_Num = reader.GetString("PO_Num"),
                            Bill_To = reader.GetString("Bill_To"),
                            City = reader.GetString("City"),
                            Sales_Man = reader.GetString("Sales_Man"),
                            Ship_Date = reader.GetDateTime("Ship_Date"),
                            Ship_Via = reader.GetString("Ship_Via"),
                            Ship_To = reader.GetString("Ship_To"),
                            Account_Cash_Id = reader.GetInt32("Account_Cash_Id"),
                            Payment_Terms = reader.GetString("Payment_Terms"),
                            Payment_Date = reader.GetDateTime("Payment_Date"),
                            Pay = reader.GetDecimal("Pay"),
                            Items = new List<SaleItemDto>()
                        };

                        sales.Add(currentSale);
                        lastSaleId = saleId;
                    }

                    // 🔹 ADD ITEM (if exists)
                    if (reader["ItemId"] != DBNull.Value)
                    {
                        currentSale.Items.Add(new SaleItemDto
                        {
                            ItemId = Convert.ToInt32(reader["ItemId"]),
                            ItemCode = reader["ItemCode"]?.ToString(),
                            ItemName = reader["ItemName"]?.ToString(),
                            Qty = Convert.ToDecimal(reader["Qty"]),
                            Price = Convert.ToDecimal(reader["Price"]),
                            Discount = Convert.ToDecimal(reader["Discount"]),
                            ItemVat = Convert.ToDecimal(reader["ItemVat"]),
                            VatP = Convert.ToDecimal(reader["VatP"]),
                            ItemTotal = Convert.ToDecimal(reader["ItemTotal"]),
                            Cost_Price = Convert.ToDecimal(reader["Cost_Price"]),
                            Cost_Center_Id = Convert.ToInt32(reader["Cost_Center_Id"]),
                            Sales_Id = Convert.ToInt32(reader["Sales_Id"])
                        });
                    }
                }

                return Ok(new { status = true, data = sales });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CheckItemValidity(int itemId, decimal salesQty, int? refId = null)
        {
            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName")
                           ?? _config["ConnectionStrings:DefaultDatabase"]
            };

            using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            // 1️⃣ Get item on-hand and type
            string editRefSql = refId.HasValue ? " AND t.reference != @refId " : "";

            string sql = $@"
SELECT
    IFNULL(
        (
            SELECT SUM(t.qty_in - t.qty_out)
            FROM tbl_item_transaction t
            WHERE t.item_id = i.id
            {editRefSql}
            AND t.type != 'Sales Invoice'
        ),
        i.on_hand
    ) AS on_hand,
    i.type,
    i.name
FROM tbl_items i
WHERE i.id = @id;
";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", itemId);
            if (refId.HasValue)
                cmd.Parameters.AddWithValue("@refId", refId.Value);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return NotFound(new { status = false, message = "Item not found" });

            string itemType = reader.GetString("type");
            string itemName = reader.GetString("name");
            decimal onHand = reader.GetDecimal("on_hand");

            // 2️⃣ Handle service items
            if (itemType == "12 - Service")
                return Json(new { status = true, message = "Service item, always valid" });

            // 3️⃣ Get total quantity from request or grid (replace GetTotalQtyInGrid logic)
            decimal totalQty = salesQty;

            // 4️⃣ Validate inventory part
            if (itemType == "11 - Inventory Part")
            {
                if (totalQty > onHand)
                    return Json(new
                    {
                        status = false,
                        message = $"Item Out Of Stock: {itemName}. Only {onHand:0.00} available, requested {totalQty:0.00}"
                    });
            }
            // 5️⃣ Validate assembly
            else if (itemType == "13 - Inventory Assembly")
            {
                string assemblySql = @"
SELECT i.name, a.qty, i.on_hand 
FROM tbl_item_assembly a
JOIN tbl_items i ON a.item_id = i.id
WHERE a.assembly_id = @assemblyId;
";

                using var assemblyCmd = new MySqlCommand(assemblySql, conn);
                assemblyCmd.Parameters.AddWithValue("@assemblyId", itemId);

                using var assemblyReader = await assemblyCmd.ExecuteReaderAsync();
                var messages = new List<string>();

                while (await assemblyReader.ReadAsync())
                {
                    string compName = assemblyReader.GetString("name");
                    decimal required = assemblyReader.GetDecimal("qty") * totalQty;
                    decimal available = assemblyReader.GetDecimal("on_hand");

                    if (required > available)
                        messages.Add($"Component Out Of Stock: {compName}. Needs {required}, available {available}");
                }

                if (messages.Count > 0)
                    return Json(new { status = false, message = string.Join("\n", messages) });
            }

            return Json(new { status = true, message = "Item is valid" });
        }


       

        [HttpPost]
        public async Task<IActionResult> SaveInvoice([FromBody] SalesInvoiceRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in" });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // =======================
                // 1. VALIDATIONS
                // =======================
                if (model.Items == null || model.Items.Count == 0)
                    return Json(new { status = false, message = "Insert items first." });

                if (model.CustomerId <= 0)
                    return Json(new { status = false, message = "Customer must be selected." });

                if (string.IsNullOrWhiteSpace(model.PaymentMethod))
                    return Json(new { status = false, message = "Payment method must be selected." });

                if (model.WarehouseId <= 0)
                    return Json(new { status = false, message = "Warehouse must be selected." });

                if (model.AccountCashId <= 0 && model.PaymentMethod == "Cash")
                    return Json(new { status = false, message = "Cash account must be selected." });

                if (model.NetTotal <= 0)
                    return Json(new { status = false, message = "Total must be bigger than zero." });

                // =======================
                // 2. INSERT OR UPDATE INVOICE
                // =======================
                long invId = 0;
                if (model.Id == 0) // INSERT
                {
                    // Generate next invoice code
                    string nextCode = "SI-0001";
                    using (var cmd = new MySqlCommand("SELECT MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)) FROM tbl_sales", conn, tx))
                    {
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != DBNull.Value && result != null)
                            nextCode = "SI-" + (Convert.ToInt32(result) + 1).ToString("D4");
                    }

                    var insertSql = @"
                INSERT INTO tbl_sales 
                (date, customer_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man, 
                 ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date,
                 total, vat, net, pay, `change`, created_by, created_date, state)
                VALUES
                (@date, @customer_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man, 
                 @ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms, @payment_date,
                 @total, @vat, @net, @pay, @change, @created_by, @created_date, 0);
                SELECT LAST_INSERT_ID();";

                    using var cmdInsert = new MySqlCommand(insertSql, conn, tx);
                    cmdInsert.Parameters.AddWithValue("@date", model.InvoiceDate);
                    cmdInsert.Parameters.AddWithValue("@customer_id", model.CustomerId);
                    cmdInsert.Parameters.AddWithValue("@invoice_id", nextCode);
                    cmdInsert.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    cmdInsert.Parameters.AddWithValue("@po_num", model.PoNo ?? "");
                    cmdInsert.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    cmdInsert.Parameters.AddWithValue("@city", model.City ?? "");
                    cmdInsert.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    cmdInsert.Parameters.AddWithValue("@ship_date", model.ShipDate);
                    cmdInsert.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    cmdInsert.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    cmdInsert.Parameters.AddWithValue("@payment_method", model.PaymentMethod);
                    cmdInsert.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    cmdInsert.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    cmdInsert.Parameters.AddWithValue("@payment_date", model.PaymentDate);
                    cmdInsert.Parameters.AddWithValue("@total", model.TotalBeforeVat);
                    cmdInsert.Parameters.AddWithValue("@vat", model.Vat);
                    cmdInsert.Parameters.AddWithValue("@net", model.NetTotal);
                    cmdInsert.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
                    cmdInsert.Parameters.AddWithValue("@change", model.PaymentMethod == "Cash" ? 0 : model.NetTotal);
                    cmdInsert.Parameters.AddWithValue("@created_by", userId);
                    cmdInsert.Parameters.AddWithValue("@created_date", DateTime.Now);

                    invId = Convert.ToInt64(await cmdInsert.ExecuteScalarAsync());

                }
                else // UPDATE
                {
                    invId = model.Id;

                    // Return items to inventory before updating
                    await ReturnItemsToInventoryAsync(conn, tx, invId);

                    var updateSql = @"
                UPDATE tbl_sales SET
                    date=@date, customer_id=@customer_id, invoice_id=@invoice_id, warehouse_id=@warehouse_id,
                    po_num=@po_num, bill_to=@bill_to, city=@city, sales_man=@sales_man, ship_date=@ship_date,
                    ship_via=@ship_via, ship_to=@ship_to, payment_method=@payment_method, account_cash_id=@account_cash_id,
                    payment_terms=@payment_terms, payment_date=@payment_date, total=@total, vat=@vat, net=@net,
                    pay=@pay, `change`=@change, modified_by=@modified_by, modified_date=@modified_date
                WHERE id=@id";

                    using var cmdUpdate = new MySqlCommand(updateSql, conn, tx);
                    cmdUpdate.Parameters.AddWithValue("@id", invId);
                    cmdUpdate.Parameters.AddWithValue("@date", model.InvoiceDate);
                    cmdUpdate.Parameters.AddWithValue("@customer_id", model.CustomerId);
                    cmdUpdate.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
                    cmdUpdate.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    cmdUpdate.Parameters.AddWithValue("@po_num", model.PoNo ?? "");
                    cmdUpdate.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    cmdUpdate.Parameters.AddWithValue("@city", model.City ?? "");
                    cmdUpdate.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    cmdUpdate.Parameters.AddWithValue("@ship_date", model.ShipDate);
                    cmdUpdate.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    cmdUpdate.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    cmdUpdate.Parameters.AddWithValue("@payment_method", model.PaymentMethod);
                    cmdUpdate.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    cmdUpdate.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    cmdUpdate.Parameters.AddWithValue("@payment_date", model.PaymentDate);
                    cmdUpdate.Parameters.AddWithValue("@total", model.TotalBeforeVat);
                    cmdUpdate.Parameters.AddWithValue("@vat", model.Vat);
                    cmdUpdate.Parameters.AddWithValue("@net", model.NetTotal);
                    cmdUpdate.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
                    cmdUpdate.Parameters.AddWithValue("@change", model.PaymentMethod == "Cash" ? 0 : model.NetTotal);
                    cmdUpdate.Parameters.AddWithValue("@modified_by", userId);
                    cmdUpdate.Parameters.AddWithValue("@modified_date", DateTime.Now);

                    await cmdUpdate.ExecuteNonQueryAsync();

                    // Delete old sales details & accounting
                    await CommonDeleteTransactionsAsync(conn, tx, invId);
                }

                // =======================
                // 3. INSERT SALES ITEMS
                // =======================
                await InsertSalesItemAsync(conn, tx, invId, model.Items);

                // =======================
                // 4. PROCESS INVENTORY
                // =======================
                await ProcessInventoryAsync(conn, tx, invId, model.Items, model.WarehouseId);

                // =======================
                // 5. ACCOUNTING ENTRIES
                // =======================
                await InsertAccountingAsync(conn, tx, invId, model, userId);

                // =======================
                // 6. COMMIT
                // =======================
                await tx.CommitAsync();

                return Json(new { status = true, message = model.Id == 0 ? "Invoice created successfully" : "Invoice updated successfully", invoiceId = invId });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { status = false, message = ex.Message });
            }
        }


        private async Task ReturnItemsToInventoryAsync(MySqlConnection conn, MySqlTransaction tx, long invoiceId)
        {
            var query = @"
        SELECT sd.item_id, sd.qty, i.method, i.type
        FROM tbl_sales_details sd
        INNER JOIN tbl_items i ON sd.item_id = i.id
        WHERE sd.sales_id = @salesId";

            using var cmd = new MySqlCommand(query, conn, tx);
            cmd.Parameters.AddWithValue("@salesId", invoiceId);

            using var reader = await cmd.ExecuteReaderAsync();
            var itemsToReturn = new List<(int ItemId, decimal Qty, string Method, string Type)>();
            while (await reader.ReadAsync())
            {
                itemsToReturn.Add((
                    reader.GetInt32("item_id"),
                    reader.GetDecimal("qty"),
                    reader.GetString("method"),
                    reader.GetString("type")
                ));
            }
            reader.Close();

            foreach (var item in itemsToReturn)
            {
                if (item.Type == "12 - Service") continue;

                if (item.Type == "13 - Inventory Assembly")
                {
                    var compQuery = "SELECT item_id, qty FROM tbl_item_assembly WHERE assembly_id=@assemblyId";
                    using var compCmd = new MySqlCommand(compQuery, conn, tx);
                    compCmd.Parameters.AddWithValue("@assemblyId", item.ItemId);

                    using var compReader = await compCmd.ExecuteReaderAsync();
                    var components = new List<(int ComponentId, decimal Qty)>();
                    while (await compReader.ReadAsync())
                    {
                        components.Add((
                            compReader.GetInt32("item_id"),
                            compReader.GetDecimal("qty") * item.Qty
                        ));
                    }
                    compReader.Close();

                    foreach (var comp in components)
                    {
                        var updateQty = "UPDATE tbl_items SET on_hand = on_hand + @qty WHERE id=@itemId";
                        using var updateCmd = new MySqlCommand(updateQty, conn, tx);
                        updateCmd.Parameters.AddWithValue("@qty", comp.Qty);
                        updateCmd.Parameters.AddWithValue("@itemId", comp.ComponentId);
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    var updateQty = "UPDATE tbl_items SET on_hand = on_hand + @qty WHERE id=@itemId";
                    using var updateCmd = new MySqlCommand(updateQty, conn, tx);
                    updateCmd.Parameters.AddWithValue("@qty", item.Qty);
                    updateCmd.Parameters.AddWithValue("@itemId", item.ItemId);
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }
        }


        private async Task CommonDeleteTransactionsAsync(MySqlConnection conn, MySqlTransaction tx, long invoiceId)
        {
            try
            {
                var deleteSalesDetails = "DELETE FROM tbl_sales_details WHERE sales_id=@id";
                using (var cmd = new MySqlCommand(deleteSalesDetails, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", invoiceId);
                    await cmd.ExecuteNonQueryAsync();
                }

                var deleteItemTrans = @"
        DELETE FROM tbl_item_transaction WHERE reference=@invId AND type='Sales Invoice';
        DELETE FROM tbl_item_card_details WHERE trans_type='Sales Invoice' AND trans_no=@invId;
        DELETE FROM tbl_cost_center_transaction WHERE ref_id=@invId AND type='Sales';
        DELETE FROM tbl_transaction WHERE transaction_id=@invId AND type='SALES';
    ";
                using (var cmd = new MySqlCommand(deleteItemTrans, conn, tx))
                {
                    cmd.Parameters.AddWithValue("@invId", invoiceId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }


        private async Task InsertSalesItemAsync(MySqlConnection conn, MySqlTransaction tx, long invoiceId, List<SalesItemRequest> items)
        {
            foreach (var item in items)
            {
                var insertDetail = @"
            INSERT INTO tbl_sales_details
            (sales_id, item_id, qty, cost_price, price, discount, vatp, vat, total, cost_center_id)
            VALUES
            (@sales_id, @item_id, @qty, @cost_price, @price, @discount, @vatp, @vat, @total, @costCenter)";

                using var cmd = new MySqlCommand(insertDetail, conn, tx);
                cmd.Parameters.AddWithValue("@sales_id", invoiceId);
                cmd.Parameters.AddWithValue("@item_id", item.ItemId);
                cmd.Parameters.AddWithValue("@qty", item.Qty);
                cmd.Parameters.AddWithValue("@cost_price", item.CostPrice);
                cmd.Parameters.AddWithValue("@price", item.Price);
                cmd.Parameters.AddWithValue("@discount", item.Discount);
                cmd.Parameters.AddWithValue("@vatp", item.VatP);
                cmd.Parameters.AddWithValue("@vat", item.Vat);
                cmd.Parameters.AddWithValue("@total", item.Total);
                cmd.Parameters.AddWithValue("@costCenter", item.CostCenterId);
                await cmd.ExecuteNonQueryAsync();
            }
        }


        private async Task ProcessInventoryAsync(MySqlConnection conn, MySqlTransaction tx, long invoiceId, List<SalesItemRequest> items, int warehouseId)
        {
            foreach (var item in items)
            {
                if (item.Type == "12 - Service") continue;

                if (item.Type == "13 - Inventory Assembly")
                {
                    var compQuery = "SELECT item_id, qty, method FROM tbl_item_assembly INNER JOIN tbl_items ON tbl_item_assembly.item_id=tbl_items.id WHERE assembly_id=@assemblyId";
                    using var cmd = new MySqlCommand(compQuery, conn, tx);
                    cmd.Parameters.AddWithValue("@assemblyId", item.ItemId);

                    using var reader = await cmd.ExecuteReaderAsync();
                    var components = new List<(int Id, decimal Qty, string Method)>();
                    while (await reader.ReadAsync())
                    {
                        components.Add((
                            reader.GetInt32("item_id"),
                            reader.GetDecimal("qty") * item.Qty,
                            reader.GetString("method")
                        ));
                    }
                    reader.Close();

                    foreach (var comp in components)
                    {
                        var updateQty = "UPDATE tbl_items SET on_hand = on_hand - @qty WHERE id=@itemId";
                        using var updateCmd = new MySqlCommand(updateQty, conn, tx);
                        updateCmd.Parameters.AddWithValue("@qty", comp.Qty);
                        updateCmd.Parameters.AddWithValue("@itemId", comp.Id);
                        await updateCmd.ExecuteNonQueryAsync();
                    }
                }
                else
                {
                    var updateQty = "UPDATE tbl_items SET on_hand = on_hand - @qty WHERE id=@itemId";
                    using var cmd = new MySqlCommand(updateQty, conn, tx);
                    cmd.Parameters.AddWithValue("@qty", item.Qty);
                    cmd.Parameters.AddWithValue("@itemId", item.ItemId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async Task InsertAccountingAsync(
       MySqlConnection conn,
       MySqlTransaction tx,
       long invoiceId,
       SalesInvoiceRequest model,
       int userId)
        {
            // Determine main account (Cash or Credit)
            string accountId = model.PaymentMethod == "Credit"
                ? model.PaymentCreditAccountId.ToString()
                : model.AccountCashId.ToString();

            // =========================
            // 1. Customer / Cash / Credit Entry
            // =========================
            if (model.NetTotal > 0)
            {
                await InsertTransactionAsync(
                    conn, tx,
                    model.InvoiceDate,
                    accountId,
                    model.PaymentMethod == "Cash" ? model.NetTotal : 0,
                    model.PaymentMethod == "Credit" ? model.NetTotal : 0,
                    invoiceId,
                    model.CustomerId,
                    model.PaymentMethod == "Credit" ? "Sales Invoice" : "Sales Invoice Cash",
                    "SALES",
                    $"Sales Invoice NO. {model.InvoiceCode}",
                    userId,
                    model.InvoiceCode
                );
            }

            // =========================
            // 2. Sales Revenue Entry
            // =========================
            await InsertTransactionAsync(
                conn, tx,
                model.InvoiceDate,
                model.SalesRevenueAccountId.ToString(),
                0,
                model.TotalBeforeVat,
                invoiceId,
                0,
                model.PaymentMethod == "Credit" ? "Sales Invoice" : "Sales Invoice Cash",
                "SALES",
                $"Sales Revenue For Invoice No. {model.InvoiceCode}",
                userId,
                model.InvoiceCode
            );

            // =========================
            // 3. VAT Output Entry (only if VAT > 0)
            // =========================
            if (model.Vat > 0)
            {
                await InsertTransactionAsync(
                    conn, tx,
                    model.InvoiceDate,
                    model.VatAccountId.ToString(),
                    0,
                    model.Vat,
                    invoiceId,
                    0,
                    model.PaymentMethod == "Credit" ? "Sales Invoice" : "Sales Invoice Cash",
                    "SALES",
                    $"Vat Output For Invoice No. {model.InvoiceCode}",
                    userId,
                    model.InvoiceCode
                );
            }

            // =========================
            // 4. COGS & Inventory Entries (only if inventory cost > 0)
            // =========================
            if (model.InventoryCost > 0)
            {
                // COGS Debit
                await InsertTransactionAsync(
                    conn, tx,
                    model.InvoiceDate,
                    model.CogsAccountId.ToString(),
                    model.InventoryCost,
                    0,
                    invoiceId,
                    0,
                    "Sales Invoice",
                    "SALES",
                    $"COGS For Sales No. {model.InvoiceCode}",
                    userId,
                    model.InvoiceCode
                );

                // Inventory Credit (Warehouse Account)
                await InsertTransactionAsync(
                    conn, tx,
                    model.InvoiceDate,
                    model.WarehouseId.ToString(),
                    0,
                    model.InventoryCost,
                    invoiceId,
                    0,
                    "Sales Invoice",
                    "SALES",
                    $"Item Sold For Sales No. {model.InvoiceCode}",
                    userId,
                    model.InvoiceCode
                );
            }
        }
        private static async Task InsertTransactionAsync(
            MySqlConnection conn,
            MySqlTransaction tx,
            DateTime date,
            string accountId,
            decimal debit,
            decimal credit,
            long transactionId,
            long humId,
            string type,
            string tType,
            string description,
            int createdBy,
            string voucherNo)
        {
            using var cmd = new MySqlCommand(@"
        INSERT INTO tbl_transaction
        (date, account_id, debit, credit, transaction_id, hum_id,
         t_type, type, description, created_by, created_date, state, voucher_no)
        VALUES
        (@date, @account_id, @debit, @credit, @transaction_id, @hum_id,
         @t_type, @type, @description, @created_by, @created_date, 0, @voucher_no)
    ", conn, tx);

            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@account_id", accountId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transaction_id", transactionId);
            cmd.Parameters.AddWithValue("@hum_id", humId);
            cmd.Parameters.AddWithValue("@t_type", tType);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@created_by", createdBy);
            cmd.Parameters.AddWithValue("@created_date", DateTime.Now);
            cmd.Parameters.AddWithValue("@voucher_no", voucherNo);

            await cmd.ExecuteNonQueryAsync();
        }


        #endregion

    }
}
