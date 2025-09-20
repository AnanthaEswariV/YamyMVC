using Microsoft.AspNetCore.Mvc;

namespace YamyProject.Controllers.Company
{
    public class RemindersController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
