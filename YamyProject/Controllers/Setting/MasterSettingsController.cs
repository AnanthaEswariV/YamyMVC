namespace YamyProject.Controllers.Setting
 {
    public class MasterSettingsController(YamyDbContext context, IListServices listServices, IAttendanceService attendanceService) : Controller
        {
        private readonly YamyDbContext _context=context;
        private readonly IListServices _listServices = listServices;
        private readonly IAttendanceService _attendanceService = attendanceService;

        public async Task<IActionResult> Index(string primary = "default", string sub = "common")
            {
            // 1) Normalize values from querystring (?primary=...&sub=...)
            if (string.IsNullOrWhiteSpace(primary))
                primary = "default";

            if (string.IsNullOrWhiteSpace(sub))
                sub = "common";

            // 2) Load data
            var accounts = await _listServices.GetAccountsAsync();
            var accountSelectList = accounts.Select(c => new SelectListItem
                {
                Value = c.Id.ToString(),
                Text = c.Name
                }).ToList();

            var defaultAccount = await _attendanceService.GetListAsync();

            // 3) Fill ViewModel USING primary/sub from URL
            var vm = new SettingsMasterViewmodel
                {
                ActivePrimary = primary,      // <— IMPORTANT
                ActiveSub = sub,          // <— IMPORTANT
                Accounts = accountSelectList,
                DefaultAccount = defaultAccount
                };
            if (primary.Equals("general", StringComparison.OrdinalIgnoreCase))
                {
                // ✅ load General Config when "General Settings" tab is active
                vm.GeneralConfig = await BuildGeneralConfigAsync();
                }

            return View("Index", vm);
            }

  

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
        //public async Task<IActionResult> MasterEmployee()
        //    {           
        //    var model = new DefaultEmplowyeeViewModel();
        //    return View(model);
        //    }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> SaveAttendance([FromBody] DefaultEmplowyeeViewModel request)
        //    {

        //    //if (!ModelState.IsValid)
        //    //    return BadRequest(new { message = "Invalid data." });
        //    if (!ModelState.IsValid)
        //        {
        //        // optional – helps debug what is wrong
        //        var errors = ModelState
        //            .Where(kvp => kvp.Value.Errors.Count > 0)
        //            .Select(kvp => new
        //                {
        //                Field = kvp.Key,
        //                Errors = kvp.Value.Errors.Select(e => e.ErrorMessage).ToArray()
        //                });

        //        return BadRequest(new { message = "Invalid data.", details = errors });
        //        }
        //    var result = await _attendanceService.SaveAttendanceAsync(request);

        //    if (!result.Success)
        //        {
        //        // differentiate duplicate vs generic error with 409
        //        if (result.Message != null && result.Message.Contains("already exists"))
        //            return StatusCode(StatusCodes.Status409Conflict, new { message = result.Message });

        //        return BadRequest(new { message = result.Message });
        //        }

        //    return Ok(new { message = result.Message });
        //    }

        //[HttpGet]
        //public async Task<IActionResult> GeneralConfig()
        //    {
        //    var DefaultTaxPercent =await _context.TblGeneralSettings.Where(g => g.Name == "DEFAULT TAX PERCENTAGE").AsNoTracking().FirstOrDefaultAsync();
        //    var GeneralConfig = await _context.TblGeneralSettings.AsNoTracking().ToListAsync();
        //     var model =new GeneralConfigViewModel
        //        {
        //         DefaultTaxPercent= DefaultTaxPercent.Value,
        //         GeneralConfig = GeneralConfig.Select(g => new GeneralConfigItemViewModel
        //             {
        //            Id = g.Id,
        //            Name = g.Name,
        //            IsChecked = g.Status==1
        //            }).ToList()
        //        };


        //    return View("GeneralConfig", model);
        //    }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> GeneralConfig([FromBody]GeneralConfigItemViewModel model)
        //    {
        //    if (!ModelState.IsValid)
        //        return View("GeneralConfig", model);

        //  //  var ids = model.Id;

        //    var entities = await _context.TblGeneralSettings
        //                                 .Where(g => g.Id == model.Id)
        //                                 .FirstOrDefaultAsync();

        //    // row["status"] = isChecked ? "1" : "0";
        //    entities.Status = model.IsChecked ? 1 : 0;                     

        //    await _context.SaveChangesAsync();

        //    TempData["ok"] = "Settings updated successfully.";
        //    return RedirectToAction(nameof(GeneralConfig));
        //    }

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
          //  return RedirectToAction(nameof(Index));
            return Ok(new { success = true });

            }

        private async Task<GeneralConfigViewModel> BuildGeneralConfigAsync()
            {
            var defaultTax = await _context.TblGeneralSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Name == "DEFAULT TAX PERCENTAGE");

            var generalConfigRows = await _context.TblGeneralSettings
                .AsNoTracking()
                .ToListAsync();

            return new GeneralConfigViewModel
                {
                DefaultTaxPercent = defaultTax?.Value ?? 0,
                GeneralConfig = generalConfigRows
                    .Select(g => new GeneralConfigItemViewModel
                        {
                        Id = g.Id,
                        Name =  g.Name,
                        IsChecked = g.Status == 1
                        })
                    .ToList()
                };
            }
        // Keep this if you still want a standalone /GeneralConfig page
        public async Task<IActionResult> GeneralConfig()
            {
            var model = await BuildGeneralConfigAsync();

            return View("GeneralConfig", model);
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneralConfig([FromBody] GeneralConfigItemViewModel model)
            {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var entity = await _context.TblGeneralSettings
                .FirstOrDefaultAsync(g => g.Id == model.Id);

            if (entity == null)
                return NotFound();

            entity.Status = model.IsChecked ? 1 : 0;
            await _context.SaveChangesAsync();

            // For AJAX this redirect is not used; just return 200 OK
            return Ok(new { success = true });
            }
        //
        }
    }