using Microsoft.AspNetCore.Mvc;

namespace YamyProject.Controllers.Company
{
    public class CompanyProfileController : Controller
    {
        public IActionResult CompanyCenter()
        {
            return View();
        }
        public IActionResult Reminders()
        {
            return View();
        }
    }
}
