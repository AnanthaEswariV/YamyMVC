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

        }
    }