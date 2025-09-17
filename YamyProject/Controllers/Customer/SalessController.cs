[Route("sales")]
public class SalessController : Controller
{
    private readonly ISalesServices _salesService;

    public SalessController(ISalesServices salesService)
    {
        _salesService = salesService;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        // Return list view. Implementation omitted for brevity.
        return View();
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        var vm = new SalesEditViewModel
        {
            Date = DateTime.UtcNow,
            NextInvoiceCode = GenerateNextSalesCode() // helper that queries DB or service
        };
        return View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SalesEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var id = await _salesService.CreateSaleAsync(vm);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var vm = await _salesService.GetSaleForEditAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SalesEditViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);
        vm.Id = id;
        var result = await _salesService.UpdateSaleAsync(vm);
        if (!result) return BadRequest("Unable to update");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("details/{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var vm = await _salesService.GetSaleForEditAsync(id);
        if (vm == null) return NotFound();
        return View(vm);
    }

    // Helper: generate next invoice code (port of GenerateNextSalesCode)
    private string GenerateNextSalesCode()
    {
        // For the controller we should call the service or db; here an example raw SQL via EF:
        var last = _salesService is null ? 0 : 0; // placeholder — call service/db
        return "SI-0001"; // replace with real call
    }
}
