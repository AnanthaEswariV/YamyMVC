namespace YamyProject.Controllers.Setting
 {
    public class MasterSettingsController (YamyDbContext context): Controller
        {
        private readonly YamyDbContext _context=context;
        public IActionResult Index()
            {
            return View();
            }
        //{used view model
        //DefaultAccountViewModel
        //SettingsMasterViewmodel
      
        /// <returns></returns>
        //}
        // Controllers/SettingsController.cs

        private async Task<IEnumerable<SelectListItem>> BuildAccountsAsync()
        {
        var list = await _context.TblCoaLevel4s
            .OrderBy(a => a.Code)
            .Select(a => new SelectListItem { Value = a.Id.ToString(), Text = a.Code + " - " + a.Name })
            .ToListAsync();
        list.Insert(0, new SelectListItem { Value = "", Text = "" });
        return list;
        }

    private async Task<IEnumerable<SelectListItem>> BuildTaxesAsync()
        {
        var list = await _context.TblTaxes
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
            .ToListAsync();
        list.Insert(0, new SelectListItem { Value = "", Text = "" });
        return list;
        }

    private async Task<IEnumerable<SelectListItem>> BuildUsersAsync()
        {
        return await _context.TblSecUsers
            .OrderBy(u => u.UserName)
            .Select(u => new SelectListItem { Value = u.Id.ToString(), Text = u.UserName })
            .ToListAsync();
        }

    [HttpGet]
    //public async Task<IActionResult> Master(string primary = "default", string sub = "common")
    //    {
    //    //var vm = new SettingsMasterVm
    //    //    {
    //    //    ActivePrimary = primary,
    //    //    ActiveSub = sub,
    //    //    Accounts = await BuildAccountsAsync(),
    //    //    Taxes = await BuildTaxesAsync(),
    //    //    Users = await BuildUsersAsync()
    //    //    };

    //    //// TODO: Load saved values into vm.* from your settings store.

    //    //return View(vm);
    //    }

    [ValidateAntiForgeryToken]
    [HttpPost]
    public async Task<IActionResult> Master(/*SettingsMasterVm vm, */string section, string? primary, string? sub)
        {
        // Refill lists (required after POST)
        //vm.Accounts = await BuildAccountsAsync();
        //vm.Taxes = await BuildTaxesAsync();
        //vm.Users = await BuildUsersAsync();
        //vm.ActivePrimary = primary ?? vm.ActivePrimary;
        //vm.ActiveSub = sub ?? vm.ActiveSub;

        //if (!ModelState.IsValid)
        //    return View(vm);

        // Persist only the section that was saved (keeps it snappy)
        switch (section)
            {
            case "default-common":
                // TODO: save vm.Common
                break;
            case "default-vendor":
                // TODO: save vm.VendorPurchase
                break;
            case "default-inventory":
                // TODO: save vm.Inventory
                break;
            case "default-customer":
                // TODO: save vm.CustomerSales
                break;
            case "default-employee":
                // TODO: save vm.Employee
                break;
            case "default-bank":
                // TODO: save vm.BankOther
                break;

            case "employee-default":
                // TODO: save vm.Attendance
                break;

            case "user-config":
                // TODO: save vm.UserConfig
                break;

            case "general":
                // TODO: save vm.General
                break;
            }

        TempData["ok"] = "Saved successfully.";
        return RedirectToAction(nameof(Master)/*, new { primary = vm.ActivePrimary, sub = vm.ActiveSub }*/);
        }
    }
}
