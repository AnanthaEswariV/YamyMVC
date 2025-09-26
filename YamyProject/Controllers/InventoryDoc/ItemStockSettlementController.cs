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
        [HttpGet("{id:int}")]
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
        [HttpGet("edit/{id:int}")]
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
                    Price = i.Price ?? 0,
                    NewOnHand = i.NewOnHand ?? 0,
                    MinusAmount = i.MinusAmount ?? 0,
                    PlusAmount = i.PlusAmount ?? 0
                }).ToList()
            };

            return View(vm);
        }

        // POST edit
        [HttpPost("edit/{id:int}")]
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
        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _svc.DeleteSettlementAsync(id, userId);
            return RedirectToAction(nameof(Index));
        }

        // API helper method equivalent of bindInvoiceItems() in your WinForms: gets items for selected settlement
        [HttpGet("{id:int}/items")]
        public async Task<IActionResult> GetItems(int id)
        {
            var vm = await _svc.GetSettlementDetailsAsync(id);
            if (vm == null) return NotFound();
            return Json(vm.Items);
        }
    }
}
