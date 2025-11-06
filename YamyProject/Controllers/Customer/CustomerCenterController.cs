using YamyProject.Services;

namespace YamyProject.Controllers
{
    public class CustomerCenterController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IEditeCustomerService _service;
        private readonly IListServices _ListServices;

        public CustomerCenterController(ICustomerService customerService, IEditeCustomerService service, IListServices listServices)
        {
            _customerService = customerService;
            _service = service;
            _ListServices = listServices;
        }

        public async Task<IActionResult> Index(string state = "Active", int? selectedId = null, string? searchText = null)
        {
            // Load all customers (filtered by state)
            var customers = await _customerService.GetAllAsync(state);

            // Apply search filter if provided
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                customers = customers.Where(c =>
                    (!string.IsNullOrEmpty(c.Name) && c.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    c.Code.ToString().Contains(searchText));
            }

            // Determine the selected customer
            TblCustomer? selectedCustomer = null;
            if (selectedId.HasValue)
            {
                selectedCustomer = customers.FirstOrDefault(c => c.Id == selectedId.Value);
            }

            // Load transactions for selected customer
            var transactions = selectedCustomer != null
                ? await _customerService.GetTransactionsAsync(selectedCustomer.Id)
                : new List<TblTransaction>();

            // Prepare the view model
            var vm = new CustomerCenterViewModel
            {
                Customers = customers.ToList(),
                SelectedCustomer = selectedCustomer,
                Transactions = transactions,
                State = state,
                SearchText = searchText
            };

            return View(vm);
        }
        // GET: Customer Details
        public async Task<IActionResult> Details(int id)
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null) return NotFound();

            var transactions = await _customerService.GetTransactionsAsync(id);
            ViewBag.Transactions = transactions;

            return View(customer);
        }
        // GET: CustomerCenter/Create
        public async Task<IActionResult> Create()
        {
            var vm = await _service.GetCreateCustomerFormAsync();


            var categories = await _ListServices.GetCustomerCategorysAsync();
            var accounts = await _ListServices.GetAccountsAsync();
            var countries = await _ListServices.GetCountriesAsync();
            var cities = await _ListServices.GetCitysAsync();


            vm.Customer ??= new TblCustomer();
            vm.Categoriess = categories.Select(c => new SelectListItem
            {
                Text = c.Name.ToString(), // Adjust according to your property names
                Value = c.Id.ToString() // Adjust according to your property names
            });
            vm.Account = accounts?.Select(a => new SelectListItem
            {
                Text = a.Name,
                Value = a.Id.ToString()
            }).ToList() ?? new List<SelectListItem>();
            vm.Countriess = countries?.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList() ?? new List<SelectListItem>();
            vm.Citys = cities?.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList() ?? new List<SelectListItem>();


            return View("Edite", vm); // reuse Edit view
        }
        // POST: CustomerCenter/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                // Reload dropdowns
                var reloadVm = await _service.GetCreateCustomerFormAsync();
                reloadVm.Customer = vm.Customer; // preserve entered data
                return View("Edite", reloadVm);
            }

            await _service.SaveCustomerAsync(vm.Customer);
            return RedirectToAction(nameof(Index));
        }
        // GET: Edit Form
        public async Task<IActionResult> Edit(int? id)
        {
            var vm = await _service.GetCustomerFormDataAsync(id);

            var categories = await _ListServices.GetCustomerCategorysAsync();
            var accounts = await _ListServices.GetAccountsAsync();
            var countries = await _ListServices.GetCountriesAsync();
            var cities = await _ListServices.GetCitysAsync();

            vm.Customer ??= new TblCustomer();
            vm.Categoriess = categories.Select(static c => new SelectListItem
            {
                Text = c.Name.ToString(), // Adjust according to your property names
                Value = c.Id.ToString() // Adjust according to your property names
            });
            vm.Account = accounts?.Select(a => new SelectListItem
            {
                Text = a.Name,
                Value = a.Id.ToString()
            }).ToList() ?? new List<SelectListItem>();
            vm.Countriess = countries?.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList() ?? new List<SelectListItem>();
            vm.Citys = cities?.Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            }).ToList() ?? new List<SelectListItem>();

            return View("_CustomerForm", vm);
        }
        // POST: Save Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerViewModel vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            await _service.SaveCustomerAsync(vm.Customer);
            return RedirectToAction(nameof(Index));
        }
        // GET: Delete Customer
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _customerService.DeleteCustomerAsync(id);
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
