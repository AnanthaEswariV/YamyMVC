namespace YamyProject.Controllers.Reports.List
    {
    public class VendorListController(IListServices listServices) : Controller
        {
        private readonly IListServices _listServices = listServices;

        public async Task<IActionResult> VendorList()
            {
            var VendorList = await _listServices.GetVendorsAsync();
            return View(VendorList);
            }
        public async Task<IActionResult> VendorCategoryList()
            {
            var VendorCategoryList = await _listServices.GetVenderCategorysAsync();
            return View(VendorCategoryList);
            }
        }
    }
