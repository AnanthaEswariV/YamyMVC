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
                    insertCmd.Parameters.AddWithValue("@project_id", projectSite);
                    insertCmd.Parameters.AddWithValue("@project_site", projectSite);

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
                    updateCmd.Parameters.AddWithValue("@project_id", projectSite);
                    updateCmd.Parameters.AddWithValue("@project_site", projectSite);
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
                0 AS Balance  -- placeholder, compute in C# if needed
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
                    balance += credit - debit; // compute balance manually

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
                            VendorName = reader["VendorName"]?.ToString(),
                            PaymentMethod = reader["PaymentMethod"]?.ToString(),
                            Total = reader.GetDecimal("Total"),
                            Vat = reader.GetDecimal("Vat"),
                            Net = reader.GetDecimal("Net"),
                            Items = new List<PurchaseItemDto>()
                        };

                        purchases.Add(currentPurchase);
                        lastPurchaseId = purchaseId;
                    }

                    // Item (only in Detailed mode)
                    if (selectionMethod != "Default" && reader["ItemName"] != DBNull.Value)
                    {
                        currentPurchase.Items.Add(new PurchaseItemDto
                        {
                            ItemName = reader["ItemName"]?.ToString(),

                            Qty = reader["Qty"] != DBNull.Value
                                ? Convert.ToDecimal(reader["Qty"])
                                : 0,

                            CostPrice = reader["CostPrice"] != DBNull.Value
                                ? Convert.ToDecimal(reader["CostPrice"])
                                : 0,

                            Vat = reader["ItemVat"] != DBNull.Value
                                ? Convert.ToDecimal(reader["ItemVat"])
                                : 0,

                            Total = reader["ItemTotal"] != DBNull.Value
                                ? Convert.ToDecimal(reader["ItemTotal"])
                                : 0
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
            CONCAT(v.code,' - ',v.name) AS VendorName,
            p.payment_method AS PaymentMethod,
            p.total AS Total,
            p.vat AS Vat,
            p.net AS Net
        FROM tbl_purchase p
        INNER JOIN tbl_vendor v ON p.vendor_id = v.id
        INNER JOIN tbl_transaction t ON p.id = t.transaction_id
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
            p.net AS Net,
            CONCAT(i.code,' - ',i.name) AS ItemName,
            d.qty AS Qty,
            d.cost_price AS CostPrice,
            d.vat AS ItemVat,
            d.total AS ItemTotal
        FROM tbl_purchase p
        INNER JOIN tbl_purchase_details d ON p.id = d.purchase_id
        INNER JOIN tbl_items i ON d.item_id = i.id
        INNER JOIN tbl_vendor v ON p.vendor_id = v.id
        INNER JOIN tbl_transaction t ON p.id = t.transaction_id
        WHERE p.state = 0
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
                        await DeleteTransactionEntry(conn, transaction, purchaseId, "PURCHASE");

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

        // 🔹 Helper Methods

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
            txnCmd.Parameters.AddWithValue("@costPrice", item.CostPrice);
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
                    "PURCHASE", $"Purchase Invoice NO. {model.InvoiceCode}", userId, DateTime.Now.Date, model.InvoiceCode);
            }

            // VAT transaction
            if (model.Vat > 0)
            {
                await AddTransactionEntry(conn, transaction, model.Date.Date, accountIds.VatId.ToString(),
                    model.Vat.ToString(), "0", purchaseId.ToString(), "0", "Purchase Invoice", "PURCHASE",
                    $"Vat Input For Invoice No. {model.InvoiceCode}", userId, DateTime.Now.Date, model.InvoiceCode);
            }

            // Inventory transaction
            if (model.TotalBefore > 0)
            {
                await AddTransactionEntry(conn, transaction, model.Date.Date, accountIds.InventoryId.ToString(),
                    model.TotalBefore.ToString(), "0", purchaseId.ToString(), "0", "Purchase Invoice", "PURCHASE",
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
            int purchaseId, int fixedAssetCategoryId)
        {
          
            var query = @"INSERT INTO tbl_fixed_assets (purchase_id, category_id) 
                         VALUES (@purchaseId, @categoryId)
                         ON DUPLICATE KEY UPDATE category_id = @categoryId;";

            await using var cmd = new MySqlCommand(query, conn, transaction);
            cmd.Parameters.AddWithValue("@purchaseId", purchaseId);
            cmd.Parameters.AddWithValue("@categoryId", fixedAssetCategoryId);
            await cmd.ExecuteNonQueryAsync();
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

        #endregion


    }
}
