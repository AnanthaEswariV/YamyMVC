namespace YamyProject.Controllers.Vendors
    {
    public class PurchasesReturnController (IPurchaseReturnService PurchaseReturnService, IListServices listServices, YamyDbContext context): Controller
        {
           private readonly IPurchaseReturnService _PurchaseReturnService = PurchaseReturnService;
           private readonly IListServices _ListServices = listServices;
           private readonly YamyDbContext _Context = context;

           public async Task<IActionResult> Index()
            {
            var Purchase = await _PurchaseReturnService.GetPurchaseREturnAsync();
            var Vendors = await _ListServices.GetVendorsAsync();
            var VendorlectList = Vendors
                .Select(c => new SelectListItem
                    {
                    Value = c.Id.ToString(),
                    Text = $"{c.Code} - {c.Name}"
                    })
                .ToList();
            var Sale = new PurchasesCenterViewModel
                {
                Purchases = Purchase,
                Vendors = VendorlectList
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

            var Vendors = await _ListServices.GetVendorsRetrunAsync();
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
            return View("PurchasesReturn",
                new PurchaseInvoiceViewModel
                    {
                    Invoce = _PurchaseReturnService.GenerateReturnInvoiceNoAsync().GetAwaiter().GetResult(),
                    NextCode = _PurchaseReturnService.GenerateReturnInvoiceNoAsync().GetAwaiter().GetResult(),
                    Date = DateOnly.FromDateTime(DateTime.Today),
                    DueTo = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    ShipDate = DateOnly.FromDateTime(DateTime.Today),

                    Vendors = VendorslectList,
                    Warehouses = WarehouseSelectList,
                    Accounts = AccountList,
                    CostCenters = CostCenterList,
                    Vat = VatList
                    });
            }
        [HttpPost]
        public async Task<IActionResult> Create(PurchaseInvoiceViewModel model)
            {
            //if (!ModelState.IsValid)
            //  return View("TaxInvoice", PopulateViewModel(model));
            if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
            if (model.VendorId is null) return BadRequest("Customer is required.");
            if (model.WarehouseId is null) return BadRequest("Warehouse is required.");

            var userId = 1;

            //  await _PurchaseCreateService.CreateTaxInvoiceAsync(model, userId);
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
                   i.CostPrice,
                   i.Method,
                   i.Type,
                   i.SalesPrice,
                   i.WarehouseId
                   }).ToListAsync();
            return Json(result);
            }
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string formType = "")
            {
            var vm = await _PurchaseReturnService.GetEditAsync(id);
            return View("PurchasesReturn", vm);
            }
        [HttpPost]
        public async Task<IActionResult> Edit(PurchaseInvoiceViewModel model)
            {
            if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
            if (model.VendorId is null) return BadRequest("Customer is required.");
            if (model.WarehouseId is null) return BadRequest("Warehouse is required.");
            var userId = 1;

            if (model.Id == 0)
                {
                await _PurchaseReturnService.CreateTaxInvoiceAsync(model, userId);
                return RedirectToAction(nameof(Index));
                }
            else
                await _PurchaseReturnService.UpdateTaxInvoiceAsync(model, userId);
            return RedirectToAction(nameof(Index));
            }


        [HttpGet("Purchase_list_modal")]
        public async Task<IActionResult> PurchaseListModal(int VendorId)
            {
            var rows = await _Context.TblPurchases
                .Where(s => s.VendorId == VendorId
                            && !_Context.TblPurchaseReturns.Any(r => r.PurchaseRefId == s.Id))
                .OrderByDescending(s => s.Date)
                .Select(s => new SalesListRowViewModel
                    {
                    Id = s.Id,
                    Inv = s.InvoiceId!,
                    Date = s.Date,
                    Amount = s.Net
                    }).OrderBy(s => s.Inv)
                .AsNoTracking()
                .ToListAsync();

            return PartialView("_PurchaseList", rows);
            }

        // replace BindItemsLoad(selectedPurchaseId)
        [HttpGet("Purchase-details")]
        public async Task<IActionResult> GetSaleDetails(int id)
            {
            var Purchase = await _Context.TblPurchases
                .Where(s => s.Id == id)
                .Select(s => new {
                    header = new
                        {
                        s.Id,
                        s.InvoiceId,
                        s.Date,
                        s.VendorId,
                        CustomerCode = s.Vendors.Code,
                        s.BillTo,
                        s.City,
                        s.PaymentMethod,
                        s.ShipDate,
                        s.ShipTo,
                        s.ShipVia,
                        s.AccountCashId,
                        s.SalesMan,
                        s.Vat,
                        s.Total,
                        s.Net
                        },
                    items = s.PurchaseDetails
                        .OrderBy(d => d.Id)
                        .Select(d => new {
                            d.Id,
                            d.ItemId,
                            ItemCode = d.Items.Code,
                            ItemName = d.Items != null ? d.Items.Name : "",
                            d.Qty,
                            d.Price,
                            d.Discount,
                            d.Vat,
                            d.Vatp,
                            d.Total,
                            d.CostCenterId
                            })
                        .ToList()
                    })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (Purchase is null) return NotFound();
            return Json(Purchase); // your JS will fill the grid
            }
        }
    }
