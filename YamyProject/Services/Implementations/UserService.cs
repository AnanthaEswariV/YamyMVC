namespace YamyProject.Services.Implementations
    {
    public class UserService(YamyDbContext context) : IUserService
        {
        private readonly YamyDbContext _context = context;

        public async Task<UserAccessViewModel> GetUSerAsync(string state)
            {
            var query = _context.TblSecUsers.AsNoTracking();

            if (state == "Active")
                query = query.Where(u => u.Active != 1);
            else if (state == "Inactive")
                query = query.Where(u => u.Active == 1);

            var users = await query.ToListAsync();

            var vm = new UserAccessViewModel
                {
                Users = users.Select(u => new UserListItemViewModel
                    {
                    Id = u.Id,
                    Name = u.FirstName,
                    IsActive = u.Active
                    }).ToList()
                };

            return vm;
            }
        public async Task<UserInfoViewModel> GetSelectUSerAsync(int Id)
            {
            var query = await _context.TblSecUsers.Where(u => u.Id == Id).ToListAsync();

            var users = query.
                Select(u => new UserInfoViewModel
                    {
                    UserCode = u.Id,
                    Name = u.FirstName,
                    UserName = u.UserName,
                    Status = u.Active == 0 ? "Active" : "Un Active"
                    }).FirstOrDefault();

            return users;
            }
        public async Task<List<PermissionCategoryViewModel>> GetUserPermissionAsync(int id)
            {
            var permissionRows = await _context.TblUserPermissions
                .Where(u => u.UserId == id) // probably .Where(p => p.UserId == id)
                .Include(u => u.SubMenu)
                    .ThenInclude(s => s.MIdNavigation)
                .AsNoTracking()
                .ToListAsync();

            var permissionCategories = permissionRows
                .GroupBy(p => new
                    {
                    MenuId = p.SubMenu.MIdNavigation.Id,
                    MenuName = p.SubMenu.MIdNavigation.Name
                    })
                .Select(g => new PermissionCategoryViewModel
                    {
                    Key = g.Key.MenuId.ToString(),
                    DisplayName = g.Key.MenuName,
                    SelectAll = g.All(x => x.CanView && x.CanEdit && x.CanDelete),

                    Items = g.Select(x => new PermissionItemViewModel
                        {
                        Id = x.SubMenu.Id,
                        Name = x.SubMenu.Name,
                        CanView = x.CanView,
                        CanEdit = x.CanEdit,
                        CanDelete = x.CanDelete
                        }).ToList()
                    })
                .OrderBy(c => c.DisplayName)
                .ToList();

            return permissionCategories;   // ✅ now the types match
            }

        public async Task<TblUserPermission> UpsertUserPermissionAsync(int userId,int subMenuId,bool canView,bool canEdit,bool canDelete)
            {
       
            var strategy = _context.Database.CreateExecutionStrategy();

            IActionResult result = null; // will be set inside the strategy

            return  await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    // Try to get existing record
                    var permission = await _context.TblUserPermissions
                .FirstOrDefaultAsync(p => p.UserId == userId && p.SubMenuId == subMenuId);

                if (permission == null)
                {
                // Not found → INSERT (equivalent to VALUES (...) part)
                permission = new TblUserPermission
                    {
                    UserId = userId,
                    SubMenuId = subMenuId,
                    CanView = canView,
                    CanEdit = canEdit,
                    CanDelete = canDelete
                    };
                _context.TblUserPermissions.Add(permission);
                }
                else
                {
                // Found → UPDATE (equivalent to ON DUPLICATE KEY UPDATE ...)
                permission.CanView = canView;
                permission.CanEdit = canEdit;
                permission.CanDelete = canDelete;
                }

                await _context.SaveChangesAsync();
                        await tx.CommitAsync();

                    return permission; // or return a ViewModel if you prefer
                    }
                catch (Exception ex)
                    {
                    await tx.RollbackAsync();
                    throw new InvalidOperationException(
                        $"ChangePermissionAsync failed: {ex.GetBaseException().Message}", ex);
                    }
            });
            }

        //
        public async Task<UserEditViewModel> GetUserForEditAsync(int? id)
            {
            var vm = new UserEditViewModel();

            if (id.HasValue && id.Value > 0)
                {
                var user = await _context.TblSecUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == id.Value);

                if (user != null)
                    {
                    vm.Id = user.Id;
                    vm.FirstName = user.FirstName;
                    vm.LastName = user.LastName;
                    vm.UserName = user.UserName;
                    vm.EmployeeId = user.EmpId;
                    vm.RoleId = user.RoleId;
                    vm.Active = user.Active == 0; // 0 = active, 1 = inactive (same as old code)
                    }
                }
            else
                {
                // defaults for new user
                vm.Active = true;
                }

            await PopulateLookupsAsync(vm);
            return vm;
            }

        public async Task PopulateLookupsAsync(UserEditViewModel model)
            {
            model.Roles = await _context.TblSecRoles
                .OrderBy(r => r.Name)
                .Select(r => new SelectListItem
                    {
                    Value = r.Id.ToString(),
                    Text = r.Name
                    })
                .ToListAsync();

            model.Employees = await _context.TblEmployees
                .OrderBy(e => e.Id)
                .Select(e => new SelectListItem
                    {
                    Value = e.Id.ToString(),
                    Text = e.Code + " - " + e.Name
                    })
                .ToListAsync();
            }

        public async Task<bool> UserNameExistsAsync(string userName, int excludeId)
            {
            return await _context.TblSecUsers
                .AnyAsync(u => u.UserName == userName && u.Id != excludeId);
            }

        public async Task SaveUserAsync(UserEditViewModel model)
            {
            // username must be unique
            bool exists = await UserNameExistsAsync(model.UserName, model.Id);
            if (exists)
                throw new InvalidOperationException("User Name already exists.");

            if (model.IsNew)
                {
                var (hash, salt) = CreatePasswordHash(model.Password ?? string.Empty);

                var entity = new TblSecUser
                    {
                    UserName = model.UserName,
                    PasswordHash = hash,
                    Salt = salt,
                    EmpId = model.EmployeeId /*? -1*/,  // same as your old code
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    RoleId = model.RoleId!.Value,
                    Active = model.Active ? 0 : 1,
                    State = 0
                    };

                _context.TblSecUsers.Add(entity);
                }
            else
                {
                var entity = await _context.TblSecUsers
                    .FirstOrDefaultAsync(u => u.Id == model.Id);

                if (entity == null)
                    throw new KeyNotFoundException("User not found.");

                entity.UserName = model.UserName;
                entity.EmpId = model.EmployeeId /*? -1*/;
                entity.FirstName = model.FirstName;
                entity.LastName = model.LastName;
                entity.RoleId = model.RoleId!.Value;
                entity.Active = model.Active ? 0 : 1;

                // optional: if password supplied on edit, change it
                if (!string.IsNullOrWhiteSpace(model.Password))
                    {
                    //var (hash, salt) = CreatePasswordHash(model.Password);
                    //entity.PasswordHash = hash;
                    //entity.Salt = salt;
                    // var (hash, salt) =
                    string salt;
                    var hash=PasswordHelper.HashPassword(model.Password,out salt);
                    entity.PasswordHash = hash;
                    entity.Salt = salt;
                    }
                }

            await _context.SaveChangesAsync();
            }

        private static (string hash, string salt) CreatePasswordHash(string password)
            {
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var salt = Convert.ToBase64String(saltBytes);

            var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256);
            var hashBytes = pbkdf2.GetBytes(32);
            var hash = Convert.ToBase64String(hashBytes);

            return (hash, salt);
            }


        }
    }

