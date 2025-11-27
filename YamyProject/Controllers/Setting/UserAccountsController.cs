namespace YamyProject.Controllers.Setting
    {
    public class UserAccountsController(IListServices ListServices,IUserService UserService,YamyDbContext context)  : Controller
        {
        //private readonly IListServices _ListServices = ListServices;
        private readonly IUserService _UserService = UserService;
        private readonly YamyDbContext _context = context;
        

    public async Task<IActionResult> Index(string state = "Active")
            {
            var user = await _UserService.GetUSerAsync(state);

            user.StatusFilter = state;
            user.SelectedUserId = 0;
            user.UserInfo = null;
            user.Categories = null;
            return View("AccessUser", user);
            //new UserAccessViewModel 
            //    { 
            //     Users = user.Users
            //    });
            }
        [HttpGet]
        public async Task<IActionResult> UserInformation(int id)
            {
            var UserInfo =await _UserService.GetSelectUSerAsync(id);
            if (UserInfo is null) return NotFound();
            return Json(UserInfo); }
        [HttpGet]
        public async Task<IActionResult> UserRoleandPermission(int id)
            { 
            var Permission = await _UserService.GetUserPermissionAsync(id);
            if (Permission is null) return NotFound();
            return Json(Permission);
            }
        [HttpPost]
        public async Task<IActionResult> ChangePermission(int UserId,int subMenuId , bool canView, bool canEdit, bool canDelete)
            {
            await _UserService.UpsertUserPermissionAsync(UserId,subMenuId,canView,canEdit,canDelete);
            //     return RedirectToAction("AccessUser");
      //      return Json(new { success = true });
            return Ok(new { status = true /*, message = "User Have Been Add " */});
            }


        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
            {
            //var model = await _UserService.GetUserForEditAsync(id);
            //return PartialView("_UserForm", model);   // partial view
            //}
            var roles = _context.TblSecRoles
        .Select(r => new SelectListItem { Value = r.Id.ToString(), Text = r.Name })
        .ToList();

            var employees = _context.TblEmployees
                .Select(e => new SelectListItem { Value = e.Id.ToString(), Text = e.Name })
                .ToList();

            UserEditViewModel model;

            if (id == null)
                {
                // new user
                model = new UserEditViewModel
                    {
                    IsNew = true,
                    Active = true,
                    Roles = roles,
                    Employees = employees
                    };
                }
            else
                {
                // edit existing
                var user = _context.TblSecUsers.Find(id);
                if (user == null) return NotFound();

                model = new UserEditViewModel
                    {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.UserName,
                    RoleId = user.RoleId,
                    EmployeeId = user.EmpId,
                    Active = user.Active==1,
                    IsNew = false,
                    Roles = roles,
                    Employees = employees
                    };
                }

            return PartialView("_UserForm", model);
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(UserEditViewModel model)
            {
            // manual validation similar to old WinForms code
            if (model.RoleId == null)
                ModelState.AddModelError(nameof(model.RoleId), "Choose Role first.");

            if (model.IsNew)
                {
                if (string.IsNullOrWhiteSpace(model.Password))
                    ModelState.AddModelError(nameof(model.Password), "Enter your new password first.");

                if (model.Password != model.ConfirmPassword)
                    ModelState.AddModelError(nameof(model.ConfirmPassword),
                        "Confirm Password mismatch with New Password, please check first.");
                }

            if (await _UserService.UserNameExistsAsync(model.UserName, model.Id))
                ModelState.AddModelError(nameof(model.UserName), "User Name already exists.");

            if (!ModelState.IsValid)
                {
                await _UserService.PopulateLookupsAsync(model);
                return PartialView("_UserForm", model);
                }

            await _UserService.SaveUserAsync(model);

            // if you are using AJAX modal, you usually return JSON
            return Json(new { success = true });
            }

        public async Task<IActionResult> EditRole(int? id)
            {
            RoleEditViewModel model;

            if (id is null || id == 0)
                model = new RoleEditViewModel();     // New role
            else
                {
                // Load existing entity from DB
                var role = await _context.TblSecRoles
                                         .AsNoTracking()
                                         .FirstOrDefaultAsync(r => r.Id == id.Value);
                // use your real PK property

                if (role == null)
                    return NotFound();

                // Map entity -> view model
                model = new RoleEditViewModel
                    {
                    Id = role.Id,        // or role.RoleId, etc.
                    Name = role.Name   // adjust to your property name
                    };
                }
                 // load existing

            return PartialView("_RoleEdit", model);
            }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRole(RoleEditViewModel model)
            {
            // Validate
            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError(nameof(model.Name), "Role Name is required.");
            //if (await _UserService.RoleNameExistsAsync(model.Name, model.Id))
            //    ModelState.AddModelError(nameof(model.Name), "Role Name already exists.");
            if (!ModelState.IsValid)
                return PartialView("_RoleEdit", model);
            // Save to DB
            if (model.Id == 0)
                {
                // New role
                var newRole = new TblSecRole
                    {
                    Name = model.Name
                    };
                _context.TblSecRoles.Add(newRole);
                }
            else
                {
                // Update existing
                var existingRole = await _context.TblSecRoles.FindAsync(model.Id);
                if (existingRole == null)
                    return NotFound();
                existingRole.Name = model.Name;
                _context.TblSecRoles.Update(existingRole);
                }
            await _context.SaveChangesAsync();
            return Json(new { success = true });
            }

        }
    }