namespace YamyProject.Controllers.Customer
{
    public class MasterCreditNoteController : Controller
    {
        private readonly ISalesCenterService _SalesService;
        private readonly IListServices _ListServices;
        private readonly ISalesCreateService _SalesCreateService;
        private readonly YamyDbContext _Context;

        public MasterCreditNoteController(ISalesCenterService salesService, IListServices listServices, YamyDbContext context, ISalesCreateService salesCreateService)
        {
            _SalesService = salesService;
            _ListServices = listServices;
            _Context = context;
            _SalesCreateService = salesCreateService;
        }
        public async Task<IActionResult> Index()
        {
            var Sales = await _SalesService.GetSalesAsync();
            var customers = await _ListServices.GetCustomersAsync();    
            var customerSelectList = customers
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Code} - {c.Name}"
                })
                .ToList();
            var Sale=new SalesCenterPageViewModel
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
            var WarehouseSelectList = Warehouse
                .Select(c => new TblWarehouse
                {
                    Id = c.Id ,            
                    Name = c.Name
                }).ToList();

            var customers = await _ListServices.GetCustomersAsync();
            var customerSelectList = customers
                .Select(c => new TblCustomer
                {

                    Id = c.Id,
                    Code=c.Code,
                    Name =  c.Name
                })
                .ToList();
            var Account = await _ListServices.GetAccountsAsync();
            var AccountList = Account
                .Select(c => new TblCoaLevel4
                {
                    Id = c.Id,
                    Name =  c.Name
                })
                .ToList();
            var Vat = await _ListServices.GetVatAsync();
            var VatList = Vat
                .Select(c => new TblTax
                {
                    Value = c.Value,
                    Name = c.Name
                }).OrderBy(c=>c.Value)
                .ToList();
            var CostCenter = await _ListServices.GetCostCenterAsync();
            var CostCenterList = CostCenter
                .Select(c => new TblCostCenter
                {
                    Id = c.Id,
                    Code = c.Code
                })
                .ToList();
            return View("TaxInvoice", 
                new TaxInvoiceViewModel{
                    Invoce= _SalesService.GenerateInvoiceNoAsync().GetAwaiter().GetResult(),
                    NextCode= _SalesService.GenerateInvoiceNoAsync().GetAwaiter().GetResult(),
                    Date =DateOnly.FromDateTime(DateTime.Today),
                    DueTo=DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    ShipDate=DateOnly.FromDateTime(DateTime.Today),
                   
                    Customers = customerSelectList,
                    Warehouses= WarehouseSelectList,
                    Accounts= AccountList,
                    CostCenter= CostCenterList,
                    Vat=VatList
                });
        }


        [HttpPost]
        public async Task<IActionResult> Create(TaxInvoiceViewModel model)
        {
            //if (!ModelState.IsValid)
            //  return View("TaxInvoice", PopulateViewModel(model));
            if (model.Items == null || model.Items.Count == 0) return BadRequest("At least one item is required.");
            if (model.CustomerId is null) return BadRequest("Customer is required.");
            if (model.WarehousesId is null) return BadRequest("Warehouse is required.");

            var userId = 1;

            var saleId = await _SalesCreateService.CreateTaxInvoiceAsync(model, userId);
            return RedirectToAction(nameof(Index), new { id = saleId });

            //var invoiceNo = _SalesService.GenerateInvoiceNoAsync().GetAwaiter().GetResult(); // or enforce provided no.

            //  if (string.IsNullOrWhiteSpace(model.Invoce)) model.Invoce = invoiceNo; // or enforce provided no.
            // var InvoiceCode = _SalesService.GenerateInvoiceNoAsync();
            //var sale = new TblSale
            //{
            //    Date = model.Date == default ? DateOnly.FromDateTime(DateTime.Today) : model.Date,
            //    CustomerId = model.CustomerId ?? 0,             
            //    InvoiceId = string.IsNullOrWhiteSpace(model.Invoce) ? invoiceNo : model.Invoce.Trim(),
            //    WarehouseId = model.WarehousesId ?? 0,
            //    PoNum = model.PONO ?? string.Empty,
            //    BillTo = model.PayTo ?? string.Empty,
            //    City = model.Emirate ?? string.Empty,
            //    SalesMan = model.SalesMane ?? string.Empty,
            //    ShipDate = model.ShipDate == default ? null : model.ShipDate,
            //    ShipVia = model.Val,                    // adjust to your meaning
            //    ShipTo = model.Ship ?? string.Empty,
            //    PaymentMethod = model.PaymentMethod ?? string.Empty,
            //    AccountCashId = model.AccountsId ?? 0,        // cash/AR account
            //    PaymentTerms = model.PaymentTerms ?? string.Empty,
            //    PaymentDate = model.DueTo == default ? (model.Date == default ? DateOnly.FromDateTime(DateTime.Today) : model.Date) : model.DueTo,
            //    Total = model.TotalBeforeVat,
            //    Vat = model.TotalVat,
            //    Net = model.NetAmount,
            //    Pay = 0,
            //    Change = 0,
            //    CreatedBy = 0,                         // TODO: set from signed-in user
            //    CreatedDate = DateOnly.FromDateTime(DateTime.Today),
            //    State = 1,
            // };


            // _Context.TblSales.Add(sale);
            // await _Context.SaveChangesAsync(); // get sale.Id



           // return RedirectToAction("Index");

        }



        //[HttpGet("create")]
        //public async Task<IActionResult> Create()
        //{

        //    var model = await _svc.GetCreateUpdateSettlementVmAsync();

        //    return View("StockSettelment", model);
        //}

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
        private TaxInvoiceViewModel PopulateViewModel(TaxInvoiceViewModel? model = null)
        {
            TaxInvoiceViewModel viewModel = model is null ? new TaxInvoiceViewModel() : model;

            //var authors = _Context.Authors.Where(a => !a.IsDeleted).OrderBy(a => a.Name).ToList();
            //var categories = _Context.Categories.Where(a => !a.IsDeleted).OrderBy(a => a.Name).ToList();

            //viewModel.Authors = _mapper.Map<IEnumerable<SelectListItem>>(authors);
            //viewModel.Categories = _mapper.Map<IEnumerable<SelectListItem>>(categories);

            return viewModel;
        }
    }
}