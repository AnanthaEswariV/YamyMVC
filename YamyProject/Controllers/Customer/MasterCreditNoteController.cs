using YamyProject.Services;

namespace YamyProject.Controllers.Customer
{
    public class MasterCreditNoteController : Controller
    {
        private readonly ISalesCenterService _SalesService;
        private readonly IListServices _ListServices;

        public MasterCreditNoteController(ISalesCenterService salesService, IListServices listServices)
        {
            _SalesService = salesService;
            _ListServices = listServices;
        }
        public async Task<IActionResult> Index()
        {
            var Sales = await _SalesService.GetSalesReportAsync();
            var customers = await _ListServices.GetCustomersAsync();    
            var customerSelectList = customers
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Code} - {c.Name}"
                })
                .ToList();

              ViewBag.Customers = customerSelectList;
            return View(Sales);
        }
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var Warehouse = await _ListServices.GetCustomersAsync();
            var WarehouseSelectList = Warehouse
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name?.ToString()
                }).ToList();

            var customers = await _ListServices.GetCustomersAsync();
            var customerSelectList = customers
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text =  c.Name?.ToString()
                })
                .ToList();
            ViewBag.Customers = customerSelectList;
            ViewBag.Warehouse = WarehouseSelectList;

            return View("TaxInvoice");
        }

    }
}