using Mysqlx;

namespace YamyProject.Controllers.GeneralJournalVoucher
    {
    public class GeneralJournalVouchersController(IGeneralJournalVouchersService JournalVouchersService, IListServices listServices) : Controller
        {
        private readonly IGeneralJournalVouchersService _JournalVouchersService = JournalVouchersService;
        private readonly IListServices _ListServices = listServices;
        public async Task<IActionResult> Index()
            {
            var journalVouchers = await _JournalVouchersService.GetCustmerData();
            return View(journalVouchers);
            }
        [HttpGet]//{""}]
        public async Task<IActionResult> GetDetails(int Id)
            {
            var Rows = await _JournalVouchersService.GetJVDetails(Id);
            return Json(Rows);
            }
        [HttpGet]
        public async Task<IActionResult> Create()
            {
            var Code = await _JournalVouchersService.GenerateNextReceiptCode();
            var Id = (int)await _JournalVouchersService.GenerateNextReceiptId();
            var Account = await _ListServices.GetAccountsAsync();
            var AccountList = Account.Select(c => new TblCoaLevel4
                {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code
                }).ToList();

            return View("JournalVouchers", new JournalVoucherViewModel
                {
                Accounts = AccountList,
                JournalCode = Code,
                Journal = Id,
                Date=DateOnly.FromDateTime(DateTime.Now)
                });
            }
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
            {
            var Account = await _ListServices.GetAccountsAsync();
            var AccountList = Account.Select(c => new TblCoaLevel4
                {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code
                }).ToList();
            var model = await _JournalVouchersService.GEtJournalVoucher(id);

            return View("JournalVouchers", new JournalVoucherViewModel
                {
                Accounts = AccountList,
               // JournalCode = Code
                });
                }
        [HttpPost]
        public async Task<IActionResult> Create(JournalVoucherViewModel model)
            {
            //if (ModelState.IsValid)
            //    {
              ////  var result =
                 await _JournalVouchersService.CreateJournalVoucher(model);
              //  if (result.IsSuccess)

                    return RedirectToAction("Index");

              //  ModelState.AddModelError(string.Empty, result.ErrorMessage);
                //}
            }

        [HttpPost]
        public async Task<IActionResult> Edit(JournalVoucherViewModel? model)
            {
            //if (ModelState.IsValid)
            //    {
                //  var result = 
                  await _JournalVouchersService.UpdateJournalVoucher(model);
                // if (result.IsSuccess)

                return RedirectToAction("Index");

                //  ModelState.AddModelError(string.Empty, result.ErrorMessage);
                //}
            }
        [HttpGet("/GeneralJournalVouchers/GetpartnerListAsync")]
        public async Task<IActionResult> GetpartnerListAsync(string Name)
            {
            var partnerListlist = await _JournalVouchersService.GetPartnersByAccountNameAsync(Name);

            if (partnerListlist == null)
                return Json(Array.Empty<object>());

            return Json(partnerListlist);
            }
        }
}