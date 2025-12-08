namespace YamyProject.Controllers.Customer
        {
        public class CustmerSalesProformasController(ISalesProformaCenterService SalesProforma, IListServices listServices, YamyDbContext context) : Controller
            {
            private readonly ISalesProformaCenterService _SalesProforma = SalesProforma;
            private readonly IListServices _ListServices = listServices;
            private readonly YamyDbContext _Context = context;

            public async Task<IActionResult> Index()
                {
                var Sales = await _SalesProforma.GetSalesAsync();
                var customers = await _ListServices.GetCustomersAsync();
                var customerSelectList = customers
                   .Select(c => new SelectListItem
                       {
                       Value = c.Id.ToString(),
                       Text = $"{c.Code} - {c.Name}"
                       })
                    .ToList();
                var Sale = new SalesCenterPageViewModel
                    {
                    Sales = Sales,
                    Customers = customerSelectList
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
                var customers = await _ListServices.GetCustomersAsync();
                var customerSelectList = customers.Select(c => new TblCustomer
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
                return View("SalesProforma",
                    new TaxInvoiceViewModel
                        {
                        Invoce = _SalesProforma.GenerateInvoiceNoAsync().GetAwaiter().GetResult(),
                        NextCode = _SalesProforma.GenerateInvoiceNoAsync().GetAwaiter().GetResult(),
                        Date = DateOnly.FromDateTime(DateTime.Today),
                        DueTo = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                        ShipDate = DateOnly.FromDateTime(DateTime.Today),
                        Customers = customerSelectList,
                        Warehouses = WarehouseSelectList,
                        Accounts = AccountList,
                        CostCenter = CostCenterList,
                        Vat = VatList
                        });
                }
            [HttpPost]
            public async Task<IActionResult> Create(TaxInvoiceViewModel model)
                {
                if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
                if (model.CustomerId is null) return BadRequest("Customer is required.");
                if (model.WarehousesId is null) return BadRequest("Warehouse is required.");

                var userId = 1;

                await _SalesProforma.CreateProformaCenterAsync(model);
                return RedirectToAction(nameof(Index));
                }

            [HttpGet]
            public async Task<IActionResult> Edit(int id)
                {
                var vm = await _SalesProforma.GetEditAsync(id);
                return View("SalesProforma", vm);
                }
            [HttpPost]
            public async Task<IActionResult> Edit(TaxInvoiceViewModel model)
                {
                //if (!ModelState.IsValid)
                //  return View("TaxInvoice", PopulateViewModel(model));
                if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
                if (model.CustomerId is null) return BadRequest("Customer is required.");
                if (model.WarehousesId is null) return BadRequest("Warehouse is required.");
                var userId = 1;
                await _SalesProforma.UpdateProformaInvoiceAsync(model);
                return RedirectToAction(nameof(Index));
                }        

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
        public async Task<IActionResult> GetItemsDetels(int Id)
            {
            var result = await _Context.TblSalesProformaDetails
             .Where(t => t.SalesId == Id)
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
