using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace YamyProject.Controllers
{
    public class MigrationController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public MigrationController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }
  
        private readonly int _warehouseId = 1;          
        private readonly int _userId = 1;          
        private readonly string _paymentMethod = "Credit";   
        private readonly int _accountCashId = 0;         
        private readonly string _revenueItemName = "ايراد ضريبي"; 
        private readonly string _city = "";
        private readonly string _salesMan = "";

        private static readonly string[] _dateFormats =
            { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy" };

        private int _arAccountId, _salesAccountId, _vatAccountId, _revenueItemId;
        private long _invoiceSeq;
        [HttpPost]
        public async Task<IActionResult> ImportSalesInvoices(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { status = false, message = "Please select a file." });

            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, file.FileName);
            using (var s = new FileStream(path, FileMode.Create)) await file.CopyToAsync(s);

            var result = await ImportSalesInvoicesAsync(path);
            return Ok(new { status = true, message = "Import completed", result });
        }
        public async Task<object> ImportSalesInvoicesAsync(string filePath)
        {
            var rows = ReadRows(filePath);

            // ----- GROUP: same SN + same Customer + same Date => one invoice, each row => one line item -----
            var groups = rows
                .Select((r, i) => (row: r, idx: i))
                .GroupBy(x => string.IsNullOrWhiteSpace(x.row.Sn)
                    ? $"__row{x.idx}"
                    : $"{x.row.Sn.Trim()}|{x.row.Customer.Trim()}|{x.row.Date:yyyy-MM-dd}")
                .ToList();

            Console.WriteLine($"Total rows read: {rows.Count}, invoices (grouped): {groups.Count}");

            int success = 0, skipped = 0, failed = 0;
            var errors = new List<string>();

            var bootBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase"),
                CharacterSet = "utf8mb4"
            };
            using (var bconn = new MySqlConnection(bootBuilder.ConnectionString))
            {
                await bconn.OpenAsync();
                await LoadDefaultsAsync(bconn);
               
            }

            foreach (var grp in groups)
            {
                var lines = grp.OrderBy(x => x.idx).Select(x => x.row).ToList();
                var headRow = lines[0];
                string sn = headRow.Sn;

                // ---- compute totals FIRST (needed by isBlankRow) ----
                decimal totalBeforeVat = lines.Sum(l => l.Price);
                decimal vat = lines.Sum(l => l.Vat);
                decimal net = lines.Sum(l => l.Total);
                bool isBlankRow = net == 0m && totalBeforeVat == 0m;
                bool isCash = _paymentMethod == "Cash";

                // fix a garbage/zero date on blank rows
                if (headRow.Date == default || headRow.Date.Year < 2000)
                    headRow.Date = DateTime.Now.Date;

                Console.WriteLine($"SN={sn} lines={lines.Count} total={totalBeforeVat} vat={vat} net={net} (t+v={totalBeforeVat + vat})");

                try
                {
                    var connBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                    {
                        Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase"),
                        CharacterSet = "utf8mb4"
                    };
                    using var conn = new MySqlConnection(connBuilder.ConnectionString);
                    await conn.OpenAsync();
                    using var tx = await conn.BeginTransactionAsync();

                    try
                    {
                        // sanity check: all lines of one group should share a customer
                        if (lines.Select(l => l.Customer.Trim()).Distinct().Count() > 1)
                            Console.WriteLine($"[WARN] SN={sn} has mixed customers; using '{headRow.Customer}'.");

                        int customerId = await ResolveCustomerIdAsync(conn, tx, headRow.Customer);
                        if (customerId <= 0)
                        {
                            if (customerId <= 0)
                            {
                                skipped++;
                                await tx.RollbackAsync();
                                continue;            
                            }
                        }

                        int projectId = await ResolveProjectIdAsync(conn, tx, headRow.Project);

                        // invoice number = SN from the Excel (zero-padded to 4 digits to match SI-#### format)
                        string invoiceCode = int.TryParse(sn.Trim(), out var snNum)
                            ? $"SI-{snNum:D4}"          // e.g. SN 187 -> SI-0187
                            : $"SI-{sn.Trim()}";        // fallback if SN isn't a plain number
                        // 1) one header
                        long invId = await InsertSalesHeaderAsync(conn, tx, headRow, invoiceCode,
                                            customerId, projectId, totalBeforeVat, vat, net, isCash);

                        // 2) one detail row per line
                        foreach (var line in lines)
                            await InsertSalesDetailAsync(conn, tx, invId, projectId, line, line.Vat);

                        // 3) transactions use the SUMMED totals (one set per invoice)
                        string transactionType = isCash ? "Sales Invoice Cash" : "Sales Invoice";
                        string debitAccountId = isCash ? _accountCashId.ToString() : _arAccountId.ToString();

                        // skip the journal entirely for a 0.00 blank row
                        if (net > 0)
                        {
                            await AddTransactionEntryAsync(conn, tx, headRow.Date, debitAccountId,
                                net, 0, invId.ToString(), customerId.ToString(),
                                transactionType, "SALES",
                                $"Sales Invoice NO. {invoiceCode}", _userId, DateTime.Now.Date, invoiceCode);

                            await AddTransactionEntryAsync(conn, tx, headRow.Date, _salesAccountId.ToString(),
                                0, totalBeforeVat, invId.ToString(), "0",
                                transactionType, "SALES",
                                $"Sales Revenue For Invoice No. {invoiceCode}", _userId, DateTime.Now.Date, invoiceCode);

                            if (vat > 0)
                            {
                                await AddTransactionEntryAsync(conn, tx, headRow.Date, _vatAccountId.ToString(),
                                    0, vat, invId.ToString(), "0",
                                    transactionType, "SALES",
                                    $"Vat Output For Invoice No. {invoiceCode}", _userId, DateTime.Now.Date, invoiceCode);
                            }
                        }

                        await tx.CommitAsync();
                        success++;
                        Console.WriteLine($"[OK]   SN={sn}  code={invoiceCode}  lines={lines.Count}  cust={customerId}  net={net}  invId={invId}");
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        failed++;
                        errors.Add($"SN {sn}: {ex.Message}");
                        Console.WriteLine($"[FAIL] SN={sn}  => {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"SN {sn}: (connection) {ex.Message}");
                    Console.WriteLine($"[FAIL] SN={sn}  (connection error) => {ex.Message}");
                }
            }

            Console.WriteLine("===========================================");
            Console.WriteLine($"Done.  Success={success}  Skipped={skipped}  Failed={failed}");
            return new { totalRows = rows.Count, invoices = groups.Count, success, skipped, failed, errors };
        }
        private async Task LoadDefaultsAsync(MySqlConnection conn)
        {
            var accounts = new Dictionary<string, int>();
            using (var cmd = new MySqlCommand("SELECT category, account_id FROM tbl_coa_config", conn))
            using (var r = await cmd.ExecuteReaderAsync())
                while (await r.ReadAsync())
                    accounts[r.GetString("category")] = r.GetInt32("account_id");

            _arAccountId = accounts.TryGetValue("Customer", out var a) ? a : 0;
            _salesAccountId = accounts.TryGetValue("Sales", out var s) ? s : 0;
            _vatAccountId = accounts.TryGetValue("Vat Output", out var v) ? v : 0;

            if (_arAccountId <= 0 || _salesAccountId <= 0 || _vatAccountId <= 0)
                throw new Exception("tbl_coa_config missing one of: Customer / Sales / Vat Output.");

            using (var cmd = new MySqlCommand("SELECT id FROM tbl_items WHERE name = @n LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@n", _revenueItemName);
                var res = await cmd.ExecuteScalarAsync();
                _revenueItemId = (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
            }
            if (_revenueItemId <= 0)
                throw new Exception($"Service item '{_revenueItemName}' not found in tbl_items.");
        }

        private async Task<long> GetMaxInvoiceSeqAsync(MySqlConnection conn)
        {
            using var cmd = new MySqlCommand(
                "SELECT MAX(CAST(SUBSTRING(invoice_id, 4) AS UNSIGNED)) FROM tbl_sales", conn);
            var res = await cmd.ExecuteScalarAsync();
            return (res != null && res != DBNull.Value) ? Convert.ToInt64(res) : 0;
        }

        // near your other fields
        private static readonly Dictionary<string, string> _customerAliases =
            new(StringComparer.Ordinal)
        {
    // sheet spelling            ->  tbl_customer spelling
    { "بخيتة المزورعى", "بخيتة المزروعى" },
                // add more here as you discover them
        };
        private async Task<int> ResolveCustomerIdAsync(MySqlConnection conn, MySqlTransaction tx, string name)
        {
            var lookup = name?.Trim() ?? "";
            if (_customerAliases.TryGetValue(lookup, out var corrected))
                lookup = corrected;                         // remap misspelled sheet name

            using var cmd = new MySqlCommand(
                "SELECT id FROM tbl_customer WHERE TRIM(name) = TRIM(@name) LIMIT 1", conn, tx);
            cmd.Parameters.AddWithValue("@name", lookup);
            var res = await cmd.ExecuteScalarAsync();
            return (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
        }

        private async Task<int> ResolveProjectIdAsync(MySqlConnection conn, MySqlTransaction tx, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            using var cmd = new MySqlCommand(
                "SELECT id FROM tbl_projects WHERE name = @name LIMIT 1", conn, tx);    // <-- table/col?
            cmd.Parameters.AddWithValue("@name", name);
            var res = await cmd.ExecuteScalarAsync();
            return (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
        }

        private async Task<long> InsertSalesHeaderAsync(MySqlConnection conn, MySqlTransaction tx,
            InvoiceRow row, string invoiceCode, int customerId, int projectId,
            decimal totalBeforeVat, decimal vat, decimal net, bool isCash)
        {
            var sql = @"
            INSERT INTO tbl_sales
            (date, customer_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man,
             ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date,
             total, vat, net, pay, `change`, created_by, created_date, state, project_id)
            VALUES
            (@date, @customer_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man,
             @ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms, @payment_date,
             @total, @vat, @net, @pay, @change, @created_by, @created_date, @state, @project_id);
            SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", row.Date);
            cmd.Parameters.AddWithValue("@customer_id", customerId);
            cmd.Parameters.AddWithValue("@invoice_id", invoiceCode);
            cmd.Parameters.AddWithValue("@warehouse_id", _warehouseId);
            cmd.Parameters.AddWithValue("@po_num", row.Sn ?? "");          // Noble serial kept for traceability
            cmd.Parameters.AddWithValue("@bill_to", row.Customer ?? "");
            cmd.Parameters.AddWithValue("@city", _city);
            cmd.Parameters.AddWithValue("@sales_man", _salesMan);
            cmd.Parameters.AddWithValue("@ship_date", row.Date);
            cmd.Parameters.AddWithValue("@ship_via", "");
            cmd.Parameters.AddWithValue("@ship_to", "");
            cmd.Parameters.AddWithValue("@payment_method", _paymentMethod);
            cmd.Parameters.AddWithValue("@account_cash_id", _accountCashId);
            cmd.Parameters.AddWithValue("@payment_terms", "");
            cmd.Parameters.AddWithValue("@payment_date", row.Date);
            cmd.Parameters.AddWithValue("@total", totalBeforeVat);
            cmd.Parameters.AddWithValue("@vat", vat);
            cmd.Parameters.AddWithValue("@net", net);
            cmd.Parameters.AddWithValue("@pay", isCash ? net : 0);
            cmd.Parameters.AddWithValue("@change", isCash ? 0 : net);   // mirrors SaveInvoice
            cmd.Parameters.AddWithValue("@created_by", _userId);
            cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
            cmd.Parameters.AddWithValue("@state", 0);
            cmd.Parameters.AddWithValue("@project_id", projectId);

            return Convert.ToInt64(await cmd.ExecuteScalarAsync());
        }

        private async Task InsertSalesDetailAsync(MySqlConnection conn, MySqlTransaction tx,
            long salesId, int projectId, InvoiceRow row, decimal vat)
        {
            try
            {
                var sql = @"
            INSERT INTO tbl_sales_details
            (sales_id, item_id, description, qty, cost_price, price, discount, vatp, vat, total, cost_center_id, project_id)
            VALUES
            (@sales_id, @item_id,@description, @qty, @cost_price, @price, @discount, @vatp, @vat, @total, @cost_center_id, @project_id);";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@sales_id", salesId);
                cmd.Parameters.AddWithValue("@item_id", _revenueItemId);
                cmd.Parameters.AddWithValue("@description", row.Description ?? "");
                cmd.Parameters.AddWithValue("@qty", row.Qty);
                cmd.Parameters.Add("@cost_price", MySqlDbType.Decimal).Value = DBNull.Value; // service => no cost
                cmd.Parameters.AddWithValue("@price", row.Price);
                cmd.Parameters.AddWithValue("@discount", 0);
                cmd.Parameters.AddWithValue("@vatp", vat > 0 ? 5 : 0);   // 5% standard / 0 for zero-rated & retention
                cmd.Parameters.AddWithValue("@vat", vat);                // note: column is INT, amount is truncated
                cmd.Parameters.AddWithValue("@total", row.Total);
                cmd.Parameters.Add("@cost_center_id", MySqlDbType.Int32).Value = DBNull.Value;
                cmd.Parameters.AddWithValue("@project_id", projectId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        private async Task AddTransactionEntryAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, string accountId, decimal debit, decimal credit, string transactionId,
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
            cmd.Parameters.AddWithValue("@type", type);          // -> column `type`  (e.g. "Sales Invoice")
            cmd.Parameters.AddWithValue("@tType", voucherName);  // -> column `t_type` (e.g. "SALES")
            cmd.Parameters.AddWithValue("@hum_id", humId);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@createdBy", createdBy);
            cmd.Parameters.AddWithValue("@createdDate", createdDate);
            cmd.Parameters.AddWithValue("@voucher_no", voucherNo);
            await cmd.ExecuteNonQueryAsync();
        }

       

        private List<InvoiceRow> ReadRows(string filePath)
        {
        var rows = new List<InvoiceRow>();
        using var wb = new XLWorkbook(filePath);
        var ws = wb.Worksheets.First();

        foreach (var r in ws.RowsUsed())
        {
            string dateCell = r.Cell("B").GetString().Trim();
            string item = r.Cell("C").GetString().Trim();

            // skip the header row(s)
            if (dateCell.Equals("Date", StringComparison.OrdinalIgnoreCase) ||
                item.Equals("Item", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrWhiteSpace(dateCell) && string.IsNullOrWhiteSpace(item)) continue;

            DateTime date;
            var bCell = r.Cell("B");
            if (bCell.DataType == XLDataType.DateTime)
                date = bCell.GetDateTime();
            else if (!TryParseDate(dateCell, out date))
            {
                Console.WriteLine($"Row {r.RowNumber()} bad date: {dateCell}");
                continue;
            }

            rows.Add(new InvoiceRow
            {
                Sn = r.Cell("D").GetString().Trim(),
                Date = date,
                Customer = r.Cell("E").GetString().Trim(),
                Project = r.Cell("F").GetString().Trim(),
                Description = r.Cell("G").GetString().Trim(),
                Qty = ParseExcelNum(r.Cell("H")),
                Price = ParseExcelNum(r.Cell("I")),
                Vat = ParseExcelNum(r.Cell("J")),   // "-" cells come back 0
                Total = ParseExcelNum(r.Cell("K")),
            });
        }

        Console.WriteLine($"ReadRows: parsed {rows.Count}");
        return rows;
    }

        private static decimal ParseExcelNum(IXLCell cell)
    {
        if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
        var s = cell.GetString().Replace(",", "").Trim();
        if (s.Length == 0 || s == "-") return 0m;
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
    }

        private static bool TryParseDate(string s, out DateTime date)
        {
            if (DateTime.TryParseExact(s, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;
            // Excel serial fallback (e.g. 44959)
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
            { date = DateTime.FromOADate(oa); return true; }
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        private static decimal ParseNum(string s)
        {
            s = (s ?? "").Replace(",", "").Trim();
            if (s.Length == 0 || s == "-") return 0m;
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
        }

        private class InvoiceRow
        {
            public string Sn { get; set; } = "";
            public DateTime Date { get; set; }
            public string Customer { get; set; } = "";
            public string Project { get; set; } = "";
            public string Description { get; set; } = "";
            public decimal Qty { get; set; }
            public decimal Price { get; set; }
            public decimal Vat { get; set; }
            public decimal Total { get; set; }
        }
    }

}
