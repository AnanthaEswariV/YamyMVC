using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace YamyRestaurant.Controllers
{
    public class CategoryController : Controller
    {

        private readonly IConfiguration _config;
        public CategoryController( IConfiguration config)
        {
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
                int? userId = HttpContext.Session.GetInt32("UserId");
                string? databaseName = HttpContext.Session.GetString("DatabaseName");

                if (userId == null || userId <= 0)
                {
                    return Json(new
                    {
                        status = false,
                        message = "Session expired"
                    });
                }

                if (string.IsNullOrEmpty(databaseName))
                {
                    return Json(new
                    {
                        status = false,
                        message = "Database session missing"
                    });
                }

                // ✅ Get base connection
                string connStr = _config.GetConnectionString("DefaultConnection");

                if (string.IsNullOrEmpty(connStr))
                {
                    return Json(new
                    {
                        status = false,
                        message = "Connection string missing"
                    });
                }

                // ✅ Change only database dynamically
                var builder = new MySqlConnectionStringBuilder(connStr)
                {
                    Database = databaseName
                };

                using var conn = new MySqlConnection(builder.ConnectionString);

                await conn.OpenAsync();

                string query = @"
            SELECT id,name,code
            FROM tbl_item_category
            ORDER BY id DESC";

                List<object> data = new();

                using var cmd = new MySqlCommand(query, conn);

                using var reader = await cmd.ExecuteReaderAsync();

                int sn = 1;

                while (await reader.ReadAsync())
                {
                    data.Add(new
                    {
                        sn = sn++,
                        id = reader["id"],
                        categoryCode = reader["code"]?.ToString(),
                        categoryName = reader["name"]?.ToString()
                    });
                }

                return Json(new
                {
                    status = true,
                    data
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = ex.Message
                });
            }
        }
    }
}
