using Microsoft.AspNetCore.Mvc;

namespace YamyProject.Controllers.Customer
{
    public class ReceiptVoucherController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
