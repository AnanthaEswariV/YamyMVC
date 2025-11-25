namespace YamyProject.Controllers.Reports.List
    {
    public class CustomerListController(IListServices listServices) : Controller
        {
        private readonly IListServices _listServices = listServices;
        public async Task<IActionResult> CustomerList()
            {
            var customerList = await _listServices.GetCustomersAsync();
            return View(customerList);
            }
        public async Task<IActionResult> CustomerCategoryList()
            {
            var customerCategoryList = await _listServices.GetCustomerCategorysAsync();
            return View(customerCategoryList);
            }

        }
    }
