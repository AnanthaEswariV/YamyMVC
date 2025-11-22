namespace YamyProject.Controllers.Setting
 {
    public class MasterSettingsController(YamyDbContext context, IListServices listServices, IAttendanceService attendanceService) : Controller
        {
        private readonly YamyDbContext _context=context;
        private readonly IListServices _listServices = listServices;
        private readonly IAttendanceService _attendanceService = attendanceService;

        public async Task<IActionResult> Index()
            {
            var Accounts = await _listServices.GetAccountsAsync();
            var AccountslectList = Accounts.Select(c => new SelectListItem
                {
                Value = c.Id.ToString(),
                Text = c.Name
                }).ToList();
            var vm = new SettingsMasterViewmodel
                {
                ActivePrimary =  "default" ,
                ActiveSub =  "common" ,
                Accounts = AccountslectList
                };
            return View("Index",vm);
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

   // [HttpGet]
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
    public async Task<IActionResult> Master(/*SettingsMasterVm vm, */string section, string? primary = "default", string? sub = "common")
        {

            var Accounts = await _listServices.GetAccountsAsync();
            var AccountslectList = Accounts.Select(c => new SelectListItem
                {
                Value = c.Id.ToString(),
                Text = c.Name
                }).ToList();

            var vm = new SettingsMasterViewmodel
                {
                ActivePrimary = string.IsNullOrWhiteSpace(primary) ? "default" : primary,
                ActiveSub = string.IsNullOrWhiteSpace(sub) ? "common" : sub,
                Accounts= AccountslectList
                };
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

        //[ValidateAntiForgeryToken]
        //[HttpPost]
        public async Task<IActionResult> MasterEmployee()
            {           
            var model = new DefaultEmplowyeeViewModel();
            return View(model);
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttendance([FromBody] DefaultEmplowyeeViewModel request)
            {

            //if (!ModelState.IsValid)
            //    return BadRequest(new { message = "Invalid data." });
            if (!ModelState.IsValid)
                {
                // optional – helps debug what is wrong
                var errors = ModelState
                    .Where(kvp => kvp.Value.Errors.Count > 0)
                    .Select(kvp => new
                        {
                        Field = kvp.Key,
                        Errors = kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
                        });

                return BadRequest(new { message = "Invalid data.", details = errors });
                }
            var result = await _attendanceService.SaveAttendanceAsync(request);

            if (!result.Success)
                {
                // differentiate duplicate vs generic error with 409
                if (result.Message != null && result.Message.Contains("already exists"))
                    return StatusCode(StatusCodes.Status409Conflict, new { message = result.Message });

                return BadRequest(new { message = result.Message });
                }

            return Ok(new { message = result.Message });
            }

        [HttpGet]
        public async Task<IActionResult> GeneralConfig()
            {
            var DefaultTaxPercent =await _context.TblGeneralSettings.Where(g => g.Name == "DEFAULT TAX PERCENTAGE").AsNoTracking().FirstOrDefaultAsync();
            var GeneralConfig = await _context.TblGeneralSettings.AsNoTracking().ToListAsync();
             var model =new GeneralConfigViewModel
                {
                 DefaultTaxPercent= DefaultTaxPercent.Value,
                 GeneralConfig = GeneralConfig.Select(g => new GeneralConfigItemViewModel
                     {
                    Id = g.Id,
                    Name = g.Name,
                    IsChecked = g.Status==1
                    }).ToList()
                };
            

            return View("GeneralConfig", model);
            }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneralConfig([FromBody]GeneralConfigItemViewModel model)
            {
            if (!ModelState.IsValid)
                return View("GeneralConfig", model);

          //  var ids = model.Id;

            var entities = await _context.TblGeneralSettings
                                         .Where(g => g.Id == model.Id)
                                         .FirstOrDefaultAsync();

            // row["status"] = isChecked ? "1" : "0";
            entities.Status = model.IsChecked ? 1 : 0;                     

            await _context.SaveChangesAsync();

            TempData["ok"] = "Settings updated successfully.";
            return RedirectToAction(nameof(GeneralConfig));
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDefaultTax(GeneralConfigViewModel model)
            {
            if (!ModelState.IsValid)
                {
              

                return View("Index", model);
                }

            if (model.DefaultTaxPercent.HasValue)
                {
                const string name = "DEFAULT TAX PERCENTAGE";

                var setting = await _context.TblGeneralSettings
                    .FirstOrDefaultAsync(g => g.Name == name);

                if (setting != null)
                    {
                  
                    setting.Value = model.DefaultTaxPercent.Value; 
                    }
                }

            await _context.SaveChangesAsync();

            TempData["ok"] = "Saved!";
            return RedirectToAction(nameof(Index));
            }


        }
    }