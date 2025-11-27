namespace YamyProject.Controllers.Subcontractor
    {
    public class SubcontractorReceiveVoucherController(IPaymentVoucherService PaymentVoucher, IListServices listServices, YamyDbContext context) : Controller
        {
        private readonly IPaymentVoucherService _PaymentVoucher = PaymentVoucher;
        private readonly IListServices _ListServices = listServices;
        private readonly YamyDbContext _context = context;

        public async Task<IActionResult> Create()
            {
            var vendor = await _ListServices.GetVendorSubcontractorsAsync();
            var vendorSelectList = vendor.Select(c => new TblVendor
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
            var Employee = await _ListServices.GetEmployeeAsync();
            var EmployeelectList = Employee.Select(c => new TblEmployee
                {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name
                }).ToList();
            return View("~/Views/ReceiveVoucher/ReceiveVoucher.cshtml", new ReceiveVoucherViewModel
                {
                Type = "Subcontractor",
                IsSubcontractor =true,
                VoucherNo = await _PaymentVoucher.GenerateNextPaymentCode(),
                Date = DateOnly.FromDateTime(DateTime.Now),
                Employees = EmployeelectList,
                Accounts = AccountList,
                vendors = vendorSelectList,
                CostCenters = CostCenterList
                }
            );
            }


        //[HttpGet("/ReceiveVoucher/GetVendorInvoiceRowsAsync")] // ← absolute path, no Area
        //public async Task<IActionResult> GetVendorInvoiceRowsAsync(int vendorId)
        //    {
        //    var rows = new List<RvItemViewModel>();
        //    int sn = 0;
        //    // 1) Customer opening balance
        //    var cust = await _context.TblVendors
        //        .Where(c => c.Id == vendorId)
        //        .Select(c => new { c.Id, c.Balance, c.Date })
        //        .FirstOrDefaultAsync();
        //    if (cust is not null)
        //        {
        //        var paidAgainstOB = await _context.TblPaymentVoucherDetails
        //            .Where(t => t.InvCode == "Subcontractor Opening Balance" && t.HumId == vendorId)
        //            .SumAsync(t => (decimal?)t.Payment) ?? 0m;
        //        var diff = (cust.Balance ?? 0m) - paidAgainstOB;
        //        if (diff != 0m)
        //            {
        //            rows.Add(new RvItemViewModel
        //                {
        //                SN = ++sn,
        //                humId = vendorId,
        //                Date = cust.Date, // fallback
        //                InvoiceNo = "Subcontractor Opening Balance",
        //                Amount = diff,
        //                });
        //            }
        //        }
        //    // 2) Sales with remaining change <> 0
        //    var sales = await _context.TblPurchases
        //        .Where(s => s.State == 0
        //                    && s.VendorId == vendorId
        //                    && s.Change != 0m)
        //        .OrderBy(s => s.Date)
        //        .Select(s => new
        //            {
        //            s.Date,
        //            s.InvoiceId,
        //            s.Change
        //            })
        //        .ToListAsync();
        //    foreach (var s in sales)
        //        {
        //        rows.Add(new RvItemViewModel
        //            {
        //            SN = ++sn,
        //            humId = vendorId,
        //            Date = s.Date,
        //            InvoiceNo = s.InvoiceId ?? "",   // this maps your `'INV NO'`
        //            Amount = s.Change,    // remaining amount                  
        //            });
        //        }
        //    if (rows is null) return NotFound();
        //    return Json(rows); // your JS will fill the grid
        //    }

        //[HttpGet("/ReceiveVoucher/GetEmployeeInvoiceRowsAsync")] // ← absolute path, no Area
        //public async Task<IActionResult> GetEmployeeInvoiceRowsAsync(int EmployeeId)
        //    {
        //    var rows = new List<RvItemViewModel>();

        //    if (rows is null) return NotFound();
        //    return Json(rows); // your JS will fill the grid
        //    }

        }
    }