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
            return View("MasterCreditNote", new CreditNoteViewModel
                {
                Customers=customerSelectList,
                DebitAccounts = AccountList,
                });
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