namespace YamyProject.Controllers
{
    public class ListsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public ListsController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        #region Item Category
        public IActionResult ItemCategory()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetItemCategory()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ApiClient");

                // ✅ read from session
                var databaseName = HttpContext.Session.GetString("DatabaseName");

                if (string.IsNullOrEmpty(databaseName))
                {
                    return Json(new { status = false, message = "No database selected. Please login again." });
                }

                // ✅ pass database name in query string
                var response = await client.GetAsync($"api/Lists/GetCategories?databaseName={databaseName}");

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { status = false, message = "Failed to fetch categories" });
                }

                var data = await response.Content.ReadAsStringAsync();
                var categories = JsonConvert.DeserializeObject<List<ItemCatoryViewModel>>(data);

                return Json(new { status = true, data = categories });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = "Error: " + ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> AddCategory(string categoryName)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var requestObj = new { CategoryName = categoryName };
                var json = JsonConvert.SerializeObject(requestObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"api/Lists/AddCategory?databaseName={database}", content);

                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> EditCategory(int id, string categoryName)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var requestObj = new { Id = id, CategoryName = categoryName };
                var json = JsonConvert.SerializeObject(requestObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Call API (PUT method)
                var response = await client.PutAsync($"api/Lists/EditCategory?databaseName={database}", content);

                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var response = await client.DeleteAsync($"api/Lists/DeleteCategory/{id}?databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Tax

        public IActionResult Tax()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetTaxes()
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var response = await client.GetAsync($"api/Lists/GetTaxes?databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> AddTax(string name, decimal value, string description)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var requestObj = new { Name = name, Value = value, Description = description };
                var json = JsonConvert.SerializeObject(requestObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"api/Lists/AddTax?databaseName={database}", content);

                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> EditTax(int id, string name, decimal value, string description)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var requestObj = new { Id = id, Name = name, Value = value, Description = description };
                var json = JsonConvert.SerializeObject(requestObj);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"api/Lists/EditTax?databaseName={database}", content);

                var result = await response.Content.ReadAsStringAsync();
                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteTax(int id)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var content = new StringContent(JsonConvert.SerializeObject(id), Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"api/Lists/DeleteTax/{id}?databaseName={database}", content);
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDeletedTaxes()
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var response = await client.GetAsync($"api/Lists/GetDeletedTaxes?databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> RestoreTax(int id)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var response = await client.PostAsync($"api/Lists/RestoreTax/{id}?databaseName={database}", null);
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Unit 

        public IActionResult Unit()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetUnit()
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.GetAsync($"api/Lists/GetUnit?databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddUnit(string name)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var payload = new { Name = name };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload),
                                                System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"api/Lists/AddUnit?databaseName={database}", content);
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditUnit(int id, string name)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                var payload = new { Id = id, Name = name };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload),
                                                System.Text.Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"api/Lists/EditUnit?databaseName={database}", content);
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteUnit(int id)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");
                var response = await client.DeleteAsync($"api/Lists/DeleteUnit?id={id}&databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region ChartOfAccount
        public IActionResult ChartOfAccount()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetCOA()
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                // call your API
                var response = await client.GetAsync($"api/Lists/GetCOAHierarchy?databaseName={database}");
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Fixed Asset Item List

        public IActionResult FixedAssetItem()
        {
            return View();
        }

        // Fetch data from API
        [HttpGet]
        public async Task<IActionResult> GetFixedAssetItem(DateTime? dateFrom, DateTime? dateTo, bool ignoreDate = false)
        {
            try
            {
                var database = HttpContext.Session.GetString("DatabaseName");
                if (string.IsNullOrEmpty(database))
                    return Json(new { status = false, message = "No database selected. Please login first." });

                var client = _httpClientFactory.CreateClient("ApiClient");

                string url = $"api/Lists/fixed-assets?databaseName={database}&ignoreDate={ignoreDate}";
                if (!ignoreDate && dateFrom.HasValue && dateTo.HasValue)
                {
                    url += $"&dateFrom={dateFrom:yyyy-MM-dd}&dateTo={dateTo:yyyy-MM-dd}";
                }

                var response = await client.GetAsync(url);
                var result = await response.Content.ReadAsStringAsync();

                return Content(result, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        #endregion

    }

}

