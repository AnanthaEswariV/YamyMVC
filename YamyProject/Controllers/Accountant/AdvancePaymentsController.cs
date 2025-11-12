namespace YamyProject.Controllers.Accountant
    {
    public class AdvancePaymentsController (IAdvancePaymentService AdvancePaymentService, IListServices listServices, YamyDbContext context) : Controller
        {
        private readonly IAdvancePaymentService _AdvancePaymentService = AdvancePaymentService;
        private readonly IListServices _ListServices = listServices;
        private readonly YamyDbContext _context = context;

        public async Task<IActionResult> Index()
            {
            var AdvancePayment =await  _AdvancePaymentService.GetAdvancePayments();
            return View(AdvancePayment);
            }
        public async Task<IActionResult> Create()
            {
            var nextCode = await _AdvancePaymentService.GenerateNextAdvancePaymentCode();
            var Vendors = await _ListServices.GetVendorsAsync();
            var VendorslectList = Vendors.Select(c => new SelectListItem
                {
                   Value = c.Id.ToString(),
                   Text =  c.Name
                   }).ToList();
           var customers = await _ListServices.GetCustomersAsync();
            var customerSelectList = customers.Select(c => new SelectListItem
                {
                   Value = c.Id.ToString(),
                   Text = c.Name                   
                }).ToList();
            var Account = await _ListServices.GetAccountsAsync();
            var AccountList = Account.Select(c => new TblCoaLevel4
                {
                Id = c.Id,
                Name = c.Name
                }).ToList();
            var CostCenter = await _ListServices.GetCostCenterAsync();
            var CostCenterList = CostCenter.Select(c => new TblCostCenter
                {
                Id = c.Id,
                Code = c.Code
                }).ToList();
            var Banks = await _ListServices.GetBanksAsync();
            var VBankslectList = Banks.Select(c => new SelectListItem
                {
                Value = c.Id.ToString(),
                Text = c.Name
                }).ToList();
            return View("AdvancePayment", new AdvancePaymentViewModel
                {
                   VoucherNo = nextCode,
                   customers=customerSelectList,
                   vendors=VendorslectList,
                   CostCenters=CostCenterList,
                   Accounts=AccountList,
                   Date = DateOnly.FromDateTime(DateTime.Now)
                });
            }
        [HttpPost]
        public async Task<IActionResult> Create(AdvancePaymentViewModel model)
            {                      
            await _AdvancePaymentService.CreateAdvancePaymentAsync(model);
            return RedirectToAction(nameof(Index));
            }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
            {
            var vm = await _AdvancePaymentService.GetEditAsync(id);
            return View("AdvancePayment", vm);
            }
        [HttpPost]
        public async Task<IActionResult> Edit(AdvancePaymentViewModel model)
            {
                 await _AdvancePaymentService.UpdateAdvancePaymentAsync(model);
            return RedirectToAction(nameof(Index));
            }
        public async Task<IActionResult> GetPartners(string term,int PaymentType)
            {
            term = term?.Trim() ?? string.Empty;
            if (PaymentType == 1)
                {
                var query = _context.TblVendors.AsNoTracking();

                if (!string.IsNullOrEmpty(term))
                    {
                    // Case-insensitive contains on Code or Name
                    query = query.Where(b =>
                        EF.Functions.Like(b.Name, $"%{term}%") ||
                        EF.Functions.Like(b.Code, $"%{term}%"));
                    }

                var items = await query
                    .OrderBy(b => b.Name)
                    .Take(50)
                    .Select(b => new
                        {
                        id = b.Id,    // <- your hidden value
                        code = b.Code,  // <- shown in "CODE - NAME"
                        name = b.Name
                        })
                    .ToListAsync();

                return Json(items);
                }
            else
                {
                var query = _context.TblCustomers.AsNoTracking();

                if (!string.IsNullOrEmpty(term))
                    {
                    // Case-insensitive contains on Code or Name
                    query = query.Where(b =>
                        EF.Functions.Like(b.Name, $"%{term}%") ||
                        EF.Functions.Like(b.Code, $"%{term}%"));
                    }

                var items = await query
                    .OrderBy(b => b.Name)
                    .Take(50)
                    .Select(b => new
                        {
                        id = b.Id,    // <- your hidden value
                        code = b.Code,  // <- shown in "CODE - NAME"
                        name = b.Name
                        })
                    .ToListAsync();

                return Json(items);
                }                        
            }

        public async Task<IActionResult> GetBanks(string term, int PaymentMethod)
            {
             term = term?.Trim() ?? string.Empty;

            if( PaymentMethod == 2)
                {
                var query = _context.TblBanks.AsNoTracking();

                if (!string.IsNullOrEmpty(term))
                    {
                    // Case-insensitive contains on Code or Name
                    query = query.Where(b =>
                        EF.Functions.Like(b.Name, $"%{term}%") ||
                        EF.Functions.Like(b.Code, $"%{term}%"));
                    }

                var items = await query
                    .OrderBy(b => b.Name)
                    .Take(50)
                    .Select(b => new
                        {
                        id = b.Id,    // <- your hidden value
                        code = b.Code,  // <- shown in "CODE - NAME"
                        name = b.Name
                        })
                    .ToListAsync();

                return Json(items);
                }
            return Json(new List<object>());

            }

        }
    }