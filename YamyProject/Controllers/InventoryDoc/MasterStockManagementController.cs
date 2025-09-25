namespace YamyProject.Controllers.InventoryDoc
{
    public class MasterStockManagementController : Controller
    {

        private readonly YamyDbContext _context;

        private readonly ILogger<MasterStockManagementController> _logger;

        private const string CategoryInventory = "Inventory";
        private const string CategoryStockSettlement = "Stock Settlement";

        public MasterStockManagementController(YamyDbContext context, ILogger<MasterStockManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IActionResult> Index(string selectionMethod = "Default", bool chkDate = false,
                                               DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            try
            {
                var settlementsQuery = _context.TblItemStockSettlements
                                               .AsNoTracking()
                                               .Where(s => s.State == 0);

                // apply date filter only if checkbox is checked and both dates provided
                if (chkDate && dateFrom.HasValue && dateTo.HasValue)
                {
                    var from = DateOnly.FromDateTime(dateFrom.Value.Date);
                    var to = DateOnly.FromDateTime(dateTo.Value.Date);
                    settlementsQuery = settlementsQuery.Where(s => s.Date >= from && s.Date <= to);
                }

                if (selectionMethod == "Default")
                {
                    // Summary per settlement (equivalent to your Default SQL)
                    var list = await settlementsQuery
                        .OrderBy(s => s.Date)
                        .Select(s => new ItemStockSettlementIndexViewModel
                        {
                            Id = s.Id,
                            Date = s.Date.HasValue ? s.Date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                            InvNo = EF.Functions.Collate(s.Code, "utf8mb4_general_ci"),
                            JvNo ="3" 
                            //_context.TblTransactions
                            //                .Where(t => t.TransactionId = s.Id)
                            //                .ToList()
                            //               .OrderByDescending(t => t.Id)
                            //               .Select(t => t.TransactionId)
                            //               .FirstOrDefault()
                        })
                        .ToListAsync();

                    // add row numbers and apply the "000" prefix like your original concat('000', ...)
                    for (var i = 0; i < list.Count; i++)
                    {
                        list[i].SN = i + 1;
                        if (!string.IsNullOrEmpty(list[i].JvNo))
                            list[i].JvNo = $"000{list[i].JvNo}";
                    }

                    var vm = new ItemStockSettlementListViewModel
                    {
                        SelectionMethod = selectionMethod,
                        Settlements = list
                    };

                    return View(vm); // Views/MasterStockManagement/Index.cshtml
                }
                else
                {
                    // Details mode: join settlement details with items (equivalent to your other SQL)
                    var details = await (from s in settlementsQuery
                                         join d in _context.TblItemStockSettlementDetails on s.Id equals d.SettleId
                                         join it in _context.TblItems on d.ItemId equals it.Id
                                         select new ItemStockSettlementDetailViewModel
                                         {
                                             SettlementId = s.Id,
                                             Date = s.Date.HasValue ? s.Date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                                             InvNo = s.Code,
                                             ItemName = it.Code + " - " + it.Name,
                                             Qty = d.OnHand,
                                             CostPrice = d.Price,
                                             NewOnHand = d.NewOnHand,
                                             MinusAmount = d.Minusamount,
                                             PlusAmount = d.Plusamount
                                         })
                                         .OrderBy(d => d.Date)
                                         .ToListAsync();

                    //for (var i = 0; i < details.Count; i++) details[i].SN = i + 1;

                    //var vm = new ItemStockSettlementDetailsListViewModel
                    //{
                    //    SelectionMethod = selectionMethod,
                    //    Details = details
                    //};

                    // You can either return the same Index view and render different sections
                    // or use a dedicated view name like "IndexDetails" — I return Index (view can branch)
                    return View("Index", details);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load item stock settlements");
                // return friendly error, do not leak internal exception
                return StatusCode(500, "An error occurred while loading settlements.");
            } 
        }
        // GET: /MasterStockManagement/FetchInvoiceItems/5
        // This corresponds to your bindInvoiceItems() helper (returns JSON for AJAX or PartialView)
        [HttpGet]
        public async Task<IActionResult> FetchInvoiceItems(int id)
        {
            var items = await _context.TblItemStockSettlementDetails
                                      .AsNoTracking()
                                      .Where(d => d.SettleId == id)
                                      .Join(_context.TblItems,
                                            d => d.ItemId,
                                            it => it.Id,
                                            (d, it) => new ItemStockSettlementDetailViewModel
                                            {
                                                ItemName = it.Code + " - " + it.Name,
                                                Qty = d.OnHand,
                                                CostPrice = d.Price,
                                                NewOnHand = d.NewOnHand
                                            })
                                      .ToListAsync();

            return Json(items); // or return PartialView("_InvoiceItems", items);
        }
        [HttpGet]
        public async Task<IActionResult> create(CancellationToken cancellationToken = default)
        {
             var settlement = new StockSettlementIndexViewModel();

            // Fetch both configs in a single round-trip (best practice)
            var categories = new[] { CategoryInventory, CategoryStockSettlement };

            var configs = await _context.Set<TblCoaConfig>()
                                        .Where(c => categories.Contains(c.Category))
                                        .ToListAsync(cancellationToken);

            var inv = configs.FirstOrDefault(c => c.Category == CategoryInventory);
            var stock = configs.FirstOrDefault(c => c.Category == CategoryStockSettlement);

            if (inv?.AccountId == null)
                settlement.Errors.Add("Inventory Account must be set first.");

            if (stock?.AccountId == null)
                settlement.Errors.Add("Stock Settlement Account must be set first.");

            // If there are configuration errors, return the same Index view and show errors there.
            // (Alternative: redirect to a config page or show a dedicated view.)
            if (settlement.Errors.Any())
            {
                _logger.LogWarning("Missing COA config(s): {Errors}", string.Join(", ", settlement.Errors));
                return   PartialView("_StockSettlement",settlement);

            }

            // Map values into the view model
            settlement.InventoryAccountId = inv!.AccountId!.Value;
            settlement.StockSettlementAccountId = stock!.AccountId!.Value;

            // Page UI state (equivalent to disabling 'newQty' cell background in WinForms)
            settlement.NewQtyReadOnly = true;
            settlement.NewQtyCssClass = "bg-light-gray"; // used by the Razor view to apply styles

            // Items would be populated here if you have items to show
            settlement.Items = new System.Collections.Generic.List<StockSettlementItemViewModel>();

            return PartialView("_StockSettlement",settlement);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create()
        {

            return Ok(200);

        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit( )
        {
            return Ok();
        }
        [HttpDelete]
        public IActionResult Delete(int id)
        {
            var isDeleted = "";//_gamesService.Delete(id);

            return Ok(isDeleted);
            // return isDeleted ? Ok() : BadRequest();
        }
    }
}
