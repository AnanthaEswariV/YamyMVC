using Microsoft.AspNetCore.Mvc;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace YamyProject.Controllers
{
    [Route("Inventory/[action]")]
    public class InventoryController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly MySqlConnection _connection;
        public InventoryController(IHttpClientFactory httpClientFactory, IConfiguration config, ApplicationDbContext applicationDbContext)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _connection = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            _applicationDbContext = applicationDbContext;
        }


        #region WareHouse

        public IActionResult WareHouse()
        {
              return View();
        }

        [IgnoreAntiforgeryToken]
        [HttpPost("SaveWarehouse")]
        public async Task<IActionResult> SaveWarehouse([FromBody] WarehouseRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter warehouse name" });

            try
            {
                // 🔎 Check duplicate
                var existingWarehouse = await _applicationDbContext.tbl_warehouse
                    .Where(w => w.Name == model.Name)
                    .FirstOrDefaultAsync();

                if (existingWarehouse != null && model.Id == 0) // insert but exists
                    return BadRequest(new { status = false, message = "Warehouse name already exists. Enter another name." });

                if (existingWarehouse != null && model.Id != existingWarehouse.Id) // update but duplicate
                    return BadRequest(new { status = false, message = "Warehouse name already exists. Enter another name." });

                if (model.Id == 0) // INSERT
                {
                    // 🔑 Generate next code
                    var lastCodeNum = await _applicationDbContext.tbl_warehouse
                        .Where(w => w.Code != null && w.Code.StartsWith("WH-"))
                        .Select(w => Convert.ToInt32(w.Code.Substring(3)))
                        .DefaultIfEmpty(0)
                        .MaxAsync();

                    var newCode = "WH-" + (lastCodeNum + 1).ToString("D4");

                    var warehouse = new TblWarehouse
                    {
                        Code = newCode,
                        Name = model.Name,
                        EmpId = model.EmpId ?? 0,
                        City = model.City,
                        BuildingName = model.BuildingName,
                        AccountId = model.AccountId ?? 0,
                        State = 0,
                        CreatedBy = model.CreatedBy,
                        CreatedDate = DateOnly.FromDateTime(DateTime.Now)
                    };

                    _applicationDbContext.tbl_warehouse.Add(warehouse);
                    await _applicationDbContext.SaveChangesAsync();

                    return Ok(new { status = true, message = "Warehouse added successfully", code = newCode });
                }
                else // UPDATE
                {
                    var warehouse = await _applicationDbContext.tbl_warehouse.FindAsync(model.Id);
                    if (warehouse == null)
                        return NotFound(new { status = false, message = "Warehouse not found" });

                    warehouse.Name = model.Name;
                    warehouse.EmpId = model.EmpId ?? 0;
                    warehouse.City = model.City;
                    warehouse.BuildingName = model.BuildingName;
                    warehouse.AccountId = model.AccountId ?? 0;

                    await _applicationDbContext.SaveChangesAsync();

                    return Ok(new { status = true, message = "Warehouse updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

    }
}
