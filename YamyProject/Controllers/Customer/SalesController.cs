
namespace YamyProject.Controllers.Customer
{
    //    public class SalesController : Controller
    //    {
    //        public IActionResult Index()
    //        {
    //            return View();
    //        }
    //    }
    //}

    public class SalesController: Controller
    {
        private readonly ISalesService _service;
        private readonly ILogger<SalesController> _logger;

        public SalesController(ISalesService service, ILogger<SalesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Query(DateTime? from,DateTime? to,int? customerId,string? paymentMethod,CancellationToken ct = default)
        {
            var results = await _service.QuerySalesAsync(from, to, customerId, paymentMethod, ct);
            return Ok(results);
        }

        // GET: api/sales/5
        [HttpGet]
        public async Task<IActionResult> Get(int id, CancellationToken ct = default)
        {
            var sale = await _service.GetSaleAsync(id, ct);
            if (sale == null) return NotFound();
            return Ok(sale);
        }

        // POST: api/sales
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SaleCreateViewModel dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var id = await _service.CreateSaleAsync(dto, ct);
                return CreatedAtAction(nameof(Get), new { id }, new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create sale.");
                return StatusCode(500, "An error occurred while creating the sale.");
            }
        }

        // PUT: api/sales/etich (interpreted as edit) or api/sales/{id}
        [HttpPut]
        public async Task<IActionResult> Edit(int id, [FromBody] SaleEditViewModel dto, CancellationToken ct = default)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (id != dto.Id) return BadRequest("Id mismatch");

            try
            {
                await _service.UpdateSaleAsync(dto, ct);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update sale {SaleId}.", id);
                return StatusCode(500, "An error occurred while updating the sale.");
            }
        }

        // DELETE: api/sales/{id}
        [HttpDelete]
        public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
        {
            try
            {
                await _service.DeleteSaleAsync(id, ct);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete sale {SaleId}.", id);
                return StatusCode(500, "An error occurred while deleting the sale.");
            }
        }





        public async Task<IActionResult> CreatTax()
        { return View(); 
        }

    }
}