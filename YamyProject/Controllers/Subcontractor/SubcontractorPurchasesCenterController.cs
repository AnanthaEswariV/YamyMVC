namespace YamyProject.Controllers.Subcontractor
    {
    public class SubcontractorPurchasesCenterController(IVendorsCenterService PurchaseService, IListServices listServices, YamyDbContext context) : Controller
        {
        private readonly IVendorsCenterService _PurchaseService = PurchaseService;
        private readonly IListServices _ListServices = listServices;
        private readonly YamyDbContext _Context = context;
        public async Task<IActionResult> Index()
            {
            var Purchase = await _PurchaseService.GetPurchaseAsync(VendorType:"Subcontractor");
            var Vendors = await _ListServices.GetVendorSubcontractorsAsync();
            var VendorSelectList = Vendors
                .Select(c => new SelectListItem
                    {
                    Value = c.Id.ToString(),
                    Text = $"{c.Code} - {c.Name}"
                    })
                .ToList();
            var Sale = new PurchasesCenterViewModel
                {
                VendorType = "Subcontractor",
                Purchases = Purchase,
                Vendors = VendorSelectList
                };
            return View("~/Views/PurchasesCenter/Index.cshtml", Sale);
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
               var Vendors = await _ListServices.GetVendorSubcontractorsAsync();
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
               return View("~/Views/PurchasesCenter/PurchasesInvoice.cshtml",
                   new PurchaseInvoiceViewModel
                       {
                       VendorType= "Subcontractor",
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
        public async Task<IActionResult> Edit(int id, string formType = "")
            {
            var vm = await _PurchaseService.GetEditAsync(id, formType);
            return View("~/Views/PurchasesCenter/PurchasesInvoice.cshtml", vm);
            }
        }
}