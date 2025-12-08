namespace YamyProject.Controllers.Vendors
    {
    public class PurchasesCenterController(IVendorsCenterService PurchaseService, IListServices listServices, YamyDbContext context) : Controller
        {
        private readonly IVendorsCenterService _PurchaseService = PurchaseService;
        private readonly IListServices _ListServices = listServices;
         private readonly YamyDbContext _Context = context;
        public async Task<IActionResult> Index()
            {
            var Purchase = await _PurchaseService.GetPurchaseAsync(VendorType:"Vendor");
            var Vendors = await _ListServices.GetVendorsAsync();
            var VendorSelectList = Vendors
                .Select(c => new SelectListItem
                    {
                    Value = c.Id.ToString(),
                    Text = $"{c.Code} - {c.Name}"
                    })
                .ToList();
            var Sale = new PurchasesCenterViewModel
                {
                Purchases = Purchase,
                Vendors = VendorSelectList
                };
            return View(Sale);
            }
        [HttpGet]
        public async Task<IActionResult> Create()
            {
            var Warehouse = await _ListServices.GetWarehousesAsync();
            var WarehouseSelectList = Warehouse.Select(c => new TblWarehouse
                {
                Id = c.Id,
                Name = c.Name
                }).ToList();
            var Vendors = await _ListServices.GetVendorsAsync();
            var VendorslectList = Vendors.Select(c => new TblVendor
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
            var Vat = await _ListServices.GetVatAsync();
            var VatList = Vat.Select(c => new SelectListItem
                {
                Value = c.Value.ToString(),
                Text = c.Name + c.Value
                }).OrderBy(c => c.Value).ToList();
            var CostCenter = await _ListServices.GetCostCenterAsync();
            var CostCenterList = CostCenter.Select(c => new TblCostCenter
                {
                Id = c.Id,
                Code = c.Code
                }).ToList();
            var FixedAsset = await _ListServices.GetFixedAssetAsync();
            var FixedAssetList = FixedAsset.Select(c => new TblFixedAssetsCategory
                {
                Id = c.Id,
                CategoryName = c.CategoryName
                }).ToList();

            return View("PurchasesInvoice",
                new PurchaseInvoiceViewModel
                    {
                    Invoce = _PurchaseService.GenerateVendorsInvoiceNoAsync().GetAwaiter().GetResult(),
                    NextCode = _PurchaseService.GenerateVendorsInvoiceNoAsync().GetAwaiter().GetResult(),
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    DueTo = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    ShipDate = DateOnly.FromDateTime(DateTime.Today),
                    Vendors = VendorslectList,
                    Warehouses = WarehouseSelectList,
                    Accounts = AccountList,
                    CostCenters = CostCenterList,
                    FixedAssets = FixedAssetList,

                    Vat = VatList
                    });
            }
        [HttpPost]
        public async Task<IActionResult> Create(PurchaseInvoiceViewModel model)
            {
            if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
            if (model.VendorId is null) return BadRequest("Vendor is required.");
            if (model.WarehouseId is null) return BadRequest("Warehouse is required.");
            var userId = 1;

            await _PurchaseService.CreateTaxInvoiceAsync(model);
            return RedirectToAction(nameof(Index));
            }
            [HttpGet]
        public async Task<IActionResult> GetItems(string term, int warehouseId)
            {
            if (string.IsNullOrEmpty(term))
                return Json(new List<object>());
            var result = await _Context.TblItems
                .Where(i => (i.Code.Contains(term) || i.Name.Contains(term)) && i.WarehouseId == warehouseId)
               .Select(i => new
                   {
                   i.Id,
                   i.Code,
                   i.Name,
                   i.SalesPrice,
                   i.Method,
                   i.Type,
                   i.CostPrice,
                   i.WarehouseId
                   }).ToListAsync();
            return Json(result);
            }

        public async Task<IActionResult> Edit(int id, string formType = "")
            {
            var vm = await _PurchaseService.GetEditAsync(id, formType);
            return View("PurchasesInvoice", vm);
            }
        [HttpPost]
        public async Task<IActionResult> Edit(PurchaseInvoiceViewModel model)
            {
            //if (!ModelState.IsValid)
            //  return View("TaxInvoice", PopulateViewModel(model));
            if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
            if (model.VendorId is null) return BadRequest("Vendor is required.");
            if (model.WarehouseId is null) return BadRequest("Warehouse is required.");
            var userId = 1;

            if (model.Id == 0)
                {
                await _PurchaseService.CreateTaxInvoiceAsync(model);
                return RedirectToAction(nameof(Index));
                }
            //else
                await _PurchaseService.UpdateTaxInvoiceAsync(model);
            return RedirectToAction(nameof(Index));
            }

        public async Task<IActionResult> GetItemsDetels(int Id)
            {
           
            var result = await _Context.TblPurchaseDetails
                .Where(t => t.PurchaseId == Id)
                .Include(t => t.Items)
               .Select(i => new
                   {
                   i.Items,
                   i.Qty,
                   i.Price,
                   i.Vat,
                   i.Total

                   }).ToListAsync();
            return Json(result);
            }

        }
    }