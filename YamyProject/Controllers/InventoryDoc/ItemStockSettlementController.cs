using System.Security.Claims;
namespace YamyProject.Controllers.InventoryDoc
{
    public class ItemStockSettlementController : Controller
    {
        private readonly IItemStockSettlementService _svc;
        private readonly IMicroserviceClientt _micro;
        private readonly ILogger<ItemStockSettlementController> _logger;
        public ItemStockSettlementController(IItemStockSettlementService svc, IMicroserviceClientt micro, ILogger<ItemStockSettlementController> logger)
        {
            _svc = svc;
            _micro = micro;
            _logger = logger;
        }
        // GET: /item-stock-settlements
        public async Task<IActionResult> Index(string selectionMethod = "Default",DateTime? from = null, DateTime? to = null)
        {
           
            var list = await _svc.GetSettlementsAsync( selectionMethod);
                return View( list); // a strongly-typed view for the default listing
        }
        // API: /item-stock-settlements/list
        [HttpGet("list")]
        public async Task<IActionResult> List(DateTime? from, DateTime? to, string defaultMode )
        {
            var list = await _svc.GetSettlementsAsync(from, to, defaultMode);
            return View("Index",list);
        }
        // API: /item-stock-settlements/{id}
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var vm = await _svc.GetSettlementDetailsAsync(id);
            if (vm == null) return NotFound();
            return Json(vm);
        }
        // GET create (view)
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {

            var model = await _svc.GetCreateUpdateSettlementVmAsync();

            return View("StockSettelment", model);
        }
        // POST create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUpdateSettlementVm vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)); // or your own user id provider
            var id = await _svc.CreateSettlementAsync(vm, userId);

            // call microservice to notify external system (non-blocking)
            try
            {
                await _micro.NotifySettlementCreatedAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Microservice notification failed for settlement {Id}", id);
                // don't fail the request — log for later retries
            }

            return RedirectToAction(nameof(Index));
        }
        // GET edit
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var data = await _svc.GetSettlementDetailsAsync(id);
            if (data == null) return NotFound();

            // map to CreateUpdateSettlementVm for editing (manual map shown)
            var vm = new CreateUpdateSettlementVm
            {
                Id = data.Id,
                Code = data.Code ?? "",
                Date = DateOnly.FromDateTime(data.Date ?? DateTime.UtcNow),
                WarehouseId = data.WarehouseId,
                Items = data.Items.Select(i => new CreateUpdateSettlementItemVm
                {
                    Id = i.Id,
                    ItemId = i.ItemId,
                    OnHand = i.OnHand ?? 0,
                    
                    ItemName=i.ItemName,
                    Price = i.Price ?? 0,
                    NewOnHand = i.NewOnHand ?? 0,
                    MinusAmount = i.MinusAmount ?? 0,
                    PlusAmount = i.PlusAmount ?? 0
                }).ToList()
            };
            var list = await _svc.GetCreateUpdateSettlementVmAsync();
              //var warehousesEntity = await _db.TblWarehouses
            //.AsNoTracking()
            //.ToListAsync();

            //var warehousesVm = warehousesEntity
            //    .Select(w => new WarehouseViewModel
            //        {
            //        Id = w.Id,
            //        Name = w.Code + " - " + w.Name
            //        })
            //    .ToList();
            vm.warehouse = list.warehouse;
            vm.WarehousesVm = list.WarehousesVm;
            return View("StockSettelment", vm);
        }
        // POST edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateUpdateSettlementVm vm)
        {
            if (id != vm.Id) return BadRequest();
            if (!ModelState.IsValid) return View(vm);

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _svc.UpdateSettlementAsync(id, vm, userId);
            return RedirectToAction(nameof(Index));
        }
        // POST delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _svc.DeleteSettlementAsync(id, userId);
            return RedirectToAction(nameof(Index));
        }
        // API helper method equivalent of bindInvoiceItems() in your WinForms: gets items for selected settlement
        [HttpGet]
        public async Task<IActionResult> GetItems(int id)
        {
            var vm = await _svc.GetSettlementDetailsAsync(id);
            if (vm == null) return NotFound();
            return Json(vm.Items);
        }
        [HttpGet]
        public async Task<IActionResult> SearchByCode(string term, int warehouseId)
        {
            var items = await _svc.GetItemsByCodeAsync(term ?? "", warehouseId);
            return Json(items.Select(i => new { i.Code, i.Name }));
        }
        [HttpGet]
        public async Task<IActionResult> SearchByName(string term, int warehouseId)
        {
            var items = await _svc.GetItemsByNameAsync(term ?? "", warehouseId);
            return Json(items.Select(i => new { i.Code, i.Name }));
        }
        [HttpGet]
        public async Task<IActionResult> GetItems(string term, int warehouseId)
        {
            var items = await _svc.GetItemsAsync(term ?? "", warehouseId);
            return Json(items.Select(i => new { i.Code, i.Name }));
        }
        [HttpGet]
        public async Task<IActionResult> GetSettlementItems(int settleId)
        {
            if (settleId <= 0)
                return BadRequest("Invalid settlement ID");

            var items = await _svc.GetSettlementItemsAsync(settleId);

            // Optionally, project to a view model to send only necessary fields
            var result = items.Select(i => new
            {
                i.Id,
                ItemName =  $"{i.Code} - {i.Name}" ,
                i.Quantity ,
                i.CostPrice ,
                i.NewOnHand 
            });

            return Json(result);
        }
    }
}
