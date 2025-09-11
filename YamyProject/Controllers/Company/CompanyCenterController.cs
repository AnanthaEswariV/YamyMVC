
using YamyProject.Core.ViewModel;

namespace YamyProject.Controllers.Company
{
    public class CompanyCenterController : Controller
    {
        private readonly ApplicationDbContext _context;
       private readonly IMapper _mapper;

        public CompanyCenterController(ApplicationDbContext context,IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }
        [HttpGet]
        public IActionResult Index()
        {
            var company=_context.TblCompanys.AsNoTracking().ToList();
            var viewModel = "";//_mapper,Map<<>> (company);
            return PartialView("CompanyList", viewModel);
        }
        [HttpGet]
        [AjaxOnly]
        public IActionResult Edit(int id)
        {
            var company = _context.TblCompanys.Find(id);

            if (company is null)
                return NotFound();

            var viewModel = _mapper.Map<Core.ViewModel.CompanyViewModel>(company);
            return PartialView("CompanyList", viewModel);
        }
        [HttpPost]
        public IActionResult Edit(CompanyViewModel model)
        { 
            if (!ModelState.IsValid)
     return BadRequest();

 var company = _context.TblCompanys.Find(model.Id);

 if (company is null)
     return NotFound();

 company = _mapper.Map(model, company);
 //company.LastUpdatedOn = DateTime.Now;
            _context.SaveChanges();

            var viewModel = _mapper.Map<CompanyViewModel>(company);

            return PartialView("CompanyList", viewModel);
        }
    }
}
