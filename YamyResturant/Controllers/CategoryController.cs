using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace YamyRestaurant.Controllers
{
    public class CategoryController : Controller
    {

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public CategoryController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }
        public IActionResult Category()
       {
             return View();
       }

        [HttpGet]
        public async Task<IActionResult> GetItemCategories()
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"SELECT id, name, code
                         FROM tbl_item_category
                         ORDER BY id DESC";

                var data = new List<object>();

                using (var cmd = new MySqlCommand(query, conn))
                {
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        int sn = 1;

                        while (await reader.ReadAsync())
                        {
                            data.Add(new
                            {
                                sn = sn++,
                                id = reader["id"],
                                categoryName = reader["name"],
                                categoryCode = reader["code"]
                            });
                        }
                    }
                }

                return Ok(new { status = true, data });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }
    }
}
