using Microsoft.AspNetCore.Mvc;
using YamyProject.Core.Models;

namespace YamyProject.Controllers
{
    public class VendorController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public VendorController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        #region Vendor CRUD Operation

        public IActionResult Vendor()
        {
            return View();  
        }

        [HttpGet]
        public async Task<IActionResult> GetVendor(string state = "All")
        {
            try
            {
                // Validate user session
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                // Build MySQL connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Main query
                string query = @"
      SELECT c.id, 
                                   CONCAT(c.code, ' - ', c.name) AS Name, 
                                   c.work_phone, c.main_phone, tc.name AS Category, c.region, 
                                    c.email,
                                    c.trn,
                                    c.mobile,
                                    c.Cat_id,
                                    c.ccemail,
                                    c.website,
                                    c.country,
                                     c.date,
                                    c.city,
                                    c.region,
                                    c.building_name,
                                    c.account_id,
                                    c.facilty_name,
                                    c.project_id,
                                    c.project_site,
                                    c.active, c.trn, COALESCE(t.Amount, 0) AS Amount
                            FROM tbl_vendor c
                            LEFT JOIN (
                                    SELECT 
                                        hum_id,
                                        SUM(credit - debit) AS Amount
                                    FROM tbl_transaction
                                    WHERE 
                                        state = 0 AND
                                        type IN (
                                            'Vendor Payment',
                                            'Petty Cash',
                                            'Purchase Invoice',
                                            'Vendor Opening Balance',
                                            'Vendor Advance Payment',
                                            'Check Cancel (Vendor)',
                                            'Purchase Return Invoice',
                                            'Debit Note',
                                            'PDC Payable'
                                )
                            GROUP BY hum_id
                        ) t ON t.hum_id = c.id
                        LEFT JOIN tbl_vendor_category tc ON c.Cat_id = tc.id WHERE c.type='Vendor'";

                // Apply state filter
                if (state == "Active Subcontractor")
                    query += " AND c.active = 0";
                else if (state == "Inactive Subcontractor")
                    query += " AND c.active != 0";

                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

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
                        Cat_id = reader.GetInt32("Cat_id"),
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
                        Date = reader["date"] != DBNull.Value ? Convert.ToDateTime(reader["date"]) : (DateTime?)null,
                        Active = reader["active"] != DBNull.Value ? Convert.ToInt32(reader["active"]) : 0
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

        [HttpPost]
        public async Task<IActionResult> SaveVendor([FromBody] VendorRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            // ===== VALIDATIONS =====
            if (!string.IsNullOrWhiteSpace(model.TRN))
            {
                if (model.TRN.Length < 3 || model.TRN.Length > 15)
                    return Json(new { status = false, message = "TRN must be between 3 and 15 characters." });
            }

            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { status = false, message = "Enter Vendor Name First." });

            if (model.AccountId == null || model.AccountId <= 0)
                return Json(new { status = false, message = "Account must be set for the Vendor." });

            if (model.OpeningBalanceDate.HasValue && model.OpeningBalanceDate.Value.Date > DateTime.Now.Date)
                return Json(new { status = false, message = "Date value must be less or equal to today." });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in" });

            decimal debit = model.Debit ?? 0;
            decimal credit = model.Credit ?? 0;
            decimal balance = credit - debit;   // same as WinForms logic

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
                var dupSql = "SELECT id FROM tbl_vendor WHERE name=@name AND type='Vendor'";
                using (var dupCmd = new MySqlCommand(dupSql, conn, (MySqlTransaction)tx))
                {
                    dupCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    var existingId = await dupCmd.ExecuteScalarAsync();

                    if (existingId != null &&
                        (model.Id == 0 || Convert.ToInt32(existingId) != model.Id))
                    {
                        return Json(new { status = false, message = "Vendor already exists. Enter another name." });
                    }
                }

                string formattedCode = model.Code;
                var projectSite = string.Join(",", model.ProjectSites);
                // ================= INSERT =================
                if (model.Id == 0)
                {
                    // Generate next Vendor code (starts from 20001)
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
             0,'Vendor', @project_id, @project_site);
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
                    insertCmd.Parameters.Add("@project_id", MySqlDbType.Int32).Value =
                                    string.IsNullOrWhiteSpace(projectSite) ? 0 : int.Parse(projectSite);
                    insertCmd.Parameters.AddWithValue("@project_site", projectSite ?? "");


                    int vendorId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                    // Process Opening Balance (vendor-specific)
                    await ProcessVendorOpeningBalanceAsync(conn, tx, vendorId, formattedCode, model, userId);

                    await tx.CommitAsync();
                    return Json(new { status = true, message = "Vendor added successfully", code = formattedCode });
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
                 project_site = @project_site, project_id = @project_id,
                building_name=@building_name, account_id=@account_id,
                trn=@trn, facilty_name=@facilty_name,
                active=@active, balance=@balance, type='Vendor'
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
                    updateCmd.Parameters.Add("@project_id", MySqlDbType.Int32).Value =
                                    string.IsNullOrWhiteSpace(projectSite) ? 0 : int.Parse(projectSite);
                    updateCmd.Parameters.AddWithValue("@project_site", projectSite ?? "");
                    //updateCmd.Parameters.AddWithValue("@project_id", projectSite);
                    //updateCmd.Parameters.AddWithValue("@project_site", projectSite);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);

                    await updateCmd.ExecuteNonQueryAsync();

                    // Remove old opening balance entries
                    var delCmd = new MySqlCommand(
                        "DELETE FROM tbl_transaction WHERE transaction_id=@id AND type='Vendor Opening Balance'",
                        conn, (MySqlTransaction)tx);
                    delCmd.Parameters.AddWithValue("@id", model.Id);
                    await delCmd.ExecuteNonQueryAsync();

                    // Process Opening Balance (vendor-specific)
                    await ProcessVendorOpeningBalanceAsync(conn, tx, model.Id, model.Code, model, userId);

                    await tx.CommitAsync();
                    return Json(new { status = true, message = "Vendor updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task ProcessVendorOpeningBalanceAsync(
    MySqlConnection conn,
    IDbTransaction tx,
    int vendorId,
    string code,
    VendorRequest model,
    int userId)
        {
            decimal credit = model.Credit ?? 0;
            decimal debit = model.Debit ?? 0;

            // Get Opening Balance Equity account ID
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

            // CREDIT ENTRY
            if (credit != 0)
            {
                // Credit to Opening Balance Equity
                await AddVendorTransactionAsync(conn, tx, model.OpeningBalanceDate, openingEquityId,
                    credit, 0, vendorId, 0,
                    "Vendor Opening Balance", "OPENING BALANCE",
                    $"Opening Balance Equity - Vendor Code - {code}", userId);

                // Debit to Vendor Account
                await AddVendorTransactionAsync(conn, tx, model.OpeningBalanceDate, model.AccountId ?? 0,
                    0, credit, vendorId, vendorId,
                    "Vendor Opening Balance", "OPENING BALANCE",
                    $"Account Payable - Vendor Code - {code}", userId);
            }

            // DEBIT ENTRY
            if (debit != 0)
            {
                // Debit to Opening Balance Equity
                await AddVendorTransactionAsync(conn, tx, model.OpeningBalanceDate, openingEquityId,
                    0, debit, vendorId, 0,
                    "Vendor Opening Balance", "OPENING BALANCE",
                    $"Opening Balance Equity - Vendor Code - {code}", userId);

                // Credit to Vendor Account
                await AddVendorTransactionAsync(conn, tx, model.OpeningBalanceDate, model.AccountId ?? 0,
                    debit, 0, vendorId, vendorId,
                    "Vendor Opening Balance", "OPENING BALANCE",
                    $"Account Payable - Vendor Code - {code}", userId);
            }
        }

        private async Task AddVendorTransactionAsync(
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

        [HttpGet]
        public async Task<IActionResult> GetVendorInvoices(int id)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };
                //
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
                t.credit,
                0 AS Balance 
            FROM tbl_transaction t
            INNER JOIN tbl_coa_level_4 ta ON t.account_id = ta.id
            WHERE t.hum_id = @id
              AND t.state = 0
              AND t.type IN (
                  'Vendor Payment', 
                  'Petty Cash', 
                  'Purchase Invoice', 
                  'Purchase Invoice Cash', 
                  'Vendor Opening Balance',
                  'Vendor Advance Payment',
                  'Check Cancel (Vendor)', 
                  'Purchase Return Invoice', 
                  'Debit Note', 
                  'PDC Payable'
              )
            ORDER BY t.id;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();
                var transactions = new List<object>();
                int snCounter = 1;
                decimal balance = 0;

                while (await reader.ReadAsync())
                {
                    string invoiceIdStr = reader["InvoiceId"]?.ToString() ?? "0";
                    int invoiceId = 0;
                    int.TryParse(invoiceIdStr, out invoiceId);

                    decimal debit = reader.IsDBNull(reader.GetOrdinal("debit")) ? 0 : reader.GetDecimal("debit");
                    decimal credit = reader.IsDBNull(reader.GetOrdinal("credit")) ? 0 : reader.GetDecimal("credit");
                    balance += credit - debit; 

                    transactions.Add(new
                    {
                        SN = snCounter++,
                        Id = Convert.ToInt32(reader["id"]),
                        InvoiceId = invoiceId,
                        Date = reader.GetDateTime("date").ToString("yyyy-MM-dd"),
                        VoucherNo = reader["VoucherNo"]?.ToString() ?? $"GV-00{invoiceId}",
                        Type = reader["type"]?.ToString() ?? "",
                        Description = reader["Description"]?.ToString() ?? "",
                        Debit = debit.ToString("N2"),
                        Credit = credit.ToString("N2"),
                        Balance = balance.ToString("N2")
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
        public async Task<IActionResult> GetVendorCashInvoices(int id)
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

                string query = @"
            SELECT 
                t.id,
                t.transaction_id AS InvoiceId,
                t.voucher_no AS VoucherNo,
                t.date,
                CONCAT(ta.code, ' - ', ta.name) AS AccountName,
                t.type,
                CASE 
                    WHEN t.type = 'Purchase Invoice Cash' 
                        THEN IF(t.debit = 0, t.credit, t.debit)
                    ELSE 0
                END AS Amount
            FROM tbl_transaction t
            INNER JOIN tbl_coa_level_4 ta ON t.account_id = ta.id
            WHERE t.hum_id = @id AND t.state = 0";

                if (includeTypes.Any())
                    query += " AND (" + string.Join(" OR ", includeTypes.Select((t, i) => $"t.type LIKE @include{i}")) + ")";

                query += " ORDER BY t.date, t.id;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                for (int i = 0; i < includeTypes.Count; i++)
                    cmd.Parameters.AddWithValue($"@include{i}", includeTypes[i]);

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
                        VoucherNo = reader["VoucherNo"]?.ToString() ?? $"GV-00{invoiceId}",
                        Date = reader.GetDateTime("date").ToString("yyyy-MM-dd"),
                        AccountName = reader["AccountName"]?.ToString() ?? "",
                        Type = reader["type"]?.ToString() ?? "",
                        Amount = amount.ToString("F2")
                    });
                }

                // Add total row
                invoices.Add(new
                {
                    SN = "Total",
                    InvoiceId = "",
                    VoucherNo = "",
                    Date = "",
                    AccountName = "Total",
                    Type = "",
                    Amount = totalAmount.ToString("F2")
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
    bool isSubcontractor = false,
    string selectionMethod = "Default",
    int page = 1,
    int pageSize = 20)
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

                // Vendor type
                query += " AND v.type = @vendorType ";
                parameters.Add(new MySqlParameter("@vendorType",
                    isSubcontractor ? "Subcontractor" : "Vendor"));

                // Filters
                if (customerId.HasValue)
                {
                    query += " AND p.vendor_id = @vendorId ";
                    parameters.Add(new MySqlParameter("@vendorId", customerId.Value));
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query += " AND p.payment_method = @payment ";
                    parameters.Add(new MySqlParameter("@payment", paymentMethod));
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

                // Grouping
                query += selectionMethod == "Default"
                    ? @" GROUP BY p.id, p.date, p.invoice_id, v.code, v.name, p.total, p.vat, p.net "
                    : @" GROUP BY p.id, p.date, p.invoice_id, v.code, v.name,
                      p.total, p.vat, p.net,
                      i.code, i.name, d.qty, d.cost_price, d.vat, d.total ";

                // Pagination
                int offset = (page - 1) * pageSize;
                query += " ORDER BY p.date DESC LIMIT @limit OFFSET @offset ";

                parameters.Add(new MySqlParameter("@limit", pageSize));
                parameters.Add(new MySqlParameter("@offset", offset));

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
                            Total = reader.GetDecimal("Total"),
                            Vat = reader.GetDecimal("Vat"),
                            Net = reader["Net"] != DBNull.Value ? reader.GetDecimal("Net") : 0,
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

                    // Item (only in Detailed mode)
                    if (reader["ItemId"] != DBNull.Value)
                    {
                        currentPurchase.Items.Add(new PurchaseItemDto
                        {
                            ItemId = reader["ItemId"] != DBNull.Value ? Convert.ToInt32(reader["ItemId"]) : 0,
                            ItemCode = reader["ItemCode"]?.ToString(),
                            ItemName = reader["ItemName"]?.ToString(),
                            Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                            CostPrice = reader["CostPrice"] != DBNull.Value ? Convert.ToDecimal(reader["CostPrice"]) : 0,
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
    i.id AS ItemId,
    pd.cost_center_id AS Cost_Center_Id
FROM tbl_purchase p
INNER JOIN tbl_vendor v ON p.vendor_id = v.id
LEFT JOIN tbl_purchase_details pd ON p.id = pd.purchase_id
LEFT JOIN tbl_items i ON pd.item_id = i.id
WHERE p.state = 0;
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
    p.vatp AS VatP,
    p.net AS Net,
    v.id AS VendorId,
    CONCAT(i.code,' - ',i.name) AS ItemName,
    d.qty AS Qty,
    d.cost_price AS CostPrice,
    d.vat AS ItemVat,
      i.code AS ItemCode,
    d.total AS ItemTotal,
    p.warehouse_id AS Warehouse_Id,
    p.po_num AS PO_Num,
    p.bill_to AS Bill_To,
    p.purchase_type AS PurchaseType,
    p.fixed_asset_category_id As FixedAssetCategoryId,
    p.city AS City,
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
WHERE p.state = 0;

    ";
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseItems(int purchaseId)
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

                var query = @"
            SELECT
                CONCAT(i.code,' - ',i.name) AS ItemName,
                d.qty AS Qty,
                d.cost_price AS CostPrice,
                d.vat AS Vat,
                d.total AS Total
            FROM tbl_purchase_details d
            INNER JOIN tbl_items i ON d.item_id = i.id
            WHERE d.purchase_id = @id
        ";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", purchaseId);

                var items = new List<PurchaseItemDto>();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new PurchaseItemDto
                    {
                        ItemName = reader["ItemName"]?.ToString(),
                        Qty = Convert.ToDecimal(reader["Qty"]),
                        CostPrice = Convert.ToDecimal(reader["CostPrice"]),
                        Vat = Convert.ToDecimal(reader["Vat"]),
                        Total = Convert.ToDecimal(reader["Total"])
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
        public async Task<IActionResult> GetVendorAndSub()
        {
            try
            {
                // Validate user session
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                // Build MySQL connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ✅ Main query (NO TYPE, NO STATE FILTER)
                string query = @"
            SELECT 
                c.id,
                CONCAT(c.code, ' - ', c.name) AS Name,
                c.work_phone,
                c.main_phone,
                tc.name AS Category,
                c.region,
                c.email,
                c.trn,
                c.mobile,
                c.Cat_id,
                c.ccemail,
                c.website,
                c.country,
                c.date,
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
                    state = 0
                    AND type IN (
                        'Vendor Payment',
                        'Petty Cash',
                        'Purchase Invoice',
                        'Vendor Opening Balance',
                        'Vendor Advance Payment',
                        'Check Cancel (Vendor)',
                        'Purchase Return Invoice',
                        'Debit Note',
                        'PDC Payable'
                    )
                GROUP BY hum_id
            ) t ON t.hum_id = c.id
            LEFT JOIN tbl_vendor_category tc ON c.Cat_id = tc.id
        ";

                await using var cmd = new MySqlCommand(query, conn);
                await using var reader = await cmd.ExecuteReaderAsync();

                var vendors = new List<object>();

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

                    vendors.Add(new
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
                        Date = reader["date"] != DBNull.Value ? Convert.ToDateTime(reader["date"]) : (DateTime?)null,
                        Active = reader["active"] != DBNull.Value ? Convert.ToInt32(reader["active"]) : 0
                    });
                }

                return Ok(new
                {
                    status = true,
                    data = vendors
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
        public async Task<IActionResult> SavePurchaseInvoice([FromBody] PurchaseInvoiceRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            if (model.Items == null || !model.Items.Any())
                return Json(new { status = false, message = "Can't Save Empty Purchase Invoice" });

            if (!await AreDefaultAccountsSet(new List<string> { "Vendor", "Purchase", "Vat Input", "Inventory" }))
                return Json(new { status = false, message = "Default accounts for invoice are not properly configured. Please check your settings." });

            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.Total <= 0)
                    return Json(new { status = false, message = $"Total Item in Row {i + 1} can't be 0 or null." });
            }

            if (model.VendorId <= 0)
                return Json(new { status = false, message = "Vendor must be selected." });

            if (model.AccountCashId <= 0)
                return Json(new { status = false, message = "Account Cash Name must be selected." });

            if (string.IsNullOrEmpty(model.PaymentMethod))
                return Json(new { status = false, message = "Payment Method must be selected." });

            if (model.WarehouseId <= 0)
                return Json(new { status = false, message = "Warehouse must be selected." });

            if (model.NetTotal <= 0)
                return Json(new { status = false, message = "Total must be greater than zero." });

            // Get account IDs from configuration
            var accountIds = await GetDefaultAccountIds();
            if (accountIds.PaymentCreditMethodId <= 0 || accountIds.VatId <= 0 ||
                accountIds.PurchaseInvoiceId <= 0 || accountIds.InventoryId <= 0)
                return Json(new { status = false, message = "Default accounts for invoice are not properly configured. Please check your settings." });

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
                await using var transaction = await conn.BeginTransactionAsync();

                try
                {
                    int purchaseId = model.Id;
                    decimal paidAmount = model.PaymentMethod == "Cash" ? model.NetTotal : 0;
                    decimal changeAmount = model.PaymentMethod != "Cash" ? model.NetTotal : 0;

                    if (purchaseId == 0) // 🔹 INSERT NEW PURCHASE INVOICE
                    {
                        model.InvoiceCode = await GenerateNextPurchaseCode(conn, transaction);

                        var insertQuery = @"
INSERT INTO tbl_purchase
(date, vendor_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man,
ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date,
total, vat, net, pay, `change`, created_by, created_date, state, purchase_type, fixed_asset_category_id, description)
VALUES
(@date, @vendor_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man,
@ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms, @payment_date,
@total, @vat, @net, @pay, @change, @created_by, @created_date, 0, @purchase_type, @fixed_asset_category_id, @description);
SELECT LAST_INSERT_ID();";

                        await using var cmd = new MySqlCommand(insertQuery, conn, transaction);
                        cmd.Parameters.AddWithValue("@date", model.Date.Date);
                        cmd.Parameters.AddWithValue("@vendor_id", model.VendorId);
                        cmd.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
                        cmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                        cmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                        cmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                        cmd.Parameters.AddWithValue("@city", model.City ?? "");
                        cmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                        cmd.Parameters.AddWithValue("@ship_date", model.ShipDate.Date);
                        cmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                        cmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                        cmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod);
                        cmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                        cmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                        cmd.Parameters.AddWithValue("@payment_date", model.PaymentDate.Date);
                        cmd.Parameters.AddWithValue("@total", model.TotalBefore);
                        cmd.Parameters.AddWithValue("@vat", model.Vat);
                        cmd.Parameters.AddWithValue("@net", model.NetTotal);
                        cmd.Parameters.AddWithValue("@pay", paidAmount);
                        cmd.Parameters.AddWithValue("@change", changeAmount);
                        cmd.Parameters.AddWithValue("@created_by", userId);
                        cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@purchase_type", model.PurchaseType ?? "");
                        cmd.Parameters.AddWithValue("@fixed_asset_category_id", model.FixedAssetCategoryId);
                        cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                        purchaseId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                        // Insert purchase items
                        await InsertPurchaseItems(conn, transaction, purchaseId, model.Items, model.Date,
                            model.InvoiceCode, model.WarehouseId, accountIds.InventoryId);

                        // Insert transactions
                        await InsertTransactions(conn, transaction, purchaseId, model, userId, accountIds);

                        // Update purchase order if applicable
                        if (!string.IsNullOrEmpty(model.PONumber))
                        {
                            await using var poCmd = new MySqlCommand(
                                @"UPDATE tbl_purchase_order SET tranfer_status=1, purchase_id=@id WHERE id=@poId",
                                conn, transaction);
                            poCmd.Parameters.AddWithValue("@poId", model.PONumber);
                            poCmd.Parameters.AddWithValue("@id", purchaseId);
                            await poCmd.ExecuteNonQueryAsync();
                        }

                        // Update or add fixed assets if applicable
                        if (purchaseId > 0 && model.FixedAssetCategoryId > 0)
                        {
                            await UpdateOrAddFixedAssets(conn, transaction, purchaseId, model.FixedAssetCategoryId);
                        }

                    }
                    else // 🔹 UPDATE EXISTING PURCHASE INVOICE
                    {
                        // Check for existing payments if credit
                        if (model.PaymentMethod != "Cash")
                        {
                            var checkPaymentQuery = @"SELECT IFNULL(SUM(payment), 0) FROM tbl_payment_voucher_details WHERE inv_id = @id";
                            await using var payCmd = new MySqlCommand(checkPaymentQuery, conn, transaction);
                            payCmd.Parameters.AddWithValue("@id", purchaseId);
                            var oldPaymentTotal = Convert.ToDecimal(await payCmd.ExecuteScalarAsync());

                            if (oldPaymentTotal > 0)
                            {
                                paidAmount = oldPaymentTotal;
                            }
                            else
                            {
                                paidAmount = 0;
                            }
                            changeAmount = model.NetTotal;
                        }
                        else
                        {
                            paidAmount = model.NetTotal;
                            changeAmount = 0;
                        }

                        var updateQuery = @"
UPDATE tbl_purchase SET
modified_by=@modified_by, modified_date=@modified_date, date=@date, sales_man=@sales_man,
vendor_id=@vendor_id, invoice_id=@invoice_id, warehouse_id=@warehouse_id,
po_num=@po_num, bill_to=@bill_to, ship_date=@ship_date,
ship_via=@ship_via, ship_to=@ship_to, payment_method=@payment_method, account_cash_id=@account_cash_id,
payment_terms=@payment_terms, payment_date=@payment_date, total=@total,
vat=@vat, net=@net, pay=@pay, `change`=@change, city=@city,
purchase_type=@purchase_type, fixed_asset_category_id=@fixed_asset_category_id, description=@description
WHERE id=@id;";

                        await using var cmd = new MySqlCommand(updateQuery, conn, transaction);
                        cmd.Parameters.AddWithValue("@id", purchaseId);
                        cmd.Parameters.AddWithValue("@date", model.Date.Date);
                        cmd.Parameters.AddWithValue("@vendor_id", model.VendorId);
                        cmd.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
                        cmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                        cmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                        cmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                        cmd.Parameters.AddWithValue("@city", model.City ?? "");
                        cmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                        cmd.Parameters.AddWithValue("@ship_date", model.ShipDate.Date);
                        cmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                        cmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                        cmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod);
                        cmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                        cmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                        cmd.Parameters.AddWithValue("@payment_date", model.PaymentDate.Date);
                        cmd.Parameters.AddWithValue("@total", model.TotalBefore);
                        cmd.Parameters.AddWithValue("@vat", model.Vat);
                        cmd.Parameters.AddWithValue("@net", model.NetTotal);
                        cmd.Parameters.AddWithValue("@pay", paidAmount);
                        cmd.Parameters.AddWithValue("@change", changeAmount);
                        cmd.Parameters.AddWithValue("@modified_by", userId);
                        cmd.Parameters.AddWithValue("@modified_date", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@purchase_type", model.PurchaseType ?? "");
                        cmd.Parameters.AddWithValue("@fixed_asset_category_id", model.FixedAssetCategoryId);
                        cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                        await cmd.ExecuteNonQueryAsync();

                        // Delete previous data
                        await ReturnItemsToInventory(conn, transaction, purchaseId);
                        await DeleteCostCenterTransactionEntry(conn, transaction, purchaseId.ToString(), "Purchase");
                        await DeleteTransactionEntry(conn, transaction, purchaseId, "Purchase Invoice");

                        // Re-insert items and transactions
                        await InsertPurchaseItems(conn, transaction, purchaseId, model.Items, model.Date,
                            model.InvoiceCode, model.WarehouseId, accountIds.InventoryId);
                        await InsertTransactions(conn, transaction, purchaseId, model, userId, accountIds);

                        // Update or add fixed assets if applicable
                        if (purchaseId > 0 && model.FixedAssetCategoryId > 0)
                        {
                            await UpdateOrAddFixedAssets(conn, transaction, purchaseId, model.FixedAssetCategoryId);
                        }
                    }

                    await transaction.CommitAsync();

                    return Json(new
                    {
                        status = true,
                        message = model.Id == 0 ? "Purchase Invoice saved successfully" : "Purchase Invoice updated successfully",
                        id = purchaseId,
                        invoiceCode = model.InvoiceCode
                    });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
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

        private async Task<DefaultAccountIds> GetDefaultAccountIds()
        {
            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            var accountIds = new DefaultAccountIds();

            var query = @"SELECT category, account_id FROM tbl_coa_config 
                         WHERE category IN ('Vendor', 'Purchase', 'Vat Input', 'Inventory')";

            await using var cmd = new MySqlCommand(query, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var category = reader["category"].ToString();
                var accountId = Convert.ToInt32(reader["account_id"]);

                switch (category)
                {
                    case "Vendor":
                        accountIds.PaymentCreditMethodId = accountId;
                        break;
                    case "Purchase":
                        accountIds.PurchaseInvoiceId = accountId;
                        break;
                    case "Vat Input":
                        accountIds.VatId = accountId;
                        break;
                    case "Inventory":
                        accountIds.InventoryId = accountId;
                        break;
                }
            }

            return accountIds;
        }

        private async Task InsertPurchaseItems(MySqlConnection conn, MySqlTransaction transaction, int purchaseId,
            List<PurchaseItemRequest> items, DateTime invoiceDate, string invoiceCode, int warehouseId, int inventoryAccountId)
        {
            // Batch insert items
            var valueList = new List<string>();
            var parameters = new List<MySqlParameter>();
            int paramIndex = 0;

            foreach (var item in items)
            {
                valueList.Add($"(@purchase_id, @item_id{paramIndex}, @qty{paramIndex}, @cost_price{paramIndex}, " +
                             $"@price{paramIndex}, @discount{paramIndex}, @vatp{paramIndex}, @vat{paramIndex}, " +
                             $"@total{paramIndex}, @cost_center{paramIndex})");

                parameters.Add(new MySqlParameter($"@item_id{paramIndex}", item.ItemId));
                parameters.Add(new MySqlParameter($"@qty{paramIndex}", item.Quantity));
                parameters.Add(new MySqlParameter($"@cost_price{paramIndex}", item.CostPrice));
                parameters.Add(new MySqlParameter($"@price{paramIndex}", item.Price));
                parameters.Add(new MySqlParameter($"@discount{paramIndex}", item.Discount));
                parameters.Add(new MySqlParameter($"@vatp{paramIndex}", item.VatPercentage));
                parameters.Add(new MySqlParameter($"@vat{paramIndex}", item.Vat));
                parameters.Add(new MySqlParameter($"@total{paramIndex}", item.Total));
                parameters.Add(new MySqlParameter($"@cost_center{paramIndex}", item.CostCenterId));

                paramIndex++;
            }

            if (valueList.Count > 0)
            {
                string sql = $@"INSERT INTO tbl_purchase_details
                    (purchase_id, item_id, qty, cost_price, price, discount, vatp, vat, total, cost_center_id)
                    VALUES {string.Join(", ", valueList)};";

                await using var cmd = new MySqlCommand(sql, conn, transaction);
                cmd.Parameters.AddWithValue("@purchase_id", purchaseId);
                foreach (var p in parameters)
                    cmd.Parameters.Add(p);

                await cmd.ExecuteNonQueryAsync();
            }

            // Insert item transactions and cost center transactions
            foreach (var item in items)
            {
                if (item.Type != "12 - Service")
                {
                    await InsertItemTransaction(conn, transaction, invoiceDate, purchaseId, invoiceCode,
                        item, warehouseId);
                }

                if (item.CostCenterId > 0)
                {
                    await InsertCostCenterTransaction(conn, transaction, invoiceDate, item.Total.ToString(),
                        "0", purchaseId.ToString(), "Purchase", "", item.CostCenterId.ToString());
                }
            }
        }

        private async Task InsertItemTransaction(MySqlConnection conn, MySqlTransaction transaction,
            DateTime date, int purchaseId, string invoiceCode, PurchaseItemRequest item, int warehouseId)
        {
            // Insert item transaction
            var insertTxnQuery = @"INSERT INTO tbl_item_transaction 
                (date, type, reference, item_id, cost_price, qty_in, sales_price, qty_out, qty_inc, description, warehouse_id) 
                VALUES (@date, @type, @reference, @itemId, @costPrice, @qtyIn, @salesPrice, @qtyOut, @qtyInc, @description, @warehouseId);";

            await using var txnCmd = new MySqlCommand(insertTxnQuery, conn, transaction);
            txnCmd.Parameters.AddWithValue("@date", date.Date);
            txnCmd.Parameters.AddWithValue("@type", "Purchase Invoice");
            txnCmd.Parameters.AddWithValue("@reference", purchaseId.ToString());
            txnCmd.Parameters.AddWithValue("@itemId", item.ItemId);
            txnCmd.Parameters.AddWithValue("@costPrice", item.Price);
            txnCmd.Parameters.AddWithValue("@qtyIn", item.Quantity);
            txnCmd.Parameters.AddWithValue("@salesPrice", 0);
            txnCmd.Parameters.AddWithValue("@qtyOut", 0);
            txnCmd.Parameters.AddWithValue("@qtyInc", item.Quantity);
            txnCmd.Parameters.AddWithValue("@description", $"Purchase Invoice No. {invoiceCode}");
            txnCmd.Parameters.AddWithValue("@warehouseId", warehouseId);

            await txnCmd.ExecuteNonQueryAsync();

            // Update on_hand quantity
            await UpdateOnHandItem(conn, transaction, item.ItemId);

            // Add item card details
            await AddItemCardDetails(conn, transaction, date, "Purchase Invoice", purchaseId.ToString(),
                item.ItemId, item.CostPrice, item.Quantity, 0, 0, item.Quantity,
                $"Purchase Invoice No. {invoiceCode}", warehouseId);
        }

        private async Task UpdateOnHandItem(MySqlConnection conn, MySqlTransaction transaction, int itemId)
        {
            var query = @"UPDATE tbl_items SET on_hand = (SELECT IFNULL(SUM(qty_in - qty_out), 0) FROM tbl_item_transaction WHERE item_id = @itemId) WHERE id = @itemId";

            await using var cmd = new MySqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@itemId", itemId);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task AddItemCardDetails(MySqlConnection conn, MySqlTransaction transaction,
            DateTime date, string type, string reference, int itemId, decimal costPrice,
            decimal qtyIn, decimal salesPrice, decimal qtyOut, decimal qtyInc, string description, int warehouseId)
        {
            string invoiceNo = "INV-" + reference;
            decimal debit = qtyIn * costPrice;
            decimal credit = qtyOut * costPrice;

            // Get current balances
            var balanceQuery = @"SELECT 
                IFNULL(SUM(qty_in - qty_out), 0) as QtyBalance,
                IFNULL(SUM(debit - credit), 0) as Balance
                FROM tbl_item_card_details WHERE itemId = @id";

            decimal qtyBalance = 0, balance = 0;

            await using (var balCmd = new MySqlCommand(balanceQuery, conn, transaction))
            {
                balCmd.Parameters.AddWithValue("@id", itemId);
                await using var reader = await balCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    qtyBalance = reader.GetDecimal(0) + (qtyIn - qtyOut);
                    balance = reader.GetDecimal(1) + (debit - credit);
                }
            }

            var insertQuery = @"INSERT INTO tbl_item_card_details 
                (itemId, date, wharehouse_id, inv_no, trans_no, trans_type, description,
                price, qty_in, qty_out, qty_balance, debit, credit, balance, fifo_qty, fifo_cost)
                VALUES (@itemId, @date, @wharehouse_id, @inv_no, @trans_no, @trans_type, @description,
                @price, @qty_in, @qty_out, @qty_balance, @debit, @credit, @balance, @fifo_qty, @fifo_cost);";

            await using var cmd = new MySqlCommand(insertQuery, conn, transaction);
            cmd.Parameters.AddWithValue("@itemId", itemId);
            cmd.Parameters.AddWithValue("@date", date.Date);
            cmd.Parameters.AddWithValue("@wharehouse_id", warehouseId);
            cmd.Parameters.AddWithValue("@inv_no", invoiceNo);
            cmd.Parameters.AddWithValue("@trans_no", reference);
            cmd.Parameters.AddWithValue("@trans_type", type);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@price", costPrice);
            cmd.Parameters.AddWithValue("@qty_in", qtyIn);
            cmd.Parameters.AddWithValue("@qty_out", qtyOut);
            cmd.Parameters.AddWithValue("@qty_balance", qtyBalance);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@balance", balance);
            cmd.Parameters.AddWithValue("@fifo_qty", 0);
            cmd.Parameters.AddWithValue("@fifo_cost", 0);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertCostCenterTransaction(MySqlConnection conn, MySqlTransaction transaction,
            DateTime date, string debit, string credit, string refId, string type, string description, string costCenterId)
        {
            var query = @"INSERT INTO tbl_cost_center_transaction 
                (type, date, ref_id, debit, credit, description, cost_center_id) 
                VALUES (@type, @date, @ref, @debit, @credit, @description, @cost_center_id);";

            await using var cmd = new MySqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@date", date.Date);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@ref", refId);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@cost_center_id", costCenterId);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertTransactions(MySqlConnection conn, MySqlTransaction transaction,
            int purchaseId, PurchaseInvoiceRequest model, int userId, DefaultAccountIds accountIds)
        {
            string accountId = model.PaymentMethod == "Credit"
                ? accountIds.PaymentCreditMethodId.ToString()
                : model.AccountCashId.ToString();

            // Main transaction entry
            if (model.NetTotal > 0)
            {
                await AddTransactionEntry(conn, transaction, model.Date.Date, accountId, "0",
                    model.NetTotal.ToString(), purchaseId.ToString(), model.VendorId.ToString(),
                    model.PaymentMethod == "Credit" ? "Purchase Invoice" : "Purchase Invoice Cash",
                    "Purchase Invoice Cash", $"Purchase Invoice NO. {model.InvoiceCode}", userId, DateTime.Now.Date, model.InvoiceCode);
            }

            // VAT transaction
            if (model.Vat > 0)
            {
                await AddTransactionEntry(conn, transaction, model.Date.Date, accountIds.VatId.ToString(),
                    model.Vat.ToString(), "0", purchaseId.ToString(), "0", "Purchase Invoice", "Purchase Invoice",
                    $"Vat Input For Invoice No. {model.InvoiceCode}", userId, DateTime.Now.Date, model.InvoiceCode);
            }

            // Inventory transaction
            if (model.TotalBefore > 0)
            {
                await AddTransactionEntry(conn, transaction, model.Date.Date, accountIds.InventoryId.ToString(),
                    model.TotalBefore.ToString(), "0", purchaseId.ToString(), "0", "Purchase Invoice", "Purchase Invoice",
                    $"Purchase For Invoice No. {model.InvoiceCode}", userId, DateTime.Now.Date, model.InvoiceCode);
            }
        }

        private async Task AddTransactionEntry(MySqlConnection conn, MySqlTransaction transaction,
            DateTime date, string accountId, string debit, string credit, string transactionId,
            string humId, string voucherName, string type, string description, int createdBy, DateTime createdDate, string voucherNo)
        {
            var query = @"INSERT INTO tbl_transaction 
                (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no) 
                VALUES (@date, @accountId, @debit, @credit, @transactionId, @hum_id, @tType, @type, @description, @createdBy, @createdDate, 0, @voucher_no);";

            await using var cmd = new MySqlCommand(query, conn, transaction);
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

        private async Task ReturnItemsToInventory(MySqlConnection conn, MySqlTransaction transaction, int purchaseId)
        {
            var deleteQuery = @"
                DELETE FROM tbl_item_transaction WHERE reference = @id AND type = 'Purchase Invoice';
                DELETE FROM tbl_item_card_details WHERE trans_type = 'Purchase Invoice' AND trans_no = @id;
                DELETE FROM tbl_purchase_details WHERE purchase_id = @id;";

            await using var cmd = new MySqlCommand(deleteQuery, conn, transaction);
            cmd.Parameters.AddWithValue("@id", purchaseId);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task DeleteCostCenterTransactionEntry(MySqlConnection conn, MySqlTransaction transaction,
            string refId, string type)
        {
            var query = @"DELETE FROM tbl_cost_center_transaction WHERE type = @type AND ref_id = @id;";

            await using var cmd = new MySqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@id", refId);
            cmd.Parameters.AddWithValue("@type", type);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task DeleteTransactionEntry(MySqlConnection conn, MySqlTransaction transaction,
            int id, string type)
        {
            var query = @"DELETE FROM tbl_transaction WHERE t_type = @tType AND transaction_id = @id;";

            await using var cmd = new MySqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@tType", type);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task UpdateOrAddFixedAssets(MySqlConnection conn, MySqlTransaction transaction,
            int purchaseId, int categoryId)
        {
            if (conn == null) throw new ArgumentNullException(nameof(conn));
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            // 1️⃣ Load purchase
            var purchaseQuery = @"
        SELECT p.net AS PurchasePrice,
               v.name AS SupplierName,
               p.invoice_id AS VoucherNo,
               p.date AS PurchaseDate
        FROM tbl_purchase p
        INNER JOIN tbl_vendor v ON p.vendor_id = v.id
        WHERE p.id = @purchaseId";

            decimal purchasePrice;
            string supplierName;
            string voucherNo;
            DateTime purchaseDate;

            await using (var cmd = new MySqlCommand(purchaseQuery, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@purchaseId", purchaseId);
                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) throw new Exception("Purchase not found");

                purchasePrice = reader.GetDecimal("PurchasePrice");
                supplierName = reader.GetString("SupplierName");
                voucherNo = reader.GetString("VoucherNo");
                purchaseDate = reader.GetDateTime("PurchaseDate");
            }

            // 2️⃣ Check if fixed asset already exists
            int oldAssetId = 0;
            var checkQuery = @"
        SELECT id FROM tbl_fixed_assets
        WHERE purchase_date = @date AND supplier = @supplier AND invoice_number = @voucherNo";

            await using (var cmd = new MySqlCommand(checkQuery, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@date", purchaseDate);
                cmd.Parameters.AddWithValue("@supplier", supplierName);
                cmd.Parameters.AddWithValue("@voucherNo", voucherNo);

                var result = await cmd.ExecuteScalarAsync();
                if (result != null) oldAssetId = Convert.ToInt32(result);
            }

            // 3️⃣ Load category info
            string assetName;
            int assetsAccount = 0, depreciationAccount = 0, expenseAccount = 0;

            var categoryQuery = @"SELECT * FROM tbl_fixed_assets_category WHERE id=@id";
            await using (var cmd = new MySqlCommand(categoryQuery, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@id", categoryId);
                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) throw new Exception("Invalid category");

                assetName = reader["category_name"].ToString();
                if (reader["assets_account_id"] != DBNull.Value) assetsAccount = Convert.ToInt32(reader["assets_account_id"]);
                if (reader["depreciation_account_id"] != DBNull.Value) depreciationAccount = Convert.ToInt32(reader["depreciation_account_id"]);
                if (reader["expence_account_id"] != DBNull.Value) expenseAccount = Convert.ToInt32(reader["expence_account_id"]);
            }

            // 4️⃣ Update or Insert
            if (oldAssetId > 0)
            {
                // UPDATE
                var updateQuery = @"
            UPDATE tbl_fixed_assets
            SET date=@date,
                category_id=@categoryId,
                supplier=@supplier,
                status='Draft',
                invoice_number=@invoice,
                purchase_date=@purchaseDate,
                end_date=@endDate,
                depreciation_life=0,
                purchase_price=@purchasePrice,
                debit_account_id=@debit,
                credit_account_id=@credit,
                expence_account_id=@expense,
                modified_by=@user,
                modified_date=@now
            WHERE id=@id";

                await using var cmd = new MySqlCommand(updateQuery, conn, transaction);
                cmd.Parameters.AddWithValue("@id", oldAssetId);
                cmd.Parameters.AddWithValue("@date", purchaseDate);
                cmd.Parameters.AddWithValue("@categoryId", categoryId);
                cmd.Parameters.AddWithValue("@supplier", supplierName);
                cmd.Parameters.AddWithValue("@invoice", voucherNo);
                cmd.Parameters.AddWithValue("@purchaseDate", purchaseDate);
                cmd.Parameters.AddWithValue("@endDate", purchaseDate);
                cmd.Parameters.AddWithValue("@purchasePrice", purchasePrice);
                cmd.Parameters.AddWithValue("@debit", assetsAccount);
                cmd.Parameters.AddWithValue("@credit", depreciationAccount);
                cmd.Parameters.AddWithValue("@expense", expenseAccount);
                cmd.Parameters.AddWithValue("@user", HttpContext.Session.GetInt32("UserId") ?? 0);
                cmd.Parameters.AddWithValue("@now", DateTime.Now);

                await cmd.ExecuteNonQueryAsync();
            }
            else
            {
                int code = 1;
                var codeQuery = "SELECT IFNULL(MAX(CAST(code AS UNSIGNED)),0)+1 FROM tbl_fixed_assets";
                await using var codeCmd = new MySqlCommand(codeQuery, conn, transaction);
                var codeResult = await codeCmd.ExecuteScalarAsync();
                code = Convert.ToInt32(codeResult);

                var insertQuery = @"
            INSERT INTO tbl_fixed_assets
            (date, code, name, brand, category_id, model, supplier, status,
             invoice_number, purchase_date, end_date, depreciation_life, purchase_price,
             debit_account_id, credit_account_id, expence_account_id, created_by, created_date, state)
            VALUES
            (@date, @code, @name, '', @categoryId, '', @supplier, 'Draft',
             @invoice, @purchaseDate, @endDate, 0, @purchasePrice,
             @debit, @credit, @expense, @user, @now, 0)";

                await using var cmd = new MySqlCommand(insertQuery, conn, transaction);
                cmd.Parameters.AddWithValue("@date", purchaseDate);
                cmd.Parameters.AddWithValue("@code", code.ToString("D5"));
                cmd.Parameters.AddWithValue("@name", assetName);
                cmd.Parameters.AddWithValue("@categoryId", categoryId);
                cmd.Parameters.AddWithValue("@supplier", supplierName);
                cmd.Parameters.AddWithValue("@invoice", voucherNo);
                cmd.Parameters.AddWithValue("@purchaseDate", purchaseDate);
                cmd.Parameters.AddWithValue("@endDate", purchaseDate);
                cmd.Parameters.AddWithValue("@purchasePrice", purchasePrice);
                cmd.Parameters.AddWithValue("@debit", assetsAccount);
                cmd.Parameters.AddWithValue("@credit", depreciationAccount);
                cmd.Parameters.AddWithValue("@expense", expenseAccount);
                cmd.Parameters.AddWithValue("@user", HttpContext.Session.GetInt32("UserId") ?? 0);
                cmd.Parameters.AddWithValue("@now", DateTime.Now);

                await cmd.ExecuteNonQueryAsync();
            }
        }


        private async Task<string> GenerateNextPurchaseCode(MySqlConnection conn, MySqlTransaction transaction)
        {
            const string prefix = "PI-";
            var query = @"SELECT IFNULL(MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)), 0) 
                         FROM tbl_purchase WHERE invoice_id LIKE 'PI-%';";

            await using var cmd = new MySqlCommand(query, conn, transaction);
            var lastNumber = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            return $"{prefix}{(lastNumber + 1):D4}";
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseInvoiceReport(int purchaseId)
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

                // 🔹 PURCHASE HEADER + VENDOR
                var purchaseCmd = new MySqlCommand(@"
SELECT 
    p.id,
    p.date,
    p.invoice_id,
    p.bill_to,
    p.city,
    p.sales_man,
    p.ship_date,
    p.ship_via,
    p.ship_to,
    p.po_num,
    p.payment_method,
    p.payment_terms,
    p.payment_date,
    p.total,
    p.vat,
    p.net,
    p.pay,
    p.`change`,
    coa.name AS accountName,

    v.name AS vendorName,
    v.main_phone,
    v.email,
    v.trn,
    v.mobile
FROM tbl_purchase p
INNER JOIN tbl_vendor v ON p.vendor_id = v.id
LEFT JOIN tbl_coa_level_4 coa ON coa.id = p.account_cash_id
WHERE p.id = @purchaseId;
", conn);

                purchaseCmd.Parameters.Add("@purchaseId", MySqlDbType.Int32).Value = purchaseId;

                PurchaseReportDto purchase = null;

                await using (var reader = await purchaseCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        purchase = new PurchaseReportDto
                        {
                            Id = reader.GetInt32("id"),
                            Date = reader.GetDateTime("date"),
                            InvoiceNo = reader.GetString("invoice_id"),
                            VendorName = reader.GetString("vendorName"),
                            BillTo = reader.IsDBNull("bill_to") ? null : reader.GetString("bill_to"),
                            City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                            SalesMan = reader.IsDBNull("sales_man") ? null : reader.GetString("sales_man"),
                            ShipDate = reader.IsDBNull("ship_date") ? (DateTime?)null : reader.GetDateTime("ship_date"),
                            ShipVia = reader.IsDBNull("ship_via") ? null : reader.GetString("ship_via"),
                            ShipTo = reader.IsDBNull("ship_to") ? null : reader.GetString("ship_to"),
                            PoNumber = reader.IsDBNull("po_num") ? null : reader.GetString("po_num"),
                            PaymentMethod = reader.IsDBNull("payment_method") ? null : reader.GetString("payment_method"),
                            PaymentTerms = reader.IsDBNull("payment_terms") ? null : reader.GetString("payment_terms"),
                            PaymentDate = reader.IsDBNull("payment_date") ? (DateTime?)null : reader.GetDateTime("payment_date"),
                            Total = reader.GetDecimal("total"),
                            Vat = reader.GetDecimal("vat"),
                            Net = reader.GetDecimal("net"),
                            Pay = reader.GetDecimal("pay"),
                            Change = reader.GetDecimal("change"),
                            AccountName = reader.IsDBNull("accountName") ? null : reader.GetString("accountName"),

                            VendorPhone = reader.IsDBNull("main_phone") ? null : reader.GetString("main_phone"),
                            VendorEmail = reader.IsDBNull("email") ? null : reader.GetString("email"),
                            VendorTRN = reader.IsDBNull("trn") ? null : reader.GetString("trn"),
                            VendorMobile = reader.IsDBNull("mobile") ? null : reader.GetString("mobile")
                        };
                    }
                }

                if (purchase == null)
                    return NotFound(new { status = false, message = "Purchase invoice not found" });

                // 🔹 PURCHASE ITEMS
                var itemCmd = new MySqlCommand(@"
SELECT 
    d.item_id,
    i.code,
    i.name,
    i.type,
    i.method,
    d.qty,
    d.cost_price,
    d.price,
    d.discount,
    d.vat,
    d.total,
    cc.name AS costCenterName,
    u.name AS unitName
FROM tbl_purchase_details d
INNER JOIN tbl_items i ON d.item_id = i.id
LEFT JOIN tbl_sub_cost_center cc ON cc.id = d.cost_center_id
LEFT JOIN tbl_unit u ON u.id = i.unit_id
WHERE d.purchase_id = @purchaseId;
", conn);

                itemCmd.Parameters.Add("@purchaseId", MySqlDbType.Int32).Value = purchaseId;

                var items = new List<PurchaseItemReportDto>();

                await using (var reader = await itemCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new PurchaseItemReportDto
                        {
                            ItemId = reader.GetInt32("item_id"),
                            Code = reader.GetString("code"),
                            Name = reader.GetString("name"),
                            Qty = Convert.ToDecimal(reader["qty"]),
                            CostPrice = Convert.ToDecimal(reader["cost_price"]),
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
                    return BadRequest(new { status = false, message = "Company info not found" });

                // 🔹 PDF GENERATION
                byte[] pdfBytes = PurchaseInvoicePdfGenerator.Generate(company, purchase, items);

                return File(
                    pdfBytes,
                    "application/pdf",
                    fileDownloadName: null
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseReceiveNoteReport(int purchaseId)
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

                // 🔹 PURCHASE HEADER + VENDOR
                var purchaseCmd = new MySqlCommand(@"
SELECT 
    p.id,
    p.date,
    p.invoice_id,
    p.ship_to,

    v.name AS vendorName,
    v.mobile
FROM tbl_purchase p
INNER JOIN tbl_vendor v ON p.vendor_id = v.id
WHERE p.id = @purchaseId;
", conn);

                purchaseCmd.Parameters.Add("@purchaseId", MySqlDbType.Int32).Value = purchaseId;

                PurchaseReportDto purchase = null;

                await using (var reader = await purchaseCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        purchase = new PurchaseReportDto
                        {
                            Id = reader.GetInt32("id"),
                            Date = reader.GetDateTime("date"),
                            InvoiceNo = reader.GetString("invoice_id"),
                            VendorName = reader.GetString("vendorName"),
                            VendorMobile = reader.IsDBNull("mobile") ? null : reader.GetString("mobile"),
                            ShipTo = reader.IsDBNull("ship_to") ? null : reader.GetString("ship_to")
                        };
                    }
                }

                if (purchase == null)
                    return NotFound(new { status = false, message = "Receive note not found" });

                // 🔹 PURCHASE ITEMS (NO PRICES USED IN PDF)
                var itemCmd = new MySqlCommand(@"
SELECT 
    d.item_id,
    i.name,
    d.qty,
    u.name AS unitName
FROM tbl_purchase_details d
INNER JOIN tbl_items i ON d.item_id = i.id
LEFT JOIN tbl_unit u ON u.id = i.unit_id
WHERE d.purchase_id = @purchaseId;
", conn);

                itemCmd.Parameters.Add("@purchaseId", MySqlDbType.Int32).Value = purchaseId;

                var items = new List<PurchaseItemReportDto>();

                await using (var reader = await itemCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new PurchaseItemReportDto
                        {
                            ItemId = reader.GetInt32("item_id"),
                            Name = reader.GetString("name"),
                            Qty = Convert.ToDecimal(reader["qty"]),
                            UnitName = reader.IsDBNull("unitName") ? null : reader.GetString("unitName")
                        });
                    }
                }

                // 🔹 COMPANY INFO
                var companyCmd = new MySqlCommand(@"
SELECT 
    name,
    phone1,
    address,
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
                            Address = reader.IsDBNull("address") ? null : reader.GetString("address"),
                            Logo = reader.IsDBNull("logoComp") ? null : (byte[])reader["logoComp"]
                        };
                    }
                }

                if (company == null)
                    return BadRequest(new { status = false, message = "Company info not found" });

                // 🔹 PDF GENERATION (RECEIVE NOTE)
                byte[] pdfBytes = PurchaseReceiveNotePdfGenerator.Generate(company, purchase, items);

                return File(
                    pdfBytes,
                    "application/pdf",
                    fileDownloadName: null
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Purchase Order Center

        public IActionResult PurchaseOrder()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchasesOrder(
    DateTime? dateFrom,
    DateTime? dateTo,
    int? customerId,
    string paymentMethod,
    string selectionMethod = "Default",
    int page = 1,
    int pageSize = 20)
        {
            try
            {
                var cs = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(cs.ConnectionString);
                await conn.OpenAsync();

                var parameters = new List<MySqlParameter>();

                string baseQuery = selectionMethod == "Default"
                    ? GetPurchaseDefaultQuerys()
                    : GetPurchaseDetailedQuerys();

                // Filters
                if (customerId.HasValue)
                {
                    baseQuery += " AND p.vendor_id = @vendorId ";
                    parameters.Add(new MySqlParameter("@vendorId", customerId));
                }

                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    baseQuery += " AND p.payment_method = @payment ";
                    parameters.Add(new MySqlParameter("@payment", paymentMethod));
                }

                if (dateFrom.HasValue)
                {
                    baseQuery += " AND p.date >= @dateFrom ";
                    parameters.Add(new MySqlParameter("@dateFrom", dateFrom.Value.Date));
                }

                if (dateTo.HasValue)
                {
                    baseQuery += " AND p.date <= @dateTo ";
                    parameters.Add(new MySqlParameter("@dateTo", dateTo.Value.Date));
                }

                //// Grouping (matches WinForms)
                //baseQuery += selectionMethod == "Default"
                //    ? @" GROUP BY p.id "
                //    : @" GROUP BY p.id, i.id, d.qty, d.cost_price, d.vat, d.total ";

                //// Pagination
                //int offset = (page - 1) * pageSize;
                //baseQuery += " ORDER BY p.date DESC LIMIT @limit OFFSET @offset ";

                //parameters.Add(new MySqlParameter("@limit", pageSize));
                //parameters.Add(new MySqlParameter("@offset", offset));

                var cmd = new MySqlCommand(baseQuery, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                var purchases = new List<PurchaseDto>();
                PurchaseDto current = null;
                int lastId = -1;

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32("Id");

                    if (id != lastId)
                    {
                        current = new PurchaseDto
                        {
                            Id = id,
                            Date = reader.GetDateTime("Date"),
                            InvoiceNo = reader["InvoiceNo"]?.ToString(),
                            VendorName = reader["VendorName"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),
                            Total = reader.GetDecimal("Total"),
                            Vat = reader.GetDecimal("Vat"),
                            Net = reader["Net"] != DBNull.Value ? reader.GetDecimal("Net") : 0,
                            WarehouseId = reader["Warehouse_Id"] != DBNull.Value ? reader.GetInt32("Warehouse_Id") : (int?)null,
                            PO_Num = reader["PO_Num"]?.ToString(),
                            BillTo = reader["Bill_To"]?.ToString(),
                            City = reader["City"]?.ToString(),
                            Ship_Date = reader["Ship_Date"] != DBNull.Value ? reader.GetDateTime("Ship_Date") : (DateTime?)null,
                            Ship_Via = reader["Ship_Via"]?.ToString(),
                            Ship_To = reader["Ship_To"]?.ToString(),
                            Account_Cash_Id = reader["Account_Cash_Id"] != DBNull.Value ? reader.GetInt32("Account_Cash_Id") : (int?)null,
                            Payment_Terms = reader["Payment_Terms"]?.ToString(),
                            Payment_Date = reader["Payment_Date"] != DBNull.Value ? reader.GetDateTime("Payment_Date") : (DateTime?)null,
                            Description = reader["Description"]?.ToString(),
                            SalesMan = reader["Sales_Man"]?.ToString(),
                            ProjectId = reader["ProjectId"] != DBNull.Value ? reader.GetInt32("ProjectId") : (int?)null,
                            Pay = reader["Pay"] != DBNull.Value ? reader.GetDecimal("Pay") : 0,
                            VendorId = reader["VendorId"] != DBNull.Value ? Convert.ToInt32(reader["VendorId"]) : 0,
                            Items = new List<PurchaseItemDto>()
                        };

                        purchases.Add(current);
                        lastId = id;
                    }

                    if (reader["ItemId"] != DBNull.Value)
                    {
                        current.Items.Add(new PurchaseItemDto
                        {
                            ItemId = reader["ItemId"] != DBNull.Value ? Convert.ToInt32(reader["ItemId"]) : 0,
                            ItemCode = reader["ItemCode"]?.ToString(),
                            ItemName = reader["ItemName"]?.ToString(),
                            Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                            CostPrice = reader["CostPrice"] != DBNull.Value ? Convert.ToDecimal(reader["CostPrice"]) : 0,
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

        private string GetPurchaseDefaultQuerys()
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
    p.net AS Net,
    p.purchase_id AS PurchaseNo,
    p.tranfer_status AS TransferStatus,
     pd.item_id AS ItemId,
    i.code AS ItemCode,
    i.name AS ItemName,
    pd.qty AS Qty,
    pd.price AS Price,
    pd.cost_price AS CostPrice,
    pd.vat AS ItemVat,
    pd.vatp AS VatP,
    pd.total AS ItemTotal,
    p.warehouse_id AS Warehouse_Id,
    p.po_num AS PO_Num,
    p.bill_to AS Bill_To,
    p.sales_man AS Sales_Man,
    p.city AS City,
    p.project_id AS ProjectId,
    p.ship_date AS Ship_Date,
    p.ship_via AS Ship_Via,
    p.vendor_id As VendorId,
    p.ship_to AS Ship_To,
    p.account_cash_id AS Account_Cash_Id,
    p.payment_terms AS Payment_Terms,
    p.payment_date AS Payment_Date,
    p.description AS Description,
    p.pay AS Pay,
    i.id AS ItemId,
    pd.cost_center_id AS Cost_Center_Id
FROM tbl_purchase_order p
INNER JOIN tbl_vendor v ON p.vendor_id = v.id
LEFT JOIN tbl_purchase_order_details pd ON p.id = pd.purchase_id
LEFT JOIN tbl_items i ON pd.item_id = i.id
WHERE p.state = 0
";
        }

        private string GetPurchaseDetailedQuerys()
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
    p.net AS Net,
    v.id AS VendorId,
    CONCAT(i.code,' - ',i.name) AS ItemName,
    d.qty AS Qty,
    d.cost_price AS CostPrice,
    d.price AS Price,
    d.vat AS ItemVat,
    d.vatp AS VatP,
      i.code AS ItemCode,
    d.total AS ItemTotal,
    p.warehouse_id AS Warehouse_Id,
    p.po_num AS PO_Num,
    p.bill_to AS Bill_To,
    p.city AS City,
    p.vendor_id As VendorId,
    p.project_id AS ProjectId,
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
    d.cost_center_id AS Cost_Center_Id
FROM tbl_purchase_order p
INNER JOIN tbl_purchase_order_details d ON p.id = d.purchase_id
INNER JOIN tbl_items i ON d.item_id = i.id
INNER JOIN tbl_vendor v ON p.vendor_id = v.id
WHERE p.state = 0
";
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderItems(int purchaseId)
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

                // Query purchase items
                var query = @"
            SELECT
                CONCAT(i.code,' - ',i.name) AS ItemName,
                d.qty AS Qty,
                d.cost_price AS CostPrice,
                d.vat AS Vat,
                d.total AS Total
            FROM tbl_purchase_order_details d
            INNER JOIN tbl_items i ON d.item_id = i.id
            WHERE d.purchase_id = @id;
        ";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", purchaseId);

                var items = new List<PurchaseItemDto>();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    items.Add(new PurchaseItemDto
                    {
                        ItemName = reader["ItemName"]?.ToString(),
                        Qty = reader["Qty"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Qty"]),
                        CostPrice = reader["CostPrice"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["CostPrice"]),
                        Vat = reader["Vat"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Vat"]),
                        Total = reader["Total"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["Total"])
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
        public async Task<IActionResult> SavePurchaseOrder([FromBody] PurchaseOrderRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            // ===== VALIDATION =====
            if (model.VendorId <= 0)
                return Json(new { status = false, message = "Vendor must be selected" });

            if (model.AccountCashId <= 0)
                return Json(new { status = false, message = "Account Cash must be selected" });

            if (model.Items == null || !model.Items.Any())
                return Json(new { status = false, message = "Insert at least one item" });

            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.Total <= 0)
                    return Json(new { status = false, message = $"Total Item in Row {i + 1} can't be 0 or null." });
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in" });

            var connStrBuilder = new MySqlConnectionStringBuilder(
                _config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ??
                           _config.GetConnectionString("DefaultDatabase")
            };

            try
            {
                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();
                await using var tx = await conn.BeginTransactionAsync();

                long purchaseId = model.Id;
                string invoiceId = model.InvoiceCode;

                // ================= INSERT =================
                if (model.Id == 0)
                {
                    if (string.IsNullOrEmpty(invoiceId))
                        invoiceId = await GenerateNextPurchaseCodeAsync(conn, tx);
                    var insertSql = @"
INSERT INTO tbl_purchase_order
(date, vendor_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man,
ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms,
payment_date, total, vat, net, pay, `change`, created_by, created_date,
state, project_id, description)
VALUES
(@date, @vendor_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man,
@ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms,
@payment_date, @total, @vat, @net, @pay, @change, @created_by, @created_date,
0, @project_id, @description);
SELECT LAST_INSERT_ID();";

                    await using var insertCmd = new MySqlCommand(insertSql, conn, (MySqlTransaction)tx);
                    insertCmd.Parameters.AddWithValue("@date", model.Date);
                    insertCmd.Parameters.AddWithValue("@vendor_id", model.VendorId);
                    insertCmd.Parameters.AddWithValue("@invoice_id", invoiceId);
                    insertCmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    insertCmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                    insertCmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    insertCmd.Parameters.AddWithValue("@city", model.City ?? "");
                    insertCmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    insertCmd.Parameters.AddWithValue("@ship_date", model.ShipDate);
                    insertCmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    insertCmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    insertCmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod ?? "");
                    insertCmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    insertCmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    insertCmd.Parameters.AddWithValue("@payment_date", model.PaymentDate);
                    insertCmd.Parameters.AddWithValue("@total", model.TotalBefore);
                    insertCmd.Parameters.AddWithValue("@vat", model.Vat);
                    insertCmd.Parameters.AddWithValue("@net", model.NetTotal);
                    insertCmd.Parameters.AddWithValue("@pay", model.Pay);
                    insertCmd.Parameters.AddWithValue("@change", model.Change);
                    insertCmd.Parameters.AddWithValue("@created_by", userId);
                    insertCmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    insertCmd.Parameters.AddWithValue("@project_id", model.ProjectId);
                    insertCmd.Parameters.AddWithValue("@description", model.Description ?? "");

                    purchaseId = Convert.ToInt64(await insertCmd.ExecuteScalarAsync());
                }
                else
                {
                    // ================= UPDATE =================
                    var updateSql = @"
UPDATE tbl_purchase_order SET
date=@date, vendor_id=@vendor_id,  warehouse_id=@warehouse_id,
po_num=@po_num, bill_to=@bill_to, city=@city, sales_man=@sales_man,
ship_date=@ship_date, ship_via=@ship_via, ship_to=@ship_to,
payment_method=@payment_method, account_cash_id=@account_cash_id,
payment_terms=@payment_terms, payment_date=@payment_date,
total=@total, vat=@vat, net=@net, pay=@pay, `change`=@change,
project_id=@project_id, description=@description
WHERE id=@id;";

                    await using var updateCmd = new MySqlCommand(updateSql, conn, (MySqlTransaction)tx);
                    updateCmd.Parameters.AddWithValue("@date", model.Date);
                    updateCmd.Parameters.AddWithValue("@vendor_id", model.VendorId);
                    updateCmd.Parameters.AddWithValue("@invoice_id", invoiceId);
                    updateCmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                    updateCmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                    updateCmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                    updateCmd.Parameters.AddWithValue("@city", model.City ?? "");
                    updateCmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                    updateCmd.Parameters.AddWithValue("@ship_date", model.ShipDate);
                    updateCmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                    updateCmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                    updateCmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod ?? "");
                    updateCmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                    updateCmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                    updateCmd.Parameters.AddWithValue("@payment_date", model.PaymentDate);
                    updateCmd.Parameters.AddWithValue("@total", model.TotalBefore);
                    updateCmd.Parameters.AddWithValue("@vat", model.Vat);
                    updateCmd.Parameters.AddWithValue("@net", model.NetTotal);
                    updateCmd.Parameters.AddWithValue("@pay", model.Pay);
                    updateCmd.Parameters.AddWithValue("@change", model.Change);
                    updateCmd.Parameters.AddWithValue("@project_id", model.ProjectId);
                    updateCmd.Parameters.AddWithValue("@description", model.Description ?? "");
                    updateCmd.Parameters.AddWithValue("@id", purchaseId);

                    await updateCmd.ExecuteNonQueryAsync();

                    // Delete old items
                    await using var delCmd = new MySqlCommand(
                        "DELETE FROM tbl_purchase_order_details WHERE purchase_id=@id",
                        conn, (MySqlTransaction)tx);
                    delCmd.Parameters.AddWithValue("@id", purchaseId);
                    await delCmd.ExecuteNonQueryAsync();
                }

                // ================= INSERT ITEMS =================
                if (model.Items != null && model.Items.Any())
                {
                    var valueList = new List<string>();
                    var parameters = new List<MySqlParameter>();
                    int paramIndex = 0;

                    foreach (var item in model.Items)
                    {
                        valueList.Add($"(@purchase_id, @item_id{paramIndex}, @qty{paramIndex}, @cost_price{paramIndex}, @price{paramIndex}, @discount{paramIndex}, @vatp{paramIndex}, @vat{paramIndex}, @total{paramIndex})");
                        parameters.Add(new MySqlParameter($"@item_id{paramIndex}", item.ItemId));
                        parameters.Add(new MySqlParameter($"@qty{paramIndex}", item.Quantity));
                        parameters.Add(new MySqlParameter($"@cost_price{paramIndex}", item.CostPrice));
                        parameters.Add(new MySqlParameter($"@price{paramIndex}", item.Price));
                        parameters.Add(new MySqlParameter($"@discount{paramIndex}", item.Discount));
                        parameters.Add(new MySqlParameter($"@vatp{paramIndex}", item.VatPercentage));
                        parameters.Add(new MySqlParameter($"@vat{paramIndex}", item.Vat));
                        parameters.Add(new MySqlParameter($"@total{paramIndex}", item.Total));
                        paramIndex++;
                    }

                    var itemsSql = $@"
INSERT INTO tbl_purchase_order_details
(purchase_id, item_id, qty, cost_price, price, discount, vatp, vat, total)
VALUES {string.Join(", ", valueList)};";

                    await using var itemsCmd = new MySqlCommand(itemsSql, conn, (MySqlTransaction)tx);
                    itemsCmd.Parameters.AddWithValue("@purchase_id", purchaseId);
                    foreach (var p in parameters)
                        itemsCmd.Parameters.Add(p);

                    await itemsCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // Return the generated invoice ID as well
                return Json(new { status = true, message = model.Id == 0 ? "Purchase Order added" : "Purchase Order updated", id = purchaseId, invoiceId = invoiceId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateNextPurchaseCodeAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            string newCode = "PO-0001";

            var sql = "SELECT MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)) AS lastCode FROM tbl_purchase_order;";

            // Attach the transaction!
            await using var cmd = new MySqlCommand(sql, conn, tx);

            await using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync() && reader["lastCode"] != DBNull.Value)
            {
                int code = int.Parse(reader["lastCode"].ToString()) + 1;
                newCode = "PO-" + code.ToString("D4");
            }

            await reader.CloseAsync();
            return newCode;
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseOrderInvoiceReport(int purchaseId)
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

                // 🔹 PURCHASE HEADER + VENDOR
                var purchaseCmd = new MySqlCommand(@"
SELECT 
    p.id,
    p.date,
    p.invoice_id,
    p.bill_to,
    (SELECT CONCAT(code, ' ', name, description) FROM tbl_projects WHERE id = p.project_id) AS city,
    p.sales_man,
    p.ship_date,
    p.ship_via,
    p.ship_to,
    p.po_num,
    p.payment_method,
    p.payment_terms,
    p.payment_date,
    p.total,
    p.vat,
    p.net,
    p.pay,
    p.`change`,
    (SELECT name FROM tbl_coa_level_4 WHERE id = p.account_cash_id) AS accountName,
    v.name AS vendorName,
    v.main_phone,
    v.email,
    v.trn,
    v.mobile
FROM tbl_purchase_order p
INNER JOIN tbl_vendor v ON p.vendor_id = v.id
WHERE p.id = @purchaseId;
", conn);
                purchaseCmd.Parameters.Add("@purchaseId", MySqlDbType.Int32).Value = purchaseId;

                PurchaseReportDto purchase = null;
                await using (var reader = await purchaseCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        purchase = new PurchaseReportDto
                        {
                            Id = reader.GetInt32("id"),
                            Date = reader.GetDateTime("date"),
                            InvoiceNo = reader.GetString("invoice_id"),
                            VendorName = reader.GetString("vendorName"),
                            BillTo = reader.IsDBNull("bill_to") ? null : reader.GetString("bill_to"),
                            City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                            SalesMan = reader.IsDBNull("sales_man") ? null : reader.GetString("sales_man"),
                            ShipDate = reader.IsDBNull("ship_date") ? (DateTime?)null : reader.GetDateTime("ship_date"),
                            ShipVia = reader.IsDBNull("ship_via") ? null : reader.GetString("ship_via"),
                            ShipTo = reader.IsDBNull("ship_to") ? null : reader.GetString("ship_to"),
                            PoNumber = reader.IsDBNull("po_num") ? null : reader.GetString("po_num"),
                            PaymentMethod = reader.IsDBNull("payment_method") ? null : reader.GetString("payment_method"),
                            PaymentTerms = reader.IsDBNull("payment_terms") ? null : reader.GetString("payment_terms"),
                            PaymentDate = reader.IsDBNull("payment_date") ? (DateTime?)null : reader.GetDateTime("payment_date"),
                            Total = reader.GetDecimal("total"),
                            Vat = reader.GetDecimal("vat"),
                            Net = reader.GetDecimal("net"),
                            Pay = reader.GetDecimal("pay"),
                            Change = reader.GetDecimal("change"),
                            AccountName = reader.IsDBNull("accountName") ? null : reader.GetString("accountName"),

                            VendorPhone = reader.IsDBNull("main_phone") ? null : reader.GetString("main_phone"),
                            VendorEmail = reader.IsDBNull("email") ? null : reader.GetString("email"),
                            VendorTRN = reader.IsDBNull("trn") ? null : reader.GetString("trn"),
                            VendorMobile = reader.IsDBNull("mobile") ? null : reader.GetString("mobile")
                        };
                    }
                }

                if (purchase == null)
                    return NotFound(new { status = false, message = "Purchase invoice not found" });

                // 🔹 PURCHASE ITEMS
                var itemCmd = new MySqlCommand(@"
SELECT 
    d.id,
    d.item_id,
    i.name,
    d.qty,
    d.cost_price,
    d.price,
    d.discount,
    ((d.qty*d.cost_price)-d.discount) AS subTotal,
    d.vat AS vatPercentage,
    d.total,
    d.cost_center_id,
    (SELECT name FROM tbl_sub_cost_center WHERE id=d.cost_center_id) AS costCenterName,
    i.unit_id,
    (SELECT name FROM tbl_unit WHERE id=i.unit_id) AS unitName,
    i.code
FROM tbl_purchase_order_details d
INNER JOIN tbl_items i ON d.item_id = i.id
WHERE d.purchase_id = @purchaseId;
", conn);
                itemCmd.Parameters.Add("@purchaseId", MySqlDbType.Int32).Value = purchaseId;

                var items = new List<PurchaseItemReportDto>();
                await using (var reader = await itemCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new PurchaseItemReportDto
                        {
                            ItemId = reader.GetInt32("item_id"),
                            Code = reader.GetString("code"),
                            Name = reader.GetString("name"),
                            Qty = Convert.ToDecimal(reader["qty"]),
                            CostPrice = Convert.ToDecimal(reader["cost_price"]),
                            Price = Convert.ToDecimal(reader["price"]),
                            Discount = Convert.ToDecimal(reader["discount"]),
                            Vat = Convert.ToDecimal(reader["vatPercentage"]),
                            Total = Convert.ToDecimal(reader["total"]),
                            SubTotal = Convert.ToDecimal(reader["subTotal"]),
                            CostCenterName = reader.IsDBNull("costCenterName") ? null : reader.GetString("costCenterName"),
                            UnitName = reader.IsDBNull("unitName") ? null : reader.GetString("unitName")
                        });
                    }
                }

                // 🔹 COMPANY INFO
                var companyCmd = new MySqlCommand("SELECT * FROM tbl_company LIMIT 1;", conn);
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
                    return BadRequest(new { status = false, message = "Company info not found" });

                // 🔹 GENERATE PDF
                byte[] pdfBytes = PurchaseOrderInvoicePdfGenerator.Generate(company, purchase, items);

                return File(pdfBytes, "application/pdf", fileDownloadName: null);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Purchase Return

        public IActionResult PurchaseReturn()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetPurchaseReturns(
    int? vendorId = null,
    string paymentMethod = null,
    DateTime? dateFrom = null,
    DateTime? dateTo = null
    )   
        {
            try
            {
                // Validate user session
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                // Build connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // ---------------- MAIN QUERY ----------------
                string query = @"
WITH Dedup AS (
    SELECT
        ROW_NUMBER() OVER (
            PARTITION BY
                pr.id,
                prd.item_id,
                prd.qty,
                prd.cost_price,
                prd.vat,
                prd.total,
                prd.cost_center_id
            ORDER BY pr.date
        ) AS rn,

        DENSE_RANK() OVER (ORDER BY pr.date) AS SN,

        pr.id AS Id,
        pr.date AS Date,
        pr.invoice_id AS InvoiceNo,
        CONCAT(v.code, ' - ', v.name) AS VendorName,
        pr.payment_method AS PaymentMethod,
        pr.total AS Total,
        pr.vat AS Vat,
        pr.net AS Net,
        pr.warehouse_id AS Warehouse_Id,
        pr.po_num AS PO_Num,
        pr.bill_to AS Bill_To,
        pr.sales_man AS Sales_Man,
        pr.city AS City,
        pr.project_id AS ProjectId,
        pr.ship_date AS Ship_Date,
        pr.ship_via AS Ship_Via,
        pr.vendor_id AS VendorId,
        pr.ship_to AS Ship_To,
        pr.account_cash_id AS Account_Cash_Id,
        pr.payment_terms AS Payment_Terms,
        pr.payment_date AS Payment_Date,
        pr.description AS Description,
        pr.pay AS Pay,

       
        CONCAT('000', t.transaction_id) AS JVNo,

        i.id AS ItemId,
        i.code AS ItemCode,
        i.name AS ItemName,
        prd.qty AS Qty,
        prd.cost_price AS CostPrice,
        prd.price AS Price,
        prd.vat AS ItemVat,
        prd.vatp AS VatP,
        prd.total AS ItemTotal,
        prd.cost_center_id AS Cost_Center_Id

    FROM tbl_purchase_return pr
    INNER JOIN tbl_vendor v ON pr.vendor_id = v.id
    LEFT JOIN tbl_transaction t ON pr.id = t.transaction_id
    LEFT JOIN tbl_purchase_return_details prd ON pr.id = prd.purchase_id
    LEFT JOIN tbl_items i ON prd.item_id = i.id
    WHERE pr.state = 0
)

SELECT
    SN,
    Id,
    Date,
    InvoiceNo,
    VendorName,
    PaymentMethod,
    Total,
    Vat,
    Net,
    Warehouse_Id,
    PO_Num,
    Bill_To,
    Sales_Man,
    City,
    ProjectId,
    Ship_Date,
    Ship_Via,
    VendorId,
    Ship_To,
    Account_Cash_Id,
    Payment_Terms,
    Payment_Date,
    Description,
    Pay,
    JVNo,
    ItemId,
    ItemCode,
    ItemName,
    Qty,
    Price,
    CostPrice,
    Price,
    ItemVat,
    VatP,
    ItemTotal,
    Cost_Center_Id
FROM Dedup
WHERE rn = 1
ORDER BY Date;

";

                var parameters = new List<MySqlParameter>();

                // Vendor filter
                if (vendorId.HasValue)
                {
                    query += " AND pr.vendor_id = @vendorId";
                    parameters.Add(new MySqlParameter("@vendorId", vendorId.Value));
                }

                // Payment method filter
                if (!string.IsNullOrEmpty(paymentMethod))
                {
                    query += " AND pr.payment_method = @payment";
                    parameters.Add(new MySqlParameter("@payment", paymentMethod));
                }

                // Date filter
                if (dateFrom.HasValue && dateTo.HasValue)
                {
                    query += " AND pr.date >= @dateFrom AND pr.date <= @dateTo";
                    parameters.Add(new MySqlParameter("@dateFrom", dateFrom.Value.Date));
                    parameters.Add(new MySqlParameter("@dateTo", dateTo.Value.Date));
                }

                query += @"
GROUP BY 
    pr.id, pr.date, pr.invoice_id,
    v.code, v.name,
    pr.payment_method,
    pr.total, pr.vat, pr.net
ORDER BY pr.date
";

                var result = new List<object>();

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();
                var returns = new List<PurchaseReturnDto>();
                PurchaseReturnDto current = null;
                int lastId = -1;

                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32("Id");

                    if (id != lastId)
                    {
                        current = new PurchaseReturnDto
                        {
                            SN = reader.GetInt32("SN"),
                            Id = id,
                            Date = reader.GetDateTime("Date"),
                            JVNo = reader["JVNo"]?.ToString(),
                            InvoiceNo = reader["InvoiceNo"]?.ToString(),
                            VendorName = reader["VendorName"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),

                            Total = Convert.ToDecimal(reader["Total"]),
                            Vat = Convert.ToDecimal(reader["Vat"]),
                            Net = Convert.ToDecimal(reader["Net"]),

                            Warehouse_Id = reader["Warehouse_Id"] != DBNull.Value ? Convert.ToInt32(reader["Warehouse_Id"]) : (int?)null,
                            PO_Num = reader["PO_Num"]?.ToString(),
                            Bill_To = reader["Bill_To"]?.ToString(),
                            Sales_Man = reader["Sales_Man"]?.ToString(),
                            City = reader["City"]?.ToString(),
                            ProjectId = reader["ProjectId"] != DBNull.Value ? Convert.ToInt32(reader["ProjectId"]) : (int?)null,
                            Ship_Date = reader["Ship_Date"] != DBNull.Value ? Convert.ToDateTime(reader["Ship_Date"]) : (DateTime?)null,
                            Ship_Via = reader["Ship_Via"]?.ToString(),
                            VendorId = Convert.ToInt32(reader["VendorId"]),
                            Ship_To = reader["Ship_To"]?.ToString(),
                            Account_Cash_Id = reader["Account_Cash_Id"] != DBNull.Value ? Convert.ToInt32(reader["Account_Cash_Id"]) : (int?)null,
                            Payment_Terms = reader["Payment_Terms"]?.ToString(),
                            Payment_Date = reader["Payment_Date"] != DBNull.Value ? Convert.ToDateTime(reader["Payment_Date"]) : (DateTime?)null,
                            Description = reader["Description"]?.ToString(),
                            Pay = reader["Pay"] != DBNull.Value ? Convert.ToDecimal(reader["Pay"]) : 0,

                            Items = new List<PurchaseReturnItemDto>()
                        };

                        returns.Add(current);
                        lastId = id;
                    }

                    // 🔹 ADD ITEM
                    if (reader["ItemId"] != DBNull.Value)
                    {
                        current.Items.Add(new PurchaseReturnItemDto
                        {
                            ItemId = Convert.ToInt32(reader["ItemId"]),
                            ItemCode = reader["ItemCode"]?.ToString(),
                            ItemName = reader["ItemName"]?.ToString(),
                            Qty = Convert.ToDecimal(reader["Qty"]),
                            CostPrice = Convert.ToDecimal(reader["Price"]),
                            Price = Convert.ToDecimal(reader["Price"]),
                            Vat = Convert.ToDecimal(reader["ItemVat"]),
                            VatP = Convert.ToDecimal(reader["VatP"]),
                            Total = Convert.ToDecimal(reader["ItemTotal"]),
                            Cost_Center_Id = reader["Cost_Center_Id"] != DBNull.Value
                                                ? Convert.ToInt32(reader["Cost_Center_Id"])
                                                : (int?)null
                        });
                    }
                }


                return Ok(new
                {
                    status = true,
                    data = returns

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
        public async Task<IActionResult> SavePurchaseReturn([FromBody] PurchaseReturnRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            if (model.Items == null || !model.Items.Any())
                return Json(new { status = false, message = "Insert Items First." });

            if (!await AreDefaultAccountsSet(new List<string> { "Vendor", "Vat Output", "Inventory" }))
                return Json(new { status = false, message = "Default accounts for invoice are not properly configured. Please check your settings." });

            // Validate items
            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                if (item.Total <= 0)
                    return Json(new { status = false, message = $"Total Item In Row {i + 1} Can't Be 0 or Null" });
            }

            if (model.VendorId <= 0)
                return Json(new { status = false, message = "Vendor Must be Selected." });

            if (model.AccountCashId <= 0)
                return Json(new { status = false, message = "Account Cash Name Must be Selected." });

            if (model.NetTotal <= 0)
                return Json(new { status = false, message = "Total Must Be Bigger Than Zero" });

            // Get account IDs from configuration
            var accountIds = await GetDefaultAccountIdss();
            if (accountIds.PaymentCreditMethodId <= 0 || accountIds.VatId <= 0 || accountIds.PurchaseReturnId <= 0)
                return Json(new { status = false, message = "Default accounts for invoice are not properly configured. Please check your settings." });

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
                await using var transaction = await conn.BeginTransactionAsync();

                try
                {
                    int purchaseReturnId = model.Id;
                    decimal paidAmount = model.PaymentMethod == "Cash" ? model.NetTotal : 0;
                    decimal changeAmount = model.PaymentMethod != "Cash" ? model.NetTotal : 0;

                    if (purchaseReturnId == 0)
                    {
                        model.InvoiceCode = await GenerateNextPurchaseReturnCode(conn, transaction);

                        var insertQuery = @"
INSERT INTO tbl_purchase_return
(date, vendor_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man,
ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date,
total, vat, net, pay, `change`, created_by, created_date, state, description)
VALUES
(@date, @vendor_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man,
@ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms, @payment_date,
@total, @vat, @net, @pay, @change, @created_by, @created_date, 0, @description);
SELECT LAST_INSERT_ID();";

                        await using var cmd = new MySqlCommand(insertQuery, conn, transaction);
                        cmd.Parameters.AddWithValue("@date", model.Date);
                        cmd.Parameters.AddWithValue("@vendor_id", model.VendorId);
                        cmd.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
                        cmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                        cmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                        cmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                        cmd.Parameters.AddWithValue("@city", model.City ?? "");
                        cmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                        cmd.Parameters.AddWithValue("@ship_date", model.ShipDate );
                        cmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                        cmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                        cmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod);
                        cmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                        cmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                        cmd.Parameters.AddWithValue("@payment_date", model.PaymentDate);
                        cmd.Parameters.AddWithValue("@total", model.TotalBefore);
                        cmd.Parameters.AddWithValue("@vat", model.Vat);
                        cmd.Parameters.AddWithValue("@net", model.NetTotal);
                        cmd.Parameters.AddWithValue("@pay", paidAmount);
                        cmd.Parameters.AddWithValue("@change", changeAmount);
                        cmd.Parameters.AddWithValue("@created_by", userId);
                        cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                        purchaseReturnId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                        // Insert purchase return items
                        await InsertPurchaseReturnItems(conn, transaction, purchaseReturnId, model.Items,
                            model.Date, model.InvoiceCode, model.WarehouseId);

                        // Insert journal transactions
                        await AddJournalTransaction(conn, transaction, purchaseReturnId, model, userId, accountIds);

                    }
                    else // 🔹 UPDATE EXISTING PURCHASE RETURN
                    {
                        var updateQuery = @"
UPDATE tbl_purchase_return SET
modified_by=@modified_by, modified_date=@modified_date, date=@date, sales_man=@sales_man,
vendor_id=@vendor_id, invoice_id=@invoice_id, warehouse_id=@warehouse_id,
po_num=@po_num, bill_to=@bill_to, ship_date=@ship_date,
ship_via=@ship_via, ship_to=@ship_to, payment_method=@payment_method, account_cash_id=@account_cash_id,
payment_terms=@payment_terms, payment_date=@payment_date, total=@total,
vat=@vat, net=@net, pay=@pay, `change`=@change, city=@city, description=@description
WHERE id=@id;";

                        await using var cmd = new MySqlCommand(updateQuery, conn, transaction);
                        cmd.Parameters.AddWithValue("@id", purchaseReturnId);
                        cmd.Parameters.AddWithValue("@date", model.Date.Date);
                        cmd.Parameters.AddWithValue("@vendor_id", model.VendorId);
                        cmd.Parameters.AddWithValue("@invoice_id", model.InvoiceCode);
                        cmd.Parameters.AddWithValue("@warehouse_id", model.WarehouseId);
                        cmd.Parameters.AddWithValue("@po_num", model.PONumber ?? "");
                        cmd.Parameters.AddWithValue("@bill_to", model.BillTo ?? "");
                        cmd.Parameters.AddWithValue("@city", model.City ?? "");
                        cmd.Parameters.AddWithValue("@sales_man", model.SalesMan ?? "");
                        cmd.Parameters.AddWithValue("@ship_date", model.ShipDate.Date);
                        cmd.Parameters.AddWithValue("@ship_via", model.ShipVia ?? "");
                        cmd.Parameters.AddWithValue("@ship_to", model.ShipTo ?? "");
                        cmd.Parameters.AddWithValue("@payment_method", model.PaymentMethod);
                        cmd.Parameters.AddWithValue("@account_cash_id", model.AccountCashId);
                        cmd.Parameters.AddWithValue("@payment_terms", model.PaymentTerms ?? "");
                        cmd.Parameters.AddWithValue("@payment_date", model.PaymentDate.Date);
                        cmd.Parameters.AddWithValue("@total", model.TotalBefore);
                        cmd.Parameters.AddWithValue("@vat", model.Vat);
                        cmd.Parameters.AddWithValue("@net", model.NetTotal);
                        cmd.Parameters.AddWithValue("@pay", paidAmount);
                        cmd.Parameters.AddWithValue("@change", changeAmount);
                        cmd.Parameters.AddWithValue("@modified_by", userId);
                        cmd.Parameters.AddWithValue("@modified_date", DateTime.Now.Date);
                        cmd.Parameters.AddWithValue("@description", model.Description ?? "");

                        await cmd.ExecuteNonQueryAsync();

                        // Return items to inventory
                        await ReturnItemsToInventorys(conn, transaction, purchaseReturnId);

                        // Delete previous item transactions and card details
                        await using var deleteCmd = new MySqlCommand(@"
                            DELETE FROM tbl_item_transaction WHERE reference = @invId AND type = 'Purchase Return Invoice';
                            DELETE FROM tbl_item_card_details WHERE trans_type = 'Purchase Return Invoice' AND trans_no = @invId;",
                            conn, transaction);
                        deleteCmd.Parameters.AddWithValue("@invId", purchaseReturnId);
                        await deleteCmd.ExecuteNonQueryAsync();

                        // Delete cost center and journal transactions
                        await DeleteCostCenterTransactionEntry(conn, transaction, purchaseReturnId.ToString(), "Purchase Return");
                        await DeleteTransactionEntry(conn, transaction, purchaseReturnId, "Purchase Return Invoice");

                        // Re-insert items and transactions
                        await InsertPurchaseReturnItems(conn, transaction, purchaseReturnId, model.Items,
                            model.Date, model.InvoiceCode, model.WarehouseId);
                        await AddJournalTransaction(conn, transaction, purchaseReturnId, model, userId, accountIds);
                    }

                    await transaction.CommitAsync();

                    return Json(new
                    {
                        status = true,
                        message = model.Id == 0 ? "Purchase Return saved successfully" : "Purchase Return updated successfully",
                        id = purchaseReturnId,
                        invoiceCode = model.InvoiceCode
                    });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        private async Task<string> GenerateNextPurchaseReturnCode(MySqlConnection conn, MySqlTransaction transaction)
        {
            string newCode = "PR-0001";

            await using var cmd = new MySqlCommand(
                "SELECT MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)) AS lastCode FROM tbl_purchase_return",
                conn, transaction);

            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                int code = Convert.ToInt32(result) + 1;
                newCode = "PR-" + code.ToString("D4");
            }

            return newCode;
        }

        private async Task InsertPurchaseReturnItems(MySqlConnection conn, MySqlTransaction transaction,
            int purchaseReturnId, List<PurchaseReturnItemRequest> items, DateTime date, string invoiceCode, int warehouseId)
        {
            var valueList = new List<string>();
            var parameters = new List<MySqlParameter>();
            int paramIndex = 0;

            foreach (var item in items)
            {
                if (item.ItemId <= 0)
                    continue;

                valueList.Add($"(@purchase_id, @item_id{paramIndex}, @qty{paramIndex}, @cost_price{paramIndex}, " +
                    $"@price{paramIndex}, @vatp{paramIndex}, @vat{paramIndex}, @total{paramIndex}, @costCenter{paramIndex})");

                parameters.Add(new MySqlParameter($"item_id{paramIndex}", item.ItemId));
                parameters.Add(new MySqlParameter($"qty{paramIndex}", item.Qty));
                parameters.Add(new MySqlParameter($"cost_price{paramIndex}", item.CostPrice));
                parameters.Add(new MySqlParameter($"price{paramIndex}", item.Price));
                parameters.Add(new MySqlParameter($"vatp{paramIndex}", item.VatPercentage));
                parameters.Add(new MySqlParameter($"vat{paramIndex}", item.Vat));
                parameters.Add(new MySqlParameter($"total{paramIndex}", item.Total));
                parameters.Add(new MySqlParameter($"costCenter{paramIndex}", item.CostCenterId));
                paramIndex++;
            }

            if (valueList.Count == 0)
                return;

            string sql = $@"
INSERT INTO tbl_purchase_return_details 
(purchase_id, item_id, qty, cost_price, price, vatp, vat, total, cost_center_id)
VALUES {string.Join(", ", valueList)}";

            await using var cmd = new MySqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("purchase_id", purchaseReturnId);
            foreach (var param in parameters)
            {
                cmd.Parameters.Add(param);
            }
            await cmd.ExecuteNonQueryAsync();

            // Process each item for inventory and cost center
            foreach (var item in items)
            {
                if (item.ItemId <= 0)
                    continue;

                // Check if item is not a service
                await using var typeCmd = new MySqlCommand(
                    "SELECT type FROM tbl_items WHERE id = @id", conn, transaction);
                typeCmd.Parameters.AddWithValue("@id", item.ItemId);
                var itemType = await typeCmd.ExecuteScalarAsync();

                if (itemType != null && itemType.ToString() != "Service")
                {
                    // Insert item transaction
                    await InsertItemTransaction(conn, transaction, date, "Purchase Return Invoice",
                        purchaseReturnId.ToString(), item.ItemId.ToString(), item.CostPrice.ToString(),
                        "0", item.Price.ToString(), item.Qty.ToString(), "0",
                        $"Purchase Return Invoice No. {invoiceCode}", warehouseId.ToString());
                }

                // Add cost center transaction
                if (item.CostCenterId > 0)
                {
                    await InsertCostCenterTransaction(conn, transaction, date, "0", item.Total.ToString(),
                        purchaseReturnId.ToString(), "Purchase Return", "", item.CostCenterId.ToString());
                }
            }
        }

        private async Task InsertItemTransaction(MySqlConnection conn, MySqlTransaction transaction,
            DateTime date, string type, string reference, string itemId, string price,
            string qtyIn, string salesPrice, string qtyOut, string qtyInc, string description, string warehouseId)
        {
            await using var cmd = new MySqlCommand(@"
INSERT INTO tbl_item_transaction 
(date, type, reference, item_id, cost_price, qty_in, sales_price, qty_out, qty_inc, description, warehouse_id)
VALUES (@date, @type, @reference, @itemId, @costPrice, @qtyIn, @sales_price, @qtyOut, @qtyInc, @description, @warehouseId);",
                conn, transaction);

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

            // Update on hand quantity
            await UpdateOnHandItem(conn, transaction, itemId);

            // Add item card details
            await AddItemCardDetails(conn, transaction, date, type, reference, itemId, price,
                qtyIn, salesPrice, qtyOut, qtyInc, description, warehouseId);
        }

        private async Task UpdateOnHandItem(MySqlConnection conn, MySqlTransaction transaction, string itemId)
        {
            await using var cmd = new MySqlCommand(@"
UPDATE tbl_items 
SET on_hand = (SELECT SUM(qty_in - qty_out) FROM tbl_item_transaction WHERE item_id = @itemId) 
WHERE id = @itemId", conn, transaction);

            cmd.Parameters.AddWithValue("@itemId", itemId);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task AddItemCardDetails(MySqlConnection conn, MySqlTransaction transaction,
            DateTime date, string type, string reference, string itemId, string costPrice,
            string qtyIn, string salesPrice, string qtyOut, string qtyInc, string description, string warehouseId)
        {
            string invoiceNo = "INV-" + reference;
            string transNo = reference;
            string transType = type;
            decimal qtyBalance = 0, debit = 0, credit = 0, price = decimal.Parse(costPrice), balance = 0;
            decimal fifoQty = 0, fifoCost = 0;
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
            await using var balanceCmd = new MySqlCommand(@"
SELECT 
    IFNULL(SUM(qty_in - qty_out), 0) as QtyBalance,
    IFNULL(SUM(debit - credit), 0) as Balance
FROM tbl_item_card_details 
WHERE itemId = @itemId", conn, transaction);

            balanceCmd.Parameters.AddWithValue("@itemId", itemId);

            await using var reader = await balanceCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                decimal _QtyBalance = Convert.ToDecimal(reader["QtyBalance"]);
                decimal _Balance = Convert.ToDecimal(reader["Balance"]);
                qtyBalance = _QtyBalance + (_qtyIn - _qtyOut);
                balance = _Balance + (debit - credit);
            }
            await reader.CloseAsync();

            // Insert card details
            await using var insertCmd = new MySqlCommand(@"
INSERT INTO tbl_item_card_details (
    itemId, date, wharehouse_id, inv_no, trans_no, trans_type, description,
    price, qty_in, qty_out, qty_balance, debit, credit, balance, fifo_qty, fifo_cost
) VALUES (
    @itemId, @date, @wharehouse_id, @inv_no, @trans_no, @trans_type, @description,
    @price, @qty_in, @qty_out, @qty_balance, @debit, @credit, @balance, @fifo_qty, @fifo_cost
);", conn, transaction);

            insertCmd.Parameters.AddWithValue("@itemId", itemId);
            insertCmd.Parameters.AddWithValue("@date", date);
            insertCmd.Parameters.AddWithValue("@wharehouse_id", warehouseId);
            insertCmd.Parameters.AddWithValue("@inv_no", invoiceNo);
            insertCmd.Parameters.AddWithValue("@trans_no", transNo);
            insertCmd.Parameters.AddWithValue("@trans_type", transType);
            insertCmd.Parameters.AddWithValue("@description", description);
            insertCmd.Parameters.AddWithValue("@price", price);
            insertCmd.Parameters.AddWithValue("@qty_in", qtyIn);
            insertCmd.Parameters.AddWithValue("@qty_out", qtyOut);
            insertCmd.Parameters.AddWithValue("@qty_balance", qtyBalance);
            insertCmd.Parameters.AddWithValue("@debit", debit);
            insertCmd.Parameters.AddWithValue("@credit", credit);
            insertCmd.Parameters.AddWithValue("@balance", balance);
            insertCmd.Parameters.AddWithValue("@fifo_qty", fifoQty);
            insertCmd.Parameters.AddWithValue("@fifo_cost", fifoCost);

            await insertCmd.ExecuteNonQueryAsync();
        }


        private async Task AddJournalTransaction(MySqlConnection conn, MySqlTransaction transaction,
            int purchaseReturnId, PurchaseReturnRequest model, int userId, (int PaymentCreditMethodId, int VatId, int PurchaseReturnId) accountIds)
        {
            // Main transaction entry (Cash/Credit account)
            await AddTransactionEntry(conn, transaction, model.Date,
                model.PaymentMethod == "Credit" ? accountIds.PaymentCreditMethodId.ToString() : model.AccountCashId.ToString(),
                "0", model.NetTotal.ToString(), purchaseReturnId.ToString(), model.VendorId.ToString(),
                "Purchase Return Invoice", "PURCHASE RETURN",
                $"Purchase Return Invoice NO. {model.InvoiceCode}", userId, DateTime.Now.Date, model.InvoiceCode);

            // VAT transaction entry
            if (model.Vat > 0)
            {
                await AddTransactionEntry(conn, transaction, model.Date,
                    accountIds.VatId.ToString(), model.Vat.ToString(), "0",
                    purchaseReturnId.ToString(), "0", "Purchase Return Invoice", "PURCHASE RETURN",
                    $"Vat Input For Invoice No. {model.InvoiceCode}", userId, DateTime.Now.Date, model.InvoiceCode);
            }

            // Purchase Return transaction entry
            await AddTransactionEntry(conn, transaction, model.Date,
                accountIds.PurchaseReturnId.ToString(), model.TotalBefore.ToString(), "0",
                purchaseReturnId.ToString(), "0", "Purchase Return Invoice", "PURCHASE RETURN",
                $"Purchase Return For Invoice No. {model.InvoiceCode}", userId, DateTime.Now.Date, model.InvoiceCode);
        }


        private async Task ReturnItemsToInventorys(MySqlConnection conn, MySqlTransaction transaction, int purchaseReturnId)
        {
            await using var cmd = new MySqlCommand(@"
SELECT tbl_purchase_return_details.*, tbl_items.method, tbl_items.type 
FROM tbl_purchase_return_details
INNER JOIN tbl_items ON tbl_purchase_return_details.item_id = tbl_items.id 
WHERE purchase_id = @id", conn, transaction);

            cmd.Parameters.AddWithValue("@id", purchaseReturnId);

            await using var reader = await cmd.ExecuteReaderAsync();
            var itemsToProcess = new List<(string itemId, string qty, string type, string detailId)>();

            while (await reader.ReadAsync())
            {
                itemsToProcess.Add((
                    reader["item_id"].ToString(),
                    reader["qty"].ToString(),
                    reader["type"].ToString(),
                    reader["id"].ToString()
                ));
            }
            await reader.CloseAsync();

            foreach (var item in itemsToProcess)
            {
                if (item.type != "Service")
                {
                    // Update on_hand
                    await using var updateCmd = new MySqlCommand(
                        "UPDATE tbl_items SET on_hand = on_hand + @qty WHERE id = @id",
                        conn, transaction);
                    updateCmd.Parameters.AddWithValue("@id", item.itemId);
                    updateCmd.Parameters.AddWithValue("@qty", item.qty);
                    await updateCmd.ExecuteNonQueryAsync();
                }

                // Delete purchase return detail
                await using var deleteDetailCmd = new MySqlCommand(
                    "DELETE FROM tbl_purchase_return_details WHERE id = @id",
                    conn, transaction);
                deleteDetailCmd.Parameters.AddWithValue("@id", item.detailId);
                await deleteDetailCmd.ExecuteNonQueryAsync();
            }
        }


        private async Task<(int PaymentCreditMethodId, int VatId, int PurchaseReturnId)> GetDefaultAccountIdss()
        {
            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            int paymentCreditMethodId = 0, vatId = 0, purchaseReturnId = 0;

            // Get Payment Credit Method ID (Vendor)
            await using (var cmd = new MySqlCommand(
                "SELECT account_id FROM tbl_coa_config WHERE category = 'Vendor' LIMIT 1", conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    paymentCreditMethodId = Convert.ToInt32(result);
            }

            // Get VAT Output ID
            await using (var cmd = new MySqlCommand(
                "SELECT account_id FROM tbl_coa_config WHERE category = 'Vat Output' LIMIT 1", conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    vatId = Convert.ToInt32(result);
            }

            // Get Purchase Return ID (Inventory)
            await using (var cmd = new MySqlCommand(
                "SELECT account_id FROM tbl_coa_config WHERE category = 'Inventory' LIMIT 1", conn))
            {
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    purchaseReturnId = Convert.ToInt32(result);
            }

            return (paymentCreditMethodId, vatId, purchaseReturnId);
        }


        [HttpGet]
        public async Task<IActionResult> GetPurchaseReturnReport(int purchaseId)
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
                var companyCmd = new MySqlCommand("SELECT * FROM tbl_company LIMIT 1;", conn);
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
                    return BadRequest(new { status = false, message = "Company info not found" });

                // 🔹 VENDOR + PURCHASE HEADER (from tbl_purchase_return p + vendor v)
                var vendorCmd = new MySqlCommand(@"
SELECT 
    p.id,
    p.date,
    p.vendor_id,
    p.invoice_id,
    p.bill_to,
    p.city,
    p.sales_man,
    p.ship_date,
    p.ship_via,
    p.ship_to,
    p.po_num,
    p.payment_method,
    p.payment_terms,
    p.payment_date,
    p.total,
    p.vat,
    p.net,
    p.pay,
    p.`change`,
    (SELECT name FROM tbl_coa_level_4 WHERE id = p.account_cash_id) accountName,
    v.name AS vendorName,
    v.main_phone,
    v.email,
    v.trn,
    v.mobile
FROM tbl_purchase_return p
INNER JOIN tbl_vendor v ON p.vendor_id = v.id
WHERE p.id = @purchaseId;
", conn);

                vendorCmd.Parameters.Add("@purchaseId", MySqlDbType.Int32).Value = purchaseId;

                PurchaseReportDto purchase = null;
                await using (var reader = await vendorCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        purchase = new PurchaseReportDto
                        {
                            Id = reader.GetInt32("id"),
                            Date = reader.GetDateTime("date"),
                            InvoiceNo = reader.GetString("invoice_id"),
                            VendorName = reader.GetString("vendorName"),
                            BillTo = reader.IsDBNull("bill_to") ? null : reader.GetString("bill_to"),
                            City = reader.IsDBNull("city") ? null : reader.GetString("city"),
                            SalesMan = reader.IsDBNull("sales_man") ? null : reader.GetString("sales_man"),
                            ShipDate = reader.IsDBNull("ship_date") ? (DateTime?)null : reader.GetDateTime("ship_date"),
                            ShipVia = reader.IsDBNull("ship_via") ? null : reader.GetString("ship_via"),
                            ShipTo = reader.IsDBNull("ship_to") ? null : reader.GetString("ship_to"),
                            PoNumber = reader.IsDBNull("po_num") ? null : reader.GetString("po_num"),
                            PaymentMethod = reader.IsDBNull("payment_method") ? null : reader.GetString("payment_method"),
                            PaymentTerms = reader.IsDBNull("payment_terms") ? null : reader.GetString("payment_terms"),
                            PaymentDate = reader.IsDBNull("payment_date") ? (DateTime?)null : reader.GetDateTime("payment_date"),
                            Total = reader.GetDecimal("total"),
                            Vat = reader.GetDecimal("vat"),
                            Net = reader.GetDecimal("net"),
                            Pay = reader.GetDecimal("pay"),
                            Change = reader.GetDecimal("change"),
                            AccountName = reader.IsDBNull("accountName") ? null : reader.GetString("accountName"),

                            VendorPhone = reader.IsDBNull("main_phone") ? null : reader.GetString("main_phone"),
                            VendorEmail = reader.IsDBNull("email") ? null : reader.GetString("email"),
                            VendorTRN = reader.IsDBNull("trn") ? null : reader.GetString("trn"),
                            VendorMobile = reader.IsDBNull("mobile") ? null : reader.GetString("mobile")
                        };
                    }
                }

                if (purchase == null)
                    return NotFound(new { status = false, message = "Purchase return invoice not found" });

                // 🔹 PURCHASE ITEMS (from tbl_purchase_return_details)
                var itemsCmd = new MySqlCommand(@"
SELECT 
    d.id,
    d.item_id,
    i.name,
    d.qty,
    d.cost_price,
    d.price,
    d.discount,
    ((d.qty * d.price) - d.discount) AS subTotal,
    d.vat AS vatPercentage,
    d.total,
    d.cost_center_id,
    (SELECT name FROM tbl_sub_cost_center WHERE id = d.cost_center_id) AS costCenterName,
    i.unit_id,
    (SELECT name FROM tbl_unit WHERE id = i.unit_id) AS unitName,
    i.code
FROM tbl_purchase_return_details d
INNER JOIN tbl_items i ON d.item_id = i.id
WHERE d.purchase_id = @purchaseId;
", conn);

                itemsCmd.Parameters.Add("@purchaseId", MySqlDbType.Int32).Value = purchaseId;

                var items = new List<PurchaseItemReportDto>();
                await using (var reader = await itemsCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new PurchaseItemReportDto
                        {
                            ItemId = reader.GetInt32("item_id"),
                            Code = reader.GetString("code"),
                            Name = reader.GetString("name"),
                            Qty = reader.IsDBNull("qty") ? 0m : Convert.ToDecimal(reader["qty"]),
                            CostPrice = reader.IsDBNull("cost_price") ? 0m : Convert.ToDecimal(reader["cost_price"]),
                            Price = reader.IsDBNull("price") ? 0m : Convert.ToDecimal(reader["price"]),
                            Discount = reader.IsDBNull("discount") ? 0m : Convert.ToDecimal(reader["discount"]),
                            Vat = reader.IsDBNull("vatPercentage") ? 0m : Convert.ToDecimal(reader["vatPercentage"]),
                            Total = reader.IsDBNull("total") ? 0m : Convert.ToDecimal(reader["total"]),
                            SubTotal = reader.IsDBNull("subTotal") ? 0m : Convert.ToDecimal(reader["subTotal"]),
                            CostCenterName = reader.IsDBNull("costCenterName") ? null : reader.GetString("costCenterName"),
                            UnitName = reader.IsDBNull("unitName") ? null : reader.GetString("unitName")
                        });
                    }
                }

                // 🔹 Generate PDF (your existing generator)
                byte[] pdfBytes = PurchaseReturnInvoicePdfGenerator.Generate(company, purchase, items);

                return File(pdfBytes, "application/pdf", fileDownloadName: null);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region Debit Note

        public IActionResult DebitNote()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDebitNotes(
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

                string query;
                var parameters = new List<MySqlParameter>();

                if (selectionMethod == "Default")
                {
                    query = @"
SELECT 
    c.id AS Id,
    c.date AS Date,
    c.invoice_id AS InvoiceNo,
    CONCAT('000', jt.MaxTransactionId) AS JvNo,
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
FROM tbl_debit_note c
LEFT JOIN (
    SELECT transaction_id, MAX(transaction_id) AS MaxTransactionId
    FROM tbl_transaction
    GROUP BY transaction_id
) jt ON c.id = jt.transaction_id

LEFT JOIN tbl_debit_note_details d ON c.id = d.ref_id
INNER JOIN tbl_customer cs ON c.credit_account = cs.id

";
                }
                else
                {
                    query = @"
SELECT 
    c.id AS Id,
    c.date AS Date,
    c.invoice_id AS InvoiceNo,
    NULL AS JvNo,
    c.amount AS Amount,
    c.vat AS Vat,
    c.credit_account AS CreditAccount,
    c.debit_account AS DebitAccount,
    ts.invoice_id AS ItemInvoiceId,
    ts.inv_no AS ItemInvoiceNo,
    ts.invoice_date AS ItemInvoiceDate,
    ts.invoice_type AS ItemInvoiceType,
    ts.amount AS ItemAmount,
    ts.total AS ItemTotal,
    ts.remaining AS ItemRemaining,
    ts.vat AS ItemVat
FROM tbl_debit_note c
INNER JOIN tbl_debit_note_details ts ON c.id = ts.ref_id
WHERE c.state = 0
";
                }

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

                query += " ORDER BY c.date DESC, c.id DESC;";

                var debitNotes = new List<DebitNoteDto>();

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await using var reader = await cmd.ExecuteReaderAsync();

                DebitNoteDto currentNote = null;
                int lastId = -1;

                while (await reader.ReadAsync())
                {
                    int id = reader.GetInt32("Id");

                    if (id != lastId)
                    {
                        // New debit note
                        currentNote = new DebitNoteDto
                        {
                            Id = id,
                            Date = reader.GetDateTime("Date"),
                            InvoiceNo = reader["InvoiceNo"]?.ToString(),
                            JvNo = reader["JvNo"]?.ToString(),
                            Amount = reader.GetDecimal("Amount"),
                            Vat = reader.GetDecimal("Vat"),
                            CreditAccount = reader.GetInt32("CreditAccount"),
                            DebitAccount = reader.GetInt32("DebitAccount"),
                            Items = new List<DebitNoteItemDto>()
                        };

                        debitNotes.Add(currentNote);
                        lastId = id;
                    }

                    if (reader["ItemInvoiceId"] != DBNull.Value)
                    {
                        currentNote.Items.Add(new DebitNoteItemDto
                        {
                            InvoiceId = Convert.ToInt32(reader["ItemInvoiceId"]),
                            InvoiceNo = reader["ItemInvoiceNo"]?.ToString(),
                            InvoiceDate = Convert.ToDateTime(reader["ItemInvoiceDate"]),
                            InvoiceType = reader["ItemInvoiceType"]?.ToString(),
                            Amount = Convert.ToDecimal(reader["ItemAmount"]),
                            Total = reader["ItemTotal"] != DBNull.Value ? Convert.ToDecimal(reader["ItemTotal"]) : 0,
                            Remaining = reader["ItemRemaining"] != DBNull.Value ? Convert.ToDecimal(reader["ItemRemaining"]) : 0,
                            Vat = Convert.ToDecimal(reader["ItemVat"])
                        });
                    }
                }

                return Ok(new { status = true, data = debitNotes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetVendorInvoicess(int vendorId)
        {
            try
            {
                // Build MySQL connection
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Query to fetch vendor invoices with remaining balance > 0
                string query = @"
            SELECT 
                s.id AS Id,
                s.date AS Date,
                s.invoice_id AS InvoiceNo,
                s.total AS Total,
                s.net AS TotalWithVAT,
                s.vat AS Vat,
                s.`change` AS Remaining
            FROM tbl_purchase s
            INNER JOIN tbl_transaction t 
                ON t.transaction_id = s.id
                AND t.`type` IN ('Vendor Opening Balance', 'Purchase Invoice')
                AND t.hum_id = s.vendor_id
            WHERE s.vendor_id = @vendorId
            GROUP BY s.id, s.date, s.invoice_id, s.total, s.net, s.vat, s.`change`
            HAVING Remaining > 0;
        ";

                var invoices = new List<object>();
                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@vendorId", vendorId);

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
        public async Task<IActionResult> GetPurchaseInvoiceItems(int purchaseId)
        {
            try
            {
                // Build MySQL connection
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Query: Join purchase details with items
                string query = @"
            SELECT 
                CONCAT(ti.code,' - ', ti.name) AS ItemName, 
                ts.qty AS Qty, 
                ts.cost_price AS Price, 
                ts.vatp AS Vat, 
                ts.total AS Total 
            FROM tbl_purchase_details ts
            INNER JOIN tbl_items ti ON ts.item_id = ti.id
            WHERE ts.purchase_id = @purchaseId;
        ";

                var items = new List<object>();
                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@purchaseId", purchaseId);

                await using var reader = await cmd.ExecuteReaderAsync();
                int count = 1;

                while (await reader.ReadAsync())
                {
                    items.Add(new
                    {
                        Sn = count,
                        ItemName = reader["ItemName"]?.ToString(),
                        Qty = reader["Qty"] != DBNull.Value ? Convert.ToDecimal(reader["Qty"]) : 0,
                        Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0,
                        Vat = reader["Vat"] != DBNull.Value ? Convert.ToDecimal(reader["Vat"]) : 0,
                        Total = reader["Total"] != DBNull.Value ? Convert.ToDecimal(reader["Total"]) : 0
                    });

                    count++;
                }

                return Ok(new { status = true, data = items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveDebitNote([FromBody] DebitNoteRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            // 1️⃣ Required accounts check
            var requiredAccounts = new List<string> { "Sales", "Vendor", "COGS", "Customer", "Vat Input", "Vat Output", "Inventory" };
            if (!await AreDefaultAccountsSet(requiredAccounts))
                return Json(new { status = false, message = "Default accounts for invoice are not properly configured. Please check your settings." });

            // 2️⃣ Vendor selection validation
            if (model.VendorId <= 0)
                return Json(new { status = false, message = "Vendor must be selected." });

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

            // 6️⃣ Default Level Accounts check
            //if (model.Level4VendorId == 0 || model.Level4VatId == 0 || model.Level4PurchaseReturn == 0 ||
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

                int debitNoteId = model.Id;
                string invCode = model.InvoiceCode;

                if (debitNoteId == 0) // Insert new Debit Note
                {
                    invCode = await GenerateNextDebitNoteCode(conn);

                    var insertQuery = @"
INSERT INTO tbl_debit_note
(date, credit_account, debit_account, type, invoice_id, amount, vat, total, description, created_by, created_date, state)
VALUES (@date, @creditAccount, @debitAccount, @type, @invoice_id, @amount, @vat, @total, @description, @createdBy, @createdDate, 0);
SELECT LAST_INSERT_ID();";

                    using var cmd = new MySqlCommand(insertQuery, conn);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@creditAccount", model.VendorId);
                    cmd.Parameters.AddWithValue("@debitAccount", model.AccountCashId);
                    cmd.Parameters.AddWithValue("@type", "Vendor");
                    cmd.Parameters.AddWithValue("@invoice_id", invCode);
                    cmd.Parameters.AddWithValue("@amount", model.Amount);
                    cmd.Parameters.AddWithValue("@vat", model.Vat);
                    cmd.Parameters.AddWithValue("@total", model.TotalAmount);
                    cmd.Parameters.AddWithValue("@description", model.Description ?? "");
                    cmd.Parameters.AddWithValue("@createdBy", userId);
                    cmd.Parameters.AddWithValue("@createdDate", DateTime.Now);

                    debitNoteId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                else // Update existing Debit Note
                {
                   
                    var updateQuery = @"
UPDATE tbl_debit_note SET 
date=@date, credit_account=@creditAccount, debit_account=@debitAccount, invoice_id=@invoice_id,
amount=@amount, vat=@vat, total=@total, description=@description,
modified_by=@modifiedBy, modified_date=@modifiedDate
WHERE id=@id;";

                    using var cmd = new MySqlCommand(updateQuery, conn);
                    cmd.Parameters.AddWithValue("@id", debitNoteId);
                    cmd.Parameters.AddWithValue("@date", model.Date);
                    cmd.Parameters.AddWithValue("@creditAccount", model.VendorId);
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
                    using var deleteCmd = new MySqlCommand("DELETE FROM tbl_debit_note_details WHERE ref_id=@id", conn);
                    deleteCmd.Parameters.AddWithValue("@id", debitNoteId);
                    await deleteCmd.ExecuteNonQueryAsync();

                    // Delete old transaction entries
                    await DeleteTransactionEntries(conn, debitNoteId, "Debit Note", invCode);
                }

                // Insert Debit Note Items
                foreach (var item in model.Items)
                {
                    var insertItemQuery = @"
INSERT INTO tbl_debit_note_details
(ref_id, inv_no, invoice_id, invoice_date, invoice_type, total, vat, amount, balance, remaining)
VALUES (@refId, @invNo, @invId, @invDate, @invType, @total, @vat, @amount, @balance, @remaining);";

                    using var itemCmd = new MySqlCommand(insertItemQuery, conn);
                    itemCmd.Parameters.AddWithValue("@refId", debitNoteId);
                    itemCmd.Parameters.AddWithValue("@invNo", invCode);
                    itemCmd.Parameters.AddWithValue("@invId", debitNoteId);
                    itemCmd.Parameters.AddWithValue("@invDate", DateTime.Now);
                    itemCmd.Parameters.AddWithValue("@invType", "PURCHASE");
                    itemCmd.Parameters.AddWithValue("@total", item.Total);
                    itemCmd.Parameters.AddWithValue("@vat", item.Vat);
                    itemCmd.Parameters.AddWithValue("@amount", item.Amount);
                    itemCmd.Parameters.AddWithValue("@balance", item.Balance);
                    itemCmd.Parameters.AddWithValue("@remaining", item.Remaining);

                    await itemCmd.ExecuteNonQueryAsync();

                    // Update purchase paid / change in tbl_purchase
                    var paidResult = await new MySqlCommand("SELECT SUM(`change`) FROM tbl_purchase WHERE id=@id", conn)
                    {
                        Parameters = { new MySqlParameter("@id", item.InvoiceId) }
                    }.ExecuteScalarAsync();

                    decimal totalPaid = paidResult != DBNull.Value ? Convert.ToDecimal(paidResult) : 0;

                    var updatePurchaseCmd = new MySqlCommand(
                        "UPDATE tbl_purchase SET pay=@pay, `change`=@change WHERE id=@id", conn);
                    updatePurchaseCmd.Parameters.AddWithValue("@pay", totalPaid);
                    updatePurchaseCmd.Parameters.AddWithValue("@change", item.Remaining);
                    updatePurchaseCmd.Parameters.AddWithValue("@id", item.InvoiceId);
                    await updatePurchaseCmd.ExecuteNonQueryAsync();

                }

                // Add transaction entries
                await AddDebitNoteTransactions(conn, debitNoteId, model, userId, invCode);

                // Audit log for main Debit Note
         
                return Json(new
                {
                    status = true,
                    message = debitNoteId == model.Id ? "Debit Note updated successfully" : "Debit Note saved successfully",
                    id = debitNoteId
                });
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }

        public static async Task<string> GenerateNextDebitNoteCode(MySqlConnection conn)
        {
            string newCode = "DN-0001";

            var query = "SELECT MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)) AS lastCode FROM tbl_debit_note";
            using var cmd = new MySqlCommand(query, conn);
            var result = await cmd.ExecuteScalarAsync();

            if (result != DBNull.Value && result != null)
            {
                int code = Convert.ToInt32(result) + 1;
                newCode = "DN-" + code.ToString("D4");
            }

            return newCode;
        }

        public static async Task DeleteTransactionEntries(MySqlConnection conn, int debitNoteId,string type, string invCode)
        {
            string transactionType = $"Debit Note {invCode}";

            var query = "DELETE FROM tbl_transaction WHERE t_type = @tType AND transaction_id = @id";

            await using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", debitNoteId);
            cmd.Parameters.AddWithValue("@tType", transactionType);

            int rowsAffected = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"{rowsAffected} transaction(s) deleted for {transactionType}");
        }


        public static async Task AddDebitNoteTransactions(MySqlConnection conn, int debitNoteId, DebitNoteRequest model, int userId, string invCode)
        {
            // 1️⃣ Vendor Debit Entry
            await AddTransactionEntry(conn, model.Date, model.AccountCashId, model.Amount.ToString(), "0",
                debitNoteId.ToString(), model.VendorId.ToString(), $"Debit Note {invCode}", $"Debit Note {invCode}",
                $"Debit Note NO. {invCode}", userId, DateTime.Now, invCode);

            // 2️⃣ VAT Entry
            if (model.Vat > 0)
            {
                await AddTransactionEntry(conn, model.Date, model.AccountCashId, model.Vat.ToString(), "0",
                    debitNoteId.ToString(), "0", $"Debit Note {invCode}", $"Debit Note {invCode}",
                    $"Vat Input For Invoice No. {invCode}", userId, DateTime.Now, invCode);
            }

            // 3️⃣ Purchase Return / Revenue Entry
            await AddTransactionEntry(conn, model.Date, model.AccountCashId, model.Amount.ToString(), "0",
                debitNoteId.ToString(), "0", $"Debit Note {invCode}", $"Debit Note {invCode}",
                $"Revenue For Invoice No. {invCode}", userId, DateTime.Now, invCode);
        }

        public static async Task AddTransactionEntry(MySqlConnection conn, DateTime date, int AccountCashId, string debit, string credit,
            string transactionId, string humId, string tType, string type, string description, int createdBy, DateTime createdDate, string voucherNo)
        {
            var query = @"
INSERT INTO tbl_transaction
(date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no)
VALUES (@date, @accountId, @debit, @credit, @transactionId, @humId, @tType, @type, @description, @createdBy, @createdDate, 0, @voucherNo);";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@accountId", AccountCashId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transactionId", transactionId);
            cmd.Parameters.AddWithValue("@humId", humId);
            cmd.Parameters.AddWithValue("@tType", tType);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@createdBy", createdBy);
            cmd.Parameters.AddWithValue("@createdDate", createdDate);
            cmd.Parameters.AddWithValue("@voucherNo", voucherNo);

            await cmd.ExecuteNonQueryAsync();
        }


        #endregion

        #region Vendor Category
        [HttpGet]
        public async Task<IActionResult> GetVendorCategories()
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

                var query = "SELECT id, name FROM tbl_vendor_category ORDER BY name";
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
        #endregion



    }
}
