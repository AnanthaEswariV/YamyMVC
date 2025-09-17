using Microsoft.AspNetCore.Mvc;

namespace YamyProject.Controllers.Customer
{
    public class CustomerCategorysController : Controller
    {
        private readonly YamyDbContext _context;

        public CustomerCategorysController(YamyDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TblCustomerCategory model)
        {
            if (!ModelState.IsValid)
            {
              
                return View(model);
            }
             
                _context.TblCustomerCategories.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

    }
}
