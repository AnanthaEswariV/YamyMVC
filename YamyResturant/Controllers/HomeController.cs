using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using YamyRestaurant.Models;
using YamyResturant.Models;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace YamyResturant.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public IActionResult Home()
        {
            return View();
        }

        #region Menu Item CRUD

        public IActionResult MenuItems()
        {
            return View();
        }
        private async Task<MySqlConnection?> OpenConnectionAsync()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            string? databaseName =
                HttpContext.Session.GetString("DatabaseName");

            if (userId <= 0 || string.IsNullOrEmpty(databaseName))
            {
                return null;
            }

            string connStr =
                _config.GetConnectionString("DefaultConnection");

            var builder =
                new MySqlConnectionStringBuilder(connStr)
                {
                    Database = databaseName
                };

            var conn =
                new MySqlConnection(builder.ConnectionString);

            await conn.OpenAsync();

            return conn;
        }


        [HttpGet]
        public async Task<IActionResult> GetMenuItems()
        {
            try
            {
                using var conn = await OpenConnectionAsync();

                if (conn == null)
                {
                    return Json(new
                    {
                        status = false,
                        message = "Session expired"
                    });
                }

                string query = @"
        SELECT 
            m.id,
            m.code,
            m.category_id,
            c.name AS category_name,
            m.subcategory_name,
            m.meal_times,
            m.price,
            m.is_active
        FROM tbl_menu_item m
        LEFT JOIN tbl_item_category c 
            ON c.id = m.category_id
        ORDER BY m.id DESC";

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
                        code = reader["code"]?.ToString(),
                        categoryId = reader["category_id"],
                        categoryName = reader["category_name"]?.ToString(),
                        subCategoryName = reader["subcategory_name"]?.ToString(),
                        mealTimes = reader["meal_times"]?.ToString(),
                        price = reader["price"],
                        isActive = reader["is_active"]
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

        // ===== SAVE MENU ITEM =====
        [HttpPost]
        public async Task<IActionResult> SaveMenuItem(
            [FromBody] MenuItemRequest model)
        {
            try
            {
                // Validation
                if (model == null)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Invalid request"
                    });
                }

                if (model.CategoryId <= 0)
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Please select category"
                    });
                }

                if (string.IsNullOrWhiteSpace(model.SubCategoryName))
                {
                    return BadRequest(new
                    {
                        status = false,
                        message = "Please enter subcategory"
                    });
                }

                using var conn = await OpenConnectionAsync();

                if (conn == null)
                {
                    return Json(new
                    {
                        status = false,
                        message = "Session expired"
                    });
                }

                // Duplicate Check
                string duplicateQuery = @"
        SELECT COUNT(*)
        FROM tbl_menu_item
        WHERE id <> @id
        AND category_id = @categoryId
        AND subcategory_name = @name";

                using (var checkCmd =
                       new MySqlCommand(duplicateQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", model.Id);
                    checkCmd.Parameters.AddWithValue("@categoryId", model.CategoryId);
                    checkCmd.Parameters.AddWithValue(
                        "@name",
                        model.SubCategoryName.Trim());

                    int exists =
                        Convert.ToInt32(
                            await checkCmd.ExecuteScalarAsync());

                    if (exists > 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Menu item already exists"
                        });
                    }
                }

                // Convert meals to comma string
                string mealTimes =
                    model.MealTimes != null
                    ? string.Join(",", model.MealTimes)
                    : "";

                // ===== INSERT =====
                if (model.Id == 0)
                {
                    int lastCode = 0;

                    string codeQuery = @"
            SELECT code
            FROM tbl_menu_item
            ORDER BY CAST(code AS UNSIGNED) DESC
            LIMIT 1";

                    using (var codeCmd =
                           new MySqlCommand(codeQuery, conn))

                    using (var reader =
                           await codeCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            if (reader["code"] != DBNull.Value)
                            {
                                lastCode =
                                    Convert.ToInt32(reader["code"]);
                            }
                        }
                    }

                    string newCode =
                        (lastCode + 1).ToString("D3");

                    string insertQuery = @"
            INSERT INTO tbl_menu_item
            (
                code,
                category_id,
                subcategory_name,
                meal_times,
                price,
                is_active
            )
            VALUES
            (
                @code,
                @categoryId,
                @name,
                @mealTimes,
                @price,
                @isActive
            )";

                    using (var insertCmd =
                           new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@code", newCode);
                        insertCmd.Parameters.AddWithValue("@categoryId", model.CategoryId);
                        insertCmd.Parameters.AddWithValue("@name", model.SubCategoryName.Trim());
                        insertCmd.Parameters.AddWithValue("@mealTimes", mealTimes);
                        insertCmd.Parameters.AddWithValue("@price", model.Price);
                        insertCmd.Parameters.AddWithValue("@isActive", model.IsActive);

                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new
                    {
                        status = true,
                        message = "Menu item added successfully"
                    });
                }

                // ===== UPDATE =====
                else
                {
                    string updateQuery = @"
            UPDATE tbl_menu_item
            SET
                category_id = @categoryId,
                subcategory_name = @name,
                meal_times = @mealTimes,
                price = @price,
                is_active = @isActive
            WHERE id = @id";

                    using (var updateCmd =
                           new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@id", model.Id);
                        updateCmd.Parameters.AddWithValue("@categoryId", model.CategoryId);
                        updateCmd.Parameters.AddWithValue("@name", model.SubCategoryName.Trim());
                        updateCmd.Parameters.AddWithValue("@mealTimes", mealTimes);
                        updateCmd.Parameters.AddWithValue("@price", model.Price);
                        updateCmd.Parameters.AddWithValue("@isActive", model.IsActive);

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new
                    {
                        status = true,
                        message = "Menu item updated successfully"
                    });
                }
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

        [HttpPost]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            try
            {
                using var conn = await OpenConnectionAsync();

                if (conn == null)
                {
                    return Json(new
                    {
                        status = false,
                        message = "Session expired"
                    });
                }

                // Check exists
                string checkQuery = @"
        SELECT COUNT(*)
        FROM tbl_menu_item
        WHERE id = @id";

                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", id);

                    int exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                    if (exists == 0)
                    {
                        return Json(new
                        {
                            status = false,
                            message = "Menu item not found"
                        });
                    }
                }

                // Delete
                string deleteQuery = @"
        DELETE FROM tbl_menu_item
        WHERE id = @id";

                using (var deleteCmd = new MySqlCommand(deleteQuery, conn))
                {
                    deleteCmd.Parameters.AddWithValue("@id", id);

                    await deleteCmd.ExecuteNonQueryAsync();
                }

                return Json(new
                {
                    status = true,
                    message = "Menu item deleted successfully"
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

        #endregion
    }
}
