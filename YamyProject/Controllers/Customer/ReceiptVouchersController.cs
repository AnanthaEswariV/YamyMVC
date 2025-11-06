namespace YamyProject.Controllers.Customer
{
    public class ReceiptVouchersController (IReceiptVoucherService ReceiptVoucher, IListServices listServices, YamyDbContext context) : Controller
    {
        private readonly IReceiptVoucherService _ReceiptVoucher = ReceiptVoucher;
        private readonly IListServices _ListServices = listServices;
        private readonly YamyDbContext _context = context;

        public async Task<IActionResult> Index(DateOnly from, DateOnly to, bool all = false, CancellationToken ct = default)
            {
            ReceiptVoucherCenterViewModel vm = await _ReceiptVoucher.QuerySalesAsync(from, to, all, ct);
            return View(vm); // ✅ passes the actual model, not a Task
            }
        public async Task<IActionResult> Create()
            {
            var customers = await _ListServices.GetCustomersRetrunAsync();
            var customerSelectList = customers.Select(c => new TblCustomer
                {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name
                }).ToList();

            var Account = await _ListServices.GetAccountsAsync();
            var AccountList = Account.Select(c => new TblCoaLevel4
                {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code
                }).ToList();
            var CostCenter = await _ListServices.GetCostCenterAsync();
            var CostCenterList = CostCenter.Select(c => new TblCostCenter
                {
                Id = c.Id,
                Code = c.Code,
                }).ToList();

            return View("ReceiptVoucher", new ReceiptVoucherViewModel
                {
                VoucherNo = await _ReceiptVoucher.GenerateNextReceiptCode(),
                Date = DateOnly.FromDateTime(DateTime.Now),
                Accounts = AccountList,
                Customers = customerSelectList,
                CostCenters = CostCenterList
                }
            );
            }
        [HttpGet("/ReceiptVouchers/GetInvoiceRowsAsync")] // ← absolute path, no Area
        public async Task<IActionResult> GetInvoiceRowsAsync(int customerId)
        {
            var rows = new List<RvItemViewModel>();
            int sn = 0;
            // 1) Customer opening balance
            var cust = await _context.TblCustomers
                .Where(c => c.Id == customerId)
                .Select(c => new { c.Id, c.Balance, c.Date })
                .FirstOrDefaultAsync();
            if (cust is not null)
                {
                var paidAgainstOB = await _context.TblReceiptVoucherDetails
                    .Where(t => t.InvCode == "C-OB" && t.HumId == customerId)
                    .SumAsync(t => (decimal?)t.Payment) ?? 0m;
                var diff = (cust.Balance ?? 0m) - paidAgainstOB;
                if (diff != 0m)
                    {
                    rows.Add(new RvItemViewModel
                        {
                        SN = ++sn,
                        humId = customerId,
                        Date = cust.Date, // fallback
                        InvoiceNo = "C-OB",
                        Amount = diff,
                       });
                    }
                }
            // 2) Sales with remaining change <> 0
            var sales = await _context.TblSales
                .Where(s => s.State == 0
                            && s.CustomerId == customerId
                            && s.Change != 0m)
                .OrderBy(s => s.Date)
                .Select(s => new
                    {
                    s.Date,
                    s.InvoiceId,
                    s.Change
                    })
                .ToListAsync();
            foreach (var s in sales)
                {
                rows.Add(new RvItemViewModel
                    {
                    SN = ++sn,
                    humId = customerId,
                    Date = s.Date,
                    InvoiceNo = s.InvoiceId ?? "",   // this maps your `'INV NO'`
                    Amount = s.Change ,    // remaining amount                  
                    });
                }
            if (rows is null) return NotFound();
            return Json(rows); // your JS will fill the grid
        }
    }
}