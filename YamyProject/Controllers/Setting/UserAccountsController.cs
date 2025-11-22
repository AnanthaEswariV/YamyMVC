
using Humanizer;

namespace YamyProject.Controllers.Setting
    {
    public class UserAccountsController(IListServices ListServices,IUserService UserService)  : Controller
        {
        private readonly IListServices _ListServices = ListServices;
        private readonly IUserService _UserService = UserService;

    public async Task<IActionResult> Index(string state = "Active")
            {
            var user = await _UserService.GetUSerAsync(state);

            return View("AccessUser",new UserAccessViewModel 
                { 
                 Users = user.Users
                });
            }
        [HttpGet]
        public async Task<IActionResult> UserInformation(int id)
            {
            var UserInfo =_UserService.GetSelectUSerAsync(id);
            if (UserInfo is null) return NotFound();
            return Json(UserInfo); }
        [HttpGet]
        public async Task<IActionResult> UserRoleandPermission(int id)
            { 
            var Permission =_UserService.GetUserPermissionAsync(id);
            if (Permission is null) return NotFound();
            return Json(Permission);
            }
        [HttpPost]
        public async Task<IActionResult> ChangePermission(int UserId,int subMenuId , bool canView, bool canEdit, bool canDelete)
            {
            await _UserService.UpsertUserPermissionAsync(UserId,subMenuId,canView,canEdit,canDelete);
            //     return RedirectToAction("AccessUser");
            return Json("");
            }
        }
    }