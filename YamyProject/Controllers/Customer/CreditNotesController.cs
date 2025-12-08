namespace YamyProject.Controllers.Customer
    {
    public class CreditNotesController(ICreditNoteService CreditVoucher, YamyDbContext context, IListServices listServices) : Controller
        {
        private readonly ICreditNoteService _CreditVoucher = CreditVoucher;
        private readonly IListServices _ListServices = listServices;
        private readonly YamyDbContext _context = context;


        public async Task<IActionResult> Index()
            {
            MasterCreditNoteViewModel vm = await _CreditVoucher.QueryCreditNoteAsync();
            return View(vm);
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
                Name = c.Name
                }).ToList();
            var code =await _CreditVoucher.GenerateNextCreditNoteCode();
            return View("MasterCreditNote", new CreditNoteViewModel
                {
                Code = code.ToString(),
                Customers = customerSelectList,
                DebitAccounts = AccountList,
                });
            }
        [HttpPost]
        public async Task<IActionResult> Create(CreditNoteViewModel  model)
            {
            //if (!ModelState.IsValid)
            //  return View("TaxInvoice", PopulateViewModel(model));
         //   if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
           // if (model.CustomerId is null) return BadRequest("Customer is required.");
            //if (model.WarehousesId is null) return BadRequest("Warehouse is required.");

            //  var userId = 1;

            await _CreditVoucher.insertInvoice(model);
            return RedirectToAction(nameof(Index));

            }
        public async Task<IActionResult> Edite(int id)
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
            return View("MasterCreditNote");
            }
        [HttpPost]
        public async Task<IActionResult> Edit(CreditNoteViewModel model)
            {
         //   if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
          //  if (model.CustomerId is null) return BadRequest("Customer is required.");
          ///  if (model.WarehousesId is null) return BadRequest("Warehouse is required.");

            if (model.Id == 0)
                {
                await _CreditVoucher.insertInvoice(model);
                return RedirectToAction(nameof(Index));
                }
            else
                await _CreditVoucher.updateInvoice(model);
            return RedirectToAction(nameof(Index));
            }
        [HttpGet]
        public async Task<IActionResult> GetInvoiceDetails(int customerId)
            {
            var rows = await _context.TblSales
       .AsNoTracking()
       .Where(s => s.CustomerId == customerId)
       .Where(s => (s.Change ) > 0m)
       .Where(s => _context.TblTransactions.Any(t =>
           (t.Type == "Customer Opening Balance" || t.Type == "Sales Invoice") &&
           t.TransactionId == s.Id &&
           t.HumId == s.CustomerId))
       .OrderBy(s => s.Date)
       .Select(s => new
           {
           s.Id,
           s.Date,
           s.InvoiceId,
           s.Total,
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