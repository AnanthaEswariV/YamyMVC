using YamyProject.Core.Models;

namespace YamyProject.Controllers.Vendors
    {
    public class ReceiveVoucherController(IPaymentVoucherService PaymentVoucher, IListServices listServices, YamyDbContext context) : Controller
        {
        private readonly IPaymentVoucherService _PaymentVoucher  = PaymentVoucher;
        private readonly IListServices _ListServices = listServices;
        private readonly YamyDbContext _context = context;


        public async Task<IActionResult> Index(DateOnly from, DateOnly to, bool all = false, CancellationToken ct = default)
            {
            ReceiveVoucherCenterViewModel vm = await _PaymentVoucher.QueryPurchaseAsync(from, to, all, ct);
            return View(vm); // ✅ passes the actual model, not a Task

            }

        public async Task<IActionResult> Create()
            {
            var vendor = await _ListServices.GetVendorsAsync();
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
            return View("ReceiveVoucher", new ReceiveVoucherViewModel
                {
                Type = "Vendor",
                Employees = EmployeelectList,
                VoucherNo = await _PaymentVoucher.GenerateNextPaymentCode(),
                Date = DateOnly.FromDateTime(DateTime.Now),
                Accounts = AccountList,
                vendors = vendorSelectList,
                CostCenters = CostCenterList
                }
            );
            }

        [HttpPost]
        public async Task<IActionResult> Create(ReceiveVoucherViewModel model)
            {

            //if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
            //if (model.CustomerId is null) return BadRequest("Customer is required.");
            //if (model.WarehousesId is null) return BadRequest("Warehouse is required.");
            //var userId = 1;
            
            await _PaymentVoucher.CreatePvAsync(model);


            return RedirectToAction(nameof(Index));
            }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
            {
            var vendor = await _ListServices.GetVendorsAsync();
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
            var vm = await _PaymentVoucher.GetEditAsync(id);
            vm.Employees = EmployeelectList;
            vm.Accounts = AccountList;
            vm.vendors = vendorSelectList;
            vm.CostCenters = CostCenterList;

            return View("ReceiveVoucher", vm);
            }

        [HttpGet("/ReceiveVoucher/GetVendorInvoiceRowsAsync")] // ← absolute path, no Area
        public async Task<IActionResult> GetVendorInvoiceRowsAsync(int vendorId)
            {
            var rows = new List<RvItemViewModel>();
            int sn = 0;
            // 1) Customer opening balance
            var cust = await _context.TblVendors
                .Where(c => c.Id == vendorId)
                .Select(c => new { c.Id, c.Balance, c.Date })
                .FirstOrDefaultAsync();
            if (cust is not null)
                {
                var paidAgainstOB = await _context.TblPaymentVoucherDetails
                    .Where(t => t.InvCode == "Vendor Opening Balance" && t.HumId == vendorId)
                    .SumAsync(t => (decimal?)t.Payment) ?? 0m;
                var diff = (cust.Balance ?? 0m) - paidAgainstOB;
                if (diff != 0m)
                    {
                    rows.Add(new RvItemViewModel
                        {
                        SN = ++sn,
                        humId = vendorId,
                        Date = cust.Date, // fallback
                        InvoiceNo = "Vendor Opening Balance",
                        Amount = diff,
                        });
                    }
                }
            // 2) Sales with remaining change <> 0
            var sales = await _context.TblPurchases
                .Where(s => s.State == 0
                            && s.VendorId == vendorId
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
                    humId = vendorId,
                    Date = s.Date,
                    InvoiceNo = s.InvoiceId ?? "",   // this maps your `'INV NO'`
                    Amount = s.Change,    // remaining amount                  
                    });
                }
            if (rows is null) return NotFound();
            return Json(rows); // your JS will fill the grid
            }

        [HttpGet("/ReceiveVoucher/GetEmployeeInvoiceRowsAsync")] // ← absolute path, no Area
        public async Task<IActionResult> GetEmployeeInvoiceRowsAsync(int EmployeeId)
            {
            var rows = new List<RvItemViewModel>();
            int sn = 0;
            var Salari = await _context.TblAttendanceSalaries
                .Where(c => c.EmpCode == EmployeeId.ToString() && c.Change != 0)
                .Select(c => new { c.Id, c.TotalLoan, c.Date,c.NetSalary,c.Pay })
                .OrderBy(c=>c.Id)
                .FirstOrDefaultAsync();
            if (Salari is not null)
                {
                rows.Add(new RvItemViewModel
                    {
                    SN = ++sn,
                    humId = EmployeeId,
                    Date = Salari.Date, // fallback
                    InvoiceNo = Salari.Id.ToString(),
                    Amount = (decimal)(Salari.NetSalary - Salari.TotalLoan - Salari.Pay),
                    Pay=false,
                    VoucherType= "Salary",
                    invId= Salari.Id

                    });
                }
            var loans = await _context.TblLoans
                  .Where(l => l.EmployeeId == EmployeeId.ToString() && l.Change > 0)
                  .Select(l=>new { l.Id, l.EmployeeId, l.LoanDate, l.Amount, l.Change })
                  .OrderBy(l=>l.LoanDate)
                  .FirstOrDefaultAsync();
            if (loans is not null)
                {
                rows.Add(new RvItemViewModel
                    {
                    SN = ++sn,
                    humId = EmployeeId,
                    Date = loans.LoanDate, // fallback
                    InvoiceNo = loans.Id.ToString(),
                    Amount = (decimal)loans.Change,
                    Pay = false,
                    VoucherType = "Employee Loan Payment",
                    invId = loans.Id

                    }
                    );
                }
                if (rows is null) return NotFound();
            return Json(rows); // your JS will fill the grid
            }
   
        }
}