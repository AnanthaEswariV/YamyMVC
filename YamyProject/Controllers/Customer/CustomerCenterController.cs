using Microsoft.AspNetCore.Mvc;
using YamyProject.Core.Models;
using YamyProject.Services;
using YamyProject.Services.Interfaces;

namespace YamyProject.Controllers
{
    public class CustomerCenterController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IEditeCustomerService _service;

        public CustomerCenterController(ICustomerService customerService, IEditeCustomerService service)
        {
            _customerService = customerService;
            _service = service;
        }

        public async Task<IActionResult> Index(string state = "Active")
        {
            var customers = await _customerService.GetAllAsync(state);
            return View(customers);
        }

        public async Task<IActionResult> Details(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null) return NotFound();

            var transactions = await _customerService.GetTransactionsAsync(id);
            ViewBag.Transactions = transactions;
            return View(customer);
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _customerService.DeleteCustomerAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            var vm = await _service.GetCustomerFormDataAsync(id);
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(CustomerViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            await _service.SaveCustomerAsync(vm.Customer);
            return RedirectToAction("Index"); // back to list
        }

    }
}
