
namespace YamyProject.Controllers.InventoryDoc
{

    public class StockSettlementController : Controller
    {
        private readonly YamyDbContext _context;
        private readonly IItemStockSettlementService _svc;
        private readonly IStockSettlementService _service;

        public StockSettlementController(IItemStockSettlementService svc, IStockSettlementService service, YamyDbContext context)
        {
            _svc = svc;
            _service = service;
            _context = context;
        }

        public async Task<IActionResult> Index(string selectionMethod = "Default", DateTime? from = null, DateTime? to = null)
        {

            var list = await _svc.GetSettlementsAsync(selectionMethod);
            return View(list); // a strongly-typed view for the default listing
        }
        [HttpGet]
        public async Task<IActionResult> List(DateTime? from, DateTime? to, string defaultMode = "Default")
        {
            var list = await _svc.GetSettlementsAsync(from, to, defaultMode);
            return View("Index", list);
        }
        [HttpGet]

        public async Task<IActionResult> Create()
        {
            var vm = new CreateUpdateSettlementViewmodel
            {
                WarehousesVm = await _service.GetWarehousesAsync()
            };
                      
            return View("SettlementVm", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUpdateSettlementViewmodel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.WarehousesVm = await _service.GetWarehousesAsync();
                return View(vm);
            }

            await _service.CreateAsync(vm, userId: 1); // Replace with actual user ID
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var vm = await _service.GetByIdAsync(id);
            vm.WarehousesVm = await _service.GetWarehousesAsync();
            return View("SettlementVm",vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateUpdateSettlementViewmodel vm)
        {
            if (!ModelState.IsValid)
            {
                vm.WarehousesVm = await _service.GetWarehousesAsync();
                return View(vm);
            }

            await _service.UpdateAsync(vm, userId: 1); // Replace with actual user ID
            return RedirectToAction("Index");
        }

        [HttpGet]

        public async Task<IActionResult> GetItems(string term,int warehouseId)
        {
            if (string.IsNullOrEmpty(term))
                return Json(new List<object>());

            var items = await _context.TblItems
                  .Where(i => (i.Code.Contains(term) || i.Name.Contains(term)) && i.WarehouseId == warehouseId)
                .Select(i => new 
                {
                    i.Id,
                    i.Code,
                    i.Method,
                    i.Type,
                    i.Name,
                    i.CostPrice,
                    i.WarehouseId,
                    i.OnHand,
                    // Calculate OnHand dynamically per warehouse (replace wId with actual warehouse id if needed)
                    qty = _context.TblItemTransactions
                              .Where(t => t.ItemId == i.Id  && t.WarehouseId == warehouseId)
                              .Sum(t => (t.QtyIn ?? 0) - (t.QtyOut ?? 0))
                })
                 .ToListAsync();

            return Json(items);
        }


    }
}
