namespace YamyProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        #region Register
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Register(CompanyViewModel companyViewModel)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                // ✅ call correct endpoint with JSON body
                var response = await client.PostAsJsonAsync("api/Account/create", companyViewModel);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<dynamic>(data);

                    return Json(new
                    {
                        status = true,
                        message = apiResponse?.Message ?? "Company created successfully!",
                        database = apiResponse?.Database
                    });
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return Json(new { status = false, message = error });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "An error occurred: " + ex.Message });
            }
        }

        #endregion

        #region CompanyList

        public IActionResult CompanyList()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanyList()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                var response = await client.GetAsync("api/Account/list");

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { status = false, message = "Failed to fetch companies" });
                }

                var data = await response.Content.ReadAsStringAsync();
                var companies = JsonConvert.DeserializeObject<List<CompanyViewModel>>(data);

                return Json(new { status = true, data = companies });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }

        #endregion

        #region Login

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
      
        [HttpPost]
        public async Task<IActionResult> LoginUser(string username, string password, string database)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                var loginRequest = new
                {
                    UserName = username,
                    Password = password,
                    Database = database   // ✅ important
                };

                var json = JsonConvert.SerializeObject(loginRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("api/Account/login", content);

                if (!response.IsSuccessStatusCode)
                    return Json(new { status = false, message = "Login failed" });


                // ✅ store selected DB in session
                HttpContext.Session.SetString("DatabaseName", database);

                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion


        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
