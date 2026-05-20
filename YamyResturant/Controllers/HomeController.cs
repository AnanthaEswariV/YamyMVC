using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using YamyResturant.Models;

namespace YamyResturant.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

      public IActionResult Home()
        {
             return View();
        }
    }
}
