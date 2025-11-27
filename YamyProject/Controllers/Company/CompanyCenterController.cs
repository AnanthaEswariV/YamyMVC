namespace YamyProject.Controllers.Company
{
    public class CompanyCenterController : Controller
    {

       private readonly YamyDbContext _context;
       private readonly IMapper _mapper;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public CompanyCenterController(YamyDbContext context,IMapper mapper, IHttpClientFactory httpClientFactory,IConfiguration config)
        {
            _context = context;
            _mapper = mapper;
            _httpClientFactory = httpClientFactory;
            _config = config;
            }
        private MySqlConnection CreateConnection()
            {
            // base connection string from appsettings.json
            var baseConn = _config.GetConnectionString("DefaultConnection");

            // database name from Session (if set), or fallback to DefaultDatabase
            var dbName = HttpContext.Session.GetString("DatabaseName")
                         ?? _config.GetConnectionString("DefaultDatabase");

            var builder = new MySqlConnectionStringBuilder(baseConn)
                {
                Database = dbName
                };

            return new MySqlConnection(builder.ConnectionString);
            }
        [HttpGet]
        public IActionResult Index()
        {
            var Company = _context.TblCompanies.AsNoTracking().ToList();

            var viewModel = _mapper.Map<IEnumerable<CompanyViewModels>>(Company);

            return View(viewModel);
            
        }
        [HttpGet]
        public IActionResult Create()
        {
            return PartialView("_Form");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CompanyViewModels model)
        {
            if (!ModelState.IsValid)
                return BadRequest();

            var company = _mapper.Map<TblCompany>(model);
           // company. = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

            _context.Add(company);
            _context.SaveChanges();

            var viewModel = _mapper.Map<CompanyViewModels>(company);

            return PartialView("_CompanyRow", viewModel);
        }
        [HttpGet]
        public async Task<IActionResult> Edit(/*int id*/)
        {
            var company = await _context.TblCompanies.FirstOrDefaultAsync();
          
            if (company is null)
                return NotFound();

            var viewModel = _mapper.Map<CompanyViewModels>(company);
            return PartialView("_Form", viewModel);
        }       
        [HttpPost]
        public IActionResult Edit(CompanyViewModel model)
        {
            //if (!ModelState.IsValid)
            //    return PartialView("_Form", model);

            var company = _context.TblCompanies.Find(model.Id);
            
             if (company is null)
                 return NotFound();
            
             _mapper.Map(model, company);
             _context.SaveChanges();
           // return RedirectToAction("Index");
            //var viewModel = _mapper.Map<CompanyViewModel>(company);
            return Json(new { success = true });

         //   return PartialView("CompanyList", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SaveVat(VatConfigurationViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Message = "Please correct the validation errors.";
                return View("Index", model); // Replace 'VatForm' with your actual View name
            }

            try
            {
                // Validation (Optional - Business rule checks)
                if (string.IsNullOrWhiteSpace(model.Vat.RegistrationNo))
                {
                    ModelState.AddModelError("Vat.RegistrationNo", "Please enter TAX Registration No.");
                    return View("Index", model);
                }

                if (string.IsNullOrWhiteSpace(model.CorporateTax.CorporateTaxNo))
                {
                    ModelState.AddModelError("CorporateTax.CorporateTaxNo", "Please enter Corporate Tax No.");
                    return View("Index", model);
                }

                // Save VAT Configuration
                _context.TblVatConfigrations.Add(model.Vat);

                // Save Corporate Tax Configuration
                _context.TblCorporateTaxConfigrations.Add(model.CorporateTax);

                await _context.SaveChangesAsync();

                TempData["Message"] = "VAT and Corporate Tax information saved successfully!";
            return Json(new { success = true });

              //  return RedirectToAction("Index"); // Or wherever your list or confirmation page is
            }
            catch (Exception ex)
            {
                // Log the error in real applications
                ViewBag.Message = $"An error occurred: {ex.Message}";
                return View("VatForm", model);
            }
        }

        public IActionResult Reminder()
        {
            return PartialView("Reminder");

        }
    }
}
