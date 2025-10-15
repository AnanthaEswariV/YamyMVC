namespace YamyProject.Controllers.InventoryDoc
{
    public class SalesProformasController : Controller
    {
        private readonly ISalesService _SalesService;
        private readonly IListServices _ListServices;
        private readonly YamyDbContext _Context;

        public SalesProformasController(ISalesService salesService, IListServices listServices, YamyDbContext context)
        {
            _SalesService = salesService;
            _ListServices = listServices;
            _Context = context;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}
