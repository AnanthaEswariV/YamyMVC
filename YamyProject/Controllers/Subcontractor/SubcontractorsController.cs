namespace YamyProject.Controllers.Subcontractor
    {
    public class SubcontractorsController(YamyDbContext context, ILogger<MasterStockManagementController> logger) : Controller
        {
        private readonly YamyDbContext _context = context;

        private readonly ILogger<MasterStockManagementController> _logger = logger;

        public async Task<IActionResult> Index()
            {
            var wanted = new[] {
                                            "Subcontractor Payment",
                                            "Petty Cash",
                                            "Purchase Invoice",
                                            "Subcontractor Opening Balance",
                                            "Check Cancel (Subcontractor)",
                                            "Purchase Return Invoice",
                                            "Debit Note",
                                            "PDC Payable"
                                        };
            // Base query: active vendor records only (state==0) and type Vendor
            var vendorsQuery = _context.TblVendors
                                  .AsNoTracking()
                                  .Where(v => v.State == 0 && v.Type == "Subcontractor");
            var items = await vendorsQuery
                .Select(v => new VendorIndexViewModel
                    {
                    Id = v.Id,
                    Name = (v.Name ?? ""),
                    Code = v.Code.ToString(),
                    WorkPhone = v.WorkPhone,
                    MainPhone = v.MainPhone,
                    CategoryName = _context.TblVendorCategories
                         .Where(c => c.Id == v.CatId)
                         .Select(c => c.Name)
                         .FirstOrDefault(),
                    Region = v.Region,
                    Email = v.Email,
                    TRN = v.Trn,
                    Balance = v.Balance ?? 0m,
                    Date = v.Date,
                    // Opening sums (same filter as in SQL)
                    OpeningDebit = _context.TblTransactions
                         .Where(t => t.State == 0
                                  && t.HumId == v.Id
                                  && wanted.Contains(t.Type))
                         .Sum(t => (decimal?)t.Debit) ?? 0m,
                    OpeningCredit = _context.TblTransactions
                          .Where(t => t.State == 0
                                   && t.HumId == v.Id
                                   && wanted.Contains(t.Type))
                          .Sum(t => (decimal?)t.Credit) ?? 0m,
                    Amount = _context.TblTransactions
                   .Where(t => t.State == 0
                            && t.HumId == v.Id
                            && wanted.Contains(t.Type))
                   .Sum(t => (decimal?)(t.Credit - t.Debit)) ?? 0m,
                    Active = (v.Active == 0)
                    })
                .ToListAsync();
            return View(items);
            }

        [HttpGet]
        public async Task<IActionResult> Header(int id)
            {
            var dto = await _context.TblVendors
        .AsNoTracking()
        .Where(v => v.Id == id && v.Type == "Subcontractor" && v.State == 0)
        .Select(v => new VendorDetailViewModel
            {
            Id = v.Id,
            Code = v.Code,
            Name = v.Name ?? "",
            TRN = v.Trn ?? "",
            Email = v.Email ?? "",
            WorkPhone = v.WorkPhone ?? "",
            MainPhone = v.MainPhone ?? "",
            Category = _context.TblVendorCategories
                            .Where(c => c.Id == v.CatId)
                            .Select(c => c.Name)
                            .FirstOrDefault() ?? "",

            Address = v.Region ?? "",
            balance = v.Balance ?? 0m
            })
        .FirstOrDefaultAsync();

            if (dto is null) return NotFound();   // 404 if vendor not found
            return Json(dto);
            }
        [HttpGet]
        public async Task<IActionResult> Transactions(int vendorId)
            {
            var wantedTypes = new[]
            {
              "Subcontractor Payment",
              "Petty Cash",
              "Purchase Invoice",
              "Subcontractor Opening Balance",
              "Check Cancel (Subcontractor)",
              "Purchase Return Invoice",
              "Debit Note",
              "PDC Payable"
             };
            // Pull raw rows first (ordered) — no explicit joins
            var raw = await _context.TblTransactions
                .AsNoTracking()
                .Where(t => t.State == 0
                         && t.HumId == vendorId
                         && wantedTypes.Contains(t.Type))
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Id)
                .Select(t => new
                    {
                    t.Id,
                    t.Date,
                    t.VoucherNo,
                    VoucherType = t.Type,
                    Description = _context.TblCoaLevel4s
                                   .Where(a => a.Id == t.AccountId)
                                   .Select(a => (a.Code) + " - " + (a.Name ?? ""))
                                   .FirstOrDefault(),
                    Debit = (decimal?)(t.Debit ?? 0m) ?? 0m,
                    Credit = (decimal?)(t.Credit ?? 0m) ?? 0m
                    })
                .ToListAsync();
            // Build VMs + running balance
            var result = new List<VendorTxnViewModel>(raw.Count);
            decimal running = 0m;
            int sn = 0;
            foreach (var r in raw)
                {
                running += (r.Credit - r.Debit);

                result.Add(new VendorTxnViewModel
                    {
                    Sn = ++sn,
                    Date = r.Date,
                    VoucherNo = r.VoucherNo ?? "",
                    VoucherType = r.VoucherType ?? "",
                    Description = r.Description,
                    Debit = r.Debit,
                    Credit = r.Credit,
                    Balance = running
                    });
                }
            return Json(result);
            }
        [HttpGet]
        public async Task<IActionResult> Cash(int vendorId)
            {
            var cashType = "Purchase Invoice Cash";

            var rows = await _context.TblTransactions
                .AsNoTracking()
                .Where(t => t.State == 0
                         && t.HumId == vendorId
                         && t.Type == cashType)
                .OrderBy(t => t.Date)
                .Select(t => new CashInvoiceRowViewModel
                    {
                    VendorId = t.HumId,
                    VoucherNo = t.VoucherNo,
                    Date = t.Date,
                    AccountName = _context.TblCoaLevel4s
                                    .Where(a => a.Id == t.AccountId)
                                    .Select(a => (a.Code) + " - " + (a.Name ?? ""))
                                    .FirstOrDefault(),
                    VoucherType = t.Type,
                    Amount = (decimal?)(t.Credit - t.Debit) ?? 0m
                    })
                .ToListAsync();

            // Number the rows (optional)
            for (int i = 0; i < rows.Count; i++) rows[i].Sn = i + 1;

            var vm = new CashInvoiceListViewModel
                {
                VendorId = vendorId,
                Rows = rows
                };

            return Json(vm);
            }


        }
    }