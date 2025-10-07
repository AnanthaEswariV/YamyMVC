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
           var categories = await _ListServices.GetCustomersAsync();

           var  categorySelectList = categories.Select(c => new SelectListItem
            {
                Text = c.Name.ToString(), // Adjust according to your property names
                Value = c.Id.ToString() // Adjust according to your property names
            });
            var Sales = await _SalesService.GetSalesReportAsync();
            Sales.Cast<SalesCenterViewModel>().ToList().ForEach(s =>
            {
                s.Customers = categorySelectList;
            });
            var customers = await _ListServices.GetCustomersAsync();
            // Map to SelectListItem
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
    }
}