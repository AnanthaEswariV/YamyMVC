namespace YamyProject.Services.Interfaces
    {
    public interface IUserService
        {
        Task<UserAccessViewModel> GetUSerAsync(string state);
        Task<UserInfoViewModel> GetSelectUSerAsync(int Id);
        Task<List<PermissionCategoryViewModel>> GetUserPermissionAsync(int id);
   //     Task<IActionResult> ChangePermissionAsnc(PermissionCategoryViewModel Model);
        Task<TblUserPermission> UpsertUserPermissionAsync(int userId, int subMenuId, bool canView, bool canEdit, bool canDelete);
        //
         Task<UserEditViewModel> GetUserForEditAsync(int? id);
        Task PopulateLookupsAsync(UserEditViewModel model);
        Task SaveUserAsync(UserEditViewModel model);
        Task<bool> UserNameExistsAsync(string userName, int excludeId);
        }
    }
