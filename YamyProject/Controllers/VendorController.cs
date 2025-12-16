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
                    ORDER BY t.id ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                ) AS Balance
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
                cmd.Parameters.Add(new MySqlParameter("@id", id));

                using var reader = await cmd.ExecuteReaderAsync();
                var transactions = new List<object>();

                while (await reader.ReadAsync())
                {
                    string invoiceId = reader["InvoiceId"]?.ToString() ?? "0";
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
                    query += " AND (" + string.Join(" OR ", includeTypes.Select((t, i) => $"t.type LIKE @include{i}")) + ")";
                if (excludeTypes.Any())
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

                // Add total row like WinForms
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




    }
}
