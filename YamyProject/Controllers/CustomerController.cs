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
        private async Task ProcessOpeningBalanceAsync(MySqlConnection conn, int customerId, string formattedCode, int userId, CustomerRequest model)
        {
            decimal debitAmount = model.Debit ?? 0;
            decimal creditAmount = model.Credit ?? 0;
            string accountId = model.AccountId?.ToString() ?? "0";

            // Get Opening Balance Equity account ID
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

            DateTime transactionDate = model.OpeningBalanceDate ?? DateTime.Now;

            // Mimic your synchronous logic

            if (creditAmount != 0)
            {
                // Credit transaction: 
                // 1) Opening Balance Equity - Debit = creditAmount, Credit=0
                await AddTransactionEntryAsync(
                    conn, transactionDate, openingBalanceEquity,
                    creditAmount.ToString(), "0",
                    customerId.ToString(), "0",
                    "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance Equity - Customer Code - {formattedCode}",
                    userId, DateTime.Now, "");

                // 2) Customer Account - Debit=0, Credit=creditAmount
                await AddTransactionEntryAsync(
                    conn, transactionDate, openingBalanceEquity,
                    "0", creditAmount.ToString(),
                    customerId.ToString(), customerId.ToString(),
                    "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance - Customer Code - {formattedCode}",
                    userId, DateTime.Now, "");
            }

            if (debitAmount != 0)
            {
                // Debit transaction:
                // 1) Opening Balance Equity - Debit=0, Credit=debitAmount
                await AddTransactionEntryAsync(
                    conn, transactionDate, openingBalanceEquity,
                    "0", debitAmount.ToString(),
                    customerId.ToString(), "0",
                    "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance Equity - Customer Code - {formattedCode}",
                    userId, DateTime.Now, "");

                // 2) Customer Account - Debit=debitAmount, Credit=0
                await AddTransactionEntryAsync(
                    conn, transactionDate, openingBalanceEquity,
                    debitAmount.ToString(), "0",
                    customerId.ToString(), customerId.ToString(),
                    "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance - Customer Code - {formattedCode}",
                    userId, DateTime.Now, "");
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

        //[HttpGet]
        //public async Task<IActionResult> GetInvoices(int id, string startDate = null, string endDate = null)
        //{
        //    try
        //    {
        //        var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
        //        {
        //            Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase"),
        //        };

        //        using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
        //        await conn.OpenAsync();

        //        string query = @"
        //SELECT
        //    ROW_NUMBER() OVER (ORDER BY t.id) AS SN,
        //    t.id,
        //    t.transaction_id AS InvoiceId,
        //    t.voucher_no AS VoucherNo,
        //    t.date,
        //    ta.name AS AccountName,
        //    t.type,
        //    t.debit,
        //    t.credit
        //FROM tbl_transaction t
        //INNER JOIN tbl_coa_level_4 ta ON t.account_id = ta.id
        //WHERE
        //    t.hum_id = @id 
        //    AND t.state = 0 
        //    AND t.type IN (
        //        'Customer Receipt', 
        //        'Sales Invoice', 
        //        'Sales Invoice Cash', 
        //        'Customer Opening Balance', 
        //        'Customer Advance Payment',
        //        'Check Cancel (Customer)', 
        //        'SalesReturn Invoice', 
        //        'Credit Note', 
        //        'PDC Receivable'
        //    )";

        //        if (!string.IsNullOrEmpty(startDate))
        //            query += " AND t.date >= @startDate";

        //        if (!string.IsNullOrEmpty(endDate))
        //            query += " AND t.date <= @endDate";

        //        query += " ORDER BY t.id;";

        //        using var cmd = new MySqlCommand(query, conn);
        //        cmd.Parameters.AddWithValue("@id", id);

        //        if (!string.IsNullOrEmpty(startDate))
        //            cmd.Parameters.AddWithValue("@startDate", DateTime.Parse(startDate));

        //        if (!string.IsNullOrEmpty(endDate))
        //            cmd.Parameters.AddWithValue("@endDate", DateTime.Parse(endDate));

        //        using var reader = await cmd.ExecuteReaderAsync();

        //        var transactions = new List<object>();
        //        decimal runningBalance = 0;
        //        decimal totalDebit = 0;
        //        decimal totalCredit = 0;
        //        int displaySN = 1;

        //        while (await reader.ReadAsync())
        //        {
        //            string type = reader["type"]?.ToString() ?? "";

        //            decimal originalDebit = reader["debit"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["debit"]);
        //            decimal originalCredit = reader["credit"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["credit"]);

        //            string voucherNo = reader["VoucherNo"] == DBNull.Value
        //                ? $"SV-00{reader["InvoiceId"]}"
        //                : reader["VoucherNo"].ToString();

        //            string dateStr = Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd");

        //            // Sales Invoice Cash: split into sales (credit) and cash receipt (debit)
        //            if (type.Equals("Sales Invoice Cash", StringComparison.OrdinalIgnoreCase))
        //            {
        //                decimal amount = originalCredit > 0 ? originalCredit : originalDebit;

        //                // Sales part increases balance (credit)
        //                runningBalance += amount;
        //                totalCredit += amount;

        //                transactions.Add(new
        //                {
        //                    SN = displaySN++,
        //                    Id = reader["id"],
        //                    InvoiceId = reader["InvoiceId"],
        //                    Date = dateStr,
        //                    VoucherNo = voucherNo,
        //                    Type = type,
        //                    AccountName = reader["AccountName"],
        //                    Debit = "0.00",
        //                    Credit = amount.ToString("N2"),
        //                    Balance = runningBalance.ToString("N2")
        //                });

        //                // Cash receipt reduces balance (debit)
        //                runningBalance -= amount;
        //                totalDebit += amount;

        //                transactions.Add(new
        //                {
        //                    SN = displaySN++,
        //                    Id = reader["id"],
        //                    InvoiceId = reader["InvoiceId"],
        //                    Date = dateStr,
        //                    VoucherNo = voucherNo,
        //                    Type = "Cash Receipt",
        //                    AccountName = reader["AccountName"],
        //                    Debit = amount.ToString("N2"),
        //                    Credit = "0.00",
        //                    Balance = runningBalance.ToString("N2")
        //                });
        //            }
        //            // Customer Receipt reduces balance (debit)
        //            else if (type.Equals("Customer Receipt", StringComparison.OrdinalIgnoreCase))
        //            {
        //                decimal amount = originalDebit > 0 ? originalDebit : originalCredit;

        //                runningBalance -= amount;
        //                totalDebit += amount;

        //                transactions.Add(new
        //                {
        //                    SN = displaySN++,
        //                    Id = reader["id"],
        //                    InvoiceId = reader["InvoiceId"],
        //                    Date = dateStr,
        //                    VoucherNo = voucherNo,
        //                    Type = type,
        //                    AccountName = reader["AccountName"],
        //                    Debit = amount.ToString("N2"),
        //                    Credit = "0.00",
        //                    Balance = runningBalance.ToString("N2")
        //                });
        //            }
        //            // Sales Invoice increases balance (credit)
        //            else if (type.Equals("Sales Invoice", StringComparison.OrdinalIgnoreCase)
        //                || type.Equals("Customer Opening Balance", StringComparison.OrdinalIgnoreCase)
        //                || type.Equals("Customer Advance Payment", StringComparison.OrdinalIgnoreCase))
        //            {
        //                decimal amount = originalCredit > 0 ? originalCredit : originalDebit;

        //                runningBalance += amount;
        //                totalCredit += amount;

        //                transactions.Add(new
        //                {
        //                    SN = displaySN++,
        //                    Id = reader["id"],
        //                    InvoiceId = reader["InvoiceId"],
        //                    Date = dateStr,
        //                    VoucherNo = voucherNo,
        //                    Type = type,
        //                    AccountName = reader["AccountName"],
        //                    Debit = "0.00",
        //                    Credit = amount.ToString("N2"),
        //                    Balance = runningBalance.ToString("N2")
        //                });
        //            }
        //            // SalesReturn Invoice, Credit Note, PDC Receivable reduce balance (debit)
        //            else if (type.Equals("SalesReturn Invoice", StringComparison.OrdinalIgnoreCase)
        //                || type.Equals("Credit Note", StringComparison.OrdinalIgnoreCase)
        //                || type.Equals("PDC Receivable", StringComparison.OrdinalIgnoreCase))
        //            {
        //                decimal amount = originalDebit > 0 ? originalDebit : originalCredit;

        //                runningBalance -= amount;
        //                totalDebit += amount;

        //                transactions.Add(new
        //                {
        //                    SN = displaySN++,
        //                    Id = reader["id"],
        //                    InvoiceId = reader["InvoiceId"],
        //                    Date = dateStr,
        //                    VoucherNo = voucherNo,
        //                    Type = type,
        //                    AccountName = reader["AccountName"],
        //                    Debit = amount.ToString("N2"),
        //                    Credit = "0.00",
        //                    Balance = runningBalance.ToString("N2")
        //                });
        //            }
        //            else
        //            {
        //                // General case: credit increases balance, debit decreases balance
        //                runningBalance += originalCredit - originalDebit;
        //                totalDebit += originalDebit;
        //                totalCredit += originalCredit;

        //                transactions.Add(new
        //                {
        //                    SN = displaySN++,
        //                    Id = reader["id"],
        //                    InvoiceId = reader["InvoiceId"],
        //                    Date = dateStr,
        //                    VoucherNo = voucherNo,
        //                    Type = type,
        //                    AccountName = reader["AccountName"],
        //                    Debit = originalDebit.ToString("N2"),
        //                    Credit = originalCredit.ToString("N2"),
        //                    Balance = runningBalance.ToString("N2")
        //                });
        //            }
        //        }

        //        // Total row
        //        transactions.Add(new
        //        {
        //            SN = "Total",
        //            Id = "",
        //            InvoiceId = "",
        //            Date = "",
        //            VoucherNo = "",
        //            Type = "",
        //            AccountName = "Total",
        //            Debit = totalDebit.ToString("N2"),
        //            Credit = totalCredit.ToString("N2"),
        //            Balance = runningBalance.ToString("N2")
        //        });

        //        return Ok(new { status = true, data = transactions });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = false, message = ex.Message });
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> GetInvoices(int id, string startDate = null, string endDate = null)
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
    t.voucher_no AS VoucherNo,
    t.date,
    ta.name AS AccountName,
    t.type,
    t.debit,
    t.credit
FROM tbl_transaction t
INNER JOIN tbl_coa_level_4 ta ON t.account_id = ta.id
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

                if (!string.IsNullOrEmpty(startDate))
                    query += " AND t.date >= @startDate";

                if (!string.IsNullOrEmpty(endDate))
                    query += " AND t.date <= @endDate";

                query += " ORDER BY t.id;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                if (!string.IsNullOrEmpty(startDate))
                    cmd.Parameters.AddWithValue("@startDate", DateTime.Parse(startDate));

                if (!string.IsNullOrEmpty(endDate))
                    cmd.Parameters.AddWithValue("@endDate", DateTime.Parse(endDate));

                using var reader = await cmd.ExecuteReaderAsync();

                var transactions = new List<object>();
                decimal runningBalance = 0;
                decimal totalDebit = 0;
                decimal totalCredit = 0;
                int displaySN = 1;

                while (await reader.ReadAsync())
                {
                    string type = reader["type"]?.ToString() ?? "";

                    decimal originalDebit = reader["debit"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["debit"]);
                    decimal originalCredit = reader["credit"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["credit"]);

                    string voucherNo = reader["VoucherNo"] == DBNull.Value
                        ? $"SV-00{reader["InvoiceId"]}"
                        : reader["VoucherNo"].ToString();

                    string dateStr = Convert.ToDateTime(reader["date"]).ToString("yyyy-MM-dd");
                    int transactionId = Convert.ToInt32(reader["id"]);
                    string invoiceIdStr = reader["InvoiceId"]?.ToString() ?? "";
                    string accountName = reader["AccountName"]?.ToString() ?? "";

                    // Sales Invoice Cash: split into sales (debit) and cash receipt (credit)
                    if (type.Equals("Sales Invoice Cash", StringComparison.OrdinalIgnoreCase))
                    {
                        decimal amount = originalDebit > 0 ? originalDebit : originalCredit;

                        // Sales part increases balance (debit)
                        runningBalance += amount;
                        totalDebit += amount;

                        transactions.Add(new
                        {
                            SN = displaySN++,
                            Id = transactionId,
                            InvoiceId = invoiceIdStr,
                            Date = dateStr,
                            VoucherNo = voucherNo,
                            Type = type,
                            AccountName = accountName,
                            Debit = amount.ToString("N2"),
                            Credit = "0.00",
                            Balance = runningBalance.ToString("N2")
                        });

                        // Cash receipt reduces balance (credit)
                        runningBalance -= amount;
                        totalCredit += amount;

                        transactions.Add(new
                        {
                            SN = displaySN++,
                            Id = transactionId,
                            InvoiceId = invoiceIdStr,
                            Date = dateStr,
                            VoucherNo = voucherNo,
                            Type = "Cash Receipt",
                            AccountName = accountName,
                            Debit = "0.00",
                            Credit = amount.ToString("N2"),
                            Balance = runningBalance.ToString("N2")
                        });
                    }
                    // Customer Receipt / Advance / PDC: credit decreases balance (payment received)
                    else if (type.Equals("Customer Receipt", StringComparison.OrdinalIgnoreCase)
                      
                        || type.Equals("Check Cancel (Customer)", StringComparison.OrdinalIgnoreCase)
                        || type.Equals("PDC Receivable", StringComparison.OrdinalIgnoreCase))
                    {
                        decimal amount = originalCredit > 0 ? originalCredit : originalDebit;

                        runningBalance -= amount;
                        totalCredit += amount;

                        transactions.Add(new
                        {
                            SN = displaySN++,
                            Id = transactionId,
                            InvoiceId = invoiceIdStr,
                            Date = dateStr,
                            VoucherNo = voucherNo,
                            Type = type,
                            AccountName = accountName,
                            Debit = "0.00",
                            Credit = amount.ToString("N2"),
                            Balance = runningBalance.ToString("N2")
                        });
                    }
                    // Sales Invoice / Opening Balance: debit increases balance (amount owed by customer)
                    else if (type.Equals("Sales Invoice", StringComparison.OrdinalIgnoreCase)
                        || type.Equals("Customer Opening Balance", StringComparison.OrdinalIgnoreCase)
                          || type.Equals("Customer Advance Payment", StringComparison.OrdinalIgnoreCase))
                    {
                        decimal amount = originalDebit > 0 ? originalDebit : originalCredit;

                        runningBalance += amount;
                        totalDebit += amount;

                        transactions.Add(new
                        {
                            SN = displaySN++,
                            Id = transactionId,
                            InvoiceId = invoiceIdStr,
                            Date = dateStr,
                            VoucherNo = voucherNo,
                            Type = type,
                            AccountName = accountName,
                            Debit = amount.ToString("N2"),
                            Credit = "0.00",
                            Balance = runningBalance.ToString("N2")
                        });
                    }
                    // SalesReturn / Credit Note: INCREASES balance shown in credit column
                    // (return means goods come back, customer owes less — shown as credit, balance decreases)
                    else if (type.Equals("SalesReturn Invoice", StringComparison.OrdinalIgnoreCase)
                        || type.Equals("Credit Note", StringComparison.OrdinalIgnoreCase))
                    {
                        decimal amount = originalCredit > 0 ? originalCredit : originalDebit;

                        runningBalance -= amount;   // reduces what customer owes
                        totalCredit += amount;

                        transactions.Add(new
                        {
                            SN = displaySN++,
                            Id = transactionId,
                            InvoiceId = invoiceIdStr,
                            Date = dateStr,
                            VoucherNo = voucherNo,
                            Type = type,
                            AccountName = accountName,
                            Debit = "0.00",
                            Credit = amount.ToString("N2"),
                            Balance = runningBalance.ToString("N2")
                        });
                    }
                    else
                    {
                        // General case: debit increases balance, credit decreases balance
                        runningBalance += originalDebit - originalCredit;
                        totalDebit += originalDebit;
                        totalCredit += originalCredit;

                        transactions.Add(new
                        {
                            SN = displaySN++,
                            Id = transactionId,
                            InvoiceId = invoiceIdStr,
                            Date = dateStr,
                            VoucherNo = voucherNo,
                            Type = type,
                            AccountName = accountName,
                            Debit = originalDebit.ToString("N2"),
                            Credit = originalCredit.ToString("N2"),
                            Balance = runningBalance.ToString("N2")
                        });
                    }
                }

                // Total row
                transactions.Add(new
                {
                    SN = "Total",
                    Id = "",
                    InvoiceId = "",
                    Date = "",
                    VoucherNo = "",
                    Type = "",
                    AccountName = "Total",
                    Debit = totalDebit.ToString("N2"),
                    Credit = totalCredit.ToString("N2"),
                    Balance = runningBalance.ToString("N2")
                });

                return Ok(new { status = true, data = transactions });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCashInvoice(int id, string startDate = null, string endDate = null)
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
                cmd.Parameters.Add(new MySqlParameter("@id", id));

                for (int i = 0; i < includeTypes.Count; i++)
                    cmd.Parameters.Add(new MySqlParameter($"@include{i}", includeTypes[i]));
                for (int i = 0; i < excludeTypes.Count; i++)
                    cmd.Parameters.Add(new MySqlParameter($"@exclude{i}", excludeTypes[i]));

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
                decimal totalAmount = 0, amount = 0;

                while (await reader.ReadAsync())
                {
                    amount = reader.IsDBNull(reader.GetOrdinal("Amount")) ? 0 : reader.GetDecimal("Amount");
                    totalAmount += amount;

                    string invoiceIdStr = reader.IsDBNull(reader.GetOrdinal("InvoiceId")) ? null : reader.GetString("InvoiceId");

                    invoices.Add(new
                    {
                        Sn = reader.GetUInt64("SN").ToString(),
                        InvoiceId = reader["InvoiceId"].ToString(),
                        VoucherNo = reader["V - No"].ToString(),
                        Date = reader.GetDateTime("Date").ToString("yyyy-MM-dd"),
                        AccountName = reader["Account Name"].ToString(),
                        Type = reader["type"].ToString(),
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
    i.sales_price AS Sales_Price,
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

                    if (reader["ItemId"] != DBNull.Value)
                    {
                        currentSale.Items.Add(new SaleItemDto
                        {
                            ItemId = reader["ItemId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ItemId"]),
                            ItemCode = reader["ItemCode"] == DBNull.Value ? string.Empty : reader["ItemCode"].ToString(),
                            ItemName = reader["ItemName"] == DBNull.Value ? string.Empty : reader["ItemName"].ToString(),

                            Qty = reader["Qty"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Qty"]),
                            Price = reader["Price"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Price"]),
                            Discount = reader["Discount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Discount"]),
                            ItemVat = reader["ItemVat"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["ItemVat"]),
                            VatP = reader["VatP"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["VatP"]),
                            ItemTotal = reader["ItemTotal"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["ItemTotal"]),
                            Cost_Price = reader["Cost_Price"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Cost_Price"]),
                            Sales_Price = reader["Sales_Price"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Sales_Price"]),
                            Cost_Center_Id = reader["Cost_Center_Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Cost_Center_Id"]),
                            Sales_Id = reader["Sales_Id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Sales_Id"])
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

        //    #region

        //    [HttpPost]
        //    public async Task<IActionResult> SaveInvoice([FromBody] SalesInvoiceRequest model)
        //    {
        //        if (model == null)
        //            return Json(new { status = false, message = "Invalid request" });

        //        int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        //        if (userId <= 0)
        //            return Json(new { status = false, message = "User not logged in" });

        //        var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
        //        {
        //            Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
        //        };

        //        using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
        //        await conn.OpenAsync();
        //        using var tx = await conn.BeginTransactionAsync();

        //        try
        //        {
        //            // =======================
        //            // 1. VALIDATIONS
        //            // =======================
        //            if (model.Items == null || model.Items.Count == 0)
        //                return Json(new { status = false, message = "Insert items first." });

        //            if (model.CustomerId <= 0)
        //                return Json(new { status = false, message = "Customer must be selected." });

        //            if (string.IsNullOrWhiteSpace(model.PaymentMethod))
        //                return Json(new { status = false, message = "Payment method must be selected." });

        //            if (model.WarehouseId <= 0)
        //                return Json(new { status = false, message = "Warehouse must be selected." });

        //            if (model.AccountCashId <= 0 && model.PaymentMethod == "Cash")
        //                return Json(new { status = false, message = "Cash account must be selected." });

        //            if (model.NetTotal <= 0)
        //                return Json(new { status = false, message = "Total must be bigger than zero." });

        //            // =======================
        //            // 2. INSERT OR UPDATE INVOICE
        //            // =======================
        //            long invId = 0;
        //            if (model.Id == 0) // INSERT
        //            {
        //                // Generate next invoice code
        //                string nextCode = "SI-0001";
        //                using (var cmd = new MySqlCommand("SELECT MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)) FROM tbl_sales", conn, tx))
        //                {
        //                    var result = await cmd.ExecuteScalarAsync();
        //                    if (result != DBNull.Value && result != null)
        //                        nextCode = "SI-" + (Convert.ToInt32(result) + 1).ToString("D4");
        //                }

        //                var insertSql = @"
        //            INSERT INTO tbl_sales 
        //            (date, customer_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man, 
        //             ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date,
        //             total, vat, net, pay, `change`, created_by, created_date, state)
        //            VALUES
        //            (@date, @customer_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man, 
        //             @ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms, @payment_date,
        //             @total, @vat, @net, @pay, @change, @created_by, @created_date, 0);
        //            SELECT LAST_INSERT_ID();";

        //                using var cmdInsert = new MySqlCommand(insertSql, conn, tx);
        //                cmdInsert.Parameters.AddWithValue("@date", model.InvoiceDate);
        //                cmdInsert.Parameters.AddWithValue("@customer_id", model.CustomerId);
        //                cmdInsert.Parameters.AddWithValue("@invoice_id", nextCode);
        //                cmdInsert.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
        //                cmdInsert.Parameters.AddWithValue("@po_num", model.PoNo ?? "");
        //                cmdInsert.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
        //                cmdInsert.Parameters.AddWithValue("@city", model.City ?? "");
        //                cmdInsert.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
        //                cmdInsert.Parameters.AddWithValue("@ship_date", model.ShipDate);
        //                cmdInsert.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
        //                cmdInsert.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
        //                cmdInsert.Parameters.AddWithValue("@payment_method", model.PaymentMethod);
        //                cmdInsert.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
        //                cmdInsert.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
        //                cmdInsert.Parameters.AddWithValue("@payment_date", model.PaymentDate);
        //                cmdInsert.Parameters.AddWithValue("@total", model.TotalBeforeVat);
        //                cmdInsert.Parameters.AddWithValue("@vat", model.Vat);
        //                cmdInsert.Parameters.AddWithValue("@net", model.NetTotal);
        //                cmdInsert.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
        //                cmdInsert.Parameters.AddWithValue("@change", model.PaymentMethod == "Cash" ? 0 : model.NetTotal);
        //                cmdInsert.Parameters.AddWithValue("@created_by", userId);
        //                cmdInsert.Parameters.AddWithValue("@created_date", DateTime.Now);

        //                invId = Convert.ToInt64(await cmdInsert.ExecuteScalarAsync());

        //            }
        //            else // UPDATE
        //            {
        //                invId = model.Id;

        //                // Return items to inventory before updating
        //                await ReturnItemsToInventoryAsync(conn, tx, invId);

        //                var updateSql = @"
        //            UPDATE tbl_sales SET
        //                date=@date, customer_id=@customer_id, invoice_id=@invoice_id, warehouse_id=@warehouse_id,
        //                po_num=@po_num, bill_to=@bill_to, city=@city, sales_man=@sales_man, ship_date=@ship_date,
        //                ship_via=@ship_via, ship_to=@ship_to, payment_method=@payment_method, account_cash_id=@account_cash_id,
        //                payment_terms=@payment_terms, payment_date=@payment_date, total=@total, vat=@vat, net=@net,
        //                pay=@pay, `change`=@change, modified_by=@modified_by, modified_date=@modified_date
        //            WHERE id=@id";

        //                using var cmdUpdate = new MySqlCommand(updateSql, conn, tx);
        //                cmdUpdate.Parameters.AddWithValue("@id", invId);
        //                cmdUpdate.Parameters.AddWithValue("@date", model.InvoiceDate);
        //                cmdUpdate.Parameters.AddWithValue("@customer_id", model.CustomerId);
        //                cmdUpdate.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
        //                cmdUpdate.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
        //                cmdUpdate.Parameters.AddWithValue("@po_num", model.PoNo ?? "");
        //                cmdUpdate.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
        //                cmdUpdate.Parameters.AddWithValue("@city", model.City ?? "");
        //                cmdUpdate.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
        //                cmdUpdate.Parameters.AddWithValue("@ship_date", model.ShipDate);
        //                cmdUpdate.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
        //                cmdUpdate.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
        //                cmdUpdate.Parameters.AddWithValue("@payment_method", model.PaymentMethod);
        //                cmdUpdate.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
        //                cmdUpdate.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
        //                cmdUpdate.Parameters.AddWithValue("@payment_date", model.PaymentDate);
        //                cmdUpdate.Parameters.AddWithValue("@total", model.TotalBeforeVat);
        //                cmdUpdate.Parameters.AddWithValue("@vat", model.Vat);
        //                cmdUpdate.Parameters.AddWithValue("@net", model.NetTotal);
        //                cmdUpdate.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
        //                cmdUpdate.Parameters.AddWithValue("@change", model.PaymentMethod == "Cash" ? 0 : model.NetTotal);
        //                cmdUpdate.Parameters.AddWithValue("@modified_by", userId);
        //                cmdUpdate.Parameters.AddWithValue("@modified_date", DateTime.Now);

        //                await cmdUpdate.ExecuteNonQueryAsync();

        //                // Delete old sales details & accounting
        //                await CommonDeleteTransactionsAsync(conn, tx, invId);
        //            }

        //            // =======================
        //            // 3. INSERT SALES ITEMS
        //            // =======================
        //            await InsertSalesItemAsync(conn, tx, invId, model.Items);

        //            // =======================
        //            // 4. PROCESS INVENTORY
        //            // =======================
        //            await ProcessInventoryAsync(conn, tx, invId, model.Items, model.WarehouseId);

        //            // =======================
        //            // 5. ACCOUNTING ENTRIES
        //            // =======================
        //            await InsertAccountingAsync(conn, tx, invId, model, userId);

        //            // =======================
        //            // 6. COMMIT
        //            // =======================
        //            await tx.CommitAsync();

        //            return Json(new { status = true, message = model.Id == 0 ? "Invoice created successfully" : "Invoice updated successfully", invoiceId = invId });
        //        }
        //        catch (Exception ex)
        //        {
        //            await tx.RollbackAsync();
        //            return Json(new { status = false, message = ex.Message });
        //        }
        //    }


        //    private async Task ReturnItemsToInventoryAsync(MySqlConnection conn, MySqlTransaction tx, long invoiceId)
        //    {
        //        var query = @"
        //    SELECT sd.item_id, sd.qty, i.method, i.type
        //    FROM tbl_sales_details sd
        //    INNER JOIN tbl_items i ON sd.item_id = i.id
        //    WHERE sd.sales_id = @salesId";

        //        using var cmd = new MySqlCommand(query, conn, tx);
        //        cmd.Parameters.AddWithValue("@salesId", invoiceId);

        //        using var reader = await cmd.ExecuteReaderAsync();
        //        var itemsToReturn = new List<(int ItemId, decimal Qty, string Method, string Type)>();
        //        while (await reader.ReadAsync())
        //        {
        //            itemsToReturn.Add((
        //                reader.GetInt32("item_id"),
        //                reader.GetDecimal("qty"),
        //                reader.GetString("method"),
        //                reader.GetString("type")
        //            ));
        //        }
        //        reader.Close();

        //        foreach (var item in itemsToReturn)
        //        {
        //            if (item.Type == "12 - Service") continue;

        //            if (item.Type == "13 - Inventory Assembly")
        //            {
        //                var compQuery = "SELECT item_id, qty FROM tbl_item_assembly WHERE assembly_id=@assemblyId";
        //                using var compCmd = new MySqlCommand(compQuery, conn, tx);
        //                compCmd.Parameters.AddWithValue("@assemblyId", item.ItemId);

        //                using var compReader = await compCmd.ExecuteReaderAsync();
        //                var components = new List<(int ComponentId, decimal Qty)>();
        //                while (await compReader.ReadAsync())
        //                {
        //                    components.Add((
        //                        compReader.GetInt32("item_id"),
        //                        compReader.GetDecimal("qty") * item.Qty
        //                    ));
        //                }
        //                compReader.Close();

        //                foreach (var comp in components)
        //                {
        //                    var updateQty = "UPDATE tbl_items SET on_hand = on_hand + @qty WHERE id=@itemId";
        //                    using var updateCmd = new MySqlCommand(updateQty, conn, tx);
        //                    updateCmd.Parameters.AddWithValue("@qty", comp.Qty);
        //                    updateCmd.Parameters.AddWithValue("@itemId", comp.ComponentId);
        //                    await updateCmd.ExecuteNonQueryAsync();
        //                }
        //            }
        //            else
        //            {
        //                var updateQty = "UPDATE tbl_items SET on_hand = on_hand + @qty WHERE id=@itemId";
        //                using var updateCmd = new MySqlCommand(updateQty, conn, tx);
        //                updateCmd.Parameters.AddWithValue("@qty", item.Qty);
        //                updateCmd.Parameters.AddWithValue("@itemId", item.ItemId);
        //                await updateCmd.ExecuteNonQueryAsync();
        //            }
        //        }
        //    }


        //    private async Task CommonDeleteTransactionsAsync(MySqlConnection conn, MySqlTransaction tx, long invoiceId)
        //    {
        //        try
        //        {
        //            var deleteSalesDetails = "DELETE FROM tbl_sales_details WHERE sales_id=@id";
        //            using (var cmd = new MySqlCommand(deleteSalesDetails, conn, tx))
        //            {
        //                cmd.Parameters.AddWithValue("@id", invoiceId);
        //                await cmd.ExecuteNonQueryAsync();
        //            }

        //            var deleteItemTrans = @"
        //    DELETE FROM tbl_item_transaction WHERE reference=@invId AND type='SALES';
        //    DELETE FROM tbl_item_card_details WHERE trans_type='Sales Invoice' AND trans_no=@invId;
        //    DELETE FROM tbl_cost_center_transaction WHERE ref_id=@invId AND type='Sales';
        //    DELETE FROM tbl_transaction WHERE transaction_id=@invId AND type='Sales Invoice Cash';
        //";
        //            using (var cmd = new MySqlCommand(deleteItemTrans, conn, tx))
        //            {
        //                cmd.Parameters.AddWithValue("@invId", invoiceId);
        //                await cmd.ExecuteNonQueryAsync();
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            throw ex;
        //        }
        //    }


        //    private async Task InsertSalesItemAsync(MySqlConnection conn, MySqlTransaction tx, long invoiceId, List<SalesItemRequest> items)
        //    {
        //        foreach (var item in items)
        //        {
        //            var insertDetail = @"
        //        INSERT INTO tbl_sales_details
        //        (sales_id, item_id, qty, cost_price, price, discount, vatp, vat, total, cost_center_id)
        //        VALUES
        //        (@sales_id, @item_id, @qty, @cost_price, @price, @discount, @vatp, @vat, @total, @costCenter)";

        //            using var cmd = new MySqlCommand(insertDetail, conn, tx);
        //            cmd.Parameters.AddWithValue("@sales_id", invoiceId);
        //            cmd.Parameters.AddWithValue("@item_id", item.ItemId);
        //            cmd.Parameters.AddWithValue("@qty", item.Qty);
        //            cmd.Parameters.AddWithValue("@cost_price", item.CostPrice);
        //            cmd.Parameters.AddWithValue("@price", item.Price);
        //            cmd.Parameters.AddWithValue("@discount", item.Discount);
        //            cmd.Parameters.AddWithValue("@vatp", item.VatP);
        //            cmd.Parameters.AddWithValue("@vat", item.Vat);
        //            cmd.Parameters.AddWithValue("@total", item.Total);
        //            cmd.Parameters.AddWithValue("@costCenter", item.CostCenterId);
        //            await cmd.ExecuteNonQueryAsync();
        //        }
        //    }


        //    private async Task ProcessInventoryAsync(MySqlConnection conn, MySqlTransaction tx, long invoiceId, List<SalesItemRequest> items, int warehouseId)
        //    {
        //        foreach (var item in items)
        //        {
        //            if (item.Type == "12 - Service") continue;

        //            if (item.Type == "13 - Inventory Assembly")
        //            {
        //                var compQuery = "SELECT item_id, qty, method FROM tbl_item_assembly INNER JOIN tbl_items ON tbl_item_assembly.item_id=tbl_items.id WHERE assembly_id=@assemblyId";
        //                using var cmd = new MySqlCommand(compQuery, conn, tx);
        //                cmd.Parameters.AddWithValue("@assemblyId", item.ItemId);

        //                using var reader = await cmd.ExecuteReaderAsync();
        //                var components = new List<(int Id, decimal Qty, string Method)>();
        //                while (await reader.ReadAsync())
        //                {
        //                    components.Add((
        //                        reader.GetInt32("item_id"),
        //                        reader.GetDecimal("qty") * item.Qty,
        //                        reader.GetString("method")
        //                    ));
        //                }
        //                reader.Close();

        //                foreach (var comp in components)
        //                {
        //                    var updateQty = "UPDATE tbl_items SET on_hand = on_hand - @qty WHERE id=@itemId";
        //                    using var updateCmd = new MySqlCommand(updateQty, conn, tx);
        //                    updateCmd.Parameters.AddWithValue("@qty", comp.Qty);
        //                    updateCmd.Parameters.AddWithValue("@itemId", comp.Id);
        //                    await updateCmd.ExecuteNonQueryAsync();
        //                }
        //            }
        //            else
        //            {
        //                var updateQty = "UPDATE tbl_items SET on_hand = on_hand - @qty WHERE id=@itemId";
        //                using var cmd = new MySqlCommand(updateQty, conn, tx);
        //                cmd.Parameters.AddWithValue("@qty", item.Qty);
        //                cmd.Parameters.AddWithValue("@itemId", item.ItemId);
        //                await cmd.ExecuteNonQueryAsync();
        //            }
        //        }
        //    }

        //    private async Task InsertAccountingAsync(
        //   MySqlConnection conn,
        //   MySqlTransaction tx,
        //   long invoiceId,
        //   SalesInvoiceRequest model,
        //   int userId)
        //    {
        //        // Determine main account (Cash or Credit)
        //        string accountId = model.PaymentMethod == "Credit"
        //            ? model.PaymentCreditAccountId.ToString()
        //            : model.AccountCashId.ToString();
        //        int level4SalesInvoice = await GetDefaultAccountId("Sales");
        //        int level4VatId = await GetDefaultAccountId("Vat Output");
        //        int level4COGS = await GetDefaultAccountId("COGS");
        //        int level4Id = await GetDefaultAccountId("Inventory");

        //        // =========================
        //        // 1. Customer / Cash / Credit Entry
        //        // =========================
        //        if (model.NetTotal > 0)
        //        {
        //            await InsertTransactionAsync(
        //                conn, tx,
        //                model.InvoiceDate,
        //                accountId,
        //                model.PaymentMethod == "Cash" ? model.NetTotal : 0,
        //                model.PaymentMethod == "Credit" ? model.NetTotal : 0,
        //                invoiceId,
        //                model.CustomerId,
        //                model.PaymentMethod == "Credit" ? "Sales Invoice" : "Sales Invoice Cash",
        //                "SALES",
        //                $"Sales Invoice NO. {model.InvoiceCode}",
        //                userId,
        //                model.InvoiceCode
        //            );
        //        }

        //        // =========================
        //        // 2. Sales Revenue Entry
        //        // =========================
        //        await InsertTransactionAsync(
        //            conn, tx,
        //            model.InvoiceDate,
        //            level4SalesInvoice.ToString(),
        //            0,
        //            model.TotalBeforeVat,
        //            invoiceId,
        //            0,
        //            model.PaymentMethod == "Credit" ? "Sales Invoice" : "Sales Invoice Cash",
        //            "SALES",
        //            $"Sales Revenue For Invoice No. {model.InvoiceCode}",
        //            userId,
        //            model.InvoiceCode
        //        );

        //        // =========================
        //        // 3. VAT Output Entry (only if VAT > 0)
        //        // =========================
        //        if (model.Vat > 0)
        //        {
        //            await InsertTransactionAsync(
        //                conn, tx,
        //                model.InvoiceDate,
        //                level4VatId.ToString(),
        //                0,
        //                model.Vat,
        //                invoiceId,
        //                0,
        //                model.PaymentMethod == "Credit" ? "Sales Invoice" : "Sales Invoice Cash",
        //                "SALES",
        //                $"Vat Output For Invoice No. {model.InvoiceCode}",
        //                userId,
        //                model.InvoiceCode
        //            );
        //        }

        //        // =========================
        //        // 4. COGS & Inventory Entries (only if inventory cost > 0)
        //        // =========================
        //        if (model.InventoryCost > 0)
        //        {
        //            // COGS Debit
        //            await InsertTransactionAsync(
        //                conn, tx,
        //                model.InvoiceDate,
        //                level4COGS.ToString(),
        //                model.InventoryCost,
        //                0,
        //                invoiceId,
        //                0,
        //                "Sales Invoice",
        //                "SALES",
        //                $"COGS For Sales No. {model.InvoiceCode}",
        //                userId,
        //                model.InvoiceCode
        //            );

        //            // Inventory Credit (Warehouse Account)
        //            await InsertTransactionAsync(
        //                conn, tx,
        //                model.InvoiceDate,
        //                level4Id.ToString(),
        //                0,
        //                model.InventoryCost,
        //                invoiceId,
        //                0,
        //                "Sales Invoice",
        //                "SALES",
        //                $"Item Sold For Sales No. {model.InvoiceCode}",
        //                userId,
        //                model.InvoiceCode
        //            );
        //        }
        //    }

        //    private static async Task InsertTransactionAsync(
        //        MySqlConnection conn,
        //        MySqlTransaction tx,
        //        DateTime date,
        //        string accountId,
        //        decimal debit,
        //        decimal credit,
        //        long transactionId,
        //        long humId,
        //        string type,
        //        string tType,
        //        string description,
        //        int createdBy,
        //        string voucherNo)
        //    {
        //        using var cmd = new MySqlCommand(@"
        //    INSERT INTO tbl_transaction
        //    (date, account_id, debit, credit, transaction_id, hum_id,
        //     t_type, type, description, created_by, created_date, state, voucher_no)
        //    VALUES
        //    (@date, @account_id, @debit, @credit, @transaction_id, @hum_id,
        //     @t_type, @type, @description, @created_by, @created_date, 0, @voucher_no)
        //", conn, tx);

        //        cmd.Parameters.AddWithValue("@date", date);
        //        cmd.Parameters.AddWithValue("@account_id", accountId);
        //        cmd.Parameters.AddWithValue("@debit", debit);
        //        cmd.Parameters.AddWithValue("@credit", credit);
        //        cmd.Parameters.AddWithValue("@transaction_id", transactionId);
        //        cmd.Parameters.AddWithValue("@hum_id", humId);
        //        cmd.Parameters.AddWithValue("@t_type", tType);
        //        cmd.Parameters.AddWithValue("@type", type);
        //        cmd.Parameters.AddWithValue("@description", description);
        //        cmd.Parameters.AddWithValue("@created_by", createdBy);
        //        cmd.Parameters.AddWithValue("@created_date", DateTime.Now);
        //        cmd.Parameters.AddWithValue("@voucher_no", voucherNo);

        //        await cmd.ExecuteNonQueryAsync();
        //    }


        //    #endregion

        [HttpGet]
        public async Task<IActionResult> GetSalesInvoiceReport(int salesId)
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

                // 🔹 SALES HEADER
                var saleCmd = new MySqlCommand(@"
SELECT
    s.id, s.date, s.invoice_id,
    c.name AS customerName,
    s.payment_method,
    s.total, s.vat, s.net,
    s.city, s.sales_man, s.ship_date,
    coa.name AS accountName
FROM tbl_sales s
INNER JOIN tbl_customer c      ON s.customer_id    = c.id
LEFT  JOIN tbl_coa_level_4 coa ON coa.id           = s.account_cash_id
WHERE s.id = @salesId;", conn);

                saleCmd.Parameters.Add("@salesId", MySqlDbType.Int32).Value = salesId;

                SaleReportDto sale = null;
                await using (var reader = await saleCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        sale = new SaleReportDto
                        {
                            Id = reader.GetInt32("id"),
                            Date = reader.GetDateTime("date"),
                            InvoiceNo = reader.GetString("invoice_id"),
                            CustomerName = reader.GetString("customerName"),
                            PaymentMethod = reader.IsDBNull("payment_method") ? null : reader.GetString("payment_method"),
                            Total = reader.GetDecimal("total"),
                            Vat = reader.GetDecimal("vat"),
                            Net = reader.GetDecimal("net"),
                            City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                            SalesMan = reader.IsDBNull("sales_man") ? null : reader.GetString("sales_man"),
                            ShipDate = reader.IsDBNull("ship_date") ? (DateTime?)null : reader.GetDateTime("ship_date"),
                            AccountName = reader.IsDBNull("accountName") ? null : reader.GetString("accountName")
                        };
                    }
                }

                if (sale == null)
                    return NotFound(new { status = false, message = "Invoice not found" });

                // 🔹 ITEMS
                var itemCmd = new MySqlCommand(@"
SELECT
    d.item_id, i.code, i.name,
    d.qty, d.price, d.discount, d.vat, d.total,
    cc.name AS costCenterName,
    u.name  AS unitName
FROM tbl_sales_details d
INNER JOIN tbl_items i              ON d.item_id     = i.id
LEFT  JOIN tbl_sub_cost_center cc   ON cc.id         = d.cost_center_id
LEFT  JOIN tbl_unit u               ON u.id          = i.unit_id
WHERE d.sales_id = @salesId;", conn);

                itemCmd.Parameters.Add("@salesId", MySqlDbType.Int32).Value = salesId;

                var items = new List<SaleItemReportDto>();
                await using (var reader = await itemCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new SaleItemReportDto
                        {
                            ItemId = reader.GetInt32("item_id"),
                            Code = reader.GetString("code"),
                            Name = reader.GetString("name"),
                            Qty = Convert.ToDecimal(reader["qty"]),
                            Price = Convert.ToDecimal(reader["price"]),
                            Discount = Convert.ToDecimal(reader["discount"]),
                            Vat = Convert.ToDecimal(reader["vat"]),
                            Total = Convert.ToDecimal(reader["total"]),
                            CostCenterName = reader.IsDBNull("costCenterName") ? null : reader.GetString("costCenterName"),
                            UnitName = reader.IsDBNull("unitName") ? null : reader.GetString("unitName")
                        });
                    }
                }

                // 🔹 COMPANY INFO
                // Added: phone2, website alongside existing fields
                var companyCmd = new MySqlCommand(@"
SELECT
    id, name,
    phone1, phone2,
    gmail, website,
    address, trn_no,
    logoComp
FROM tbl_company
LIMIT 1;", conn);

                CompanyReportDto company = null;
                await using (var reader = await companyCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        company = new CompanyReportDto
                        {
                            Name = reader.GetString("name"),
                            Phone = reader.IsDBNull("phone1") ? null : reader.GetString("phone1"),
                            Phone2 = reader.IsDBNull("phone2") ? null : reader.GetString("phone2"),
                            Email = reader.IsDBNull("gmail") ? null : reader.GetString("gmail"),
                            Website = reader.IsDBNull("website") ? null : reader.GetString("website"),
                            Address = reader.IsDBNull("address") ? null : reader.GetString("address"),
                            TRN = reader.IsDBNull("trn_no") ? null : reader.GetString("trn_no"),
                            Logo = reader.IsDBNull("logoComp") ? null : (byte[])reader["logoComp"]
                        };
                    }
                }

                if (company == null)
                    return BadRequest(new { status = false, message = "Company info not found" });

                // 🔹 PDF GENERATION
                byte[] pdfBytes = SapInvoicePdfGenerator.Generate(company, sale, items);

                return File(pdfBytes, "application/pdf", fileDownloadName: null);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
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
                // 1. GET DEFAULT ACCOUNTS
                // =======================
                var defaultAccounts = await GetDefaultAccountsAsync(conn, tx);

                int level4PaymentCreditMethodId = defaultAccounts.ContainsKey("Customer")
                    ? defaultAccounts["Customer"] : 0;
                int level4VatId = defaultAccounts.ContainsKey("Vat Output")
                    ? defaultAccounts["Vat Output"] : 0;
                int level4SalesInvoice = defaultAccounts.ContainsKey("Sales")
                    ? defaultAccounts["Sales"] : 0;
                int level4COGS = defaultAccounts.ContainsKey("COGS")
                    ? defaultAccounts["COGS"] : 0;
                int level4Inventory = defaultAccounts.ContainsKey("Inventory")
                    ? defaultAccounts["Inventory"] : 0;
               

                // Set default cash account if not provided
                if (model.AccountCashId <= 0 && model.PaymentMethod == "Cash")
                {
                    model.AccountCashId = defaultAccounts.ContainsKey("Invoice Payment Cash Method")
                        ? defaultAccounts["Invoice Payment Cash Method"] : 0;
                }

                // =======================
                // 2. VALIDATIONS
                // =======================

                // Validate item totals
                if (model.Items != null && model.Items.Count > 0)
                {
                    for (int i = 0; i < model.Items.Count; i++)
                    {
                        if (model.Items[i].Total <= 0)
                            return Json(new
                            {
                                status = false,
                                message = $"Total Item In Row {i + 1} Can't Be 0 or Null"
                            });
                    }
                }

                if (model.CustomerId <= 0)
                    return Json(new { status = false, message = "Customer Must be Selected." });

                if (model.AccountCashId <= 0)
                    return Json(new { status = false, message = "Account Cash Name Must be Selected." });

                if (string.IsNullOrWhiteSpace(model.PaymentMethod))
                    return Json(new { status = false, message = "Payment Method Must be Selected." });

                if (model.WarehouseId <= 0)
                    return Json(new { status = false, message = "Warehouse Must be Selected." });

                if (model.Items == null || model.Items.Count == 0)
                    return Json(new { status = false, message = "Insert Items First." });

                if (model.NetTotal <= 0)
                    return Json(new { status = false, message = "Total Must Be Bigger Than Zero" });

                // Check default accounts configuration
                if (level4PaymentCreditMethodId <= 0 || level4VatId <= 0 ||
                    level4SalesInvoice <= 0 || level4COGS <= 0 || level4Inventory <= 0)
                {
                    return Json(new
                    {
                        status = false,
                        message = "Default accounts for invoice are not properly configured. Please check your settings."
                    });
                }

                // Stock validation (if not allowing negative stock)
                bool allowItemWithoutQty = HttpContext.Session.GetInt32("AllowItemWithoutQty") == 1;

                if (!allowItemWithoutQty)
                {
                    var validationResult = await ValidateItemsStockAsync(conn, tx, model.Items, model.Id);
                    if (!validationResult.IsValid)
                        return Json(new { status = false, message = validationResult.Message });
                }

                // =======================
                // 3. INSERT INVOICE
                // =======================
                long invId = model.Id;
                string invoiceCode = model.InvoiceCode;

                if (model.Id == 0) // New Invoice
                {
                    // Generate next invoice code
                    if (string.IsNullOrEmpty(invoiceCode))
                    {
                        invoiceCode = "SI-0001";
                        using (var cmd = new MySqlCommand(
                            "SELECT MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)) FROM tbl_sales", conn, tx))
                        {
                            var result = await cmd.ExecuteScalarAsync();
                            if (result != DBNull.Value && result != null)
                                invoiceCode = "SI-" + (Convert.ToInt32(result) + 1).ToString("D4");
                        }
                    }

                    var insertSql = @"
                INSERT INTO tbl_sales 
                (date, customer_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man, 
                 ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date,
                 total, vat, net, pay, `change`, created_by, created_date, state)
                VALUES
                (@date, @customer_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man, 
                 @ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms, @payment_date,
                 @total, @vat, @net, @pay, @change, @created_by, @created_date, @state);
                SELECT LAST_INSERT_ID();";

                    using var cmdInsert = new MySqlCommand(insertSql, conn, tx);
                    cmdInsert.Parameters.AddWithValue("@date", model.InvoiceDate.Date);
                    cmdInsert.Parameters.AddWithValue("@customer_id", model.CustomerId);
                    cmdInsert.Parameters.AddWithValue("@invoice_id", invoiceCode);
                    cmdInsert.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    cmdInsert.Parameters.AddWithValue("@po_num", model.PoNo ?? "");
                    cmdInsert.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    cmdInsert.Parameters.AddWithValue("@city", model.City ?? "");
                    cmdInsert.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    cmdInsert.Parameters.AddWithValue("@ship_date", model.ShipDate.Date);
                    cmdInsert.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    cmdInsert.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    cmdInsert.Parameters.AddWithValue("@payment_method", model.PaymentMethod);
                    cmdInsert.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    cmdInsert.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    cmdInsert.Parameters.AddWithValue("@payment_date", model.PaymentDate.Date);
                    cmdInsert.Parameters.AddWithValue("@total", model.TotalBeforeVat);
                    cmdInsert.Parameters.AddWithValue("@vat", model.Vat);
                    cmdInsert.Parameters.AddWithValue("@net", model.NetTotal);
                    cmdInsert.Parameters.AddWithValue("@pay",
                        model.PaymentMethod == "Cash" ? model.NetTotal : 0);
                    cmdInsert.Parameters.AddWithValue("@change",
                        model.PaymentMethod == "Cash" ? 0 : model.NetTotal);
                    cmdInsert.Parameters.AddWithValue("@created_by", userId);
                    cmdInsert.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                    cmdInsert.Parameters.AddWithValue("@state", 0);

                    invId = Convert.ToInt64(await cmdInsert.ExecuteScalarAsync());
                }
                if (model.Id != 0) // Existing Invoice
                {
                    // Update existing invoice
                    var updateSql = @"
        UPDATE tbl_sales
        SET
            date = @date,
            customer_id = @customer_id,
            invoice_id = @invoice_id,
            warehouse_id = @warehouse_id,
            po_num = @po_num,
            bill_to = @bill_to,
            city = @city,
            sales_man = @sales_man,
            ship_date = @ship_date,
            ship_via = @ship_via,
            ship_to = @ship_to,
            payment_method = @payment_method,
            account_cash_id = @account_cash_id,
            payment_terms = @payment_terms,
            payment_date = @payment_date,
            total = @total,
            vat = @vat,
            net = @net,
            pay = @pay,
            `change` = @change,
            modified_by = @modified_by,
            modified_date = @modified_date,
            state = @state
        WHERE id = @id;
    ";

                    using var cmdUpdate = new MySqlCommand(updateSql, conn, tx);
                    cmdUpdate.Parameters.AddWithValue("@date", model.InvoiceDate.Date);
                    cmdUpdate.Parameters.AddWithValue("@customer_id", model.CustomerId);
                    cmdUpdate.Parameters.AddWithValue("@invoice_id", invoiceCode);
                    cmdUpdate.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    cmdUpdate.Parameters.AddWithValue("@po_num", model.PoNo ?? "");
                    cmdUpdate.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    cmdUpdate.Parameters.AddWithValue("@city", model.City ?? "");
                    cmdUpdate.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    cmdUpdate.Parameters.AddWithValue("@ship_date", model.ShipDate.Date);
                    cmdUpdate.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    cmdUpdate.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    cmdUpdate.Parameters.AddWithValue("@payment_method", model.PaymentMethod);
                    cmdUpdate.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    cmdUpdate.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    cmdUpdate.Parameters.AddWithValue("@payment_date", model.PaymentDate.Date);
                    cmdUpdate.Parameters.AddWithValue("@total", model.TotalBeforeVat);
                    cmdUpdate.Parameters.AddWithValue("@vat", model.Vat);
                    cmdUpdate.Parameters.AddWithValue("@net", model.NetTotal);
                    cmdUpdate.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
                    cmdUpdate.Parameters.AddWithValue("@change", model.PaymentMethod == "Cash" ? 0 : model.NetTotal);
                    cmdUpdate.Parameters.AddWithValue("@modified_by", userId);
                    cmdUpdate.Parameters.AddWithValue("@modified_date", DateTime.Now.Date);
                    cmdUpdate.Parameters.AddWithValue("@state", 0);
                    cmdUpdate.Parameters.AddWithValue("@id", model.Id); 

                    await cmdUpdate.ExecuteNonQueryAsync();

                    await CommonDelete.ReturnItemsToInventoryAsync(conn, tx, invId);
                    await CommonDelete.DeleteSalesDetailsAsync(conn, tx, invId);
                    await CommonDelete.DeleteItemTransactionsAsync(conn, tx, invId);
                    await CommonDelete.DeleteCostCenterTransactionEntryAsync(conn, tx, invId.ToString(), "Sales");
                    await CommonDelete.DeleteTransactionEntryAsync(conn, tx, invId, "SALES");
                }


                // =======================
                // 4. INSERT SALES ITEMS & CALCULATE INVENTORY COST
                // =======================
                decimal itemInventoryCost = await InsertInvoiceItemsAsync(conn, tx, invId,
                    model.Items, model.InvoiceDate, invoiceCode, model.WarehouseId, allowItemWithoutQty);

                // =======================
                // 5. TRANSFER FROM QUOTATION/ORDER
                // =======================
                //if (!string.IsNullOrEmpty(model.FormType))
                //{
                //    if (model.FormType == "SQ")
                //    {
                //        using var cmdTransfer = new MySqlCommand(
                //            "UPDATE tbl_sales_quotation SET tranfer_status=1, sales_id=@id WHERE id=@sqId",
                //            conn, tx);
                //        cmdTransfer.Parameters.AddWithValue("@sqId", model.PoNo);
                //        cmdTransfer.Parameters.AddWithValue("@id", invId);
                //        await cmdTransfer.ExecuteNonQueryAsync();
                //    }
                //    else if (model.FormType == "SO")
                //    {
                //        using var cmdTransfer = new MySqlCommand(
                //            "UPDATE tbl_sales_order SET tranfer_status=1, sales_id=@id WHERE id=@soId",
                //            conn, tx);
                //        cmdTransfer.Parameters.AddWithValue("@soId", model.PoNo);
                //        cmdTransfer.Parameters.AddWithValue("@id", invId);
                //        await cmdTransfer.ExecuteNonQueryAsync();
                //    }
                //}

                // =======================
                // 6. ACCOUNTING TRANSACTIONS
                // =======================
                if (model.NetTotal > 0)
                {
                    string transactionType = model.PaymentMethod == "Credit"
                        ? "Sales Invoice"
                        : "Sales Invoice Cash";

                    string accountId = model.PaymentMethod == "Credit"
                        ? level4PaymentCreditMethodId.ToString()
                        : model.AccountCashId.ToString();

                    // 1️⃣ Debit: Cash / A/R
                    await AddTransactionEntryAsync(conn, tx, model.InvoiceDate.Date,
                        accountId,
                        model.NetTotal.ToString(), "0",
                        invId.ToString(), model.CustomerId.ToString(),
                        transactionType, "SALES",
                        $"Sales Invoice NO. {invoiceCode}",
                        userId, DateTime.Now.Date, invoiceCode);

                    // 2️⃣ Credit: Sales Revenue (Total Before VAT)
                    await AddTransactionEntryAsync(conn, tx, model.InvoiceDate.Date,
                        level4SalesInvoice.ToString(),
                        "0", model.TotalBeforeVat.ToString(),
                        invId.ToString(), "0",
                        transactionType, "SALES",
                        $"Sales Revenue For Invoice No. {invoiceCode}",
                        userId, DateTime.Now.Date, invoiceCode);

                    // 3️⃣ Credit: VAT (if > 0)
                    if (model.Vat > 0)
                    {
                        await AddTransactionEntryAsync(conn, tx, model.InvoiceDate.Date,
                            level4VatId.ToString(),
                            "0", model.Vat.ToString(),
                            invId.ToString(), "0",
                            transactionType, "SALES",
                            $"Vat Output For Invoice No. {invoiceCode}",
                            userId, DateTime.Now.Date, invoiceCode);
                    }

                    // 4️⃣ COGS + Inventory
                    if (itemInventoryCost > 0)
                    {
                        int warehouseAccountId = defaultAccounts.ContainsKey("COGS")
                ? defaultAccounts["COGS"] : 0;

                        using (var cmdWarehouse = new MySqlCommand(
                            "SELECT account_id FROM tbl_warehouse WHERE id = @id", conn, tx))
                        {
                            cmdWarehouse.Parameters.AddWithValue("@id", model.WarehouseId);
                            var result = await cmdWarehouse.ExecuteScalarAsync();

                            ////if (result != null && result != DBNull.Value)
                            ////    warehouseAccountId = Convert.ToInt32(result);
                        }

                        // 4.1 Debit COGS
                        await AddTransactionEntryAsync(conn, tx, model.InvoiceDate.Date,
                            level4COGS.ToString(),
                            itemInventoryCost.ToString("N2"), "0",
                            invId.ToString(), "0",
                            "Sales Invoice", "SALES",
                            $"COGS For Sales No. {invoiceCode}",
                            userId, DateTime.Now.Date, invoiceCode);

                        // 4.2 Credit Inventory (NO FALLBACK — same as desktop)
                        await AddTransactionEntryAsync(conn, tx, model.InvoiceDate.Date,
                            warehouseAccountId.ToString(),
                            "0", itemInventoryCost.ToString("N2"),
                            invId.ToString(), "0",
                            "Sales Invoice", "SALES",
                            $"Item Sold For Sales No. {invoiceCode}",
                            userId, DateTime.Now.Date, invoiceCode);
                    }
                }


                // =======================
                // 8. COMMIT TRANSACTION
                // =======================
                await tx.CommitAsync();

                return Json(new
                {
                    status = true,
                    message = "Invoice created successfully",
                    invoiceId = invId,
                    invoiceCode = invoiceCode
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { status = false, message = ex.Message });
            }
        }


        public static class CommonDelete
        {
           
            public static async Task ReturnItemsToInventoryAsync(MySqlConnection conn, MySqlTransaction tx, long invoiceId)
            {
                var sql = @"UPDATE tbl_items i
                    INNER JOIN tbl_item_transaction t ON i.id = t.item_id
                    SET i.on_hand = i.on_hand + t.qty_out
                    WHERE t.reference = @refId AND t.type = 'Sales Invoice'";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@refId", invoiceId);
                await cmd.ExecuteNonQueryAsync();
            }

            public static async Task DeleteSalesDetailsAsync(MySqlConnection conn, MySqlTransaction tx, long salesId)
            {
                var sql = "DELETE FROM tbl_sales_details WHERE sales_id = @salesId";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@salesId", salesId);
                await cmd.ExecuteNonQueryAsync();
            }

         
            public static async Task DeleteItemTransactionsAsync(MySqlConnection conn, MySqlTransaction tx, long invoiceId)
            {
                var sql = @"DELETE FROM tbl_item_transaction WHERE reference = @invId AND type = 'Sales Invoice';
                    DELETE FROM tbl_item_card_details WHERE trans_type = 'Sales Invoice' AND trans_no = @invId";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@invId", invoiceId);
                await cmd.ExecuteNonQueryAsync();
            }

            public static async Task DeleteCostCenterTransactionEntryAsync(MySqlConnection conn, MySqlTransaction tx, string refId, string type)
            {
                var sql = "DELETE FROM tbl_cost_center_transaction WHERE type = @type AND ref_id = @id";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@id", refId);
                cmd.Parameters.AddWithValue("@type", type);
                await cmd.ExecuteNonQueryAsync();
            }

            public static async Task DeleteTransactionEntryAsync(MySqlConnection conn, MySqlTransaction tx, long transactionId, string type)
            {
                var sql = "DELETE FROM tbl_transaction WHERE t_type = @tType AND transaction_id = @id";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@id", transactionId);
                cmd.Parameters.AddWithValue("@tType", type);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        private async Task<Dictionary<string, int>> GetDefaultAccountsAsync(
            MySqlConnection conn, MySqlTransaction tx)
        {
            var defaultAccounts = new Dictionary<string, int>();

            var query = @"
        SELECT category, account_id 
        FROM tbl_coa_config";

            using var cmd = new MySqlCommand(query, conn, tx);
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                string accountType = reader.GetString("category");
                int accountId = reader.GetInt32("account_id");
                defaultAccounts[accountType] = accountId;
            }

            return defaultAccounts;
        }

        //private async Task<int> GetDefaultAccountsAsync(MySqlConnection conn, MySqlTransaction tx)
        //{
        //    int accountId = 0;

        //    var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
        //    {
        //        Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
        //    };

        //    await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
        //    await conn.OpenAsync();

        //    var query = "SELECT account_id FROM tbl_coa_config WHERE category = @category LIMIT 1";
        //    await using var cmd = new MySqlCommand(query, conn);
        //    cmd.Parameters.AddWithValue("@category", category);

        //    var result = await cmd.ExecuteScalarAsync();
        //    if (result != null && result != DBNull.Value)
        //        accountId = Convert.ToInt32(result);

        //    return accountId;
        //}

        private async Task<(bool IsValid, string Message)> ValidateItemsStockAsync(
            MySqlConnection conn, MySqlTransaction tx, List<SalesItemRequest> items, long? refId = null)
        {
            var errorMessages = new List<string>();

            foreach (var item in items)
            {
                string editRefSql = refId.HasValue && refId.Value > 0
                    ? " AND t.reference != @refId "
                    : "";

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
            WHERE i.id = @id;";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@id", item.ItemId);
                if (refId.HasValue && refId.Value > 0)
                    cmd.Parameters.AddWithValue("@refId", refId.Value);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    errorMessages.Add($"Item ID {item.ItemId} not found");
                    await reader.CloseAsync();
                    continue;
                }

                string itemType = reader.GetString("type");
                string itemName = reader.GetString("name");
                decimal onHand = reader.GetDecimal("on_hand");
                await reader.CloseAsync();

                // Skip service items
                if (itemType == "12 - Service")
                    continue;

                // Validate inventory part
                if (itemType == "11 - Inventory Part")
                {
                    if (item.Qty > onHand)
                    {
                        errorMessages.Add(
                            $"Item Out Of Stock: {itemName}. Only {onHand:0.00} available, requested {item.Qty:0.00}");
                    }
                }
                // Validate assembly
                else if (itemType == "13 - Inventory Assembly")
                {
                    string assemblySql = @"
                SELECT i.name, a.qty, i.on_hand 
                FROM tbl_item_assembly a
                JOIN tbl_items i ON a.item_id = i.id
                WHERE a.assembly_id = @assemblyId;";

                    using var assemblyCmd = new MySqlCommand(assemblySql, conn, tx);
                    assemblyCmd.Parameters.AddWithValue("@assemblyId", item.ItemId);
                    using var assemblyReader = await assemblyCmd.ExecuteReaderAsync();

                    while (await assemblyReader.ReadAsync())
                    {
                        string compName = assemblyReader.GetString("name");
                        decimal required = assemblyReader.GetDecimal("qty") * item.Qty;
                        decimal available = assemblyReader.GetDecimal("on_hand");

                        if (required > available)
                        {
                            errorMessages.Add(
                                $"Component Out Of Stock: {compName}. Needs {required:0.00}, available {available:0.00}");
                        }
                    }
                    await assemblyReader.CloseAsync();
                }
            }

            if (errorMessages.Count > 0)
                return (false, string.Join("\n", errorMessages));

            return (true, "All items are valid");
        }

        private async Task<decimal> InsertInvoiceItemsAsync(MySqlConnection conn, MySqlTransaction tx,
            long invId, List<SalesItemRequest> items, DateTime invoiceDate, string invoiceCode,
            int warehouseId, bool allowItemWithoutQty)
        {
            decimal totalCostAllItems = 0;

            foreach (var item in items)
            {
                // Insert sales detail
                var insertDetailSql = @"
            INSERT INTO tbl_sales_details 
            (sales_id, item_id, qty, cost_price, price, discount, vatp, vat, total, cost_center_id)
            VALUES 
            (@sales_id, @item_id, @qty, @cost_price, @price, @discount, @vatp, @vat, @total, @costCenter);";

                using (var cmdDetail = new MySqlCommand(insertDetailSql, conn, tx))
                {
                    cmdDetail.Parameters.AddWithValue("@sales_id", invId);
                    cmdDetail.Parameters.AddWithValue("@item_id", item.ItemId);
                    cmdDetail.Parameters.AddWithValue("@qty", item.Qty);
                    cmdDetail.Parameters.AddWithValue("@price", item.Price);
                    cmdDetail.Parameters.AddWithValue("@cost_price",
                        item.CostPrice > 0 ? item.CostPrice : (object)DBNull.Value);
                    cmdDetail.Parameters.AddWithValue("@discount", item.Discount ?? 0);
                    cmdDetail.Parameters.AddWithValue("@vat", item.Vat);
                    cmdDetail.Parameters.AddWithValue("@vatp", item.VatP);
                    cmdDetail.Parameters.AddWithValue("@total", item.Total);
                    cmdDetail.Parameters.AddWithValue("@costCenter",
                        item.CostCenterId > 0 ? item.CostCenterId : (object)DBNull.Value);
                    await cmdDetail.ExecuteNonQueryAsync();
                }

                // Skip services
                if (item.Type == "12 - Service")
                    continue;

                decimal costAmount = 0;

                // Handle Inventory Assembly
                if (item.Type == "13 - Inventory Assembly")
                {
                    var componentsQuery = @"
                SELECT item_id, qty, 
                       (SELECT method FROM tbl_items WHERE tbl_items.id = tbl_item_assembly.item_id) as method 
                FROM tbl_item_assembly 
                WHERE assembly_id = @assemblyId";

                    using (var cmdComponents = new MySqlCommand(componentsQuery, conn, tx))
                    {
                        cmdComponents.Parameters.AddWithValue("@assemblyId", item.ItemId);
                        using var readerComponents = await cmdComponents.ExecuteReaderAsync();

                        var components = new List<(int itemId, decimal qty, string method)>();
                        while (await readerComponents.ReadAsync())
                        {
                            components.Add((
                                readerComponents.GetInt32("item_id"),
                                readerComponents.GetDecimal("qty") * item.Qty,
                                readerComponents.GetString("method").Trim()
                            ));
                        }
                        await readerComponents.CloseAsync();

                        foreach (var component in components)
                        {
                            // Update on_hand
                            using var cmdUpdate = new MySqlCommand(
                                "UPDATE tbl_items SET on_hand = on_hand - @qty WHERE id = @componentId",
                                conn, tx);
                            cmdUpdate.Parameters.AddWithValue("@qty", component.qty);
                            cmdUpdate.Parameters.AddWithValue("@componentId", component.itemId);
                            await cmdUpdate.ExecuteNonQueryAsync();

                            // Insert transaction and get cost
                            decimal costReturned = await InsertItemTransactionAsync(conn, tx,
                                component.itemId, component.qty, item.Price, component.method,
                                invoiceDate, invId, invoiceCode, warehouseId, allowItemWithoutQty);

                            costAmount = costReturned;
                            totalCostAllItems += costReturned;
                        }
                    }
                }
                else // Regular inventory item
                {
                    using (var cmdItem = new MySqlCommand("SELECT * FROM tbl_items WHERE id=@id", conn, tx))
                    {
                        cmdItem.Parameters.AddWithValue("@id", item.ItemId);
                        using var readerItem = await cmdItem.ExecuteReaderAsync();

                        if (await readerItem.ReadAsync())
                        {
                            decimal onHand = readerItem.GetDecimal("on_hand");
                            string method = readerItem.GetString("method");
                            await readerItem.CloseAsync();

                            // Update on_hand
                            using var cmdUpdate = new MySqlCommand(
                                "UPDATE tbl_items SET on_hand = @newQty WHERE id = @id", conn, tx);
                            cmdUpdate.Parameters.AddWithValue("@newQty", onHand - item.Qty);
                            cmdUpdate.Parameters.AddWithValue("@id", item.ItemId);
                            await cmdUpdate.ExecuteNonQueryAsync();

                            // Insert transaction and get cost
                            decimal costReturned = await InsertItemTransactionAsync(conn, tx,
                                item.ItemId, item.Qty, item.Price, method, invoiceDate, invId,
                                invoiceCode, warehouseId, allowItemWithoutQty);

                            costAmount = costReturned;
                            totalCostAllItems += costReturned;
                        }
                        else
                        {
                            await readerItem.CloseAsync();
                        }
                    }
                }

                // Insert cost center transaction
                if (item.CostCenterId.HasValue && item.CostCenterId.Value > 0)
                {
                    await InsertCostCenterTransactionAsync(conn, tx, invoiceDate, "0",
                        item.Total.ToString(), invId.ToString(), "Sales", "",
                        item.CostCenterId.Value.ToString());
                }
            }

            return totalCostAllItems;
        }

        private async Task<decimal> InsertItemTransactionAsync(MySqlConnection conn, MySqlTransaction tx,
            int itemId, decimal qty, decimal salesPrice, string method, DateTime invoiceDate,
            long invId, string invoiceCode, int warehouseId, bool allowItemWithoutQty)
        {
            if (allowItemWithoutQty)
            {
                // Allow selling without stock check
                decimal costPrice = 0;

                // Try to get recent cost price
                using (var cmd = new MySqlCommand(@"
            SELECT cost_price FROM tbl_item_transaction 
            WHERE item_id = @id AND date <= @date 
            ORDER BY date DESC LIMIT 1", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", itemId);
                    cmd.Parameters.AddWithValue("@date", invoiceDate);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                        costPrice = Convert.ToDecimal(result);
                }

                decimal totalCost = costPrice * qty;

                await InsertItemTransactionRecordAsync(conn, tx, invoiceDate.Date, "Sales Invoice",
                    invId.ToString(), itemId.ToString(), costPrice.ToString(), "0",
                    salesPrice.ToString(), qty.ToString(), "0",
                    $"Sales Invoice No. {invoiceCode} (Negative Stock)", warehouseId.ToString());

                return totalCost;
            }
            else
            {
                if (itemId <= 0 || qty <= 0)
                    return 0;

                decimal costPrice = 0;
                decimal totalCost = 0;

                if (method == "fifo" || method == "lifo")
                {
                    string orderBy = method == "fifo" ? "ASC" : "DESC";
                    decimal remainingQty = qty;

                    var transQuery = $@"
                SELECT * FROM tbl_item_transaction 
                WHERE date <= @date AND qty_inc > 0 AND item_id = @id 
                ORDER BY id {orderBy}";

                    using (var cmdTrans = new MySqlCommand(transQuery, conn, tx))
                    {
                        cmdTrans.Parameters.AddWithValue("@id", itemId);
                        cmdTrans.Parameters.AddWithValue("@date", invoiceDate);
                        using var reader = await cmdTrans.ExecuteReaderAsync();

                        var transactions = new List<(int id, decimal qtyInc, decimal costPrice)>();
                        while (await reader.ReadAsync())
                        {
                            transactions.Add((
                                reader.GetInt32("id"),
                                reader.GetDecimal("qty_inc"),
                                reader.GetDecimal("cost_price")
                            ));
                        }
                        await reader.CloseAsync();

                        foreach (var trans in transactions)
                        {
                            if (remainingQty <= 0) break;

                            decimal availableQty = trans.qtyInc;
                            costPrice = trans.costPrice;
                            decimal qtyToUse = Math.Min(remainingQty, availableQty);

                            remainingQty -= qtyToUse;
                            totalCost += costPrice * qtyToUse;

                            await InsertItemTransactionRecordAsync(conn, tx, invoiceDate.Date,
                                "Sales Invoice", invId.ToString(), itemId.ToString(),
                                costPrice.ToString(), "0", salesPrice.ToString(), qtyToUse.ToString(),
                                "0", $"Sales Invoice No. {invoiceCode}", warehouseId.ToString());

                            // Update qty_inc
                            using var cmdUpdate = new MySqlCommand(
                                "UPDATE tbl_item_transaction SET qty_inc = qty_inc - @qty WHERE id = @id",
                                conn, tx);
                            cmdUpdate.Parameters.AddWithValue("@qty", qtyToUse);
                            cmdUpdate.Parameters.AddWithValue("@id", trans.id);
                            await cmdUpdate.ExecuteNonQueryAsync();
                        }
                    }

                    // Handle remaining quantity (negative stock)
                    if (remainingQty > 0)
                    {
                        if (costPrice <= 0)
                        {
                            using (var cmd = new MySqlCommand(@"
                        SELECT cost_price FROM tbl_item_transaction 
                        WHERE item_id = @id AND date <= @date 
                        ORDER BY date DESC LIMIT 1", conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@id", itemId);
                                cmd.Parameters.AddWithValue("@date", invoiceDate);
                                var result = await cmd.ExecuteScalarAsync();
                                if (result != null && result != DBNull.Value)
                                    costPrice = Convert.ToDecimal(result);
                            }
                        }

                        totalCost += costPrice * remainingQty;

                        await InsertItemTransactionRecordAsync(conn, tx, invoiceDate.Date,
                            "Sales Invoice", invId.ToString(), itemId.ToString(),
                            costPrice.ToString(), "0", salesPrice.ToString(), remainingQty.ToString(),
                            "0", $"Sales Invoice No. {invoiceCode} (Neg. Stock)", warehouseId.ToString());
                    }
                }

                //        else // Average cost method
                //        {
                //            using (var cmd = new MySqlCommand(@"
                //SELECT 
                //    CASE 
                //        WHEN SUM(qty_in - qty_out) = 0 THEN 0
                //        ELSE SUM((qty_in - qty_out) * cost_price) / SUM(qty_in - qty_out)
                //    END AS cost_price 
                //FROM tbl_item_transaction 
                //WHERE item_id = @id AND date <= @date", conn, tx))
                //            {
                //                cmd.Parameters.AddWithValue("@id", itemId);
                //                cmd.Parameters.AddWithValue("@date", invoiceDate);
                //                var result = await cmd.ExecuteScalarAsync();
                //                if (result != null && result != DBNull.Value)
                //                    costPrice = Convert.ToDecimal(result);
                //            }

                //            await InsertItemTransactionRecordAsync(conn, tx, invoiceDate.Date, "Sales Invoice",
                //                invId.ToString(), itemId.ToString(), costPrice.ToString(), "0",
                //                salesPrice.ToString(), qty.ToString(), "0",
                //                $"Sales Invoice No. {invoiceCode}", warehouseId.ToString());

                //            // Get cost from item card details
                //            using (var cmdCard = new MySqlCommand(@"
                //SELECT (balance / qty_balance) cost 
                //FROM tbl_item_card_details 
                //WHERE DATE <= @date AND trans_type = 'Purchase Invoice' AND itemId = @itemId
                //ORDER BY trans_no DESC LIMIT 1", conn, tx))
                //            {
                //                cmdCard.Parameters.AddWithValue("@date", invoiceDate.Date);
                //                cmdCard.Parameters.AddWithValue("@itemId", itemId.ToString());
                //                using var reader = await cmdCard.ExecuteReaderAsync();

                //                if (await reader.ReadAsync())
                //                {
                //                    totalCost = reader.GetDecimal("cost") * qty;
                //                }
                //                await reader.CloseAsync();
                //            }
                //        }


                else // Average cost method
                {
                    // Get weighted average cost from remaining inventory (qty_inc)
                    using (var cmd = new MySqlCommand(@"
        SELECT 
            CASE 
                WHEN SUM(qty_inc) = 0 THEN 0
                ELSE SUM(qty_inc * cost_price) / SUM(qty_inc)
            END AS cost_price 
        FROM tbl_item_transaction 
        WHERE item_id = @id 
          AND date <= @date
          AND qty_inc > 0", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@id", itemId);
                        cmd.Parameters.AddWithValue("@date", invoiceDate);
                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                            costPrice = Convert.ToDecimal(result);
                    }

                    // Fallback: If no cost found, get the last purchase price
                    if (costPrice == 0)
                    {
                        using (var cmdFallback = new MySqlCommand(@"
            SELECT cost_price 
            FROM tbl_item_transaction 
            WHERE item_id = @id 
              AND date <= @date 
              AND qty_in > 0
            ORDER BY date DESC, id DESC
            LIMIT 1", conn, tx))
                        {
                            cmdFallback.Parameters.AddWithValue("@id", itemId);
                            cmdFallback.Parameters.AddWithValue("@date", invoiceDate);
                            var result = await cmdFallback.ExecuteScalarAsync();
                            if (result != null && result != DBNull.Value)
                                costPrice = Convert.ToDecimal(result);
                        }
                    }

                    // Calculate total cost for this sale
                    totalCost = costPrice * qty;

                    // Insert the transaction record
                    await InsertItemTransactionRecordAsync(conn, tx, invoiceDate.Date, "Sales Invoice",
                        invId.ToString(), itemId.ToString(), costPrice.ToString(), "0",
                        salesPrice.ToString(), qty.ToString(), "0",
                        $"Sales Invoice No. {invoiceCode}", warehouseId.ToString());
                }


                return totalCost;
            }
        }

        private async Task InsertItemTransactionRecordAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, string type, string reference, string itemId, string price,
            string qtyIn, string salesPrice, string qtyOut, string qtyInc, string description,
            string warehouseId)
        {
            var sql = @"
        INSERT INTO tbl_item_transaction 
        (date, type, reference, item_id, cost_price, qty_in, sales_price, qty_out, qty_inc, description, warehouse_id) 
        VALUES (@date, @type, @reference, @itemId, @costPrice, @qtyIn, @sales_price, @qtyOut, @qtyInc, @description, @warehouseId);";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@reference", reference);
            cmd.Parameters.AddWithValue("@itemId", itemId);
            cmd.Parameters.AddWithValue("@costPrice", price);
            cmd.Parameters.AddWithValue("@sales_price", salesPrice);
            cmd.Parameters.AddWithValue("@qtyIn", qtyIn);
            cmd.Parameters.AddWithValue("@qtyOut", qtyOut);
            cmd.Parameters.AddWithValue("@qtyInc", qtyInc);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@warehouseId", warehouseId);
            await cmd.ExecuteNonQueryAsync();

            await UpdateOnHandItemAsync(conn, tx, itemId);
            await AddItemCardDetailsAsync(conn, tx, date, type, reference, itemId, price,
                qtyIn, salesPrice, qtyOut, qtyInc, description, warehouseId);
        }

        private async Task UpdateOnHandItemAsync(MySqlConnection conn, MySqlTransaction tx, string itemId)
        {
            var sql = @"
        UPDATE tbl_items 
        SET on_hand = (SELECT SUM(qty_in - qty_out) FROM tbl_item_transaction WHERE item_id = @itemId) 
        WHERE id = @itemId";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@itemId", itemId);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task AddItemCardDetailsAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, string type, string reference, string itemId, string costPrice,
            string qtyIn, string salesPrice, string qtyOut, string qtyInc, string description,
            string warehouseId)
        {
            string invoiceNo = "INV-" + reference;
            string transNo = reference;
            string transType = type;
            decimal qtyBalance = 0;
            decimal debit = 0;
            decimal credit = 0;
            decimal price = decimal.Parse(costPrice);
            decimal balance = 0;
            decimal fifoQty = 0;
            decimal fifoCost = 0;
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
            using (var cmdQty = new MySqlCommand(
                "SELECT IFNULL(SUM(qty_in-qty_out),0) as QtyBalance FROM tbl_item_card_details WHERE itemId = @id",
                conn, tx))
            {
                cmdQty.Parameters.AddWithValue("@id", itemId);
                var result = await cmdQty.ExecuteScalarAsync();
                decimal _QtyBalance = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                qtyBalance = _QtyBalance + (_qtyIn - _qtyOut);
            }

            using (var cmdBal = new MySqlCommand(
                "SELECT IFNULL(SUM(debit-credit),0) as Balance FROM tbl_item_card_details WHERE itemId = @id",
                conn, tx))
            {
                cmdBal.Parameters.AddWithValue("@id", itemId);
                var result = await cmdBal.ExecuteScalarAsync();
                decimal _Balance = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                balance = _Balance + (debit - credit);
            }

            var insertSql = @"
        INSERT INTO tbl_item_card_details (
            itemId, date, wharehouse_id, inv_no, trans_no, trans_type, description,
            price, qty_in, qty_out, qty_balance, debit, credit, balance, fifo_qty, fifo_cost
        ) VALUES (
            @itemId, @date, @wharehouse_id, @inv_no, @trans_no, @trans_type, @description,
            @price, @qty_in, @qty_out, @qty_balance, @debit, @credit, @balance, @fifo_qty, @fifo_cost
        );";

            using var cmdInsert = new MySqlCommand(insertSql, conn, tx);
            cmdInsert.Parameters.AddWithValue("@itemId", itemId);
            cmdInsert.Parameters.AddWithValue("@date", date);
            cmdInsert.Parameters.AddWithValue("@wharehouse_id", warehouseId);
            cmdInsert.Parameters.AddWithValue("@inv_no", invoiceNo);
            cmdInsert.Parameters.AddWithValue("@trans_no", transNo);
            cmdInsert.Parameters.AddWithValue("@trans_type", transType);
            cmdInsert.Parameters.AddWithValue("@description", description);
            cmdInsert.Parameters.AddWithValue("@price", price);
            cmdInsert.Parameters.AddWithValue("@qty_in", qtyIn);
            cmdInsert.Parameters.AddWithValue("@qty_out", qtyOut);
            cmdInsert.Parameters.AddWithValue("@qty_balance", qtyBalance);
            cmdInsert.Parameters.AddWithValue("@debit", debit);
            cmdInsert.Parameters.AddWithValue("@credit", credit);
            cmdInsert.Parameters.AddWithValue("@balance", balance);
            cmdInsert.Parameters.AddWithValue("@fifo_qty", fifoQty);
            cmdInsert.Parameters.AddWithValue("@fifo_cost", fifoCost);
            await cmdInsert.ExecuteNonQueryAsync();
        }

        private async Task AddTransactionEntryAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, string accountId, string debit, string credit, string transactionId,
            string humId, string type, string voucherName, string description, int createdBy,
            DateTime createdDate, string voucherNo)
        {
            var sql = @"
        INSERT INTO tbl_transaction 
        (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no) 
        VALUES (@date, @accountId, @debit, @credit, @transactionId, @hum_id, @tType, @type, @description, @createdBy, @createdDate, 0, @voucher_no);";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transactionId", transactionId);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@tType", voucherName);
            cmd.Parameters.AddWithValue("@hum_id", humId);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@createdBy", createdBy);
            cmd.Parameters.AddWithValue("@createdDate", createdDate);
            cmd.Parameters.AddWithValue("@voucher_no", voucherNo);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertCostCenterTransactionAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, string debit, string credit, string transactionId, string type,
            string description, string costCenterId)
        {
            var sql = @"
        INSERT INTO tbl_cost_center_transaction 
        (date, cost_center_id, debit, credit, ref_id, type, description) 
        VALUES (@date, @costCenterId, @debit, @credit, @ref_id, @type, @description);";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@costCenterId", costCenterId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@ref_id", transactionId);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@description", description);
            await cmd.ExecuteNonQueryAsync();
        }

        #endregion

        #region Sales Quotation

        public IActionResult SalesQuotation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesQuotation(
        DateTime? dateFrom,
        DateTime? dateTo,
        int? customerId,
        string paymentMethod,
        string selectionMethod = "Default")
        {
            try
            {
                // 🔹 Setup MySQL connection
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔹 Choose query based on selection method
                string query = selectionMethod == "Default" ? GetDefaultQuery() : GetDetailedQuery();

                // 🔹 Build parameters
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
                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query += " AND s.payment_method = @payment ";
                    parameters.Add(new MySqlParameter("@payment", paymentMethod));
                }

                query += " ORDER BY s.date DESC, s.id;";

                // 🔹 Prepare list to hold sales
                var sales = new List<SalesQuotationDto>();

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();

                SalesQuotationDto currentSale = null;
                int lastSaleId = -1;

                while (await reader.ReadAsync())
                {
                    int saleId = reader.GetInt32("Id");

                    if (saleId != lastSaleId)
                    {
                        currentSale = new SalesQuotationDto
                        {
                            Id = saleId,
                            Date = reader.GetDateTime("Date"),
                            InvoiceCode = reader["InvoiceNo"]?.ToString(),
                            CustomerId = reader.GetInt32("CustomerId"),
                            CustomerName = reader["CustomerName"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),
                            Total = reader.GetDecimal("Total"),
                            Vat = reader.GetDecimal("Vat"),
                            Net = reader.GetDecimal("Net"),
                            TranferStatus = reader["TranferStatus"] != DBNull.Value
                                ? Convert.ToInt32(reader["TranferStatus"])
                                : 0,
                            WarehouseId = reader.GetInt32("Warehouse_Id"),
                            PONumber = reader["PO_Num"]?.ToString(),
                            BillTo = reader["Bill_To"]?.ToString(),
                            City = reader["City"]?.ToString(),
                            SalesMan = reader["Sales_Man"]?.ToString(),
                            ShipDate = reader.GetDateTime("Ship_Date"),
                            ShipVia = reader["Ship_Via"]?.ToString(),
                            ShipTo = reader["Ship_To"]?.ToString(),
                            AccountCashId = reader.GetInt32("Account_Cash_Id"),
                            PaymentTerms = reader["Payment_Terms"]?.ToString(),
                            PaymentDate = reader.GetDateTime("Payment_Date"),
                            Description = reader["Description"]?.ToString(),
                            Items = new List<SalesQuotationItemDto>()
                        };

                        sales.Add(currentSale);
                        lastSaleId = saleId;
                    }

                    if (reader["ItemId"] != DBNull.Value)
                    {
                        currentSale.Items.Add(new SalesQuotationItemDto
                        {
                            ItemId = Convert.ToInt32(reader["ItemId"]),
                            ItemCode = reader["ItemCode"]?.ToString(),
                            ItemName = reader["ItemName"]?.ToString(),
                            Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                            Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0,
                            Sales_Price = reader["Sales_Price"] != DBNull.Value ? Convert.ToDecimal(reader["Sales_Price"]) : 0,
                            Vat = reader["ItemVat"] != DBNull.Value ? Convert.ToDecimal(reader["ItemVat"]) : 0,
                            Total = reader["ItemTotal"] != DBNull.Value ? Convert.ToDecimal(reader["ItemTotal"]) : 0,
                            CostCenterId = reader["Cost_Center_Id"] != DBNull.Value
                                ? Convert.ToInt32(reader["Cost_Center_Id"])
                                : 0,
                            VatP = reader["VatP"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["VatP"]),
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

        private string GetDefaultQuery()
        {
            return @"
SELECT 
    s.id AS Id,
    s.date AS Date,
    s.invoice_id AS InvoiceNo,
    c.id AS CustomerId,
    CONCAT(c.code,' - ',c.name) AS CustomerName,
    s.payment_method AS PaymentMethod,
    s.total AS Total,
    s.vat AS Vat,
    s.net AS Net,
    s.tranfer_status AS TranferStatus,
    i.id AS ItemId,
    i.code AS ItemCode,
    i.name AS ItemName,
    i.sales_price AS Sales_Price,
    sd.qty AS Qty,
    sd.price AS Price,
    sd.vat AS ItemVat,
     sd.vatp AS VatP,
    s.warehouse_id AS Warehouse_Id,
    s.po_num AS PO_Num,
    s.bill_to AS Bill_To,
    s.city  AS City,
    s.sales_man AS Sales_Man,
    s.ship_date AS Ship_Date,
    s.ship_via AS Ship_Via,
    s.ship_to AS Ship_To,
    s.account_cash_id AS Account_Cash_Id,  
    s.payment_terms AS Payment_Terms,
    s.payment_date AS Payment_Date,
    s.description AS Description,
    s.pay AS Pay,
    sd.cost_center_id AS Cost_Center_Id,
    sd.total AS ItemTotal
FROM tbl_sales_quotation s
INNER JOIN tbl_customer c ON s.customer_id = c.id
LEFT JOIN tbl_sales_quotation_details sd ON s.id = sd.sales_id
LEFT JOIN tbl_items i ON sd.item_id = i.id
WHERE s.state = 0
";
        }

        private string GetDetailedQuery()
        {
            return @"
SELECT 
    s.id AS Id,
    s.date AS Date,
    s.invoice_id AS InvoiceNo,
    c.id AS CustomerId,
    CONCAT(c.code,' - ',c.name) AS CustomerName,
    s.payment_method AS PaymentMethod,
    s.total AS Total,
    s.vat AS Vat,
    s.net AS Net,
    s.tranfer_status AS TranferStatus,
    i.id AS ItemId,
    i.code AS ItemCode,
    i.name AS ItemName,
    i.sales_price AS Sales_Price,
    sd.qty AS Qty,
    sd.vatp AS VatP,
    sd.price AS Price,
    sd.vat AS ItemVat,
    s.warehouse_id AS Warehouse_Id,
    s.po_num AS PO_Num,
    s.bill_to AS Bill_To,
    s.city  AS City,
    s.sales_man AS Sales_Man,
    s.ship_date AS Ship_Date,
    s.ship_via AS Ship_Via,
    s.ship_to AS Ship_To,
    s.account_cash_id AS Account_Cash_Id,  
    s.payment_terms AS Payment_Terms,
    s.payment_date AS Payment_Date,
    s.description AS Description,
    s.pay AS Pay,
    sd.cost_center_id AS Cost_Center_Id,
    sd.total AS ItemTotal
FROM tbl_sales_quotation s
INNER JOIN tbl_customer c ON s.customer_id = c.id
INNER JOIN tbl_sales_quotation_details sd ON s.id = sd.sales_id
INNER JOIN tbl_items i ON sd.item_id = i.id
WHERE s.state = 0
";
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuotation([FromBody] QuotationRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            // Basic validations
            if (model.CustomerId <= 0)
                return Json(new { status = false, message = "Customer must be selected." });

            if (model.AccountCashId <= 0)
                return Json(new { status = false, message = "Account cash must be selected." });

            if (model.Items == null || !model.Items.Any())
                return Json(new { status = false, message = "Insert at least one item." });

            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.Total <= 0)
                    return Json(new { status = false, message = $"Total Item in Row {i + 1} can't be 0 or null." });
            }

            if (model.NetTotal <= 0)
                return Json(new { status = false, message = "Total must be greater than zero." });

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

                int quotationId = model.Id;

                if (quotationId == 0) // Insert
                {

                    model.InvoiceCode = GenerateNextSalesCode(conn);
                    var insertQuery = @"INSERT INTO tbl_sales_quotation 
(date, customer_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man, ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date, total, vat, net, pay, `change`, created_by, created_date, state, description) 
VALUES (@date, @customer_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man, @ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms, @payment_date, @total, @vat, @net, @pay, @change, @created_by, @created_date, 0, @description);
SELECT LAST_INSERT_ID();";

                    using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@customer_id", model.CustomerId);
                    cmd.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
                    cmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    cmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                    cmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    cmd.Parameters.AddWithValue("@city", model.City ?? "");
                    cmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    cmd.Parameters.AddWithValue("@ship_date", model.ShipDate);
                    cmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    cmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    cmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod ?? "");
                    cmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    cmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    cmd.Parameters.AddWithValue("@payment_date", model.PaymentDate);
                    cmd.Parameters.AddWithValue("@total", model.TotalBefore);
                    cmd.Parameters.AddWithValue("@vat", model.Vat);
                    cmd.Parameters.AddWithValue("@net", model.NetTotal);
                    cmd.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@change", model.PaymentMethod != "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@created_by", userId);
                    cmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                    quotationId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                else // Update
                {
                    var updateQuery = @"UPDATE tbl_sales_quotation SET 
date=@date, customer_id=@customer_id, invoice_id=@invoice_id, warehouse_id=@warehouse_id, po_num=@po_num, bill_to=@bill_to, city=@city, sales_man=@sales_man,
ship_date=@ship_date, ship_via=@ship_via, ship_to=@ship_to, payment_method=@payment_method, account_cash_id=@account_cash_id, payment_terms=@payment_terms, payment_date=@payment_date,
total=@total, vat=@vat, net=@net, pay=@pay, `change`=@change, modified_by=@modified_by, modified_date=@modified_date, description=@description
WHERE id=@id;";

                    using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", quotationId);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@customer_id", model.CustomerId);
                    cmd.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
                    cmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    cmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                    cmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    cmd.Parameters.AddWithValue("@city", model.City ?? "");
                    cmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    cmd.Parameters.AddWithValue("@ship_date", model.ShipDate);
                    cmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    cmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    cmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod ?? "");
                    cmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    cmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    cmd.Parameters.AddWithValue("@payment_date", model.PaymentDate);
                    cmd.Parameters.AddWithValue("@total", model.TotalBefore);
                    cmd.Parameters.AddWithValue("@vat", model.Vat);
                    cmd.Parameters.AddWithValue("@net", model.NetTotal);
                    cmd.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@change", model.PaymentMethod != "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@modified_by", userId);
                    cmd.Parameters.AddWithValue("@modified_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                    await cmd.ExecuteNonQueryAsync();

                    // Delete existing items first
                    using var deleteCmd = new MySqlCommand("DELETE FROM tbl_sales_quotation_details WHERE sales_id=@id", conn);
                    deleteCmd.Parameters.AddWithValue("@id", quotationId);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                // Insert quotation items
                foreach (var item in model.Items)
                {
                    var insertItemQuery = @"INSERT INTO tbl_sales_quotation_details
(sales_id, item_id, qty, cost_price, price, vatp, vat, total, cost_center_id)
VALUES (@sales_id, @item_id, @qty, @cost_price, @price, @vatp, @vat, @total, @cost_center_id);";

                    using var itemCmd = new MySqlCommand(insertItemQuery, conn);
                    itemCmd.Parameters.AddWithValue("@sales_id", quotationId);
                    itemCmd.Parameters.AddWithValue("@item_id", item.ItemId);
                    itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@cost_price", item.CostPrice);
                    itemCmd.Parameters.AddWithValue("@price", item.Price);
                    itemCmd.Parameters.AddWithValue("@vatp", item.VatPercentage);
                    itemCmd.Parameters.AddWithValue("@vat", item.Vat);
                    itemCmd.Parameters.AddWithValue("@total", item.Total);
                    itemCmd.Parameters.AddWithValue("@cost_center_id", item.CostCenterId);
                    await itemCmd.ExecuteNonQueryAsync();
                }

                return Json(new { status = true, message = quotationId == model.Id ? "Quotation updated successfully" : "Quotation saved successfully", id = quotationId });
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }


        private string GenerateNextSalesCode(MySqlConnection conn)
        {
            const string prefix = "QU-";

            var query = @"
        SELECT IFNULL(MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)), 0)
        FROM tbl_sales_quotation
        WHERE invoice_id LIKE 'QU-%';
    ";

            using var cmd = new MySqlCommand(query, conn);
            var lastNumber = Convert.ToInt32(cmd.ExecuteScalar());

            return $"{prefix}{(lastNumber + 1):D4}";
        }

        [HttpGet]
        public async Task<IActionResult> GetDefaultAccount([FromQuery] string type)
        {
            try
            {
                int accountId = 0;

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = "SELECT category, account_id FROM tbl_coa_config WHERE category = @type LIMIT 1";
                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@type", type);

                var result = await cmd.ExecuteScalarAsync();
                if (result != null)
                    accountId = Convert.ToInt32(result);

                return Ok(new { status = true, accountId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
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

                if (method == "Cash")
                {
                    // Get default Cash account from database
                    accountId = await GetDefaultAccountId("Invoice Payment Cash Method");
                    paymentTermsEnabled = false;
                }
                else if (method == "Credit")
                {
                    // Get default Customer account from database
                    accountId = await GetDefaultAccountId("Customer");
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

        [HttpGet]
        public async Task<IActionResult> GetSalesQuotationInvoice(int salesId)
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

                // 🔹 COMPANY INFO
                var companyCmd = new MySqlCommand(@"
SELECT 
    name,
    phone1,
    gmail,
    address,
    trn_no,
    logoComp
FROM tbl_company
LIMIT 1;
", conn);

                CompanyReportDto company = null;

                await using (var reader = await companyCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        company = new CompanyReportDto
                        {
                            Name = reader.GetString("name"),
                            Phone = reader.IsDBNull("phone1") ? null : reader.GetString("phone1"),
                            Email = reader.IsDBNull("gmail") ? null : reader.GetString("gmail"),
                            Address = reader.IsDBNull("address") ? null : reader.GetString("address"),
                            TRN = reader.IsDBNull("trn_no") ? null : reader.GetString("trn_no"),
                            Logo = reader.IsDBNull("logoComp") ? null : (byte[])reader["logoComp"]
                        };
                    }
                }

                if (company == null)
                    return BadRequest("Company info not found");

                // 🔹 SALES HEADER (Quotation)
                var salesCmd = new MySqlCommand(@"
SELECT 
    s.id,
    s.date,
    s.invoice_id,
    s.bill_to,
    s.city,
    s.sales_man,
    s.ship_date,
    s.ship_via,
    s.ship_to,
    s.po_num,
    s.payment_method,
    s.payment_terms,
    s.payment_date,
    s.total,
    s.vat,
    s.net,
    s.pay,
    s.change,
    c.name AS customerName,
    c.main_phone,
    c.email,
    c.trn,
    coa.name AS accountName
FROM tbl_sales_quotation s
INNER JOIN tbl_customer c ON s.customer_id = c.id
LEFT JOIN tbl_coa_level_4 coa ON coa.id = s.account_cash_id
WHERE s.id = @salesId;
", conn);

                salesCmd.Parameters.Add("@salesId", MySqlDbType.Int32).Value = salesId;

                SaleReportDto sale = null;

                await using (var reader = await salesCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        sale = new SaleReportDto
                        {
                            Id = reader.GetInt32("id"),
                            Date = reader.GetDateTime("date"),
                            InvoiceNo = reader.GetString("invoice_id"),
                            CustomerName = reader.GetString("customerName"),
                            City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                            SalesMan = reader.IsDBNull("sales_man") ? null : reader.GetString("sales_man"),
                            ShipDate = reader.IsDBNull("ship_date") ? null : reader.GetDateTime("ship_date"),
                            PaymentMethod = reader.IsDBNull("payment_method") ? null : reader.GetString("payment_method"),
                            Total = reader.GetDecimal("total"),
                            Vat = reader.GetDecimal("vat"),
                            Net = reader.GetDecimal("net"),
                            AccountName = reader.IsDBNull("accountName") ? null : reader.GetString("accountName")
                        };
                    }
                }

                if (sale == null)
                    return NotFound("Quotation not found");

                // 🔹 ITEM DETAILS
                var itemCmd = new MySqlCommand(@"
SELECT 
    d.item_id,
    i.code,
    i.name,
    d.qty,
    d.cost_price,
    d.price,
    (d.qty * d.cost_price) AS subCostTotal,
    (d.qty * d.price) AS subPriceTotal,
    d.discount,
    ((d.qty * d.price) - d.discount) AS subTotal,
    d.vatp,
    d.vat,
    d.total,
    cc.name AS costCenterName,
    u.name AS unitName
FROM tbl_sales_quotation_details d
INNER JOIN tbl_items i ON d.item_id = i.id
LEFT JOIN tbl_sub_cost_center cc ON cc.id = d.cost_center_id
LEFT JOIN tbl_unit u ON u.id = i.unit_id
WHERE d.sales_id = @salesId;
", conn);

                itemCmd.Parameters.Add("@salesId", MySqlDbType.Int32).Value = salesId;

                var items = new List<SaleItemReportDto>();

                await using (var reader = await itemCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new SaleItemReportDto
                        {
                            ItemId = reader.GetInt32("item_id"),
                            Code = reader.GetString("code"),
                            Name = reader.GetString("name"),
                            Qty = Convert.ToDecimal(reader["qty"]),
                            Price = Convert.ToDecimal(reader["price"]),
                            Discount = Convert.ToDecimal(reader["discount"]),
                            Vat = Convert.ToDecimal(reader["vat"]),
                            Total = Convert.ToDecimal(reader["total"]),
                            CostCenterName = reader.IsDBNull("costCenterName")
                                ? null
                                : reader.GetString("costCenterName"),
                            UnitName = reader.IsDBNull("unitName")
                                ? null
                                : reader.GetString("unitName")
                        });
                    }
                }

                // 🔹 PDF GENERATION (Crystal replacement)
                byte[] pdfBytes = SalesQuotationPdfGenerator.Generate(company, sale, items);

                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }


        #endregion

        #region Sales Order Center

        public IActionResult SalesOrder()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesOrders(
    DateTime? dateFrom,
    DateTime? dateTo,
    int? customerId,
    string paymentMethod,
    string selectionMethod = "Default")
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

                string query = selectionMethod == "Default" ? GetDefaultQuerys() : GetDetailedQuerys();
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
                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query += " AND s.payment_method = @payment ";
                    parameters.Add(new MySqlParameter("@payment", paymentMethod));
                }

                query += " ORDER BY s.date DESC, s.id;";

                var salesOrders = new List<SalesQuotationDto>();
                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();
                SalesQuotationDto currentOrder = null;
                int lastOrderId = -1;

                while (await reader.ReadAsync())
                {
                    int orderId = reader.GetInt32("Id");

                    if (orderId != lastOrderId)
                    {
                        currentOrder = new SalesQuotationDto
                        {
                            Id = orderId,
                            Date = reader.GetDateTime("Date"),
                            InvoiceCode = reader["InvoiceNo"]?.ToString(),
                            CustomerId = reader.GetInt32("CustomerId"),
                            CustomerName = reader["CustomerName"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),
                            Total = reader.GetDecimal("Total"),
                            Vat = reader.GetDecimal("Vat"),
                            Net = reader.GetDecimal("Net"),
                            TranferStatus = reader["TranferStatus"] != DBNull.Value
                                ? Convert.ToInt32(reader["TranferStatus"])
                                : 0,
                            WarehouseId = reader.GetInt32("Warehouse_Id"),
                            PONumber = reader["PO_Num"]?.ToString(),
                            BillTo = reader["Bill_To"]?.ToString(),
                            City = reader["City"]?.ToString(),
                            SalesMan = reader["Sales_Man"]?.ToString(),
                            ShipDate = reader.GetDateTime("Ship_Date"),
                            ShipVia = reader["Ship_Via"]?.ToString(),
                            ShipTo = reader["Ship_To"]?.ToString(),
                            AccountCashId = reader.GetInt32("Account_Cash_Id"),
                            PaymentTerms = reader["Payment_Terms"]?.ToString(),
                            PaymentDate = reader.GetDateTime("Payment_Date"),
                            Description = reader["Description"]?.ToString(),
                            Items = new List<SalesQuotationItemDto>()
                        };
                        salesOrders.Add(currentOrder);
                        lastOrderId = orderId;
                    }

                    if (reader["ItemId"] != DBNull.Value)
                    {
                        currentOrder.Items.Add(new SalesQuotationItemDto
                        {
                            ItemId = Convert.ToInt32(reader["ItemId"]),
                            ItemCode = reader["ItemCode"]?.ToString(),
                            ItemName = reader["ItemName"]?.ToString(),
                            Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                            Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0,
                            Sales_Price = reader["Sales_Price"] != DBNull.Value ? Convert.ToDecimal(reader["Sales_Price"]) : 0,
                            Vat = reader["ItemVat"] != DBNull.Value ? Convert.ToDecimal(reader["ItemVat"]) : 0,
                            VatP = reader["VatP"] != DBNull.Value ? Convert.ToDecimal(reader["VatP"]) : 0,
                            Total = reader["ItemTotal"] != DBNull.Value ? Convert.ToDecimal(reader["ItemTotal"]) : 0,
                            CostCenterId = reader["Cost_Center_Id"] != DBNull.Value
                                ? Convert.ToInt32(reader["Cost_Center_Id"])
                                : 0
                        });
                    }
                }
         
                return Ok(new { status = true, data = salesOrders });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private string GetDefaultQuerys()
        {
            return @"
SELECT 
    s.id AS Id,
    s.date AS Date,
    s.invoice_id AS InvoiceNo,
    c.id AS CustomerId,
    CONCAT(c.code,' - ',c.name) AS CustomerName,
    s.payment_method AS PaymentMethod,
    s.total AS Total,
    s.vat AS Vat,
    s.net AS Net,
    s.tranfer_status AS TranferStatus,
    i.id AS ItemId,
    i.code AS ItemCode,
    i.name AS ItemName,
    i.sales_price AS Sales_Price,
    sd.qty AS Qty,
    sd.price AS Price,
    sd.vatp AS VatP,
    sd.vat AS ItemVat,
    s.warehouse_id AS Warehouse_Id,
    s.po_num AS PO_Num,
    s.bill_to AS Bill_To,
    s.city  AS City,
    s.sales_man AS Sales_Man,
    s.ship_date AS Ship_Date,
    s.ship_via AS Ship_Via,
    s.ship_to AS Ship_To,
    s.account_cash_id AS Account_Cash_Id,  
    s.payment_terms AS Payment_Terms,
    s.payment_date AS Payment_Date,
    s.description AS Description,
    s.pay AS Pay,
    sd.cost_center_id AS Cost_Center_Id,
    sd.total AS ItemTotal
FROM tbl_sales_order s
INNER JOIN tbl_customer c ON s.customer_id = c.id
LEFT JOIN tbl_sales_order_details sd ON s.id = sd.sales_id
LEFT JOIN tbl_items i ON sd.item_id = i.id
WHERE s.state = 0";
        }

        private string GetDetailedQuerys()
        {
            return @"
SELECT 
    s.id AS Id,
    s.date AS Date,
    s.invoice_id AS InvoiceNo,
    c.id AS CustomerId,
    CONCAT(c.code,' - ',c.name) AS CustomerName,
    s.payment_method AS PaymentMethod,
    s.total AS Total,
    s.vat AS Vat,
    s.net AS Net,
    s.tranfer_status AS TranferStatus,
    i.id AS ItemId,
    i.code AS ItemCode,
    i.name AS ItemName,
    i.sales_price AS Sales_Price,
    sd.qty AS Qty,
    sd.price AS Price,
    sd.vatp AS VatP,
    sd.vat AS ItemVat,
    s.warehouse_id AS Warehouse_Id,
    s.po_num AS PO_Num,
    s.bill_to AS Bill_To,
    s.city  AS City,
    s.sales_man AS Sales_Man,
    s.ship_date AS Ship_Date,
    s.ship_via AS Ship_Via,
    s.ship_to AS Ship_To,
    s.account_cash_id AS Account_Cash_Id,  
    s.payment_terms AS Payment_Terms,
    s.payment_date AS Payment_Date,
    s.description AS Description,
    s.pay AS Pay,
    sd.cost_center_id AS Cost_Center_Id,
    sd.total AS ItemTotal
FROM tbl_sales_order s
INNER JOIN tbl_customer c ON s.customer_id = c.id
INNER JOIN tbl_sales_order_details sd ON s.id = sd.sales_id
INNER JOIN tbl_items i ON sd.item_id = i.id
WHERE s.state = 0";
        }


        [HttpPost]
        public async Task<IActionResult> SaveSalesOrder([FromBody] QuotationRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            // 🔹 Validations
            if (model.CustomerId <= 0)
                return Json(new { status = false, message = "Customer must be selected." });

            if (model.AccountCashId <= 0)
                return Json(new { status = false, message = "Account cash must be selected." });

            if (model.Items == null || !model.Items.Any())
                return Json(new { status = false, message = "Insert at least one item." });

            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.Total <= 0)
                    return Json(new { status = false, message = $"Total Item in Row {i + 1} can't be 0 or null." });
            }

            if (model.NetTotal <= 0)
                return Json(new { status = false, message = "Total must be greater than zero." });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in." });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            try
            {
                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                int salesOrderId = model.Id;

                if (salesOrderId == 0) // 🔹 Insert new
                {
                    model.InvoiceCode = await GenerateNextSalesOrderCode(conn);

                    var insertQuery = @"
INSERT INTO tbl_sales_order
(date, customer_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man,
ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date,
total, vat, net, pay, `change`, created_by, created_date, state, description)
VALUES
(@date, @customer_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man,
@ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms, @payment_date,
@total, @vat, @net, @pay, @change, @created_by, @created_date, 0, @description);
SELECT LAST_INSERT_ID();";

                    await using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@customer_id", model.CustomerId);
                    cmd.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
                    cmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    cmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                    cmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    cmd.Parameters.AddWithValue("@city", model.City ?? "");
                    cmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    cmd.Parameters.AddWithValue("@ship_date", model.ShipDate);
                    cmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    cmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    cmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod ?? "");
                    cmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    cmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    cmd.Parameters.AddWithValue("@payment_date", model.PaymentDate);
                    cmd.Parameters.AddWithValue("@total", model.TotalBefore);
                    cmd.Parameters.AddWithValue("@vat", model.Vat);
                    cmd.Parameters.AddWithValue("@net", model.NetTotal);
                    cmd.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@change", model.PaymentMethod != "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@created_by", userId);
                    cmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                    salesOrderId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                else // 🔹 Update existing
                {
                    var updateQuery = @"
UPDATE tbl_sales_order SET
date=@date, customer_id=@customer_id, invoice_id=@invoice_id, warehouse_id=@warehouse_id, po_num=@po_num, bill_to=@bill_to, city=@city, sales_man=@sales_man,
ship_date=@ship_date, ship_via=@ship_via, ship_to=@ship_to, payment_method=@payment_method, account_cash_id=@account_cash_id,
payment_terms=@payment_terms, payment_date=@payment_date, total=@total, vat=@vat, net=@net,
pay=@pay, `change`=@change, modified_by=@modified_by, modified_date=@modified_date, description=@description
WHERE id=@id;";

                    await using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", salesOrderId);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@customer_id", model.CustomerId);
                    cmd.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
                    cmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    cmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                    cmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    cmd.Parameters.AddWithValue("@city", model.City ?? "");
                    cmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    cmd.Parameters.AddWithValue("@ship_date", model.ShipDate);
                    cmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    cmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    cmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod ?? "");
                    cmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    cmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    cmd.Parameters.AddWithValue("@payment_date", model.PaymentDate);
                    cmd.Parameters.AddWithValue("@total", model.TotalBefore);
                    cmd.Parameters.AddWithValue("@vat", model.Vat);
                    cmd.Parameters.AddWithValue("@net", model.NetTotal);
                    cmd.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@change", model.PaymentMethod != "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@modified_by", userId);
                    cmd.Parameters.AddWithValue("@modified_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                    await cmd.ExecuteNonQueryAsync();

                    // Delete previous items
                    await using var deleteCmd = new MySqlCommand("DELETE FROM tbl_sales_order_details WHERE sales_id=@id", conn);
                    deleteCmd.Parameters.AddWithValue("@id", salesOrderId);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                // 🔹 Insert items
                foreach (var item in model.Items)
                {
                    var insertItemQuery = @"
INSERT INTO tbl_sales_order_details
(sales_id, item_id, qty, cost_price, price, vatp, vat, total, cost_center_id)
VALUES
(@sales_id, @item_id, @qty, @cost_price, @price, @vatp, @vat, @total, @cost_center_id);";

                    await using var itemCmd = new MySqlCommand(insertItemQuery, conn);
                    itemCmd.Parameters.AddWithValue("@sales_id", salesOrderId);
                    itemCmd.Parameters.AddWithValue("@item_id", item.ItemId);
                    itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@cost_price", item.CostPrice);
                    itemCmd.Parameters.AddWithValue("@price", item.Price);
                    itemCmd.Parameters.AddWithValue("@vatp", item.VatPercentage);
                    itemCmd.Parameters.AddWithValue("@vat", item.Vat);
                    itemCmd.Parameters.AddWithValue("@total", item.Total);
                    itemCmd.Parameters.AddWithValue("@cost_center_id", item.CostCenterId);
                    await itemCmd.ExecuteNonQueryAsync();
                }

                return Json(new { status = true, message = salesOrderId == model.Id ? "Sales Order updated successfully" : "Sales Order saved successfully", id = salesOrderId });
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateNextSalesOrderCode(MySqlConnection conn)
        {
            const string prefix = "SO-";

            var query = @"
SELECT IFNULL(MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)), 0)
FROM tbl_sales_order
WHERE invoice_id LIKE 'SO-%';";

            await using var cmd = new MySqlCommand(query, conn);
            var lastNumber = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return $"{prefix}{(lastNumber + 1):D4}";
        }


        [HttpGet]
        public async Task<IActionResult> GetSalesOrderInvoice(int salesId)
        {
            try
            {
                // Build connection string (supports dynamic database selection)
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ---------------- COMPANY INFO ----------------
                var companyCmd = new MySqlCommand(@"
SELECT 
    name,
    phone1,
    gmail,
    address,
    trn_no,
    logoComp
FROM tbl_company
WHERE id = @id;
", conn);
                companyCmd.Parameters.Add("@id", MySqlDbType.Int32).Value = 1;

                CompanyReportDto company = null;
                await using (var reader = await companyCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        company = new CompanyReportDto
                        {
                            Name = reader.GetString("name"),
                            Phone = reader.IsDBNull("phone1") ? null : reader.GetString("phone1"),
                            Email = reader.IsDBNull("gmail") ? null : reader.GetString("gmail"),
                            Address = reader.IsDBNull("address") ? null : reader.GetString("address"),
                            TRN = reader.IsDBNull("trn_no") ? null : reader.GetString("trn_no"),
                            Logo = reader.IsDBNull("logoComp") ? null : (byte[])reader["logoComp"]
                        };
                    }
                }

                if (company == null)
                    return BadRequest("Company info not found");

                // ---------------- SALES HEADER ----------------
                var salesCmd = new MySqlCommand(@"
SELECT 
    s.id,
    s.date,
    s.invoice_id,
    s.bill_to,
    s.city,
    s.sales_man,
    s.ship_date,
    s.ship_via,
    s.ship_to,
    s.po_num,
    s.payment_method,
    s.payment_terms,
    s.payment_date,
    s.total,
    s.vat,
    s.net,
    s.pay,
    s.change,
    c.name AS customerName,
    c.main_phone,
    c.email,
    c.trn,
    (SELECT name FROM tbl_coa_level_4 WHERE id=s.account_cash_id) AS accountName
FROM tbl_sales_order s
INNER JOIN tbl_customer c ON s.customer_id = c.id
WHERE s.id = @salesId;
", conn);
                salesCmd.Parameters.Add("@salesId", MySqlDbType.Int32).Value = salesId;

                SaleReportDto sale = null;
                await using (var reader = await salesCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        sale = new SaleReportDto
                        {
                            Id = reader.GetInt32("id"),
                            Date = reader.GetDateTime("date"),
                            InvoiceNo = reader.GetString("invoice_id"),
                            CustomerName = reader.GetString("customerName"),
                            City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                            SalesMan = reader.IsDBNull("sales_man") ? null : reader.GetString("sales_man"),
                            ShipDate = reader.IsDBNull("ship_date") ? null : reader.GetDateTime("ship_date"),
                            PaymentMethod = reader.IsDBNull("payment_method") ? null : reader.GetString("payment_method"),
                            Total = reader.GetDecimal("total"),
                            Vat = reader.GetDecimal("vat"),
                            Net = reader.GetDecimal("net"),
                            AccountName = reader.IsDBNull("accountName") ? null : reader.GetString("accountName")
                        };
                    }
                }

                if (sale == null)
                    return NotFound("Sales order not found");

                // ---------------- ITEM DETAILS ----------------
                var itemCmd = new MySqlCommand(@"
SELECT 
    d.id,
    d.item_id,
    i.code,
    i.name,
    d.qty,
    d.cost_price,
    d.price,
    (d.qty*d.cost_price) AS subCostTotal,
    (d.qty*d.price) AS subPriceTotal,
    d.discount,
    ((d.qty*d.price)-d.discount) AS subTotal,
    d.vatp AS vatAmount,
    d.vat AS vatPercentage,
    d.total,
    (SELECT name FROM tbl_sub_cost_center WHERE id=d.cost_center_id) AS costCenterName,
    (SELECT name FROM tbl_unit WHERE id=i.unit_id) AS unitName
FROM tbl_sales_order_details d
INNER JOIN tbl_items i ON d.item_id = i.id
WHERE d.sales_id = @salesId;
", conn);
                itemCmd.Parameters.Add("@salesId", MySqlDbType.Int32).Value = salesId;

                var items = new List<SaleItemReportDto>();
                await using (var reader = await itemCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new SaleItemReportDto
                        {
                            ItemId = reader.GetInt32("item_id"),
                            Code = reader.GetString("code"),
                            Name = reader.GetString("name"),
                            Qty = Convert.ToDecimal(reader["qty"]),
                            Price = Convert.ToDecimal(reader["price"]),
                            Discount = Convert.ToDecimal(reader["discount"]),
                            Vat = Convert.ToDecimal(reader["vatPercentage"]),
                            Total = Convert.ToDecimal(reader["total"]),
                            CostCenterName = reader.IsDBNull("costCenterName") ? null : reader.GetString("costCenterName"),
                            UnitName = reader.IsDBNull("unitName") ? null : reader.GetString("unitName")
                        });
                    }
                }

                // ---------------- PDF GENERATION ----------------
                byte[] pdfBytes = SalesOrderPdfGenerator.Generate(company, sale, items);

                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

        #region Sales Performa Center

        public IActionResult SalesPerforma()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetSalesProformas(
    DateTime? dateFrom,
    DateTime? dateTo,
    int? customerId,
    string paymentMethod,
    string selectionMethod = "Default")
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

                string query = selectionMethod == "Default" ? GetDefaultQueryss() : GetDetailedQueryss();
                var parameters = new List<MySqlParameter>();

                // Filters
                if (dateFrom.HasValue)
                {
                    query += " AND sp.date >= @dateFrom ";
                    parameters.Add(new MySqlParameter("@dateFrom", dateFrom.Value.Date));
                }

                if (dateTo.HasValue)
                {
                    query += " AND sp.date <= @dateTo ";
                    parameters.Add(new MySqlParameter("@dateTo", dateTo.Value.Date));
                }

                if (customerId.HasValue)
                {
                    query += " AND sp.customer_id = @customerId ";
                    parameters.Add(new MySqlParameter("@customerId", customerId.Value));
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query += " AND sp.payment_method = @payment ";
                    parameters.Add(new MySqlParameter("@payment", paymentMethod));
                }

                query += " ORDER BY sp.date DESC, sp.id;";

                var proformas = new List<SalesQuotationDto>();
                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();
                SalesQuotationDto currentProforma = null;
                int lastId = -1;

                while (await reader.ReadAsync())
                {
                    int proformaId = reader.GetInt32("Id");

                    // New proforma
                    if (proformaId != lastId)
                    {
                        currentProforma = new SalesQuotationDto
                        {
                            Id = proformaId,
                            Date = reader.GetDateTime("Date"),
                            InvoiceCode = reader["InvoiceNo"]?.ToString(),
                            CustomerId = reader.GetInt32("CustomerId"),
                            CustomerName = reader["CustomerName"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),
                            Total = reader.GetDecimal("Total"),
                            Vat = reader.GetDecimal("Vat"),
                            Net = reader.GetDecimal("Net"),
                            TranferStatus = reader["TranferStatus"] != DBNull.Value ? Convert.ToInt32(reader["TranferStatus"]) : 0,
                            WarehouseId = reader.GetInt32("Warehouse_Id"),
                            PONumber = reader["PO_Num"]?.ToString(),
                            BillTo = reader["Bill_To"]?.ToString(),
                            City = reader["City"]?.ToString(),
                            SalesMan = reader["Sales_Man"]?.ToString(),
                            ShipDate = reader.GetDateTime("Ship_Date"),
                            ShipVia = reader["Ship_Via"]?.ToString(),
                            ShipTo = reader["Ship_To"]?.ToString(),
                            AccountCashId = reader.GetInt32("Account_Cash_Id"),
                            PaymentTerms = reader["Payment_Terms"]?.ToString(),
                            PaymentDate = reader.GetDateTime("Payment_Date"),
                            Description = reader["Description"]?.ToString(),
                            Items = new List<SalesQuotationItemDto>()
                        };
                        proformas.Add(currentProforma);
                        lastId = proformaId;
                    }

                    // Add item if exists
                    if (reader["ItemId"] != DBNull.Value)
                    {
                        currentProforma.Items.Add(new SalesQuotationItemDto
                        {
                            ItemId = Convert.ToInt32(reader["ItemId"]),
                            ItemCode = reader["ItemCode"]?.ToString(),
                            ItemName = reader["ItemName"]?.ToString(),
                            Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                            Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0,
                            Sales_Price = reader["Sales_Price"] != DBNull.Value ? Convert.ToDecimal(reader["Sales_Price"]) : 0,
                            Vat = reader["ItemVat"] != DBNull.Value ? Convert.ToDecimal(reader["ItemVat"]) : 0,
                            VatP = reader["VatP"] != DBNull.Value ? Convert.ToDecimal(reader["VatP"]) : 0,
                            Total = reader["ItemTotal"] != DBNull.Value ? Convert.ToDecimal(reader["ItemTotal"]) : 0,
                            CostCenterId = reader["Cost_Center_Id"] != DBNull.Value ? Convert.ToInt32(reader["Cost_Center_Id"]) : 0
                        });
                    }
                }

                return Ok(new { status = true, data = proformas });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private string GetDefaultQueryss()
        {
            return @"
SELECT 
    sp.id AS Id,
    sp.date AS Date,
    sp.invoice_id AS InvoiceNo,
    c.id AS CustomerId,
    CONCAT(c.code,' - ',c.name) AS CustomerName,
    sp.payment_method AS PaymentMethod,
    sp.total AS Total,
    sp.vat AS Vat,
    sp.net AS Net,
    sp.tranfer_status AS TranferStatus,
    i.id AS ItemId,
    i.code AS ItemCode,
    i.name AS ItemName,
    i.sales_price AS Sales_Price,
    sd.qty AS Qty,
    sd.price AS Price,
    sd.vat AS ItemVat,
    sd.vatp AS VatP,
    sp.warehouse_id AS Warehouse_Id,
    sp.po_num AS PO_Num,
    sp.bill_to AS Bill_To,
    sp.city  AS City,
    sp.sales_man AS Sales_Man,
    sp.ship_date AS Ship_Date,
    sp.ship_via AS Ship_Via,
    sp.ship_to AS Ship_To,
    sp.account_cash_id AS Account_Cash_Id,  
    sp.payment_terms AS Payment_Terms,
    sp.payment_date AS Payment_Date,
    sp.description AS Description,
    sp.pay AS Pay,
    sd.cost_center_id AS Cost_Center_Id,
    sd.total AS ItemTotal
FROM tbl_sales_proforma sp
INNER JOIN tbl_customer c ON sp.customer_id = c.id
LEFT JOIN tbl_sales_proforma_details sd ON sp.id = sd.sales_id
LEFT JOIN tbl_items i ON sd.item_id = i.id
WHERE sp.state = 0";
        }

        private string GetDetailedQueryss()
        {
            return @"
SELECT 
    sp.id AS Id,
    sp.date AS Date,
    sp.invoice_id AS InvoiceNo,
    c.id AS CustomerId,
    CONCAT(c.code,' - ',c.name) AS CustomerName,
    sp.payment_method AS PaymentMethod,
    sp.total AS Total,
    sp.vat AS Vat,
    sp.net AS Net,
    sp.tranfer_status AS TranferStatus,
    i.id AS ItemId,
    i.code AS ItemCode,
    i.name AS ItemName,
     i.sales_price AS Sales_Price,
    sd.qty AS Qty,
    sd.price AS Price,
    sd.vat AS ItemVat,
    sd.vatp AS VatP,
    sp.warehouse_id AS Warehouse_Id,
    sp.po_num AS PO_Num,
    sp.bill_to AS Bill_To,
    sp.city  AS City,
    sp.sales_man AS Sales_Man,
    sp.ship_date AS Ship_Date,
    sp.ship_via AS Ship_Via,
    sp.ship_to AS Ship_To,
    sp.account_cash_id AS Account_Cash_Id,  
    sp.payment_terms AS Payment_Terms,
    sp.payment_date AS Payment_Date,
    sp.description AS Description,
    sp.pay AS Pay,
    sd.cost_center_id AS Cost_Center_Id,
    sd.total AS ItemTotal
FROM tbl_sales_proforma sp
INNER JOIN tbl_customer c ON sp.customer_id = c.id
INNER JOIN tbl_sales_proforma_details sd ON sp.id = sd.sales_id
INNER JOIN tbl_items i ON sd.item_id = i.id
WHERE sp.state = 0";
        }

        [HttpPost]
        public async Task<IActionResult> SaveProformaInvoice([FromBody] QuotationRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            // 🔹 Validations
            if (model.CustomerId <= 0)
                return Json(new { status = false, message = "Customer must be selected." });

            if (model.AccountCashId <= 0)
                return Json(new { status = false, message = "Account cash must be selected." });

            if (model.Items == null || !model.Items.Any())
                return Json(new { status = false, message = "Insert at least one item." });

            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.Total <= 0)
                    return Json(new { status = false, message = $"Total Item in Row {i + 1} can't be 0 or null." });
            }

            if (model.NetTotal <= 0)
                return Json(new { status = false, message = "Total must be greater than zero." });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in." });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            try
            {
                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                int proformaId = model.Id;

                if (proformaId == 0) // 🔹 Insert new
                {
                    model.InvoiceCode = await GenerateNextProformaCode(conn);

                    var insertQuery = @"
INSERT INTO tbl_sales_proforma
(date, customer_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man,
ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date,
total, vat, net, pay, `change`, created_by, created_date, state, description)
VALUES
(@date, @customer_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man,
@ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms, @payment_date,
@total, @vat, @net, @pay, @change, @created_by, @created_date, 0, @description);
SELECT LAST_INSERT_ID();";

                    await using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@customer_id", model.CustomerId);
                    cmd.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
                    cmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    cmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                    cmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    cmd.Parameters.AddWithValue("@city", model.City ?? "");
                    cmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    cmd.Parameters.AddWithValue("@ship_date", model.ShipDate);
                    cmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    cmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    cmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod ?? "");
                    cmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    cmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    cmd.Parameters.AddWithValue("@payment_date", model.PaymentDate);
                    cmd.Parameters.AddWithValue("@total", model.TotalBefore);
                    cmd.Parameters.AddWithValue("@vat", model.Vat);
                    cmd.Parameters.AddWithValue("@net", model.NetTotal);
                    cmd.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@change", model.PaymentMethod != "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@created_by", userId);
                    cmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                    proformaId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                else // 🔹 Update existing
                {
                    var updateQuery = @"
UPDATE tbl_sales_proforma SET
date=@date, customer_id=@customer_id, invoice_id=@invoice_id, warehouse_id=@warehouse_id, po_num=@po_num, bill_to=@bill_to, city=@city, sales_man=@sales_man,
ship_date=@ship_date, ship_via=@ship_via, ship_to=@ship_to, payment_method=@payment_method, account_cash_id=@account_cash_id,
payment_terms=@payment_terms, payment_date=@payment_date, total=@total, vat=@vat, net=@net,
pay=@pay, `change`=@change, modified_by=@modified_by, modified_date=@modified_date, description=@description
WHERE id=@id;";

                    await using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", proformaId);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@customer_id", model.CustomerId);
                    cmd.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
                    cmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    cmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                    cmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    cmd.Parameters.AddWithValue("@city", model.City ?? "");
                    cmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    cmd.Parameters.AddWithValue("@ship_date", model.ShipDate);
                    cmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    cmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    cmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod ?? "");
                    cmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    cmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    cmd.Parameters.AddWithValue("@payment_date", model.PaymentDate);
                    cmd.Parameters.AddWithValue("@total", model.TotalBefore);
                    cmd.Parameters.AddWithValue("@vat", model.Vat);
                    cmd.Parameters.AddWithValue("@net", model.NetTotal);
                    cmd.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@change", model.PaymentMethod != "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@modified_by", userId);
                    cmd.Parameters.AddWithValue("@modified_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                    await cmd.ExecuteNonQueryAsync();

                    // Delete previous items
                    await using var deleteCmd = new MySqlCommand("DELETE FROM tbl_sales_proforma_details WHERE sales_id=@id", conn);
                    deleteCmd.Parameters.AddWithValue("@id", proformaId);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                // 🔹 Insert items
                foreach (var item in model.Items)
                {
                    var insertItemQuery = @"
INSERT INTO tbl_sales_proforma_details
(sales_id, item_id, qty, cost_price, price, vatp, vat, total, cost_center_id)
VALUES
(@sales_id, @item_id, @qty, @cost_price, @price, @vatp, @vat, @total, @cost_center_id);";

                    await using var itemCmd = new MySqlCommand(insertItemQuery, conn);
                    itemCmd.Parameters.AddWithValue("@sales_id", proformaId);
                    itemCmd.Parameters.AddWithValue("@item_id", item.ItemId);
                    itemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@cost_price", item.CostPrice);
                    itemCmd.Parameters.AddWithValue("@price", item.Price);
                    itemCmd.Parameters.AddWithValue("@vatp", item.VatPercentage);
                    itemCmd.Parameters.AddWithValue("@vat", item.Vat);
                    itemCmd.Parameters.AddWithValue("@total", item.Total);
                    itemCmd.Parameters.AddWithValue("@cost_center_id", item.CostCenterId);
                    await itemCmd.ExecuteNonQueryAsync();
                }

                return Json(new { status = true, message = proformaId == model.Id ? "Proforma Invoice updated successfully" : "Proforma Invoice saved successfully", id = proformaId });
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }

        // 🔹 Helper to generate next invoice code
        private async Task<string> GenerateNextProformaCode(MySqlConnection conn)
        {
            const string prefix = "SP-";
            var query = @"SELECT IFNULL(MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)), 0) FROM tbl_sales_proforma WHERE invoice_id LIKE 'SP-%';";
            await using var cmd = new MySqlCommand(query, conn);
            var lastNumber = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return $"{prefix}{(lastNumber + 1):D4}";
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesProformaInvoice(int salesId)
        {
            try
            {
                // Build connection string (supports dynamic database selection)
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ---------------- COMPANY INFO ----------------
                var companyCmd = new MySqlCommand(@"
SELECT 
    name,
    phone1,
    gmail,
    address,
    trn_no,
    logoComp
FROM tbl_company
LIMIT 1;
", conn);

                CompanyReportDto company = null;
                await using (var reader = await companyCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        company = new CompanyReportDto
                        {
                            Name = reader.GetString("name"),
                            Phone = reader.IsDBNull("phone1") ? null : reader.GetString("phone1"),
                            Email = reader.IsDBNull("gmail") ? null : reader.GetString("gmail"),
                            Address = reader.IsDBNull("address") ? null : reader.GetString("address"),
                            TRN = reader.IsDBNull("trn_no") ? null : reader.GetString("trn_no"),
                            Logo = reader.IsDBNull("logoComp") ? null : (byte[])reader["logoComp"]
                        };
                    }
                }

                if (company == null)
                    return BadRequest("Company info not found");

                // ---------------- SALES HEADER ----------------
                var salesCmd = new MySqlCommand(@"
SELECT 
    s.id,
    s.date,
    s.invoice_id,
    s.bill_to,
    s.city,
    s.sales_man,
    s.ship_date,
    s.ship_via,
    s.ship_to,
    s.po_num,
    s.payment_method,
    s.payment_terms,
    s.payment_date,
    s.total,
    s.vat,
    s.net,
    s.pay,
    s.change,
    c.name AS customerName,
    c.main_phone,
    c.email,
    c.trn,
    (SELECT name FROM tbl_coa_level_4 WHERE id=s.account_cash_id) AS accountName
FROM tbl_sales_proforma s
INNER JOIN tbl_customer c ON s.customer_id = c.id
WHERE s.id = @salesId;
", conn);

                salesCmd.Parameters.Add("@salesId", MySqlDbType.Int32).Value = salesId;

                SaleReportDto sale = null;
                await using (var reader = await salesCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        sale = new SaleReportDto
                        {
                            Id = reader.GetInt32("id"),
                            Date = reader.GetDateTime("date"),
                            InvoiceNo = reader.GetString("invoice_id"),
                            CustomerName = reader.GetString("customerName"),
                            City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                            SalesMan = reader.IsDBNull("sales_man") ? null : reader.GetString("sales_man"),
                            ShipDate = reader.IsDBNull("ship_date") ? null : reader.GetDateTime("ship_date"),
                            PaymentMethod = reader.IsDBNull("payment_method") ? null : reader.GetString("payment_method"),
                            Total = reader.GetDecimal("total"),
                            Vat = reader.GetDecimal("vat"),
                            Net = reader.GetDecimal("net"),
                            AccountName = reader.IsDBNull("accountName") ? null : reader.GetString("accountName")
                        };
                    }
                }

                if (sale == null)
                    return NotFound("Sales proforma not found");

                // ---------------- ITEM DETAILS ----------------
                var itemCmd = new MySqlCommand(@"
SELECT 
    d.id,
    d.item_id,
    i.code,
    i.name,
    d.qty,
    d.cost_price,
    d.price,
    (d.qty*d.cost_price) AS subCostTotal,
    (d.qty*d.price) AS subPriceTotal,
    d.discount,
    ((d.qty*d.price)-d.discount) AS subTotal,
    d.vatp AS vatAmount,
    d.vat AS vatPercentage,
    d.total,
    (SELECT name FROM tbl_sub_cost_center WHERE id=d.cost_center_id) AS costCenterName,
    (SELECT name FROM tbl_unit WHERE id=i.unit_id) AS unitName
FROM tbl_sales_proforma_details d
INNER JOIN tbl_items i ON d.item_id = i.id
WHERE d.sales_id = @salesId;
", conn);

                itemCmd.Parameters.Add("@salesId", MySqlDbType.Int32).Value = salesId;

                var items = new List<SaleItemReportDto>();
                await using (var reader = await itemCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new SaleItemReportDto
                        {
                            ItemId = reader.GetInt32("item_id"),
                            Code = reader.GetString("code"),
                            Name = reader.GetString("name"),
                            Qty = Convert.ToDecimal(reader["qty"]),
                            Price = Convert.ToDecimal(reader["price"]),
                            Discount = Convert.ToDecimal(reader["discount"]),
                            Vat = Convert.ToDecimal(reader["vatPercentage"]),
                            Total = Convert.ToDecimal(reader["total"]),
                            CostCenterName = reader.IsDBNull("costCenterName") ? null : reader.GetString("costCenterName"),
                            UnitName = reader.IsDBNull("unitName") ? null : reader.GetString("unitName")
                        });
                    }
                }

                // ---------------- PDF GENERATION ----------------
                byte[] pdfBytes = SalesProformaPdfGenerator.Generate(company, sale, items);

                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

        #region Sales Return

        public IActionResult SalesReturn()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetSalesReturnReport(
    DateTime? dateFrom,
    DateTime? dateTo,
    int? customerId,
    string paymentMethod = null)
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

                // 🔹 Base query
                string query = @"
SELECT
    sr.id AS Id,
    sr.date AS Date,
    sr.invoice_id AS InvoiceNo,
    c.id AS CustomerId,
    CONCAT(c.code, '-', c.name) AS CustomerName,
    sr.payment_method AS PaymentMethod,
    sr.total AS Total,
    sr.warehouse_id AS Warehouse_Id,
    sr.po_num AS PO_Num,
    sr.bill_to AS Bill_To,
    sr.city AS City,
    sr.sales_man AS Sales_Man,
    sr.ship_date AS Ship_Date,
    sr.ship_via AS Ship_Via,
    sr.ship_to AS Ship_To,
    sr.account_cash_id AS Account_Cash_Id,  
    sr.payment_terms AS Payment_Terms,
    sr.payment_date AS Payment_Date,
    sr.description AS Description,
    sr.pay AS Pay,
    sr.vat AS Vat,
    sr.net AS Net,
    CONCAT('000', MAX(t.transaction_id)) AS JvNo,
    i.id AS ItemId,
    i.code AS ItemCode,
    i.name AS ItemName,
    i.sales_price AS Sales_Price,
    sd.qty AS Qty,
    sd.price AS Price,
    sd.vat AS ItemVat,
    sd.vatp AS VatP,
    sd.total AS ItemTotal,
    sd.cost_center_id AS Cost_Center_Id
FROM tbl_sales_return sr
INNER JOIN tbl_transaction t 
    ON sr.id = t.transaction_id
INNER JOIN tbl_customer c 
    ON sr.customer_id = c.id
LEFT JOIN tbl_sales_return_details sd 
    ON sr.id = sd.sales_id
LEFT JOIN tbl_items i 
    ON sd.item_id = i.id
WHERE sr.state = 0
GROUP BY 
    sr.id,
    sr.date,
    sr.invoice_id,
    c.id,
    c.code,
    c.name,
    sr.payment_method,
    sr.total,
    sr.warehouse_id,
    sr.po_num,
    sr.bill_to,
    sr.city,
    sr.sales_man,
    sr.ship_date,
    sr.ship_via,
    sr.ship_to,
    sr.account_cash_id,
    sr.payment_terms,
    sr.payment_date,
    sr.description,
    sr.pay,
    sr.vat,
    sr.net,
    i.id,
    i.code,
    i.name,
    i.sales_price,
    sd.qty,
    sd.price,
    sd.vat,
    sd.vatp,
    sd.total,
    sd.cost_center_id
ORDER BY sr.date DESC, sr.id;

";

                var parameters = new List<MySqlParameter>();

                // 🔹 Filters
                if (dateFrom.HasValue)
                {
                    query += " AND sr.date >= @dateFrom ";
                    parameters.Add(new MySqlParameter("@dateFrom", dateFrom.Value.Date));
                }

                if (dateTo.HasValue)
                {
                    query += " AND sr.date <= @dateTo ";
                    parameters.Add(new MySqlParameter("@dateTo", dateTo.Value.Date));
                }

                if (customerId.HasValue)
                {
                    query += " AND sr.customer_id = @customerId ";
                    parameters.Add(new MySqlParameter("@customerId", customerId.Value));
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query += " AND sr.payment_method = @paymentMethod ";
                    parameters.Add(new MySqlParameter("@paymentMethod", paymentMethod));
                }

                query += @"
GROUP BY sr.id, sr.date, sr.invoice_id, c.code, c.name, sr.payment_method, sr.total, sr.vat, sr.net
ORDER BY sr.date DESC, sr.id;
";

                var salesReturns = new List<SalesQuotationDto>();

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();

                SalesQuotationDto currentOrder = null;
                int lastOrderId = -1;
                while (await reader.ReadAsync())
                {
                    int orderId = reader.GetInt32("Id");

                    if (orderId != lastOrderId)
                    {
                        currentOrder = new SalesQuotationDto
                        {
                            Id = orderId,
                            Date = reader.GetDateTime("Date"),
                            InvoiceCode = reader["InvoiceNo"]?.ToString(),
                            CustomerId = reader.GetInt32("CustomerId"),
                            CustomerName = reader["CustomerName"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),
                            Total = reader.GetDecimal("Total"),
                            Vat = reader.GetDecimal("Vat"),
                            Net = reader.GetDecimal("Net"),
                            WarehouseId = reader.GetInt32("Warehouse_Id"),
                            PONumber = reader["PO_Num"]?.ToString(),
                            BillTo = reader["Bill_To"]?.ToString(),
                            City = reader["City"]?.ToString(),
                            SalesMan = reader["Sales_Man"]?.ToString(),
                            ShipDate = reader.GetDateTime("Ship_Date"),
                            ShipVia = reader["Ship_Via"]?.ToString(),
                            ShipTo = reader["Ship_To"]?.ToString(),
                            AccountCashId = reader.GetInt32("Account_Cash_Id"),
                            PaymentTerms = reader["Payment_Terms"]?.ToString(),
                            PaymentDate = reader.GetDateTime("Payment_Date"),
                            Description = reader["Description"]?.ToString(),
                            Items = new List<SalesQuotationItemDto>()
                        };
                        salesReturns.Add(currentOrder);
                        lastOrderId = orderId;
                    }

                    if (reader["ItemId"] != DBNull.Value)
                    {
                        currentOrder.Items.Add(new SalesQuotationItemDto
                        {
                            ItemId = Convert.ToInt32(reader["ItemId"]),
                            ItemCode = reader["ItemCode"]?.ToString(),
                            ItemName = reader["ItemName"]?.ToString(),
                            Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                            Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0,
                            Sales_Price = reader["Sales_Price"] != DBNull.Value ? Convert.ToDecimal(reader["Sales_Price"]) : 0,
                            Vat = reader["ItemVat"] != DBNull.Value ? Convert.ToDecimal(reader["ItemVat"]) : 0,
                            VatP = reader["VatP"] != DBNull.Value ? Convert.ToDecimal(reader["VatP"]) : 0,
                            Total = reader["ItemTotal"] != DBNull.Value ? Convert.ToDecimal(reader["ItemTotal"]) : 0,
                            CostCenterId = reader["Cost_Center_Id"] != DBNull.Value
                                ? Convert.ToInt32(reader["Cost_Center_Id"])
                                : 0
                        });
                    }
                }

                return Ok(new { status = true, data = salesReturns });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveSalesReturnInvoice([FromBody] SalesReturnRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            // ===== VALIDATIONS =====
            if (model.CustomerId <= 0)
                return Json(new { status = false, message = "Customer must be selected." });

            if (model.AccountCashId <= 0)
                return Json(new { status = false, message = "Account Cash Name must be selected." });

            if (model.Items == null || !model.Items.Any())
                return Json(new { status = false, message = "Insert items first." });

            for (int i = 0; i < model.Items.Count; i++)
            {
                if (model.Items[i].Total <= 0)
                    return Json(new { status = false, message = $"Total Item In Row {i + 1} Can't Be 0 or Null" });
            }

            if (model.NetTotal <= 0)
                return Json(new { status = false, message = "Total must be bigger than zero." });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in." });

            var csb = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName")
            };

            await using var conn = new MySqlConnection(csb.ConnectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                int salesReturnId = model.Id;

                // ===== INSERT =====
                if (salesReturnId == 0)
                {
                    model.InvoiceCode = await GenerateNextSalesReturnCode(conn, tx);

                    var sql = @"
INSERT INTO tbl_sales_return
(date, customer_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man,
 ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date,
 total, vat, net, pay, `change`, created_by, created_date, state, description)
VALUES
(@date,@customer,@invoice,@warehouse,@po,@bill,@city,@salesman,
 @shipdate,@shipvia,@shipto,@paymethod,@account,@terms,@paydate,
 @total,@vat,@net,@pay,@change,@user,@created,0,@desc);
SELECT LAST_INSERT_ID();";

                    var cmd = new MySqlCommand(sql, conn, (MySqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@customer", model.CustomerId);
                    cmd.Parameters.AddWithValue("@invoice", model.InvoiceCode);
                    cmd.Parameters.AddWithValue("@warehouse", model.WarehouseId);
                    cmd.Parameters.AddWithValue("@po", model.PONumber ?? "");
                    cmd.Parameters.AddWithValue("@bill", model.BillTo ?? "");
                    cmd.Parameters.AddWithValue("@city", model.City ?? "");
                    cmd.Parameters.AddWithValue("@salesman", model.SalesMan ?? "");
                    cmd.Parameters.AddWithValue("@shipdate", model.ShipDate);
                    cmd.Parameters.AddWithValue("@shipvia", model.ShipVia ?? "");
                    cmd.Parameters.AddWithValue("@shipto", model.ShipTo ?? "");
                    cmd.Parameters.AddWithValue("@paymethod", model.PaymentMethod);
                    cmd.Parameters.AddWithValue("@account", model.AccountCashId);
                    cmd.Parameters.AddWithValue("@terms", model.PaymentTerms ?? "");
                    cmd.Parameters.AddWithValue("@paydate", model.PaymentDate);
                    cmd.Parameters.AddWithValue("@total", model.TotalBefore);
                    cmd.Parameters.AddWithValue("@vat", model.Vat);
                    cmd.Parameters.AddWithValue("@net", model.NetTotal);
                    cmd.Parameters.AddWithValue("@pay", model.PaymentMethod == "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@change", model.PaymentMethod != "Cash" ? model.NetTotal : 0);
                    cmd.Parameters.AddWithValue("@user", userId);
                    cmd.Parameters.AddWithValue("@created", DateTime.Now);
                    cmd.Parameters.AddWithValue("@desc", model.Description ?? "");

                    salesReturnId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                else
                {
                    await ReturnItemsToInventory(conn, tx, salesReturnId);
                    await DeleteSalesReturnTransactions(conn, tx, salesReturnId);
                }
                // ===== ITEMS + COST CENTER =====
                foreach (var item in model.Items)
                {
                    await InsertSalesReturnItem(conn, tx, salesReturnId, item, model);

                    await InsertCostCenterTransaction(
                        conn,
                        tx,
                        model.Date,
                        item.Total,          // debit (item total)
                        0m,                  // credit
                        salesReturnId,       // reference id
                        "Sales Return",      // type
                        "",                  // description (if needed)
                        item.CostCenterId
                    );
                }

                // ===== ACCOUNTING (once per invoice) =====
                await InsertAccountingEntries(conn, tx, salesReturnId, model, userId);

                await tx.CommitAsync();

                return Json(new
                {
                    status = true,
                    message = "Sales Return Invoice Saved",
                    id = salesReturnId
                }); 
            }

            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return Json(new { status = false, message = ex.Message });
            }
        }
        private async Task InsertSalesReturnItem(MySqlConnection conn, MySqlTransaction tx,
            int salesId, SalesReturnItemRequest item, SalesReturnRequest model)
        {
            var sql = @"
INSERT INTO tbl_sales_return_details
(sales_id,item_id,qty,cost_price,price,vatp,vat,total,cost_center_id)
VALUES
(@sales,@item,@qty,@cost,@price,@vatp,@vat,@total,@cc);";

            var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@sales", salesId);
            cmd.Parameters.AddWithValue("@item", item.ItemId);
            cmd.Parameters.AddWithValue("@qty", item.Quantity);
            cmd.Parameters.AddWithValue("@cost", item.CostPrice);
            cmd.Parameters.AddWithValue("@price", item.Price);
            cmd.Parameters.AddWithValue("@vatp", item.VatPercentage);
            cmd.Parameters.AddWithValue("@vat", item.Vat);
            cmd.Parameters.AddWithValue("@total", item.Total);
            cmd.Parameters.AddWithValue("@cc", item.CostCenterId);
            await cmd.ExecuteNonQueryAsync();

            await InsertItemTransaction(conn, tx, salesId, item, model);
        }
        private async Task InsertItemTransaction(MySqlConnection conn, MySqlTransaction tx,
            int salesId, SalesReturnItemRequest item, SalesReturnRequest model)
        {
            var sql = @"
INSERT INTO tbl_item_transaction
(date,type,reference,item_id,cost_price,qty_in,sales_price,qty_out,qty_inc,description,warehouse_id)
VALUES
(@date,'SalesReturn Invoice',@ref,@item,@cost,@qty,@price,0,@qty,@desc,@wh);";
            //SalesReturn Invoice
            var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", model.Date);
            cmd.Parameters.AddWithValue("@ref", salesId);
            cmd.Parameters.AddWithValue("@item", item.ItemId);
            cmd.Parameters.AddWithValue("@cost", item.Price);
            cmd.Parameters.AddWithValue("@qty", item.Quantity);
            cmd.Parameters.AddWithValue("@price", item.Price);
            cmd.Parameters.AddWithValue("@desc", "Sales Return Invoice No. " + model.InvoiceCode);
            cmd.Parameters.AddWithValue("@wh", model.WarehouseId);
            await cmd.ExecuteNonQueryAsync();

            await UpdateOnHandItem(conn, tx, item.ItemId);
        }
        private async Task UpdateOnHandItem(MySqlConnection conn, MySqlTransaction tx, int itemId)
        {
            var sql = @"UPDATE tbl_items SET on_hand =
               (SELECT SUM(qty_in - qty_out) FROM tbl_item_transaction WHERE item_id=@id)
               WHERE id=@id";
            var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@id", itemId);
            await cmd.ExecuteNonQueryAsync();
        }
        private async Task InsertAccountingEntries(MySqlConnection conn, MySqlTransaction tx,
         int salesId, SalesReturnRequest model, int userId)
        {
            if (model.NetTotal <= 0) return; // Skip if total <= 0

            // 1️⃣ Cash / Credit
            //string accountId = model.PaymentMethod == "Credit"
            //    ? model.PaymentCreditAccountId.ToString()
            //    : model.AccountCashId.ToString();

            string accountId = model.PaymentMethod == "Credit"
            ? model.PaymentCreditAccountId.ToString()
            : model.AccountCashId.ToString();
            int level4VatId = await GetDefaultAccountId("Vat Output");
            int level4SalesReturnId = await GetDefaultAccountId("SalesReturn");
            var sql = @"
INSERT INTO tbl_transaction
(date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no)
VALUES
(@date, @acc, @debit, @credit, @id, @hum_id, 'SALES RETURN', 'SalesReturn Invoice', @desc, @user, @created, 0, @voucher);";

            using (var cmd = new MySqlCommand(sql, conn, tx))
            {
                // --- Cash / Credit entry ---
                cmd.Parameters.AddWithValue("@date", model.Date);
                cmd.Parameters.AddWithValue("@acc", accountId);
                cmd.Parameters.AddWithValue("@debit", 0);
                cmd.Parameters.AddWithValue("@credit", model.NetTotal);
                cmd.Parameters.AddWithValue("@id", salesId);
                cmd.Parameters.AddWithValue("@hum_id", model.CustomerId);
                cmd.Parameters.AddWithValue("@desc", $"SalesReturn Invoice NO. {model.InvoiceCode}");
                cmd.Parameters.AddWithValue("@user", userId);
                cmd.Parameters.AddWithValue("@created", DateTime.Now);
                cmd.Parameters.AddWithValue("@voucher", model.InvoiceCode);
                await cmd.ExecuteNonQueryAsync();

                cmd.Parameters.Clear();

                // --- VAT entry (if > 0) ---
                if (model.Vat > 0)
                {
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@acc", level4VatId.ToString());
                    cmd.Parameters.AddWithValue("@debit", model.Vat);
                    cmd.Parameters.AddWithValue("@credit", 0);
                    cmd.Parameters.AddWithValue("@id", salesId);
                    cmd.Parameters.AddWithValue("@hum_id",0);
                    cmd.Parameters.AddWithValue("@desc", $"Vat Output For Return Invoice No. {model.InvoiceCode}");
                    cmd.Parameters.AddWithValue("@user", userId);
                    cmd.Parameters.AddWithValue("@created", DateTime.Now);
                    cmd.Parameters.AddWithValue("@voucher", model.InvoiceCode);
                    await cmd.ExecuteNonQueryAsync();

                    cmd.Parameters.Clear();
                }

                // --- Sales Return account entry ---
                cmd.Parameters.AddWithValue("@date", model.Date);
                cmd.Parameters.AddWithValue("@acc", level4SalesReturnId.ToString());
                cmd.Parameters.AddWithValue("@debit", model.TotalBefore);
                cmd.Parameters.AddWithValue("@credit", 0);
                cmd.Parameters.AddWithValue("@id", salesId);
                cmd.Parameters.AddWithValue("@hum_id", 0);
                cmd.Parameters.AddWithValue("@desc", $"SalesReturn For Invoice No. {model.InvoiceCode}");
                cmd.Parameters.AddWithValue("@user", userId);
                cmd.Parameters.AddWithValue("@created", DateTime.Now);
                cmd.Parameters.AddWithValue("@voucher", model.InvoiceCode);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private async Task ReturnItemsToInventory(MySqlConnection conn, MySqlTransaction tx, int id)
        {
            var items = new List<(int itemId, decimal qty)>();

            // Read all return item quantities FIRST
            await using (var cmd = new MySqlCommand("SELECT item_id, qty FROM tbl_sales_return_details WHERE sales_id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add((
                        reader.GetInt32("item_id"),
                        reader.GetDecimal("qty")
                    ));
                }
            } // <<< reader is now closed

            // Now update inventory for each
            foreach (var (itemId, qty) in items)
            {
                await using var upd = new MySqlCommand(
                    "UPDATE tbl_items SET on_hand = on_hand + @qty WHERE id = @item", conn, tx);
                upd.Parameters.AddWithValue("@qty", qty);
                upd.Parameters.AddWithValue("@item", itemId);
                await upd.ExecuteNonQueryAsync();
            }

            // Finally delete the details
            await using var del = new MySqlCommand(
                "DELETE FROM tbl_sales_return_details WHERE sales_id = @id", conn, tx);
            del.Parameters.AddWithValue("@id", id);
            await del.ExecuteNonQueryAsync();
        }

        private async Task DeleteSalesReturnTransactions(MySqlConnection conn, MySqlTransaction tx, int id)
        {
            await new MySqlCommand(
                "DELETE FROM tbl_item_transaction WHERE reference=@id AND type='Sales Return Invoice'",
                conn, tx)
            { Parameters = { new("@id", id) } }.ExecuteNonQueryAsync();

            await new MySqlCommand(
                "DELETE FROM tbl_transaction WHERE transaction_id=@id AND t_type='SALES RETURN'",
                conn, tx)
            { Parameters = { new("@id", id) } }.ExecuteNonQueryAsync();
        }
        private async Task<string> GenerateNextSalesReturnCode(MySqlConnection conn, MySqlTransaction tx)
        {
            var cmd = new MySqlCommand(
                "SELECT MAX(CAST(SUBSTRING(invoice_id,5) AS UNSIGNED)) FROM tbl_sales_return",
                conn, tx);

            var result = await cmd.ExecuteScalarAsync();
            int next = result == DBNull.Value ? 1 : Convert.ToInt32(result) + 1;
            return "SRI-" + next.ToString("D5");
        }

        private async Task InsertCostCenterTransaction(
    MySqlConnection conn,
    MySqlTransaction tx,
    DateTime date,
    decimal debit,
    decimal credit,
    int refId,
    string type,
    string description,
    int costCenterId)
        {
            if (costCenterId <= 0)
                return; 

            var sql = @"
INSERT INTO tbl_cost_center_transaction
(type, date, ref_id, debit, credit, description, cost_center_id)
VALUES
(@type, @date, @ref, @debit, @credit, @desc, @cc);";

            await using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@ref", refId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@desc", description ?? "");
            cmd.Parameters.AddWithValue("@cc", costCenterId);

            await cmd.ExecuteNonQueryAsync();
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesReturnInvoice(int salesId)
        {
            try
            {
                // ---------------- DATABASE CONNECTION ----------------
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ---------------- COMPANY INFO ----------------
                var companyCmd = new MySqlCommand(@"
SELECT 
    name,
    phone1,
    gmail,
    address,
    trn_no,
    logoComp
FROM tbl_company
LIMIT 1;
", conn);

                CompanyReportDto company = null;
                await using (var reader = await companyCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        company = new CompanyReportDto
                        {
                            Name = reader.GetString("name"),
                            Phone = reader.IsDBNull("phone1") ? null : reader.GetString("phone1"),
                            Email = reader.IsDBNull("gmail") ? null : reader.GetString("gmail"),
                            Address = reader.IsDBNull("address") ? null : reader.GetString("address"),
                            TRN = reader.IsDBNull("trn_no") ? null : reader.GetString("trn_no"),
                            Logo = reader.IsDBNull("logoComp") ? null : (byte[])reader["logoComp"]
                        };
                    }
                }

                if (company == null)
                    return BadRequest("Company info not found");

                // ---------------- SALES RETURN HEADER ----------------
                var salesCmd = new MySqlCommand(@"
SELECT 
    s.id,
    s.date,
    s.invoice_id,
    s.bill_to,
    s.city,
    s.sales_man,
    s.ship_date,
    s.ship_via,
    s.ship_to,
    s.po_num,
    s.payment_method,
    s.payment_terms,
    s.payment_date,
    s.total,
    s.vat,
    s.net,
    s.pay,
    s.change,
    c.name AS customerName,
    c.main_phone,
    c.email,
    c.trn,
    (SELECT name FROM tbl_coa_level_4 WHERE id=s.account_cash_id) AS accountName
FROM tbl_sales_return s
INNER JOIN tbl_customer c ON s.customer_id = c.id
WHERE s.id = @salesId;
", conn);

                salesCmd.Parameters.Add("@salesId", MySqlDbType.Int32).Value = salesId;

                SaleReportDto sale = null;
                await using (var reader = await salesCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        sale = new SaleReportDto
                        {
                            Id = reader.GetInt32("id"),
                            Date = reader.GetDateTime("date"),
                            InvoiceNo = reader.GetString("invoice_id"),
                            CustomerName = reader.GetString("customerName"),
                            City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                            SalesMan = reader.IsDBNull("sales_man") ? null : reader.GetString("sales_man"),
                            ShipDate = reader.IsDBNull("ship_date") ? null : reader.GetDateTime("ship_date"),
                            PaymentMethod = reader.IsDBNull("payment_method") ? null : reader.GetString("payment_method"),
                            Total = reader.GetDecimal("total"),
                            Vat = reader.GetDecimal("vat"),
                            Net = reader.GetDecimal("net"),
                            AccountName = reader.IsDBNull("accountName") ? null : reader.GetString("accountName")
                        };
                    }
                }

                if (sale == null)
                    return NotFound("Sales return not found");

                // ---------------- ITEM DETAILS ----------------
                var itemCmd = new MySqlCommand(@"
SELECT 
    d.id,
    d.item_id,
    i.code,
    i.name,
    d.qty,
    d.cost_price,
    d.price,
    (d.qty*d.cost_price) AS subCostTotal,
    (d.qty*d.price) AS subPriceTotal,
    d.discount,
    ((d.qty*d.price)-d.discount) AS subTotal,
    d.vatp AS vatAmount,
    d.vat AS vatPercentage,
    d.total,
    (SELECT name FROM tbl_sub_cost_center WHERE id=d.cost_center_id) AS costCenterName,
    (SELECT name FROM tbl_unit WHERE id=i.unit_id) AS unitName
FROM tbl_sales_return_details d
INNER JOIN tbl_items i ON d.item_id = i.id
WHERE d.sales_id = @salesId;
", conn);

                itemCmd.Parameters.Add("@salesId", MySqlDbType.Int32).Value = salesId;

                var items = new List<SaleItemReportDto>();
                await using (var reader = await itemCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new SaleItemReportDto
                        {
                            ItemId = reader.GetInt32("item_id"),
                            Code = reader.GetString("code"),
                            Name = reader.GetString("name"),
                            Qty = reader.IsDBNull(reader.GetOrdinal("qty")) ? 0 : Convert.ToDecimal(reader["qty"]),
                            Price = reader.IsDBNull(reader.GetOrdinal("price")) ? 0 : Convert.ToDecimal(reader["price"]),
                            Discount = reader.IsDBNull(reader.GetOrdinal("discount")) ? 0 : Convert.ToDecimal(reader["discount"]),
                            Vat = reader.IsDBNull(reader.GetOrdinal("vatPercentage")) ? 0 : Convert.ToDecimal(reader["vatPercentage"]),
                            Total = reader.IsDBNull(reader.GetOrdinal("total")) ? 0 : Convert.ToDecimal(reader["total"]),
                            CostCenterName = reader.IsDBNull(reader.GetOrdinal("costCenterName")) ? null : reader.GetString("costCenterName"),
                            UnitName = reader.IsDBNull(reader.GetOrdinal("unitName")) ? null : reader.GetString("unitName")
                        });
                    }
                }


                // ---------------- PDF GENERATION ----------------
                byte[] pdfBytes = SalesReturnPdfGenerator.Generate(company, sale, items);

                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        #endregion

        #region Credit Note

        public IActionResult CreditNote()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCreditNotes(
    DateTime? dateFrom,
    DateTime? dateTo,
    string selectionMethod = "Default")
        {
            try
            {
                // Build connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Base query: join transactions (get max transaction per credit note) & items
                string query = @"
SELECT 
    c.id AS Id,
    c.date AS Date,
    c.invoice_id AS InvoiceNo,
    jt.MaxTransactionId AS JvNo,
    c.amount AS Amount,
    c.vat AS Vat,
    c.total AS TotalAmount,
    c.description AS Description,
    c.credit_account AS CreditAccount,
    c.debit_account AS DebitAccount,
    d.invoice_id AS ItemInvoiceId,
    d.inv_no AS ItemInvoiceNo,
    d.invoice_date AS ItemInvoiceDate,
    d.invoice_type AS ItemInvoiceType,
    d.amount AS ItemAmount,
    d.total AS ItemTotal,
    d.remaining AS ItemRemaining,
    d.vat AS ItemVat
FROM tbl_credit_note c
-- Subquery to get max transaction ID for each credit note
LEFT JOIN (
    SELECT transaction_id, MAX(transaction_id) AS MaxTransactionId
    FROM tbl_transaction
    GROUP BY transaction_id
) jt ON c.id = jt.transaction_id
-- Join credit note details
LEFT JOIN tbl_credit_note_details d ON c.id = d.ref_id
WHERE c.state = 0
";

                var parameters = new List<MySqlParameter>();

                // Apply date filters
                if (dateFrom.HasValue)
                {
                    query += " AND c.date >= @dateFrom ";
                    parameters.Add(new MySqlParameter("@dateFrom", dateFrom.Value.Date));
                }

                if (dateTo.HasValue)
                {
                    query += " AND c.date <= @dateTo ";
                    parameters.Add(new MySqlParameter("@dateTo", dateTo.Value.Date));
                }

                query += " ORDER BY c.date DESC, c.id DESC;"; // sort by date & id

                var creditNotes = new List<CreditNoteDto>();

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();

                CreditNoteDto currentNote = null;
                int lastId = -1;

                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32("Id");

                    if (id != lastId)
                    {
                        // New credit note
                        currentNote = new CreditNoteDto
                        {
                            Id = id,
                            Date = reader.GetDateTime("Date"),
                            InvoiceNo = reader["InvoiceNo"]?.ToString(),
                            JvNo = reader["JvNo"]?.ToString(),
                            Amount = reader.GetDecimal("Amount"),
                            Vat = reader.GetDecimal("Vat"),
                            TotalAmount = reader["TotalAmount"] != DBNull.Value
                                          ? reader.GetDecimal("TotalAmount")
                                          : 0,
                            Description = reader["Description"]?.ToString(),
                            CreditAccount = reader.GetInt32("CreditAccount"),
                            DebitAccount = reader.GetInt32("DebitAccount"),
                            Items = new List<CreditNoteItemDto>()
                        };

                        creditNotes.Add(currentNote);
                        lastId = id;
                    }

                    // Add item if exists
                    if (reader["ItemInvoiceId"] != DBNull.Value)
                    {
                        currentNote.Items.Add(new CreditNoteItemDto
                        {
                            InvoiceId = Convert.ToInt32(reader["ItemInvoiceId"]),
                            InvoiceNo = reader["ItemInvoiceNo"]?.ToString(),
                            InvoiceDate = Convert.ToDateTime(reader["ItemInvoiceDate"]),
                            InvoiceType = reader["ItemInvoiceType"]?.ToString(),
                            Amount = Convert.ToDecimal(reader["ItemAmount"]),
                            Total = reader["ItemTotal"] != DBNull.Value
                                    ? Convert.ToDecimal(reader["ItemTotal"])
                                    : 0,
                            Remaining = reader["ItemRemaining"] != DBNull.Value
                                        ? Convert.ToDecimal(reader["ItemRemaining"])
                                        : 0,
                            Vat = Convert.ToDecimal(reader["ItemVat"])
                        });
                    }
                }

                return Ok(new { status = true, data = creditNotes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerInvoices(int customerId)
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

                // Query
                string query = @"
            SELECT 
                s.id AS Id,
                s.date AS Date,
                s.invoice_id AS InvoiceNo,
                s.total AS Total,
                s.net AS TotalWithVAT,
                s.vat AS Vat,
                s.change AS Remaining
            FROM tbl_sales s
            INNER JOIN tbl_transaction t 
                ON t.transaction_id = s.id
                AND t.`type` IN ('Customer Opening Balance', 'Sales Invoice')
                AND t.hum_id = s.customer_id
            WHERE s.customer_id = @customerId
            GROUP BY s.id, s.date, s.invoice_id, s.total, s.net, s.vat, s.change
            HAVING Remaining > 0;
        ";

                var invoices = new List<object>();

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@customerId", customerId);

                await using var reader = await cmd.ExecuteReaderAsync();
                int count = 1;
                while (await reader.ReadAsync())
                {
                    DateTime date = reader.GetDateTime("Date");
                    invoices.Add(new
                    {
                        Sn = count,
                        Date = date.ToString("yyyy-MM-dd"),  
                        FormattedDate = date.ToString("dd/MM/yyyy"),
                        InvoiceNo = reader["InvoiceNo"]?.ToString(),
                        Id = reader["Id"]?.ToString(),
                        TotalWithVAT = reader["TotalWithVAT"] != DBNull.Value ? Convert.ToDecimal(reader["TotalWithVAT"]) : 0,
                        Vat = reader["Vat"] != DBNull.Value ? Convert.ToDecimal(reader["Vat"]) : 0,
                        Remaining = reader["Remaining"] != DBNull.Value ? Convert.ToDecimal(reader["Remaining"]) : 0,
                        Selected = false 
                    });
                    count++;
                }

                return Ok(new { status = true, data = invoices });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceItems(int salesId)
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

                string query = @"
            SELECT 
                CONCAT(ti.code,' - ', ti.name) AS ItemName, 
                ts.qty AS Qty, 
                ts.price AS Price, 
                ts.vatp AS Vat, 
                ts.total AS Total 
            FROM tbl_sales_details ts 
            INNER JOIN tbl_items ti ON ts.item_id = ti.id 
            WHERE ts.sales_id = @salesId
        ";

                var items = new List<object>();

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@salesId", salesId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new
                    {
                        ItemName = reader["ItemName"]?.ToString(),
                        Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                        Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0,
                        Vat = reader["Vat"] != DBNull.Value ? Convert.ToDecimal(reader["Vat"]) : 0,
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

        [HttpGet]
        public async Task<IActionResult> GetDefaultAccounts()
        {
            try
            {
                var result = new
                {
                    customerAccountId = await GetDefaultAccountId("Invoice Payment Cash Method"),
                    vatOutputAccountId = await GetDefaultAccountId("Vat Output"),
                    salesReturnAccountId = await GetDefaultAccountId("SalesReturn"),
                    level4PaymentCreditMethodId = await GetDefaultAccountId("Customer"),
                    level4VatId = await GetDefaultAccountId("Vat Output"),
                    level4SalesInvoice = await GetDefaultAccountId("Sales"),
                    level4COGS = await GetDefaultAccountId("COGS"),
                    level4Inventory = await GetDefaultAccountId("Inventory")
                };

                return Ok(new
                {
                    status = true,
                    data = result
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

        [HttpPost]
        public async Task<IActionResult> SaveCreditNote([FromBody] CreditNoteRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            // 1️⃣ Required accounts check
            var requiredAccounts = new List<string> { "Sales", "Vendor", "COGS", "Customer", "Vat Input", "Vat Output", "Inventory" };
            if (!await AreDefaultAccountsSet(requiredAccounts))
                return Json(new { status = false, message = "Default accounts for invoice are not properly configured. Please check your settings." });

            // 2️⃣ Customer selection validation
            if (model.CustomerId <= 0)
                return Json(new { status = false, message = "Customer must be selected." });

            // 3️⃣ Account selection validation
            if (model.AccountCashId <= 0)
                return Json(new { status = false, message = "Debit account must be selected." });

            // 4️⃣ Item validations
            if (model.Items == null || !model.Items.Any())
                return Json(new { status = false, message = "Insert at least one item." });

            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.Total <= 0)
                    return Json(new { status = false, message = $"Total Item in Row {i + 1} can't be 0 or null." });
            }

            // 5️⃣ Total / Net validations
            if (model.TotalAmount <= 0)
                return Json(new { status = false, message = "Total must be greater than zero." });

            //// 6️⃣ Default Level Accounts check (level4CustomerId, level4VatId, etc.)
            //if (model.Level4CustomerId == 0 || model.Level4VatId == 0 || model.Level4SalesReturn == 0 ||
            //    model.Level4COGS == 0 || model.Level4Inventory == 0)
            //    return Json(new { status = false, message = "Default accounts for invoice are not properly configured. Please check your settings." });

            // 7️⃣ User validation
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

                int creditNoteId = model.Id;
                string invCode = model.InvoiceCode;

                if (creditNoteId == 0) // Insert new Credit Note
                {
                    invCode = await GenerateNextCreditNoteCode(conn);

                    var insertQuery = @"
INSERT INTO tbl_credit_note
(date, credit_account, debit_account, type, invoice_id, amount, vat, total, description, created_by, created_date, state)
VALUES (@date, @creditAccount, @debitAccount, @type, @invoice_id, @amount, @vat, @total, @description, @createdBy, @createdDate, 0);
SELECT LAST_INSERT_ID();";

                    using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@creditAccount", model.CustomerId);
                    cmd.Parameters.AddWithValue("@debitAccount", model.AccountCashId);
                    cmd.Parameters.AddWithValue("@type", "Customer");
                    cmd.Parameters.AddWithValue("@invoice_id", invCode);
                    cmd.Parameters.AddWithValue("@amount", model.Amount);
                    cmd.Parameters.AddWithValue("@vat", model.Vat);
                    cmd.Parameters.AddWithValue("@total", model.TotalAmount);
                    cmd.Parameters.AddWithValue("@description", model.Description ?? "");
                    cmd.Parameters.AddWithValue("@createdBy", userId);
                    cmd.Parameters.AddWithValue("@createdDate", DateTime.Now);

                    creditNoteId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                else // Update existing Credit Note
                {
                    var updateQuery = @"
UPDATE tbl_credit_note SET 
date=@date, credit_account=@creditAccount, debit_account=@debitAccount, invoice_id=@invoice_id,
amount=@amount, vat=@vat, total=@total, description=@description,
modified_by=@modifiedBy, modified_date=@modifiedDate
WHERE id=@id;";

                    using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", creditNoteId);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@creditAccount", model.CustomerId);
                    cmd.Parameters.AddWithValue("@debitAccount", model.AccountCashId);
                    cmd.Parameters.AddWithValue("@invoice_id", invCode);
                    cmd.Parameters.AddWithValue("@amount", model.Amount);
                    cmd.Parameters.AddWithValue("@vat", model.Vat);
                    cmd.Parameters.AddWithValue("@total", model.TotalAmount);
                    cmd.Parameters.AddWithValue("@description", model.Description ?? "");
                    cmd.Parameters.AddWithValue("@modifiedBy", userId);
                    cmd.Parameters.AddWithValue("@modifiedDate", DateTime.Now);

                    await cmd.ExecuteNonQueryAsync();

                    // Delete old items first
                    using var deleteCmd = new MySqlCommand("DELETE FROM tbl_credit_note_details WHERE ref_id=@id", conn);
                    deleteCmd.Parameters.AddWithValue("@id", creditNoteId);
                    await deleteCmd.ExecuteNonQueryAsync();

                    // Delete old transaction entries
                    await DeleteTransactionEntries(conn, creditNoteId, "Credit Note", invCode);
                }

                // Insert Credit Note Items
                foreach (var item in model.Items)
                {
                    var insertItemQuery = @"
INSERT INTO tbl_credit_note_details
(ref_id, inv_no, invoice_id, invoice_date, invoice_type, total, vat, amount, balance, remaining)
VALUES (@refId, @invNo, @invId, @invDate, @invType, @total, @vat, @amount, @balance, @remaining);";

                    using var itemCmd = new MySqlCommand(insertItemQuery, conn);
                    itemCmd.Parameters.AddWithValue("@refId", creditNoteId);
                    itemCmd.Parameters.AddWithValue("@invNo", invCode);
                    itemCmd.Parameters.AddWithValue("@invId", creditNoteId);
                    itemCmd.Parameters.AddWithValue("@invDate", DateTime.Now);
                    itemCmd.Parameters.AddWithValue("@invType", "SALES");
                    itemCmd.Parameters.AddWithValue("@total", item.Total);
                    itemCmd.Parameters.AddWithValue("@vat", item.Vat);
                    itemCmd.Parameters.AddWithValue("@amount", item.Amount);
                    itemCmd.Parameters.AddWithValue("@balance", item.Balance);
                    itemCmd.Parameters.AddWithValue("@remaining", item.Remaining);
                    
                    await itemCmd.ExecuteNonQueryAsync();

                    // Update invoice paid / change in tbl_sales
                    var paidResult = await new MySqlCommand("SELECT SUM(`change`) FROM tbl_sales WHERE id=@id", conn)
                    {
                        Parameters = { new MySqlParameter("@id", item.InvoiceId) }
                    }.ExecuteScalarAsync();

                    decimal totalPaid = paidResult != DBNull.Value ? Convert.ToDecimal(paidResult) : 0;

                    var updateSalesCmd = new MySqlCommand(
                        "UPDATE tbl_sales SET pay=@pay, `change`=@change WHERE id=@id", conn);
                    updateSalesCmd.Parameters.AddWithValue("@pay", totalPaid);
                    updateSalesCmd.Parameters.AddWithValue("@change", item.Remaining);
                    updateSalesCmd.Parameters.AddWithValue("@id", item.InvoiceId);
                    await updateSalesCmd.ExecuteNonQueryAsync();
                }

                // Add transaction entries
                await AddCreditNoteTransactions(conn, creditNoteId, model, userId, invCode);


                return Json(new
                {
                    status = true,
                    message = creditNoteId == model.Id ? "Credit Note updated successfully" : "Credit Note saved successfully",
                    id = creditNoteId
                });
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }

        public static async Task<string> GenerateNextCreditNoteCode(MySqlConnection conn)
        {
            string newCode = "CN-0001";

            var query = "SELECT MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)) AS lastCode FROM tbl_credit_note";
            using var cmd = new MySqlCommand(query, conn);
            var result = await cmd.ExecuteScalarAsync();

            if (result != DBNull.Value && result != null)
            {
                int code = Convert.ToInt32(result) + 1;
                newCode = "CN-" + code.ToString("D4");
            }

            return newCode;
        }
        public static async Task DeleteTransactionEntries(MySqlConnection conn, int debitNoteId, string type, string invCode)
        {
            string transactionType = $"Credit Note {invCode}";

            var query = "DELETE FROM tbl_transaction WHERE t_type = @tType AND transaction_id = @id";

            await using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", debitNoteId);
            cmd.Parameters.AddWithValue("@tType", transactionType);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"{rowsAffected} transaction(s) deleted for {transactionType}");
        }


        public static async Task AddCreditNoteTransactions(MySqlConnection conn, int creditNoteId, CreditNoteRequest model, int userId, string invCode)
        {
            // 1️⃣ Customer Debit Entry
            await AddTransactionEntry(conn,
                date: model.Date,
                accountId: model.AccountCashId, // Debit account (customer)
                debit: "0",
                credit: model.TotalAmount.ToString(),
                transactionId: creditNoteId.ToString(),        // Use credit note ID
                humId: model.CustomerId.ToString(),  // Customer/Account as hum_id
                tType: $"Credit Note {invCode}",
                type: $"Credit Note {invCode}",
                description: $"Credit Note {invCode}",
                createdBy: userId,
                createdDate: DateTime.Now,
                voucherNo: invCode
            );

            // 2️⃣ VAT Entry (if > 0)
            if (model.Vat > 0)
            {
                await AddTransactionEntry(conn,
                    date: model.Date,
                    accountId: model.AccountCashId,    // VAT account
                    debit: model.Vat.ToString(),
                    credit: "0",
                    transactionId: creditNoteId.ToString(),
                    humId: "0",
                    tType: $"Credit Note {invCode}",
                    type: $"Credit Note {invCode}",
                    description: $"Vat Output For Invoice No. {invCode}",
                    createdBy: userId,
                    createdDate: DateTime.Now,
                    voucherNo: invCode
                );
            }

            // 3️⃣ Sales Return / Revenue Entry
            await AddTransactionEntry(conn,
                date: model.Date,
                accountId: model.AccountCashId,  // Revenue account
                debit: model.Amount.ToString(),
                credit: "0",
                transactionId: creditNoteId.ToString(),
                humId: "0",
                tType: $"Credit Note {invCode}",
                type: $"Credit Note {invCode}",
                description: $"Revenue For Invoice No. {invCode}",
                createdBy: userId,
                createdDate: DateTime.Now,
                voucherNo: invCode
            );
        }

        public static async Task AddTransactionEntry(MySqlConnection conn, DateTime date, int accountId, string debit, string credit,
            string transactionId, string humId, string tType, string type, string description, int createdBy, DateTime createdDate, string voucherNo)
        {
            var query = @"
INSERT INTO tbl_transaction
(date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no)
VALUES (@date, @accountId, @debit, @credit, @transactionId, @humId, @tType, @type, @description, @createdBy, @createdDate, 0, @voucherNo);";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transactionId", transactionId);
            cmd.Parameters.AddWithValue("@humId", humId);
            cmd.Parameters.AddWithValue("@tType", tType);
            cmd.Parameters.AddWithValue("@type", "Credit Note");
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@createdBy", createdBy);
            cmd.Parameters.AddWithValue("@createdDate", createdDate);
            cmd.Parameters.AddWithValue("@voucherNo", voucherNo);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<bool> AreDefaultAccountsSet(List<string> accountCategories)
        {
            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            string categories = string.Join(",", accountCategories.Select(c => $"'{c}'"));
            string query = $"SELECT COUNT(*) FROM tbl_coa_config WHERE category IN ({categories})";

            await using var cmd = new MySqlCommand(query, conn);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

            return count == accountCategories.Count;
        }

        #endregion

    }
}
