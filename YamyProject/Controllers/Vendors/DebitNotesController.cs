namespace YamyProject.Controllers.Vendors
    {
    public class DebitNotesController(IDebitNotesService DebitVoucher, YamyDbContext context, IListServices listServices) : Controller
        {
        private readonly IDebitNotesService _DebitVoucher = DebitVoucher;
        private readonly IListServices _ListServices = listServices;
        private readonly YamyDbContext _context = context;

        public async Task<IActionResult> Index()
            {
            MasterDebitNoteViewModel model = await _DebitVoucher.QueryDebitNoteAsync();

            return View(model);
            }
        public async Task<IActionResult> Create()
            {

            var Vendors = await _ListServices.GetVendorsAsync();
            var VendorSelectList = Vendors.Select(c => new TblVendor
                {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name
                }).ToList();
            var Account = await _ListServices.GetAccountsAsync();
            var AccountList = Account.Select(c => new TblCoaLevel4
                {
                Id = c.Id,
                Name = c.Name
                }).ToList();
            return View("MasterDebitNote", new DebitNoteViewModel
                {
                Vendors = VendorSelectList,
                DebitAccounts = AccountList,
                });
            }
        [HttpPost]
        public async Task<IActionResult> Create(DebitNoteViewModel model)
            {
            //if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
            //if (model.VendorId is null) return BadRequest("Vendor is required.");
             var userId = 1;
          //  var result = 
                await _DebitVoucher.CreateDebitNoteAsync(model);
            //if(result)
            return RedirectToAction(nameof(Index));
           // return View(model);
            }

        public async Task<IActionResult> Edite(int id)
            {
            var Vendors = await _ListServices.GetVendorsAsync();
            var VendorSelectList = Vendors.Select(c => new TblVendor
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

            return View("MasterDebitNote");
            }
        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails(int VendorId)
            {
            var rows = await _context.TblPurchases
       .AsNoTracking()
       .Where(s => s.VendorId == VendorId)
       .Where(s => (s.Change) > 0m)
       .Where(s => _context.TblTransactions.Any(t =>
           (t.Type == "Vendor Opening Balance" || t.Type == "Purchase Invoice") &&
           t.TransactionId == s.Id &&
           t.HumId == s.VendorId))
       .OrderBy(s => s.Date)
       .Select(s => new
           {
           s.Id,
           s.Date,
           s.InvoiceId,          
           s.Total,
           s.Net,
           TotalWithVAT = s.Net,
           s.Vat,
           Remaining = s.Change
           })
       .ToListAsync();

            if (rows is null) return NotFound();
            return Json(rows); // your JS will fill the grid
            }
        }
    }