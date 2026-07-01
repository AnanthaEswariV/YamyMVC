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
        //private readonly int _userId = 1;          
       private readonly string _paymentMethod = "Credit";   
        private readonly int _accountCashId = 0;         
        private readonly string _revenueItemName = "ايراد ضريبي"; 
        private readonly string _city = "";
        private readonly string _salesMan = "";
        private long _vendorSeq;
        private readonly int _serviceItemId = 1;          
        private readonly string _purchaseType = "Service";
        private readonly int _fallbackItemId = 1;     
        private readonly Dictionary<string, int> _itemCache = new();
        private Dictionary<string, int> _vendorByName = new();
        // private readonly string _paymentType = "Vendor";
        private readonly string _method = "Cheque";
        private readonly bool _isSubContractor = false;
        // resolved defaults (fallbacks)
        private int _apAccountId;          // default Accounts Payable (coa_config "Vendor")
        private int _pdcPayableAccountId;  // default credit side (coa_config "PDC Payable")
        private long _pvSeq;               // running PV number
        private readonly Dictionary<string, int> _accountByName = new();
        private readonly Dictionary<string, int> _accountByBank = new();

        private readonly string _paymentType = "General";
        private int _arAccountId;        // default Customer A/R (coa_config "Customer")
        private int _cashAccountId;      // fallback bank/cash account if a name doesn't resolve
        private int _userId;
        private readonly Dictionary<string, int> _projectByName = new();

        #region SalesInvoice

        private static readonly string[] _dateFormats =
            { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "MM/dd/yyyy" };

        private int  _salesAccountId, _vatAccountId, _revenueItemId;
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

        #endregion

        #region PurchaseInvoice Subcontractor

        private int _vendorApAccountId, _purchaseAccountId, _vatInputAccountId, _retentionAccountId;
        private readonly Dictionary<string, decimal> _retentionByInvoice = new();
        [HttpPost]
        public async Task<IActionResult> ImportPurchaseInvoices(IFormFile file)
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { status = false, message = "Please select a file." });

                var dir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, file.FileName);
                using (var s = new FileStream(path, FileMode.Create)) await file.CopyToAsync(s);

                var result = await ImportPurchaseInvoicesAsync(path);
                return Ok(new { status = true, message = "Import completed", result });
            }

        public async Task<object> ImportPurchaseInvoicesAsync(string filePath)
            {
                var rows = ReadRowss(filePath);

                // group: same Invoice No + Vendor + Date => one purchase invoice
                var groups = rows
                    .Select((r, i) => (row: r, idx: i))
                    .GroupBy(x => $"{x.row.InvoiceNo.Trim()}|{x.row.Vendor.Trim()}|{x.row.Date:yyyy-MM-dd}")
                    .ToList();

                Console.WriteLine($"Total rows: {rows.Count}, purchase invoices (grouped): {groups.Count}");

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
                    await LoadDefaultsAsyncs(bconn);
                }

                foreach (var grp in groups)
                {
                    var allLines = grp.OrderBy(x => x.idx).Select(x => x.row).ToList();
                    var headRow = allLines[0];
                    string invNo = headRow.InvoiceNo.Trim();

                    // split: real items vs discount rows
                    var itemLines = allLines.Where(l => !l.IsDiscount).ToList();
                    var discTotal = allLines.Where(l => l.IsDiscount).Sum(l => Math.Abs(l.Total));

                    if (itemLines.Count == 0)
                    {
                        skipped++;
                        errors.Add($"INV {invNo}: only discount rows, no item line.");
                        continue;
                    }

                    // apply the whole discount to the FIRST item
                    itemLines[0].Discount = discTotal;
                    itemLines[0].Total -= discTotal;   // reduce first item's net (VAT here is 0)

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
                            if (allLines.Select(l => l.Vendor.Trim()).Distinct().Count() > 1)
                                Console.WriteLine($"[WARN] INV={invNo} mixed vendors; using '{headRow.Vendor}'.");

                            int vendorId = await ResolveVendorIdAsync(conn, tx, headRow.Vendor);
                            if (vendorId <= 0)
                            {
                                skipped++;
                                errors.Add($"INV {invNo}: vendor not found '{headRow.Vendor}'");
                                await tx.RollbackAsync();
                                continue;
                            }
                            int projectId = await ResolveProjectIdAsyncs(conn, tx, headRow.Project);

                        // invoice totals (after discount on first item)
                        decimal totalBefore = itemLines.Sum(l => l.Total);
                        decimal vat = itemLines.Sum(l => l.Vat);
                        decimal net = totalBefore + vat;
                        bool isCash = _paymentMethod == "Cash";
                        decimal retentionAmount = _retentionByInvoice.TryGetValue(invNo, out var ra) ? ra : 0m;
                        decimal retentionPct = net > 0 ? Math.Round(retentionAmount / net * 100m, 2) : 0m;

                        // invoice number = Excel Invoice No
                        string invoiceCode = int.TryParse(invNo, out var n) ? $"PI-{n:D4}" : $"PI-{invNo}";

                        // 1) header
                        long purchaseId = await InsertPurchaseHeaderAsync(conn, tx, headRow, invoiceCode,
                     vendorId, projectId, totalBefore, vat, net, isCash, retentionAmount, retentionPct);

                        // 2) detail rows (one per item)
                        foreach (var it in itemLines)
                                await InsertPurchaseDetailAsync(conn, tx, purchaseId, projectId, it);

                            // 3) transactions
                            string debitDesc = isCash ? "Purchase Invoice Cash" : "Purchase Invoice";
                            string apAccount = isCash ? _accountCashId.ToString() : _vendorApAccountId.ToString();

                            // 3a) Debit expense (per item, by its account code; fallback Purchase)
                            foreach (var it in itemLines)
                            {
                                int expAcc = await ResolveAccountIdByCodeAsync(conn, tx, it.AccountCode);
                                if (expAcc <= 0) expAcc = _purchaseAccountId;
                                if (it.Total != 0)
                                    await AddTransactionEntryAsyncs(conn, tx, headRow.Date, expAcc.ToString(),
                                        it.Total, 0, purchaseId.ToString(), "0",
                                        "Purchase Invoice", debitDesc,
                                        $"Expense For Invoice No. {invoiceCode}", _userId, DateTime.Now.Date, invoiceCode);
                            }

                            // 3b) Debit VAT Input
                            if (vat > 0)
                                await AddTransactionEntryAsyncs(conn, tx, headRow.Date, _vatInputAccountId.ToString(),
                                    vat, 0, purchaseId.ToString(), "0",
                                    "Purchase Invoice", debitDesc,
                                    $"Vat Input For Invoice No. {invoiceCode}", _userId, DateTime.Now.Date, invoiceCode);


                        // 3c) Credit Vendor A/P — reduced by retention so the entry balances
                        decimal vendorCredit = net - retentionAmount;
                        if (vendorCredit != 0)
                            await AddTransactionEntryAsyncs(conn, tx, headRow.Date, apAccount,
                                0, vendorCredit, purchaseId.ToString(), vendorId.ToString(),
                                "Purchase Invoice", debitDesc,
                                $"Purchase Invoice NO. {invoiceCode}", _userId, DateTime.Now.Date, invoiceCode);

                        // 3d) Credit Retention Payable (held back from subcontractor)
                        if (retentionAmount > 0 && _retentionAccountId > 0)
                            await AddTransactionEntryAsyncs(conn, tx, headRow.Date, _retentionAccountId.ToString(),
                                0, retentionAmount, purchaseId.ToString(), vendorId.ToString(),
                                "Purchase Invoice", debitDesc,
                                $"Retention From Subcontractors For Invoice No. {invoiceCode}", _userId, DateTime.Now.Date, invoiceCode);

                        await tx.CommitAsync();
                            success++;
                            Console.WriteLine($"[OK]   INV={invNo} code={invoiceCode} items={itemLines.Count} disc={discTotal} net={net} id={purchaseId}");
                        }
                        catch (Exception ex)
                        {
                            await tx.RollbackAsync();
                            failed++;
                            errors.Add($"INV {invNo}: {ex.Message}");
                            Console.WriteLine($"[FAIL] INV={invNo} => {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        errors.Add($"INV {invNo}: (connection) {ex.Message}");
                        Console.WriteLine($"[FAIL] INV={invNo} (conn) => {ex.Message}");
                    }
                }

                Console.WriteLine("===========================================");
                Console.WriteLine($"Done.  Success={success}  Skipped={skipped}  Failed={failed}");
                return new { totalRows = rows.Count, invoices = groups.Count, success, skipped, failed, errors };
            }

        private async Task LoadDefaultsAsyncs(MySqlConnection conn)
            {
                var acc = new Dictionary<string, int>();
                using (var cmd = new MySqlCommand("SELECT category, account_id FROM tbl_coa_config", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        acc[r.GetString("category")] = r.GetInt32("account_id");

                _vendorApAccountId = acc.TryGetValue("Vendor", out var v) ? v : 0;
                _purchaseAccountId = acc.TryGetValue("Purchase", out var p) ? p : 0;
                _vatInputAccountId = acc.TryGetValue("Vat Input", out var t) ? t : 0;
                _retentionAccountId = acc.TryGetValue("Retention", out var rs) ? rs : 0;  
            if (_vendorApAccountId <= 0 || _purchaseAccountId <= 0 || _vatInputAccountId <= 0)
                    throw new Exception("tbl_coa_config missing one of: Vendor / Purchase / Vat Input.");
            }

        //private async Task<int> ResolveVendorIdAsync(MySqlConnection conn, MySqlTransaction tx, string name)
        //    {
        //        using var cmd = new MySqlCommand(
        //            "SELECT id FROM tbl_vendor WHERE TRIM(name) = TRIM(@name) LIMIT 1", conn, tx);
        //        cmd.Parameters.AddWithValue("@name", name);
        //        var res = await cmd.ExecuteScalarAsync();
        //        return (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
        //    }

        private async Task<int> ResolveProjectIdAsyncs(MySqlConnection conn, MySqlTransaction tx, string name)
            {
                if (string.IsNullOrWhiteSpace(name)) return 0;
                using var cmd = new MySqlCommand(
                    "SELECT id FROM tbl_projects WHERE TRIM(name) = TRIM(@name) LIMIT 1", conn, tx);  // TODO table/col
                cmd.Parameters.AddWithValue("@name", name);
                var res = await cmd.ExecuteScalarAsync();
                return (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
            }

        private async Task<int> ResolveAccountIdByCodeAsync(MySqlConnection conn, MySqlTransaction tx, string code)
            {
                if (string.IsNullOrWhiteSpace(code) || code.Trim().Equals("Discount", StringComparison.OrdinalIgnoreCase))
                    return 0;
                using var cmd = new MySqlCommand(
                    "SELECT id FROM tbl_coa_level_4 WHERE code = @code LIMIT 1", conn, tx);   // TODO confirm table/col
                cmd.Parameters.AddWithValue("@code", code.Trim());
                var res = await cmd.ExecuteScalarAsync();
                return (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
            }

        private async Task<long> InsertPurchaseHeaderAsync(MySqlConnection conn, MySqlTransaction tx,
         PurchaseRow row, string invoiceCode, int vendorId, int projectId,
         decimal totalBefore, decimal vat, decimal net, bool isCash, decimal retentionAmount, decimal retentionPct)
        {
                var sql = @"
                INSERT INTO tbl_purchase
                (date, vendor_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man,
                 ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date,
                 total, vat, net, pay, `change`, created_by, created_date, state, purchase_type,
                 fixed_asset_category_id, project_id,retention_percentage, retention_amount)
                VALUES
                (@date, @vendor_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man,
                 @ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms, @payment_date,
                 @total, @vat, @net, @pay, @change, @created_by, @created_date, 0, @purchase_type,
                 0, @project_id,@retention_percentage, @retention_amount);
                SELECT LAST_INSERT_ID();";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@date", row.Date);
                cmd.Parameters.AddWithValue("@vendor_id", vendorId);
                cmd.Parameters.AddWithValue("@invoice_id", invoiceCode);
                cmd.Parameters.AddWithValue("@warehouse_id", _warehouseId);
                cmd.Parameters.AddWithValue("@po_num", row.InvoiceNo ?? "");   
                cmd.Parameters.AddWithValue("@bill_to", row.Vendor ?? "");
                cmd.Parameters.AddWithValue("@city", _city);
                cmd.Parameters.AddWithValue("@sales_man", _salesMan);
                cmd.Parameters.AddWithValue("@ship_date", row.Date);
                cmd.Parameters.AddWithValue("@ship_via", "");
                cmd.Parameters.AddWithValue("@ship_to", "");
                cmd.Parameters.AddWithValue("@payment_method", _paymentMethod);
                cmd.Parameters.AddWithValue("@account_cash_id", _accountCashId);
                cmd.Parameters.AddWithValue("@payment_terms", "");
                cmd.Parameters.AddWithValue("@payment_date", row.Date);
                cmd.Parameters.AddWithValue("@total", totalBefore);
                cmd.Parameters.AddWithValue("@vat", vat);
                cmd.Parameters.AddWithValue("@net", net);
                cmd.Parameters.AddWithValue("@pay", isCash ? net : 0);
                cmd.Parameters.AddWithValue("@change", isCash ? 0 : net);
                cmd.Parameters.AddWithValue("@created_by", _userId);
                cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                cmd.Parameters.AddWithValue("@purchase_type", _purchaseType);
                cmd.Parameters.AddWithValue("@project_id", projectId);
                cmd.Parameters.AddWithValue("@retention_percentage", retentionPct);     
                cmd.Parameters.AddWithValue("@retention_amount", retentionAmount);

            return Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }

        private async Task InsertPurchaseDetailAsync(MySqlConnection conn, MySqlTransaction tx,
            long purchaseId, int projectId, PurchaseRow it)
            {
            try
            {
                var sql = @"
                INSERT INTO tbl_purchase_details
                (purchase_id, item_id, description, qty, cost_price, price, discount, vatp, vat, total, cost_center_id)
                VALUES
                (@purchase_id, @item_id,@description, @qty, @cost_price, @price, @discount, @vatp, @vat, @total, @cost_center_id);";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@purchase_id", purchaseId);
                cmd.Parameters.AddWithValue("@item_id", _serviceItemId);
                cmd.Parameters.AddWithValue("@description", it.Description);
                cmd.Parameters.AddWithValue("@qty", it.Qty);
                cmd.Parameters.AddWithValue("@cost_price", it.Price);
                cmd.Parameters.AddWithValue("@price", it.Price);
                cmd.Parameters.AddWithValue("@discount", it.Discount);
                cmd.Parameters.AddWithValue("@vatp", it.Vat > 0 ? 5 : 0);
                cmd.Parameters.AddWithValue("@vat", it.Vat);
                cmd.Parameters.AddWithValue("@total", it.Total + it.Vat);   // line total incl vat
                cmd.Parameters.Add("@cost_center_id", MySqlDbType.Int32).Value =
                    projectId > 0 ? projectId : (object)DBNull.Value;       // map project->cost center if you use it
                await cmd.ExecuteNonQueryAsync();
            }
            catch(Exception ex)
            {
                throw ex;
            }
            }

        private async Task AddTransactionEntryAsyncs(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, string accountId, decimal debit, decimal credit, string transactionId,
            string humId, string voucherName, string type, string description, int createdBy,
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
                cmd.Parameters.AddWithValue("@tType", voucherName);  // t_type column ("Purchase Invoice")
                cmd.Parameters.AddWithValue("@type", type);          // type column   ("Purchase Invoice"/"...Cash")
                cmd.Parameters.AddWithValue("@hum_id", humId);
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@createdBy", createdBy);
                cmd.Parameters.AddWithValue("@createdDate", createdDate);
                cmd.Parameters.AddWithValue("@voucher_no", voucherNo);
                await cmd.ExecuteNonQueryAsync();
            }

        private List<PurchaseRow> ReadRowss(string filePath)
            {
                var rows = new List<PurchaseRow>();
                using var wb = new XLWorkbook(filePath);
                var ws = wb.Worksheets.First();

                foreach (var r in ws.RowsUsed())
                {
                    string dateCell = r.Cell("B").GetString().Trim();
                    string invNo = r.Cell("C").GetString().Trim();

                    if (dateCell.Equals("Date", StringComparison.OrdinalIgnoreCase)) continue;        // header
                    if (string.IsNullOrWhiteSpace(dateCell) && string.IsNullOrWhiteSpace(invNo)) continue;

                    DateTime date;
                    var bCell = r.Cell("B");
                    if (bCell.DataType == XLDataType.DateTime) date = bCell.GetDateTime();
                    else if (!TryParseDates(dateCell, out date)) { Console.WriteLine($"Row {r.RowNumber()} bad date: {dateCell}"); continue; }

                    string acctCode = r.Cell("F").GetString().Trim();
                    string oInv = r.Cell("O").GetString().Trim();
                    if (oInv.Length > 0 && !oInv.Equals("Invoice", StringComparison.OrdinalIgnoreCase))
                    {
                        decimal retAmt = ParseExcelNums(r.Cell("P"));
                        _retentionByInvoice[oInv] = retAmt;   
                    }
                rows.Add(new PurchaseRow
                    {
                        Date = date,
                        InvoiceNo = invNo,
                        Description = r.Cell("D").GetString().Trim(),
                        Vendor = r.Cell("E").GetString().Trim(),
                        AccountCode = acctCode,
                        Project = r.Cell("H").GetString().Trim(),
                        Qty = ParseExcelNums(r.Cell("I")),
                        Price = ParseExcelNums(r.Cell("J")),
                        Total = ParseExcelNums(r.Cell("K")), 
                        Vat = ParseExcelNums(r.Cell("L")),
                        IsDiscount = acctCode.Equals("Discount", StringComparison.OrdinalIgnoreCase),
                        Discount = 0m
                    });
                }

                Console.WriteLine($"ReadRows: parsed {rows.Count}");
                return rows;
            }

        private static decimal ParseExcelNums(IXLCell cell)
            {
                if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
                var s = cell.GetString().Replace(",", "").Trim();
                if (s.Length == 0 || s == "-") return 0m;
                return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
            }

        private static bool TryParseDates(string s, out DateTime date)
            {
                if (DateTime.TryParseExact(s, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    return true;
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
                { date = DateTime.FromOADate(oa); return true; }
                return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
            }

        private class PurchaseRow
            {
                public DateTime Date { get; set; }
                public string InvoiceNo { get; set; } = "";
                public string Description { get; set; } = "";
                public string Vendor { get; set; } = "";
                public string AccountCode { get; set; } = "";
                public string Project { get; set; } = "";
                public decimal Qty { get; set; }
                public decimal Price { get; set; }
                public decimal Total { get; set; }  // before VAT, after disc
                public decimal Vat { get; set; }
                public decimal Discount { get; set; }
                public bool IsDiscount { get; set; }
            }

        #endregion

        #region PurchaseInvoice Vendor

         [HttpPost]
         public async Task<IActionResult> ImportPurchaseInvoices2(IFormFile file)
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { status = false, message = "Please select a file." });

                var dir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, file.FileName);
                using (var s = new FileStream(path, FileMode.Create)) await file.CopyToAsync(s);

                var result = await ImportPurchaseInvoices2Async(path);
                return Ok(new { status = true, message = "Import completed", result });
            }

         public async Task<object> ImportPurchaseInvoices2Async(string filePath)
            {
                var rows = Read2Rows(filePath);

                var groups = rows
                    .Select((r, i) => (row: r, idx: i))
                    .GroupBy(x => $"{x.row.InvoiceNo.Trim()}|{x.row.Vendor.Trim()}|{x.row.Date:yyyy-MM-dd}")
                    .ToList();

                Console.WriteLine($"Total rows: {rows.Count}, purchase invoices (grouped): {groups.Count}");

                int success = 0, skipped = 0, failed = 0;
                var errors = new List<string>();
                var skippedInvoices = new List<string>();
                var bootBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase"),
                    CharacterSet = "utf8mb4"
                };
                using (var bconn = new MySqlConnection(bootBuilder.ConnectionString))
                {
                    await bconn.OpenAsync();
                    await LoadDefaults2Async(bconn);
                }

                foreach (var grp in groups)
                {
                    var lines = grp.OrderBy(x => x.idx).Select(x => x.row).ToList();
                    var headRow = lines[0];
                    string invNo = headRow.InvoiceNo.Trim();
                
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
                            if (lines.Select(l => l.Vendor.Trim()).Distinct().Count() > 1)
                                Console.WriteLine($"[WARN] INV={invNo} mixed vendors; using '{headRow.Vendor}'.");

                        int vendorId = await GetOrCreateVendorIdAsync(conn, tx, headRow.Vendor);
                        if (vendorId <= 0)
                        {
                            skipped++;

                            string skipInfo = $"[SKIP] INV={invNo} Vendor='{headRow.Vendor}' Date={headRow.Date:yyyy-MM-dd}";

                            skippedInvoices.Add(skipInfo);

                            Console.WriteLine(skipInfo);

                            errors.Add(skipInfo);

                            await tx.RollbackAsync();
                            continue;
                        }

                        int projectId = await ResolveProjectIdAsync(conn, tx, headRow.Project);

                            decimal totalBefore = lines.Sum(l => l.Amount);
                            decimal vat = lines.Sum(l => l.Vat);
                            decimal net = lines.Sum(l => l.Total);
                            bool isCash = _paymentMethod == "Cash";

                            string invoiceCode = int.TryParse(invNo, out var n) ? $"PI-{n:D4}" : $"PI-{invNo}";

                            long purchaseId = await InsertPurchaseHeaderAsync(conn, tx, headRow, invoiceCode,
                                                    vendorId, projectId, totalBefore, vat, net, isCash);

                            foreach (var it in lines)
                            {
                                int itemId = await ResolveItemIdAsync(conn, tx, it.ItemName);
                                await InsertPurchaseDetailAsync(conn, tx, purchaseId, projectId, itemId, it);
                            }

                            string debitDesc = isCash ? "Purchase Invoice Cash" : "Purchase Invoice";
                            string apAccount = isCash ? _accountCashId.ToString() : _vendorApAccountId.ToString();

                            // Debit Purchase (Amount before VAT)
                            if (totalBefore != 0)
                                await AddTransactionEntry2Async(conn, tx, headRow.Date, _purchaseAccountId.ToString(),
                                    totalBefore, 0, purchaseId.ToString(), "0",
                                    "Purchase Invoice", debitDesc,
                                    $"Purchase For Invoice No. {invoiceCode}", _userId, DateTime.Now.Date, invoiceCode);

                            // Debit VAT Input
                            if (vat > 0)
                                await AddTransactionEntry2Async(conn, tx, headRow.Date, _vatInputAccountId.ToString(),
                                    vat, 0, purchaseId.ToString(), "0",
                                    "Purchase Invoice", debitDesc,
                                    $"Vat Input For Invoice No. {invoiceCode}", _userId, DateTime.Now.Date, invoiceCode);

                            // Credit Vendor A/P (net)
                            await AddTransactionEntry2Async(conn, tx, headRow.Date, apAccount,
                                0, net, purchaseId.ToString(), vendorId.ToString(),
                                "Purchase Invoice", debitDesc,
                                $"Purchase Invoice NO. {invoiceCode}", _userId, DateTime.Now.Date, invoiceCode);

                            await tx.CommitAsync();
                            success++;
                            Console.WriteLine($"[OK]   INV={invNo} code={invoiceCode} items={lines.Count} net={net} id={purchaseId}");
                        }
                        catch (Exception ex)
                        {
                            await tx.RollbackAsync();
                            failed++;
                            errors.Add($"INV {invNo}: {ex.Message}");
                            Console.WriteLine($"[FAIL] INV={invNo} => {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        errors.Add($"INV {invNo}: (connection) {ex.Message}");
                        Console.WriteLine($"[FAIL] INV={invNo} (conn) => {ex.Message}");
                    }
                }

                Console.WriteLine("===========================================");
                Console.WriteLine($"Done.  Success={success}  Skipped={skipped}  Failed={failed}");
                //return new { totalRows = rows.Count, invoices = groups.Count, success, skipped, failed, errors };
                return new { totalRows = rows.Count,invoices = groups.Count,success, skipped, failed, skippedInvoices, errors};
        }

         private async Task LoadDefaults2Async(MySqlConnection conn)
            {
                var acc = new Dictionary<string, int>();
                using (var cmd = new MySqlCommand("SELECT category, account_id FROM tbl_coa_config", conn))
                using (var r = await cmd.ExecuteReaderAsync())
                    while (await r.ReadAsync())
                        acc[r.GetString("category")] = r.GetInt32("account_id");

                _vendorApAccountId = acc.TryGetValue("Vendor", out var v) ? v : 0;
                _purchaseAccountId = acc.TryGetValue("Purchase", out var p) ? p : 0;
                _vatInputAccountId = acc.TryGetValue("Vat Input", out var t) ? t : 0;

                if (_vendorApAccountId <= 0 || _purchaseAccountId <= 0 || _vatInputAccountId <= 0)
                    throw new Exception("tbl_coa_config missing one of: Vendor / Purchase / Vat Input.");
            }


        private async Task<int> ResolveItemIdAsync(MySqlConnection conn, MySqlTransaction tx, string name)
        {
            var key = (name ?? "").Trim().Trim('\uFEFF', '\u200E', '\u200F');
            if (key.Length == 0) return _fallbackItemId;
            if (_itemCache.TryGetValue(key, out var cached)) return cached;

            // exact first, then "ends with name" to absorb a "code - name" prefix
            using var cmd = new MySqlCommand(@"
        SELECT id FROM tbl_items
        WHERE TRIM(name) = TRIM(@name)
           OR TRIM(name) LIKE CONCAT('%', TRIM(@name))
        ORDER BY (TRIM(name) = TRIM(@name)) DESC
        LIMIT 1", conn, tx);
            cmd.Parameters.AddWithValue("@name", key);
            var res = await cmd.ExecuteScalarAsync();
            int id = (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : _fallbackItemId;

            if (id == _fallbackItemId)
                Console.WriteLine($"[ITEM?] no match for '{key}' -> fallback {_fallbackItemId}");  // see which names miss

            _itemCache[key] = id;
            return id;
        }
        private async Task<long> InsertPurchaseHeaderAsync(MySqlConnection conn, MySqlTransaction tx,
             PurchaseRows row, string invoiceCode, int vendorId, int projectId,
             decimal totalBefore, decimal vat, decimal net, bool isCash)
            {
                var sql = @"
                INSERT INTO tbl_purchase
                (date, vendor_id, invoice_id, warehouse_id, po_num, bill_to, city, sales_man,
                 ship_date, ship_via, ship_to, payment_method, account_cash_id, payment_terms, payment_date,
                 total, vat, net, pay, `change`, created_by, created_date, state, purchase_type,
                 fixed_asset_category_id, project_id)
                VALUES
                (@date, @vendor_id, @invoice_id, @warehouse_id, @po_num, @bill_to, @city, @sales_man,
                 @ship_date, @ship_via, @ship_to, @payment_method, @account_cash_id, @payment_terms, @payment_date,
                 @total, @vat, @net, @pay, @change, @created_by, @created_date, 0, @purchase_type,
                 1, @project_id);
                SELECT LAST_INSERT_ID();";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@date", row.Date);
                cmd.Parameters.AddWithValue("@vendor_id", vendorId);
                cmd.Parameters.AddWithValue("@invoice_id", invoiceCode);
                cmd.Parameters.AddWithValue("@warehouse_id", _warehouseId);
                cmd.Parameters.AddWithValue("@po_num", row.InvoiceNo ?? "");
                cmd.Parameters.AddWithValue("@bill_to", row.Vendor ?? "");
                cmd.Parameters.AddWithValue("@city", _city);
                cmd.Parameters.AddWithValue("@sales_man", _salesMan);
                cmd.Parameters.AddWithValue("@ship_date", row.Date);
                cmd.Parameters.AddWithValue("@ship_via", "");
                cmd.Parameters.AddWithValue("@ship_to", "");
                cmd.Parameters.AddWithValue("@payment_method", _paymentMethod);
                cmd.Parameters.AddWithValue("@account_cash_id", _accountCashId);
                cmd.Parameters.AddWithValue("@payment_terms", "");
                cmd.Parameters.AddWithValue("@payment_date", row.Date);
                cmd.Parameters.AddWithValue("@total", totalBefore);
                cmd.Parameters.AddWithValue("@vat", vat);
                cmd.Parameters.AddWithValue("@net", net);
                cmd.Parameters.AddWithValue("@pay", isCash ? net : 0);
                cmd.Parameters.AddWithValue("@change", isCash ? 0 : net);
                cmd.Parameters.AddWithValue("@created_by", _userId);
                cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                cmd.Parameters.AddWithValue("@purchase_type", _purchaseType);
                cmd.Parameters.AddWithValue("@project_id", projectId);

                return Convert.ToInt64(await cmd.ExecuteScalarAsync());
            }

         private async Task InsertPurchaseDetailAsync(MySqlConnection conn, MySqlTransaction tx,
             long purchaseId, int projectId, int itemId, PurchaseRows it)
            {
                decimal unitPrice = it.Qty > 0 ? it.Amount / it.Qty : it.Amount;

                var sql = @"
                INSERT INTO tbl_purchase_details
                (purchase_id, item_id, description, qty, cost_price, price, discount, vatp, vat, total, cost_center_id)
                VALUES
                (@purchase_id, @item_id, @description, @qty, @cost_price, @price, 0, @vatp, @vat, @total, @cost_center_id);";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@purchase_id", purchaseId);
                cmd.Parameters.AddWithValue("@item_id", itemId);
                cmd.Parameters.AddWithValue("@description", it.Description ?? "");
                cmd.Parameters.AddWithValue("@qty", it.Qty);
                cmd.Parameters.AddWithValue("@cost_price", unitPrice);
                cmd.Parameters.AddWithValue("@price", unitPrice);
                cmd.Parameters.AddWithValue("@vatp", it.Vat > 0 ? 5 : 0);
                cmd.Parameters.AddWithValue("@vat", it.Vat);
                cmd.Parameters.AddWithValue("@total", it.Total);     // line total incl VAT
                cmd.Parameters.Add("@cost_center_id", MySqlDbType.Int32).Value =
                    projectId > 0 ? projectId : (object)DBNull.Value;
                await cmd.ExecuteNonQueryAsync();
            }

         private async Task AddTransactionEntry2Async(MySqlConnection conn, MySqlTransaction tx,
             DateTime date, string accountId, decimal debit, decimal credit, string transactionId,
             string humId, string voucherName, string type, string description, int createdBy,
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
                cmd.Parameters.AddWithValue("@tType", voucherName);
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@hum_id", humId);
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@createdBy", createdBy);
                cmd.Parameters.AddWithValue("@createdDate", createdDate);
                cmd.Parameters.AddWithValue("@voucher_no", voucherNo);
                await cmd.ExecuteNonQueryAsync();
            }

         private List<PurchaseRows> Read2Rows(string filePath)
            {
                var rows = new List<PurchaseRows>();
                using var wb = new XLWorkbook(filePath);
                var ws = wb.Worksheets.First();

                foreach (var r in ws.RowsUsed())
                {
                    string dateCell = r.Cell("B").GetString().Trim();
                    string invNo = r.Cell("C").GetString().Trim();

                    if (dateCell.Equals("Date", StringComparison.OrdinalIgnoreCase)) continue;
                    if (string.IsNullOrWhiteSpace(dateCell) && string.IsNullOrWhiteSpace(invNo)) continue;

                    DateTime date;
                    var bCell = r.Cell("B");
                    if (bCell.DataType == XLDataType.DateTime) date = bCell.GetDateTime();
                    else if (!TryParse2Date(dateCell, out date)) { Console.WriteLine($"Row {r.RowNumber()} bad date: {dateCell}"); continue; }

                    rows.Add(new PurchaseRows
                    {
                        Date = date,
                        InvoiceNo = invNo,
                        Vendor = r.Cell("D").GetString().Trim(),
                        Description = r.Cell("E").GetString().Trim(),
                        Project = r.Cell("F").GetString().Trim(),
                        ItemName = r.Cell("G").GetString().Trim(),
                        Qty = ParseExcel2Num(r.Cell("H")),
                        Amount = ParseExcel2Num(r.Cell("I")),
                        Vat = ParseExcel2Num(r.Cell("J")),
                        Total = ParseExcel2Num(r.Cell("K")),
                    });
                }

                Console.WriteLine($"ReadRows: parsed {rows.Count}");
                return rows;
            }

         private static decimal ParseExcel2Num(IXLCell cell)
            {
                if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
                var s = cell.GetString().Replace(",", "").Trim();
                if (s.Length == 0 || s == "-") return 0m;
                return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
            }

         private static bool TryParse2Date(string s, out DateTime date)
            {
                if (DateTime.TryParseExact(s, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                    return true;
                if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
                { date = DateTime.FromOADate(oa); return true; }
                return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
            }

         private class PurchaseRows
         {
             public DateTime Date { get; set; }
             public string InvoiceNo { get; set; } = "";
             public string Vendor { get; set; } = "";
             public string Description { get; set; } = "";
             public string Project { get; set; } = "";
             public string ItemName { get; set; } = "";
             public decimal Qty { get; set; }
             public decimal Amount { get; set; }  // before VAT
             public decimal Vat { get; set; }
             public decimal Total { get; set; }  // incl VAT
         }

        private async Task<int> GetOrCreateVendorIdAsync(MySqlConnection conn, MySqlTransaction tx, string name)
        {
            try
            {
                var nm = (name ?? "").Trim().Trim('\uFEFF', '\u200E', '\u200F');
                if (nm.Length == 0) return 0;

                // 1) try existing
                using (var find = new MySqlCommand(
                    "SELECT id FROM tbl_vendor WHERE TRIM(name) = TRIM(@name) LIMIT 1", conn, tx))
                {
                    find.Parameters.AddWithValue("@name", nm);
                    var res = await find.ExecuteScalarAsync();
                    if (res != null && res != DBNull.Value) return Convert.ToInt32(res);
                }

                // 2) not found -> create it
                //    Adjust columns to YOUR tbl_vendor. Provide values for every NOT-NULL column.
                using (var ins = new MySqlCommand(@"
        INSERT INTO tbl_vendor (name, code, created_by, created_date, state, type)
        VALUES (@name, @code, @createdBy, @createdDate, 0, @type);
        SELECT LAST_INSERT_ID();", conn, tx))
                {
                    ins.Parameters.AddWithValue("@name", nm);
                    ins.Parameters.AddWithValue("@code", await NextVendorCodeAsync(conn, tx));   
                    ins.Parameters.AddWithValue("@createdBy", _userId);
                    ins.Parameters.AddWithValue("@createdDate", DateTime.Now.Date);
                    ins.Parameters.AddWithValue("@type", "Vendor");
                    int newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
                    Console.WriteLine($"[VENDOR+] created '{nm}' -> id {newId}");
                    return newId;
                }
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        // sequential vendor code like V-0001 (drop if your table auto-generates or doesn't have 'code')
        private async Task<int> NextVendorCodeAsync(MySqlConnection conn, MySqlTransaction tx)
        {
            using var cmd = new MySqlCommand(
                "SELECT IFNULL(MAX(CAST(code AS UNSIGNED)), 0) + 1 FROM tbl_vendor", conn, tx);
            var res = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(res);
        }

        #endregion

        #region Payment Voucher

        [HttpPost]
        public async Task<IActionResult> ImportPdcPayments(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { status = false, message = "Please select a file." });

            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, file.FileName);
            using (var s = new FileStream(path, FileMode.Create)) await file.CopyToAsync(s);

            var result = await ImportPdcPaymentsAsync(path);
            return Ok(new { status = true, message = "Import completed", result });
        }

        public async Task<object> ImportPdcPaymentsAsync(string filePath)
        {
            // NOTE: ReadRows() now returns one *line item* per Excel row (as before),
            // but multiple rows can share the same SN. GroupRows() collapses those
            // into a single voucher per SN, which is what the sheet actually represents
            // (e.g. SN 48 has 15 rows, same date/debit/credit/partner, different amounts).
            var flatRows = ReadRowsPDC(filePath);
            var vouchers = GroupRows(flatRows);
            Console.WriteLine($"Total PDC rows: {flatRows.Count}  ->  Vouchers: {vouchers.Count}");

            int success = 0, failed = 0;
            var errors = new List<string>();

            var bootBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase"),
                CharacterSet = "utf8mb4"
            };
            using (var bconn = new MySqlConnection(bootBuilder.ConnectionString))
            {
                await bconn.OpenAsync();
                await LoadDefaultsPDCAsync(bconn);
            }

            foreach (var voucher in vouchers)
            {
                string sn = voucher.Sn;
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
                        int vendorId = await ResolveVendorsIdAsync(conn, tx, voucher.Vendor); // hum_id (0 if not found)

                        // Debit account from "Payment to (debit)"; fallback default A/P
                        int debitAccountId = await ResolveAccountByNameAsync(conn, tx, voucher.AccountName);
                        if (debitAccountId <= 0) debitAccountId = _apAccountId;

                        // Credit account from "Payment From (Credit)"; fallback default PDC payable
                        int creditAccountId = await ResolveAccountByBankAsync(conn, tx, voucher.Bank);
                        if (creditAccountId <= 0) creditAccountId = _pdcPayableAccountId;

                        if (debitAccountId <= 0 || creditAccountId <= 0)
                        {
                            failed++;
                            errors.Add($"SN {sn}: debit/credit account not resolved (acct='{voucher.AccountName}', bank='{voucher.Bank}')");
                            await tx.RollbackAsync();
                            continue;
                        }

                        decimal totalAmount = voucher.Lines.Sum(l => l.Amount);
                        string code = int.TryParse(voucher.Sn.Trim(), out var snNum) ? $"PV-{snNum:D4}" : $"PV-{voucher.Sn.Trim()}";

                        // 1) tbl_payment_voucher  -- ONE row per SN, amount = sum of its line items
                        int pvId = await InsertVoucherAsync(conn, tx, voucher, code, totalAmount, debitAccountId, creditAccountId);

                        // 2) tbl_check_details -- only for cheque payments; Cash rows have no cheque
                        if (string.Equals(voucher.PaymentMethod, "Cheque", StringComparison.OrdinalIgnoreCase))
                        {
                            await InsertCheckDetailsAsync(conn, tx, voucher, pvId, totalAmount);
                        }

                        // 3) tbl_payment_voucher_details -- ONE row per Excel line item under this SN
                        foreach (var line in voucher.Lines)
                        {
                            await InsertVoucherDetailAsync(conn, tx, voucher, line, pvId, vendorId);
                        }

                        // 4) journals: Debit A/P, Credit PDC/Bank -- once, for the voucher total
                        string tType = _isSubContractor ? "Subcontractor Payment" : "Vendor Payment";
                        await AddTransactionsEntryAsync(conn, tx, voucher.Date, debitAccountId.ToString(),
                            totalAmount, 0, pvId.ToString(), vendorId > 0 ? vendorId.ToString() : "0",
                            tType, tType, $"Payment Voucher NO. {code}", _userId, DateTime.Now, code);
                        await AddTransactionsEntryAsync(conn, tx, voucher.Date, creditAccountId.ToString(),
                            0, totalAmount, pvId.ToString(), "0",
                            tType, tType, $"Payment Voucher NO. {code}", _userId, DateTime.Now, code);

                        // 5) cost-center debit/credit (0 center) -- once, for the voucher total
                        await InsertCostCenterAsync(conn, tx, voucher.Date, totalAmount, 0, pvId.ToString(), "Payment", "Payment Debit Entry", "0");
                        await InsertCostCenterAsync(conn, tx, voucher.Date, 0, totalAmount, pvId.ToString(), "Payment", "Payment Credit Entry", "0");

                        await tx.CommitAsync();
                        success++;
                        Console.WriteLine($"[OK]   SN={sn} code={code} vendor={vendorId} amt={totalAmount} lines={voucher.Lines.Count} pvId={pvId}");
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        failed++;
                        errors.Add($"SN {sn}: {ex.Message}");
                        Console.WriteLine($"[FAIL] SN={sn} => {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"SN {sn}: (connection) {ex.Message}");
                }
            }

            Console.WriteLine("===========================================");
            Console.WriteLine($"Done.  Success={success}  Failed={failed}");
            return new { totalRows = flatRows.Count, totalVouchers = vouchers.Count, success, failed, errors };
        }

        private async Task InsertVoucherDetailAsync(MySqlConnection conn, MySqlTransaction tx,
            PdcVoucher voucher, PdcLine line, int pvId, int vendorId)
        {
            var sql = @"
    INSERT INTO tbl_payment_voucher_details
    (date, payment_id, hum_id, inv_id, inv_code, total, payment, description, voucher_type)
    VALUES (@date, @payment_id, @hum_id, 0, @inv_code, @total, @payment, @description, @voucher_type);";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", voucher.Date);
            cmd.Parameters.AddWithValue("@payment_id", pvId);
            cmd.Parameters.AddWithValue("@hum_id", vendorId);
            cmd.Parameters.AddWithValue("@inv_code", voucher.Sn ?? "");           // source serial for traceability
            cmd.Parameters.AddWithValue("@total", line.Amount);
            cmd.Parameters.AddWithValue("@payment", line.Amount);
            cmd.Parameters.AddWithValue("@description", Trunc(line.Details, 250));
            cmd.Parameters.AddWithValue("@voucher_type", _isSubContractor ? "Subcontractor Payment" : "Vendor Payment");
            await cmd.ExecuteNonQueryAsync();
        }

        // ---------------- defaults / lookups ----------------
        private async Task LoadDefaultsPDCAsync(MySqlConnection conn)
        {
            var acc = new Dictionary<string, int>();
            using (var cmd = new MySqlCommand("SELECT category, account_id FROM tbl_coa_config", conn))
            using (var r = await cmd.ExecuteReaderAsync())
                while (await r.ReadAsync())
                    acc[r.GetString("category")] = r.GetInt32("account_id");

            _apAccountId = acc.TryGetValue("Vendor", out var v) ? v : 0;                       // A/P
            _pdcPayableAccountId = acc.TryGetValue("PDC Payable", out var pdc) ? pdc : 0;       // add this category, or hard-set below

            if (_apAccountId <= 0)
                throw new Exception("tbl_coa_config missing 'Vendor' (A/P) account.");
        }

        private async Task<int> ResolveVendorsIdAsync(MySqlConnection conn, MySqlTransaction tx, string name)
        {
            using var cmd = new MySqlCommand("SELECT id FROM tbl_vendor WHERE TRIM(name)=TRIM(@n) LIMIT 1", conn, tx);
            cmd.Parameters.AddWithValue("@n", name ?? "");
            var res = await cmd.ExecuteScalarAsync();
            return (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
        }

        // resolve a GL account PK from its NAME (cached)
        private async Task<int> ResolveAccountByNameAsync(MySqlConnection conn, MySqlTransaction tx, string name)
        {
            var key = (name ?? "").Trim();
            if (key.Length == 0) return 0;
            if (_accountByName.TryGetValue(key, out var c)) return c;
            using var cmd = new MySqlCommand("SELECT id FROM tbl_coa_level_4 WHERE TRIM(name)=TRIM(@n) LIMIT 1", conn, tx);
            cmd.Parameters.AddWithValue("@n", key);
            var res = await cmd.ExecuteScalarAsync();
            int id = (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
            _accountByName[key] = id;
            return id;
        }

        private async Task<int> ResolveAccountByBankAsync(MySqlConnection conn, MySqlTransaction tx, string bank)
        {
            var key = (bank ?? "").Trim();
            if (key.Length == 0) return 0;
            if (_accountByBank.TryGetValue(key, out var c)) return c;
            // try to match a GL account whose name contains the credit-side name (e.g. "Petty Cash Imprest", a bank name); else 0 -> fallback PDC
            using var cmd = new MySqlCommand(
                "SELECT id FROM tbl_coa_level_4 WHERE TRIM(name)=TRIM(@n) OR TRIM(name) LIKE CONCAT('%',TRIM(@n),'%') LIMIT 1", conn, tx);
            cmd.Parameters.AddWithValue("@n", key);
            var res = await cmd.ExecuteScalarAsync();
            int id = (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
            _accountByBank[key] = id;
            return id;
        }

        // ---------------- inserts ----------------
        private async Task<int> InsertVoucherAsync(MySqlConnection conn, MySqlTransaction tx,
            PdcVoucher voucher, string code, decimal totalAmount, int debitAccountId, int creditAccountId)
        {
            var sql = @"
        INSERT INTO tbl_payment_voucher
        (date, code, type, method, amount, debit_account_id, debit_cost_center_id, description,
         credit_account_id, credit_cost_center_id, bank_id, bank_account_id, book_no, check_name,
         check_no, check_date, trans_date, trans_name, trans_ref, created_by, created_date, state)
        VALUES
        (@date, @code, @type, @method, @amount, @debit_account_id, 0, @description,
         @credit_account_id, 0, 0, 0, 0, @check_name,
         @check_no, @check_date, NULL, '', '', @created_by, @created_date, 0);
        SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", voucher.Date);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@type", _paymentType);
            cmd.Parameters.AddWithValue("@method", voucher.PaymentMethod ?? _method);
            cmd.Parameters.AddWithValue("@amount", totalAmount);
            cmd.Parameters.AddWithValue("@debit_account_id", debitAccountId);
            cmd.Parameters.AddWithValue("@description", Trunc(voucher.CombinedDetails, 250));
            cmd.Parameters.AddWithValue("@credit_account_id", creditAccountId);
            cmd.Parameters.AddWithValue("@check_name", Trunc(voucher.Vendor, 100));
            cmd.Parameters.AddWithValue("@check_no", voucher.ChequeNo ?? "");
            // No cheque-date column exists in this sheet; use the voucher date for cheque rows, NULL for cash.
            cmd.Parameters.AddWithValue("@check_date",
                string.Equals(voucher.PaymentMethod, "Cheque", StringComparison.OrdinalIgnoreCase)
                    ? (object)voucher.Date
                    : DBNull.Value);
            cmd.Parameters.AddWithValue("@created_by", _userId);
            cmd.Parameters.AddWithValue("@created_date", DateTime.Now);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task InsertCheckDetailsAsync(MySqlConnection conn, MySqlTransaction tx,
            PdcVoucher voucher, int pvId, decimal totalAmount)
        {
            var sql = @"
        INSERT INTO tbl_check_details
        (date, check_id, check_no, check_date, check_type, pvc_no, check_name, amount, state)
        VALUES (@date, 0, @check_no, @check_date, 'Payment', @pvc_no, @check_name, @amount, 'New');";
            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", voucher.Date);
            cmd.Parameters.AddWithValue("@check_no", voucher.ChequeNo ?? "");
            cmd.Parameters.AddWithValue("@check_date", voucher.Date); // no dedicated cheque-date column in source sheet
            cmd.Parameters.AddWithValue("@pvc_no", pvId);
            cmd.Parameters.AddWithValue("@check_name", Trunc(voucher.Vendor, 100));
            cmd.Parameters.AddWithValue("@amount", totalAmount);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task AddTransactionsEntryAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, string accountId, decimal debit, decimal credit, string transactionId,
            string humId, string tType, string type, string description, int createdBy,
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
            cmd.Parameters.AddWithValue("@hum_id", humId);
            cmd.Parameters.AddWithValue("@tType", tType);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@createdBy", createdBy);
            cmd.Parameters.AddWithValue("@createdDate", createdDate);
            cmd.Parameters.AddWithValue("@voucher_no", voucherNo);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertCostCenterAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, decimal debit, decimal credit, string refId, string type, string description, string costCenterId)
        {
            var sql = @"
        INSERT INTO tbl_cost_center_transaction
        (type, date, ref_id, debit, credit, description, cost_center_id)
        VALUES (@type, @date, @ref, @debit, @credit, @description, @cc);";
            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@ref", refId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@cc", costCenterId);
            await cmd.ExecuteNonQueryAsync();
        }
        private List<PdcLine> ReadRowsPDC(string filePath)
        {
            var rows = new List<PdcLine>();
            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.First();

            foreach (var r in ws.RowsUsed())
            {
                string a = r.Cell("A").GetString().Trim();
                // skip banners + header
                if (a.StartsWith("Type", StringComparison.OrdinalIgnoreCase) ||
                    a.StartsWith("From Date", StringComparison.OrdinalIgnoreCase) ||
                    a.Equals("Date", StringComparison.OrdinalIgnoreCase)) continue;
                if (a.Length == 0) continue;

                // Only import approved rows if a Status column is present
                string status = r.Cell("K").GetString().Trim();
                if (status.Length > 0 && !status.Equals("Approved", StringComparison.OrdinalIgnoreCase))
                    continue;

                DateTime date;
                var aCell = r.Cell("A");
                if (aCell.DataType == XLDataType.DateTime) date = aCell.GetDateTime();
                else if (!TryParsesDate(a, out date)) { Console.WriteLine($"Row {r.RowNumber()} bad date: {a}"); continue; }

                rows.Add(new PdcLine
                {
                    Date = date,
                    Sn = r.Cell("B").GetString().Trim(),
                    AccountName = r.Cell("C").GetString().Trim(),   // Payment to (debit)
                    Bank = r.Cell("D").GetString().Trim(),          // Payment From (Credit)
                  /* Vendor = r.Cell("G").GetString().Trim(),    */ // Partner (only populated on first line of a group)
                    PaymentMethod = r.Cell("H").GetString().Trim(), // "Cash" or "Cheque"
                    ChequeNo = r.Cell("I").GetString().Trim(),
                    Details = (r.Cell("G").GetString().Trim() + " " + r.Cell("F").GetString().Trim() + " " + r.Cell("E").GetString().Trim()).Trim(),
                    Amount = ParseNum(r.Cell("J")),
                });
            }
            Console.WriteLine($"ReadRows: parsed {rows.Count}");
            return rows;
        }

        // Collapses same-SN line items into a single voucher, since the sheet uses
        // one SN per voucher with several expense lines underneath it.
        private List<PdcVoucher> GroupRows(List<PdcLine> lines)
        {
            var vouchers = new List<PdcVoucher>();
            foreach (var g in lines.GroupBy(l => l.Sn))
            {
                var first = g.First();
                vouchers.Add(new PdcVoucher
                {
                    Sn = g.Key,
                    Date = first.Date,
                    AccountName = first.AccountName,
                    Bank = first.Bank,
                    //Vendor = g.Select(l => l.Vendor).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "",
                    PaymentMethod = first.PaymentMethod,
                    ChequeNo = g.Select(l => l.ChequeNo).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)) ?? "",
                    Lines = g.ToList(),
                });
            }
            return vouchers;
        }

        private static string Trunc(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length > max ? s.Substring(0, max) : s);

        private static decimal ParseNum(IXLCell cell)
        {
            if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
            var s = cell.GetString().Replace(",", "").Trim();
            if (s.Length == 0 || s == "-") return 0m;
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
        }

        private static bool TryParsesDate(string s, out DateTime date)
        {
            if (DateTime.TryParseExact(s, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
            { date = DateTime.FromOADate(oa); return true; }
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        // One raw Excel row (an expense line item under a voucher SN)
        private class PdcLine
        {
            public DateTime Date { get; set; }
            public string Sn { get; set; } = "";
            public string AccountName { get; set; } = "";
            public string Bank { get; set; } = "";
            public string Vendor { get; set; } = "";
            public string PaymentMethod { get; set; } = "";
            public string ChequeNo { get; set; } = "";
            public string Details { get; set; } = "";
            public decimal Amount { get; set; }
        }

        // One voucher (grouped by SN), containing 1+ line items
        private class PdcVoucher
        {
            public string Sn { get; set; } = "";
            public DateTime Date { get; set; }
            public string AccountName { get; set; } = "";
            public string Bank { get; set; } = "";
            public string Vendor { get; set; } = "";
            public string PaymentMethod { get; set; } = "";
            public string ChequeNo { get; set; } = "";
            public List<PdcLine> Lines { get; set; } = new();
            public string CombinedDetails =>
                string.Join(" | ", Lines.Select(l => l.Details).Where(d => !string.IsNullOrWhiteSpace(d)).Distinct());
        }

        #endregion

        #region Receipt Voucher

        [HttpPost]
        public async Task<IActionResult> ImportReceipts(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { status = false, message = "Please select a file." });

            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, file.FileName);
            using (var s = new FileStream(path, FileMode.Create)) await file.CopyToAsync(s);

            var result = await ImportReceiptsAsync(path);
            return Ok(new { status = true, message = "Import completed", result });
        }

        public async Task<object> ImportReceiptsAsync(string filePath)
        {
            var rows = RseadRows(filePath);

            // group by SN -> one receipt voucher per serial
            var groups = rows
                .Select((r, i) => (row: r, idx: i))
                .GroupBy(x => x.row.Sn.Trim())
                .ToList();

            Console.WriteLine($"Total rows: {rows.Count}, receipts (by SN): {groups.Count}");

            int success = 0, skipped = 0, failed = 0, newCustomers = 0;
            var errors = new List<string>();
            var newCustomersList = new List<string>();

            var bootBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase"),
                CharacterSet = "utf8mb4"
            };
            using (var bconn = new MySqlConnection(bootBuilder.ConnectionString))
            {
                await bconn.OpenAsync();
                await LoadsDefaultsAsync(bconn);
            }

            foreach (var grp in groups)
            {
                var lines = grp.OrderBy(x => x.idx).Select(x => x.row).ToList();
                var headRow = lines[0];
                string sn = headRow.Sn.Trim();

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
                        if (lines.Select(l => l.Customer.Trim()).Distinct().Count() > 1)
                            Console.WriteLine($"[WARN] SN={sn} mixed customers; using '{headRow.Customer}'.");

                        int customerId = await ResolvesCustomerIdAsync(conn, tx, headRow.Customer);
                        if (customerId <= 0)
                        {
                            // MODIFIED: Auto-insert missing customer instead of skipping
                            customerId = await InsertNewCustomerAsync(conn, tx, headRow.Customer);
                            if (customerId <= 0)
                            {
                                skipped++;
                                errors.Add($"SN {sn}: failed to create customer '{headRow.Customer}'");
                                await tx.RollbackAsync();
                                continue;
                            }
                            newCustomers++;
                            newCustomersList.Add($"'{headRow.Customer}' (ID={customerId})");
                            Console.WriteLine($"[NEW CUST] Created customer: '{headRow.Customer}' with ID={customerId}");
                        }

                        // money received into bank/cash account (column G); fallback default cash
                        int debitAccountId = await ResolvesAccountByNameAsync(conn, tx, headRow.Account);
                        if (debitAccountId <= 0) debitAccountId = _cashAccountId;
                        int creditAccountId = _arAccountId;     // Customer A/R

                        if (debitAccountId <= 0 || creditAccountId <= 0)
                        {
                            failed++;
                            errors.Add($"SN {sn}: account not resolved (acct='{headRow.Account}')");
                            await tx.RollbackAsync();
                            continue;
                        }

                        decimal amount = lines.Sum(l => l.Amount);
                        string method = NormalizeMethod(headRow.Type);   // Cheque / Cash
                        string code = int.TryParse(sn, out var n) ? $"RV-{n:D4}" : $"RV-{sn}";

                        // FIXED: Combine description + ALL cheque numbers (including "0")
                        var chqNos = lines
                            .Where(l => !string.IsNullOrWhiteSpace(l.ChequeNo))
                            .Select(l => SanitizeChequeNo(l.ChequeNo.Trim()))
                            .Distinct()
                            .ToList();

                        string desc = headRow.Description ?? "";
                        if (chqNos.Count > 0)
                        {
                            string chqNosStr = string.Join(", ", chqNos);
                            desc = $"{desc} {chqNosStr}".Trim();
                        }

                        // 1) tbl_receipt_voucher
                        int rvId = await InsertVoucherAsync(conn, tx, headRow, code, method, amount,
                                        debitAccountId, creditAccountId, desc);

                        // 2) tbl_check_details — one per cheque line
                        if (method == "Cheque")
                            foreach (var l in lines)
                                await InsertCheckDetailsAsync(conn, tx, l, rvId);

                        // 3) journals: Debit Bank/Cash, Credit Customer A/R (hum_id = customer)
                        await AddTransactionssEntryAsync(conn, tx, headRow.Date, debitAccountId.ToString(),
                            amount, 0, rvId.ToString(), "0",
                            "RECEIPT", "Customer Receipt", $"Receipt Voucher NO. {code}", _userId, DateTime.Now.Date, code);
                        await AddTransactionssEntryAsync(conn, tx, headRow.Date, creditAccountId.ToString(),
                            0, amount, rvId.ToString(), customerId.ToString(),
                            "RECEIPT", "Customer Receipt", $"Receipt Voucher NO. {code}", _userId, DateTime.Now.Date, code);

                        // 4) cost-center debit/credit (0 center)
                        await InsertCostsCenterAsync(conn, tx, headRow.Date, amount, 0, rvId.ToString(), "Receipt", "Receipt Debit Entry", "0");
                        await InsertCostsCenterAsync(conn, tx, headRow.Date, 0, amount, rvId.ToString(), "Receipt", "Receipt Credit Entry", "0");

                        // 5) INSERT receipt voucher details
                        foreach (var line in lines)
                        {
                            await InsertReceiptVoucherDetailsAsync(conn, tx, headRow.Date, rvId, customerId, line, desc);
                        }

                        await tx.CommitAsync();
                        success++;
                        Console.WriteLine($"[OK]   SN={sn} code={code} cust={customerId} cheques={lines.Count} amt={amount} rvId={rvId}");
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        failed++;
                        errors.Add($"SN {sn}: {ex.Message}");
                        Console.WriteLine($"[FAIL] SN={sn} => {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"SN {sn}: (connection) {ex.Message}");
                }
            }

            Console.WriteLine("===========================================");
            Console.WriteLine($"Done.  Success={success}  Skipped={skipped}  Failed={failed}  New Customers={newCustomers}");
            return new
            {
                totalRows = rows.Count,
                receipts = groups.Count,
                success,
                skipped,
                failed,
                newCustomers,
                errors,
                newCustomersList,
                skippedList = errors.Where(e => e.Contains("customer not found") || e.Contains("failed to create customer")).ToList()
            };
        }

     
        private async Task<int> InsertNewCustomerAsync(MySqlConnection conn, MySqlTransaction tx, string customerName)
        {
            try
            {
                var sql = @"
        INSERT INTO tbl_customer (name, email, created_date, created_by, state)
        VALUES (@name, '', @created_date, @created_by, 1);
        SELECT LAST_INSERT_ID();";

                using var cmd = new MySqlCommand(sql, conn, tx);
                cmd.Parameters.AddWithValue("@name", Truncc(customerName ?? "", 200));
                cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                cmd.Parameters.AddWithValue("@created_by", _userId);

                var result = await cmd.ExecuteScalarAsync();
                return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to create customer '{customerName}': {ex.Message}");
                return 0;
            }
        }

        private static string SanitizeChequeNo(string chequeNo)
        {
            if (string.IsNullOrWhiteSpace(chequeNo))
                return "";

            // Extract only digits
            string digitsOnly = new string(chequeNo.Where(char.IsDigit).ToArray());

            // If no digits found (contains text/Arabic), return "0"
            if (string.IsNullOrWhiteSpace(digitsOnly))
                return "0";

            // If has digits and also has text, return "0"
            bool hasNonDigits = chequeNo.Any(c => !char.IsDigit(c) && !char.IsWhiteSpace(c));
            if (hasNonDigits)
                return "0";

            // Pure digits - return as-is
            return digitsOnly;
        }

        // ============== INSERT: Receipt Voucher Details ==============
        private async Task InsertReceiptVoucherDetailsAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, int paymentId, int customerId, ReceiptRow row, string description)
        {
            var sql = @"
    INSERT INTO tbl_receipt_voucher_details
    (date, payment_id, hum_id, inv_id, inv_code, total, payment, description)
    VALUES (@date, @payment_id, @hum_id, @inv_id, @inv_code, @total, @payment, @description)";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@payment_id", paymentId);
            cmd.Parameters.AddWithValue("@hum_id", customerId);
            cmd.Parameters.AddWithValue("@inv_id", 0);
            cmd.Parameters.AddWithValue("@inv_code", "");
            cmd.Parameters.AddWithValue("@total", row.Amount);
            cmd.Parameters.AddWithValue("@payment", row.Amount);
            cmd.Parameters.AddWithValue("@description", Truncc(description, 500));

            await cmd.ExecuteNonQueryAsync();
        }

        // ---------------- defaults / lookups ----------------
        private async Task LoadsDefaultsAsync(MySqlConnection conn)
        {
            var acc = new Dictionary<string, int>();
            using (var cmd = new MySqlCommand("SELECT category, account_id FROM tbl_coa_config", conn))
            using (var r = await cmd.ExecuteReaderAsync())
                while (await r.ReadAsync())
                    acc[r.GetString("category")] = r.GetInt32("account_id");

            _arAccountId = acc.TryGetValue("Customer", out var c) ? c : 0;
            _cashAccountId = acc.TryGetValue("Cash", out var ca) ? ca : 0;

            if (_arAccountId <= 0)
                throw new Exception("tbl_coa_config missing 'Customer' (A/R) account.");
        }

        private async Task<int> ResolvesCustomerIdAsync(MySqlConnection conn, MySqlTransaction tx, string name)
        {
            using var cmd = new MySqlCommand("SELECT id FROM tbl_customer WHERE TRIM(name)=TRIM(@n) LIMIT 1", conn, tx);
            cmd.Parameters.AddWithValue("@n", name ?? "");
            var res = await cmd.ExecuteScalarAsync();
            return (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
        }

        private async Task<int> ResolvesAccountByNameAsync(MySqlConnection conn, MySqlTransaction tx, string name)
        {
            var key = (name ?? "").Trim();
            if (key.Length == 0) return 0;
            if (_accountByName.TryGetValue(key, out var c)) return c;
            using var cmd = new MySqlCommand(
                "SELECT id FROM tbl_coa_level_4 WHERE TRIM(name)=TRIM(@n) OR TRIM(name) LIKE CONCAT('%',TRIM(@n),'%') LIMIT 1", conn, tx);
            cmd.Parameters.AddWithValue("@n", key);
            var res = await cmd.ExecuteScalarAsync();
            int id = (res != null && res != DBNull.Value) ? Convert.ToInt32(res) : 0;
            _accountByName[key] = id;
            return id;
        }

        private static string NormalizeMethod(string t)
        {
            var s = (t ?? "").Trim();
            if (s.Equals("Cheque", StringComparison.OrdinalIgnoreCase) ||
                s.Equals("Check", StringComparison.OrdinalIgnoreCase)) return "Cheque";
            if (s.Equals("Transfer", StringComparison.OrdinalIgnoreCase)) return "Transfer";
            return "Cash";
        }

        // ---------------- inserts ----------------
        private async Task<int> InsertVoucherAsync(MySqlConnection conn, MySqlTransaction tx,
            ReceiptRow row, string code, string method, decimal amount,
            int debitAccountId, int creditAccountId, string description)
        {
            var sql = @"
    INSERT INTO tbl_receipt_voucher
    (date, code, type, method, amount, debit_account_id, debit_cost_center_id,
     credit_account_id, credit_cost_center_id, bank_id, bank_account_id, book_no,
     check_name, check_no, check_date, trans_date, trans_name, trans_ref,
     created_by, created_date, state)
    VALUES
    (@date, @code, @type, @method, @amount, @debit_account_id, 0,
     @credit_account_id, 0, 0, 0, 0,
     @check_name, @check_no, @check_date, NULL, '', '',
     @created_by, @created_date, 0);
    SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", row.Date);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@type", _paymentType);
            cmd.Parameters.AddWithValue("@method", method);
            cmd.Parameters.AddWithValue("@amount", amount);
            cmd.Parameters.AddWithValue("@debit_account_id", debitAccountId);
            cmd.Parameters.AddWithValue("@credit_account_id", creditAccountId);
            cmd.Parameters.AddWithValue("@check_name", Truncc(row.Customer, 100));

            // FIXED: Store cheque as-is if valid, or "0" if Arabic
            string sanitizedChequeNo = SanitizeChequeNo(row.ChequeNo ?? "");
            if (method == "Cheque" && !string.IsNullOrEmpty(sanitizedChequeNo))
            {
                cmd.Parameters.AddWithValue("@check_no", (object)sanitizedChequeNo);
            }
            else
            {
                cmd.Parameters.AddWithValue("@check_no", DBNull.Value);
            }

            cmd.Parameters.AddWithValue("@check_date", method == "Cheque" && row.ChequeDate.HasValue ? (object)row.ChequeDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@created_by", _userId);
            cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);

            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task InsertCheckDetailsAsync(MySqlConnection conn, MySqlTransaction tx, ReceiptRow row, int rvId)
        {
            var sql = @"
    INSERT INTO tbl_check_details
    (date, check_id, check_no, check_date, check_type, pvc_no, check_name, amount, state)
    VALUES (@date, 0, @check_no, @check_date, 'Receipt', @pvc_no, @check_name, @amount, 'New');";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", row.Date);

            // FIXED: Store sanitized cheque number (or "0" if Arabic)
            string sanitizedChequeNo = SanitizeChequeNo(row.ChequeNo ?? "");
            cmd.Parameters.AddWithValue("@check_no", sanitizedChequeNo);

            cmd.Parameters.AddWithValue("@check_date", (object?)row.ChequeDate ?? row.Date);
            cmd.Parameters.AddWithValue("@pvc_no", rvId);
            cmd.Parameters.AddWithValue("@check_name", Truncc(row.Customer, 100));
            cmd.Parameters.AddWithValue("@amount", row.Amount);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task AddTransactionssEntryAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, string accountId, decimal debit, decimal credit, string transactionId,
            string humId, string tType, string type, string description, int createdBy,
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
            cmd.Parameters.AddWithValue("@hum_id", humId);
            cmd.Parameters.AddWithValue("@tType", tType);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@createdBy", createdBy);
            cmd.Parameters.AddWithValue("@createdDate", createdDate);
            cmd.Parameters.AddWithValue("@voucher_no", voucherNo);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertCostsCenterAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, decimal debit, decimal credit, string refId, string type, string description, string costCenterId)
        {
            var sql = @"
    INSERT INTO tbl_cost_center_transaction
    (type, date, ref_id, debit, credit, description, cost_center_id)
    VALUES (@type, @date, @ref, @debit, @credit, @description, @cc);";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@ref", refId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@cc", costCenterId);

            await cmd.ExecuteNonQueryAsync();
        }

        // ---------------- reader ----------------
        private List<ReceiptRow> RseadRows(string filePath)
        {
            var rows = new List<ReceiptRow>();
            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.First();

            foreach (var r in ws.RowsUsed())
            {
                string dateCell = r.Cell("B").GetString().Trim();
                string sn = r.Cell("C").GetString().Trim();

                if (dateCell.Equals("Date", StringComparison.OrdinalIgnoreCase)) continue;
                if (sn.Equals("SN", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrWhiteSpace(dateCell) && string.IsNullOrWhiteSpace(sn)) continue;
                if (string.IsNullOrWhiteSpace(sn)) continue;

                DateTime date;
                var bCell = r.Cell("B");
                if (bCell.DataType == XLDataType.DateTime) date = bCell.GetDateTime();
                else if (!TryParseDatee(dateCell, out date)) { Console.WriteLine($"Row {r.RowNumber()} bad date: {dateCell}"); continue; }

                DateTime? chqDate = null;
                var iCell = r.Cell("I");
                if (iCell.DataType == XLDataType.DateTime) chqDate = iCell.GetDateTime();
                else if (TryParseDatee(iCell.GetString().Trim(), out var cd)) chqDate = cd;

                rows.Add(new ReceiptRow
                {
                    Date = date,
                    Sn = sn,
                    Customer = r.Cell("E").GetString().Trim(),
                    Description = r.Cell("F").GetString().Trim(),
                    Account = r.Cell("G").GetString().Trim(),
                    ChequeNo = r.Cell("H").GetString().Trim(),
                    ChequeDate = chqDate,
                    Type = r.Cell("J").GetString().Trim(),
                    Amount = ParseNumm(r.Cell("K")),
                });
            }
            Console.WriteLine($"ReadRows: parsed {rows.Count}");
            return rows;
        }

        private static string Truncc(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length > max ? s.Substring(0, max) : s);

        private static decimal ParseNumm(IXLCell cell)
        {
            if (cell.DataType == XLDataType.Number) return (decimal)cell.GetDouble();
            var s = cell.GetString().Replace(",", "").Trim();
            if (s.Length == 0 || s == "-") return 0m;
            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
        }

        private static bool TryParseDatee(string s, out DateTime date)
        {
            if (DateTime.TryParseExact(s, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
            { date = DateTime.FromOADate(oa); return true; }
            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        private class ReceiptRow
        {
            public DateTime Date { get; set; }
            public string Sn { get; set; } = "";
            public string Customer { get; set; } = "";
            public string Description { get; set; } = "";
            public string Account { get; set; } = "";
            public string ChequeNo { get; set; } = "";
            public DateTime? ChequeDate { get; set; }
            public string Type { get; set; } = "";
            public decimal Amount { get; set; }
        }

        #endregion

        #region Journal Voucher

        [HttpPost]
        public async Task<IActionResult> ImportJournalVouchers(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { status = false, message = "Please select a file." });

            var dir = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, file.FileName);
            using (var s = new FileStream(path, FileMode.Create))
                await file.CopyToAsync(s);

            var result = await ImportJournalVouchersAsync(path);
            return Ok(new { status = true, message = "Import completed", result });
        }

        public async Task<object> ImportJournalVouchersAsync(string filePath)
        {
            var rows = ReadJVRows(filePath);

            // Group by SN -> one journal voucher per serial
            var groups = rows
                .Select((r, i) => (row: r, idx: i))
                .GroupBy(x => x.row.Sn.Trim())
                .ToList();

            Console.WriteLine($"Total rows: {rows.Count}, journal vouchers (by SN): {groups.Count}");

            int success = 0, skipped = 0, failed = 0;
            var errors = new List<string>();
            var failedList = new List<object>();   // structured: { sn, code, reason }

            // Get userId from session
            _userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (_userId <= 0)
                return new { status = false, error = "User not authenticated" };

            var bootBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase"),
                CharacterSet = "utf8mb4"
            };
            using (var bconn = new MySqlConnection(bootBuilder.ConnectionString))
            {
                await bconn.OpenAsync();
                await LoadAccountCacheAsync(bconn);
                await LoadVendorCacheAsync(bconn);
            }

            foreach (var grp in groups)
            {
                var lines = grp.OrderBy(x => x.idx).Select(x => x.row).ToList();
                var headRow = lines[0];
                string sn = headRow.Sn.Trim();
                string jvCode = $"JV-{sn}"; // Use SN as code, no auto-increment

                // remember which project names we created in THIS group, so we can
                // drop them from the cache if the group rolls back (avoids stale ids)
                var createdProjectKeys = new List<string>();

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
                        decimal totalDebit = lines.Sum(l => l.Debit);
                        decimal totalCredit = lines.Sum(l => l.Credit);

                        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
                        {
                            failed++;
                            errors.Add($"SN {sn}: Debit ({totalDebit}) ≠ Credit ({totalCredit})");
                            failedList.Add(new { sn, code = jvCode, reason = $"Debit {totalDebit} ≠ Credit {totalCredit}" });
                            await tx.RollbackAsync();
                            InvalidateProjects(createdProjectKeys);
                            continue;
                        }

                        if (totalDebit <= 0 || totalCredit <= 0)
                        {
                            skipped++;
                            errors.Add($"SN {sn}: Debit or Credit is zero or negative");
                            await tx.RollbackAsync();
                            InvalidateProjects(createdProjectKeys);
                            continue;
                        }

                        // 1) header
                        int jvId = await InsertJournalVoucherAsync(conn, tx, headRow, jvCode, totalDebit, totalCredit);
                        if (jvId <= 0)
                        {
                            failed++;
                            errors.Add($"SN {sn}: Failed to insert journal voucher header");
                            failedList.Add(new { sn, code = jvCode, reason = "Header insert failed" });
                            await tx.RollbackAsync();
                            InvalidateProjects(createdProjectKeys);
                            continue;
                        }

                        // 2) details + GL transactions
                        foreach (var line in lines)
                        {
                            int accountId = await ResolveAccountIdAsync(conn, tx, line.AccountName);
                            if (accountId <= 0)
                            {
                                failed++;
                                errors.Add($"SN {sn}: Account '{line.AccountName}' not found");
                                failedList.Add(new { sn, code = jvCode, reason = $"Account not found: '{line.AccountName}'" });
                                await tx.RollbackAsync();
                                InvalidateProjects(createdProjectKeys);
                                goto NextGroup;
                            }

                            // Resolve vendor ID (partner name → vendor ID)
                            int vendorId = await ResolveVendorIdAsync(conn, tx, line.Partner);
                            if (!string.IsNullOrWhiteSpace(line.Partner) && vendorId <= 0)
                            {
                                failed++;
                                errors.Add($"SN {sn}: Vendor '{line.Partner}' not found");
                                failedList.Add(new { sn, code = jvCode, reason = $"Vendor not found: '{line.Partner}'" });
                                await tx.RollbackAsync();
                                InvalidateProjects(createdProjectKeys);
                                goto NextGroup;
                            }

                            int projectId = await GetOrCreateProjectIdAsync(conn, tx, line.Project, createdProjectKeys);

                            await InsertJVDetailAsync(conn, tx, headRow.Date, jvId, accountId,
                                line.Debit, line.Credit, line.Description, vendorId, projectId);

                            if (line.Debit > 0)
                                await InsertJVTransactionAsync(conn, tx, headRow.Date, accountId,
                                    line.Debit, 0, jvId, line.Description, jvCode, vendorId);

                            if (line.Credit > 0)
                                await InsertJVTransactionAsync(conn, tx, headRow.Date, accountId,
                                    0, line.Credit, jvId, line.Description, jvCode, vendorId);
                        }

                        await tx.CommitAsync();
                        success++;
                        Console.WriteLine($"[OK] SN={sn} code={jvCode} lines={lines.Count} debit={totalDebit} credit={totalCredit} jvId={jvId}");
                    }
                    catch (Exception ex)
                    {
                        await tx.RollbackAsync();
                        InvalidateProjects(createdProjectKeys);
                        failed++;
                        errors.Add($"SN {sn}: {ex.Message}");
                        failedList.Add(new { sn, code = jvCode, reason = ex.Message });
                        Console.WriteLine($"[FAIL] SN={sn} => {ex.Message}");
                    }

                NextGroup:;
                }
                catch (Exception ex)
                {
                    failed++;
                    errors.Add($"SN {sn}: (connection) {ex.Message}");
                    failedList.Add(new { sn, code = jvCode, reason = $"(connection) {ex.Message}" });
                }
            }

            Console.WriteLine("===========================================");
            Console.WriteLine($"Done. Success={success} Skipped={skipped} Failed={failed}");
            if (failedList.Count > 0)
                Console.WriteLine("FAILURES:\n" + string.Join("\n", errors));
            return new
            {
                totalRows = rows.Count,
                journalVouchers = groups.Count,
                success,
                skipped,
                failed,
                failedList,
                errors
            };
        }

        // drop project-cache entries that were created in a rolled-back group
        private void InvalidateProjects(List<string> keys)
        {
            foreach (var k in keys)
                _projectByName.Remove(k);
            keys.Clear();
        }

        // ============== INSERT METHODS ==============

        private async Task<int> InsertJournalVoucherAsync(MySqlConnection conn, MySqlTransaction tx,
            JVRow headRow, string code, decimal debit, decimal credit)
        {
            var sql = @"
INSERT INTO tbl_journal_voucher
(date, code, debit, credit, created_by, created_date, state)
VALUES (@date, @code, @debit, @credit, @created_by, @created_date, 0);
SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", headRow.Date);
            cmd.Parameters.AddWithValue("@code", code);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@created_by", _userId);
            cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);

            var result = await cmd.ExecuteScalarAsync();
            return (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;
        }

        private async Task InsertJVDetailAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, int jvId, int accountId, decimal debit, decimal credit,
            string description, int vendorId, int projectId)
        {
            var sql = @"
INSERT INTO tbl_journal_voucher_details
(date, debit, credit, inv_id, description, account_id, partner, project_id)
VALUES (@date, @debit, @credit, @inv_id, @description, @account_id, @partner, @project_id)";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@inv_id", jvId);
            cmd.Parameters.AddWithValue("@description", Truncate(description ?? "", 500));
            cmd.Parameters.AddWithValue("@account_id", accountId);
            int defaultVendorId = 1;  // "No Vendor"
            cmd.Parameters.AddWithValue("@partner", vendorId > 0 ? vendorId : defaultVendorId);
            cmd.Parameters.AddWithValue("@project_id", projectId > 0 ? projectId : 0);

            await cmd.ExecuteNonQueryAsync();
        }

        private async Task InsertJVTransactionAsync(MySqlConnection conn, MySqlTransaction tx,
            DateTime date, int accountId, decimal debit, decimal credit, int jvId,
            string description, string voucherCode, int vendorId)
        {
            var sql = @"
INSERT INTO tbl_transaction
(date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no)
VALUES (@date, @account_id, @debit, @credit, @transaction_id, @hum_id, @t_type, @type, @description, @created_by, @created_date, 0, @voucher_no)";

            using var cmd = new MySqlCommand(sql, conn, tx);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@account_id", accountId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transaction_id", jvId);
            cmd.Parameters.AddWithValue("@hum_id", 0);
            cmd.Parameters.AddWithValue("@t_type", "JOURNAL");
            cmd.Parameters.AddWithValue("@type", "JOURNAL VOUCHER");
            // use the real line description; fall back to the boilerplate if empty
            cmd.Parameters.AddWithValue("@description",
                string.IsNullOrWhiteSpace(description) ? $"Journal Voucher NO. {voucherCode}" : Truncate(description, 250));
            cmd.Parameters.AddWithValue("@created_by", _userId);
            cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
            cmd.Parameters.AddWithValue("@voucher_no", voucherCode);

            await cmd.ExecuteNonQueryAsync();
        }

        // ============== HELPERS ==============

        private async Task LoadAccountCacheAsync(MySqlConnection conn)
        {
            using var cmd = new MySqlCommand(
                "SELECT id, name FROM tbl_coa_level_4 ORDER BY name", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var key = reader.GetString("name").Trim();
                var id = reader.GetInt32("id");
                if (!_accountByName.ContainsKey(key))
                    _accountByName[key] = id;
            }
        }

        // Load vendor cache at startup
        private async Task LoadVendorCacheAsync(MySqlConnection conn)
        {
            using var cmd = new MySqlCommand(
                "SELECT id, name FROM tbl_vendor ORDER BY name", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var key = reader.GetString("name").Trim();
                var id = reader.GetInt32("id");
                if (!_vendorByName.ContainsKey(key))
                    _vendorByName[key] = id;
            }
        }

        // Resolve vendor name to ID; returns 0 if not found or name is empty
        private async Task<int> ResolveVendorIdAsync(MySqlConnection conn, MySqlTransaction tx, string vendorName)
        {
            var key = (vendorName ?? "").Trim();
            if (key.Length == 0) return 0;  // optional vendor field

            // Check cache first
            if (_vendorByName.TryGetValue(key, out var cachedId))
                return cachedId;

            // Query database
            using var cmd = new MySqlCommand(
                "SELECT id FROM tbl_vendor WHERE TRIM(name)=TRIM(@n) OR TRIM(name) LIKE CONCAT('%',TRIM(@n),'%') LIMIT 1",
                conn, tx);
            cmd.Parameters.AddWithValue("@n", key);
            var result = await cmd.ExecuteScalarAsync();
            int id = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;

            // Cache the result
            _vendorByName[key] = id;
            return id;
        }

        // get-or-create project; records newly created keys so they can be invalidated on rollback
        private async Task<int> GetOrCreateProjectIdAsync(MySqlConnection conn, MySqlTransaction tx,
            string name, List<string> createdKeys)
        {
            var key = (name ?? "").Trim().Trim('\uFEFF', '\u200E', '\u200F');
            if (key.Length == 0) return 0;                       // contra lines: no project
            if (_projectByName.TryGetValue(key, out var cached)) return cached;

            // try existing
            using (var find = new MySqlCommand(
                "SELECT id FROM tbl_projects WHERE TRIM(name)=TRIM(@n) LIMIT 1", conn, tx))
            {
                find.Parameters.AddWithValue("@n", key);
                var res = await find.ExecuteScalarAsync();
                if (res != null && res != DBNull.Value)
                {
                    int existing = Convert.ToInt32(res);
                    _projectByName[key] = existing;
                    return existing;
                }
            }

            // create — adjust columns to YOUR tbl_projects (fill every NOT-NULL column)
            using (var ins = new MySqlCommand(@"
INSERT INTO tbl_projects (name)
VALUES (@n);
SELECT LAST_INSERT_ID();", conn, tx))
            {
                ins.Parameters.AddWithValue("@n", key);
                int newId = Convert.ToInt32(await ins.ExecuteScalarAsync());
                _projectByName[key] = newId;
                createdKeys.Add(key);     // so we can roll the cache back if this group fails
                Console.WriteLine($"[PROJECT+] created '{key}' -> id {newId}");
                return newId;
            }
        }

        private async Task<int> ResolveAccountIdAsync(MySqlConnection conn, MySqlTransaction tx, string accountName)
        {
            var key = (accountName ?? "").Trim();
            if (key.Length == 0) return 0;

            if (_accountByName.TryGetValue(key, out var cachedId))
                return cachedId;

            using var cmd = new MySqlCommand(
                "SELECT id FROM tbl_coa_level_4 WHERE TRIM(name)=TRIM(@n) OR TRIM(name) LIKE CONCAT('%',TRIM(@n),'%') LIMIT 1",
                conn, tx);
            cmd.Parameters.AddWithValue("@n", key);
            var result = await cmd.ExecuteScalarAsync();
            int id = (result != null && result != DBNull.Value) ? Convert.ToInt32(result) : 0;

            _accountByName[key] = id;
            return id;
        }

        // ============== EXCEL READER ==============
        // Robust reader: instead of a hardcoded Skip(N), it scans for the header row
        // (the row whose first cell == "SN") and processes every data row after it.
        // This works no matter how many blank/metadata/preamble rows the export has.

        private List<JVRow> ReadJVRows(string filePath)
        {
            var rows = new List<JVRow>();
            using var wb = new XLWorkbook(filePath);
            var ws = wb.Worksheets.First();

            var snDateCache = new Dictionary<string, DateTime>();
            var snProjectCache = new Dictionary<string, string>();   // header line's project, inherited by contra lines

            bool headerSeen = false;   // becomes true once we pass the "SN" column-header row

            foreach (var r in ws.RowsUsed())
            {
                // Read the SN cell, resolving a formula result to its number/text if needed
                var aCell = r.Cell("A");
                string snStr = aCell.GetString().Trim();
                if (snStr.StartsWith("="))
                {
                    snStr = aCell.DataType == XLDataType.Number
                        ? ((int)aCell.GetDouble()).ToString()
                        : aCell.GetString().Trim();
                }

                // The column-header row: mark it and skip. Everything before it is preamble.
                if (snStr.Equals("SN", StringComparison.OrdinalIgnoreCase))
                {
                    headerSeen = true;
                    continue;
                }

                // Ignore any preamble/metadata rows that appear before the header.
                if (!headerSeen)
                    continue;

                // Blank SN with no usable data -> skip
                if (string.IsNullOrWhiteSpace(snStr))
                    continue;

                string sn = snStr;
                string dateStr = r.Cell("B").GetString().Trim();

                DateTime date;
                if (string.IsNullOrWhiteSpace(dateStr))
                {
                    if (snDateCache.ContainsKey(sn))
                        date = snDateCache[sn];
                    else
                    {
                        Console.WriteLine($"Row {r.RowNumber()}: SN={sn} has no cached date - skipping");
                        continue;
                    }
                }
                else
                {
                    var bCell = r.Cell("B");
                    if (bCell.DataType == XLDataType.DateTime)
                        date = bCell.GetDateTime();
                    else if (!TryParsewDate(dateStr, out date))
                    {
                        Console.WriteLine($"Row {r.RowNumber()} bad date format: '{dateStr}' - skipping");
                        continue;
                    }

                    if (!snDateCache.ContainsKey(sn))
                        snDateCache[sn] = date;
                }

                string partner = r.Cell("C").GetString().Trim();   // Vendor name
                string accountName = r.Cell("D").GetString().Trim();   // real GL account
                string project = r.Cell("E").GetString().Trim();   // Project (blank on contra lines)
                string description = r.Cell("H").GetString().Trim();

                // project: header line has it; detail (contra) lines are blank -> inherit the SN's project
                if (string.IsNullOrWhiteSpace(project))
                {
                    if (snProjectCache.TryGetValue(sn, out var cachedProj))
                        project = cachedProj;            // reuse header's project for this SN
                }
                else
                {
                    if (!snProjectCache.ContainsKey(sn))
                        snProjectCache[sn] = project;    // cache header's project for this SN
                }

                decimal debit = ParseNumber(r.Cell("I"));
                decimal credit = ParseNumber(r.Cell("J"));

                if (string.IsNullOrWhiteSpace(accountName))
                    continue;
                if (debit == 0 && credit == 0)
                    continue;

                rows.Add(new JVRow
                {
                    Sn = sn,
                    Date = date,
                    AccountName = accountName,
                    Partner = partner,  // Keep as vendor name string; will be resolved to ID during import
                    Project = project,
                    Description = description,
                    Debit = debit,
                    Credit = credit
                });
            }

            if (!headerSeen)
                Console.WriteLine("WARNING: no 'SN' header row found - nothing was parsed. Check the sheet/columns.");

            Console.WriteLine($"ReadJVRows: parsed {rows.Count} detail lines from Excel");
            return rows;
        }

        // ============== UTILITIES ==============

        private static string Truncate(string s, int max) =>
            string.IsNullOrEmpty(s) ? "" : (s.Length > max ? s.Substring(0, max) : s);

        private static decimal ParseNumber(IXLCell cell)
        {
            if (cell.DataType == XLDataType.Number)
                return (decimal)cell.GetDouble();

            var s = cell.GetString().Replace(",", "").Trim();
            if (s.Length == 0 || s == "-")
                return 0m;

            return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;
        }

        private static bool TryParsewDate(string s, out DateTime date)
        {
            if (DateTime.TryParseExact(s, _dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;

            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
            {
                try { date = DateTime.FromOADate(oa); return true; }
                catch { }
            }

            return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        // ============== DATA MODEL ==============

        private class JVRow
        {
            public string Sn { get; set; } = "";
            public DateTime Date { get; set; }
            public string AccountName { get; set; } = "";
            public string Partner { get; set; } = "";  // Vendor name (resolved to ID during import)
            public string Project { get; set; } = "";
            public string Description { get; set; } = "";
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
        }

        #endregion
    }
}





