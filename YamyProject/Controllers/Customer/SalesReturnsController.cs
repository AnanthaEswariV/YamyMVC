namespace YamyProject.Controllers.Customer
    {
    public class SalesReturnsController(ISalesReturnService salesService, IListServices listServices, YamyDbContext context) : Controller
        {

        private readonly ISalesReturnService _SalesReturnService = salesService;
        private readonly IListServices _ListServices= listServices;
        private readonly YamyDbContext _Context= context;
        public async Task<IActionResult> Index()
            {
            var Sales = await _SalesReturnService.GetSalesREturnAsync();
            var customers = await _ListServices.GetCustomersRetrunAsync();
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
            var customers = await _ListServices.GetCustomersRetrunAsync();
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
            return View("ReturnForm",
                new SalesReturnViewModel
                    {
                    Invoce = _SalesReturnService.GenerateReturnInvoiceNoAsync().GetAwaiter().GetResult(),
                    NextCode = _SalesReturnService.GenerateReturnInvoiceNoAsync().GetAwaiter().GetResult(),
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
        public async Task<IActionResult> Create(SalesReturnViewModel model)
            {
            //if (!ModelState.IsValid)
            //  return View("TaxInvoice", PopulateViewModel(model));
            if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
            if (model.CustomerId is null) return BadRequest("Customer is required.");
            if (model.WarehousesId is null) return BadRequest("Warehouse is required.");

          //  var userId = 1;

            await _SalesReturnService.CreateSalesReturnInvoiceAsync(model);
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
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
            {
            var vm = await _SalesReturnService.GetEditAsync(id);
            return View("ReturnForm", vm);
            }
        [HttpPost]
        public async Task<IActionResult> Edit(SalesReturnViewModel model)
            {
            if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
            if (model.CustomerId is null) return BadRequest("Customer is required.");
            if (model.WarehousesId is null) return BadRequest("Warehouse is required.");
            var userId = 1;

            if (model.Id == 0)
                {
                await _SalesReturnService.CreateSalesReturnInvoiceAsync(model);
                return RedirectToAction(nameof(Index));
                }
            else
                await _SalesReturnService.UpdateSalesReturnInvoiceAsync(model);
            return RedirectToAction(nameof(Index));
            }


        [HttpGet("sales-list-modal")]
        public async Task<IActionResult> SalesListModal(int customerId)
            {
            var rows = await _Context.TblSales
                .Where(s => s.CustomerId == customerId
                            && !_Context.TblSalesReturns.Any(r => r.SalesRefId == s.Id))
                .OrderByDescending(s => s.Date)
                .Select(s => new SalesListRowViewModel
                    {
                    Id = s.Id,
                    Inv = s.InvoiceId!,
                    Date = s.Date,
                    Amount = s.Net 
                    }).OrderBy(s=> s.Inv)
                .AsNoTracking()
                .ToListAsync();

            return PartialView("_SalesList", rows);
            }

        // replace BindItemsLoad(selectedSalesId)
        [HttpGet("sale-details")]
        public async Task<IActionResult> GetSaleDetails(int id)
            {
            var sale = await _Context.TblSales
                .Where(s => s.Id == id)
                .Select(s => new {
                    header = new
                        {
                        s.Id,
                        s.InvoiceId,
                        s.Date,
                        s.CustomerId,
                        CustomerCode = s.Customer.Code,
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
                    items = s.TblSalesDetails
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

            if (sale is null) return NotFound();
            return Json(sale); // your JS will fill the grid
            }
        }
 }
   