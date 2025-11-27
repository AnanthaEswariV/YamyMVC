namespace YamyProject.Controllers.Reports.Corporate
    {
    public class CorporateController (IVatCorporateService  corporateService) : Controller
        {
        private readonly  IVatCorporateService  _CorporateService = corporateService;
        public async Task<IActionResult> Index(DateOnly from=default, DateOnly to=default)
            {
            
            from = new DateOnly(DateTime.Today.Year, 1, 1);
            to = new DateOnly(DateTime.Today.Year, 12, 31);

            var model = await _CorporateService.GetVatCorporateAsync(from, to);
            return View(model);
         //return View();
            }
        }
    }