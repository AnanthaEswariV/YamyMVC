using System;
using YamyProject.Controllers.InventoryDoc;

namespace YamyProject.Controllers.Vendors
{
    public class VendorsController : Controller
    {
        private readonly YamyDbContext _context;

        private readonly ILogger<MasterStockManagementController> _logger;

        public VendorsController(YamyDbContext context, ILogger<MasterStockManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<IActionResult> Index()
        { 
        
            // Base query: active vendor records only (state==0) and type Vendor
            var vendorsQuery = _context.TblVendors
                                  .AsNoTracking()
                                  .Where(v => v.State == 0 && v.Type == "Vendor");
            var items = await vendorsQuery
                .Select(v => new VendorIndexViewModel
                {
                    Id = v.Id,
                    Code = v.Code.ToString("D5"),
                    Name = v.Name,
                    CategoryName = _context.TblVendorCategories
                                      .Where(c => c.Id == v.CatId)
                                      .Select(c => c.Name)
                                      .FirstOrDefault(),
                    AccountName = _context.TblCoaLevel4s
                                      .Where(a => a.Id == v.AccountId)
                                      .Select(a => a.Name)
                                      .FirstOrDefault(),
                    Balance = v.Balance ?? 0m,
                    Date = v.Date.HasValue ? v.Date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                    // Opening balance: sum up all opening transactions created earlier.
                    OpeningDebit = _context.TblTransactions
                                      .Where(t => t.HumId == v.Id && t.Type == "Vendor Opening Balance")
                                      .Sum(t => (decimal?)t.Debit) ?? 0m,
                    OpeningCredit = _context.TblTransactions
                                      .Where(t => t.HumId == v.Id && t.Type == "Vendor Opening Balance")
                                      .Sum(t => (decimal?)t.Credit) ?? 0m,
                    Active = (v.Active == 0)
                })
                .ToListAsync();

        
            return View();
        }

        public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
        {
            var vendor = await _context.TblVendors
                                  .AsNoTracking()
                                  .Where(v => v.Id == id && v.State == 0)
                                  .FirstOrDefaultAsync(cancellationToken);

            if (vendor == null) return NotFound();

            var vm = new VendorEditViewModel
            {
                Id = vendor.Id,
                Code = vendor.Code.ToString("D5"),
                Name = vendor.Name,
                CatId = vendor.CatId,
                AccountId = vendor.AccountId,
                Trn = vendor.Trn,
                MainPhone = vendor.MainPhone,
                WorkPhone = vendor.WorkPhone,
                Mobile = vendor.Mobile,
                Email = vendor.Email,
                Ccemail = vendor.Ccemail,
                Website = vendor.Website,
                Country = vendor.Country,
                City = vendor.City,
                Region = vendor.Region,
                BuildingName = vendor.BuildingName,
                Date = vendor.Date.HasValue ? vendor.Date.Value.ToDateTime(TimeOnly.MinValue) : (DateTime?)null,
                OpeningDebit = await _context.TblTransactions.Where(t => t.HumId == id && t.Type == "Vendor Opening Balance").SumAsync(t => (decimal?)t.Debit) ?? 0m,
                OpeningCredit = await _context.TblTransactions.Where(t => t.HumId == id && t.Type == "Vendor Opening Balance").SumAsync(t => (decimal?)t.Credit) ?? 0m,
                Active = vendor.Active == 0,
                CategorySelectList = await _context.TblVendorCategories.AsNoTracking().Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToListAsync(cancellationToken),
                AccountSelectList = await _context.TblCoaLevel4s.AsNoTracking().Select(a => new SelectListItem(a.Name, a.Id.ToString())).ToListAsync(cancellationToken)
            };

            return View(vm);
        }

    }
}



