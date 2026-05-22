using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using YamyRestaurant.Models;

namespace YamyRestaurant.Controllers
{
    public class CategoryController : Controller
    {

        private readonly IConfiguration _config;
        public CategoryController( IConfiguration config)
        {
            _config = config;
        }

        #region Category CRUD

        public IActionResult Category()
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
        public async Task<IActionResult> GetItemCategories()
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
            SELECT id,name,code
            FROM tbl_item_category
            ORDER BY id DESC";

                List<object> data = new();

                using var cmd =
                    new MySqlCommand(query, conn);

                using var reader =
                    await cmd.ExecuteReaderAsync();

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

        [HttpPost]
        public async Task<IActionResult> SaveCategory([FromBody] CategoryRequest model)
        {
            // ✅ Validation
            if (model == null)
                return BadRequest(new
                {
                    status = false,
                    message = "Invalid request"
                });

            if (string.IsNullOrWhiteSpace(model.CategoryName))
                return BadRequest(new
                {
                    status = false,
                    message = "Please enter category name"
                });

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

                // ✅ Duplicate check
                string duplicateQuery = @"
            SELECT COUNT(*)
            FROM tbl_item_category
            WHERE id <> @id
            AND name = @name";

                using (var checkCmd =
                       new MySqlCommand(duplicateQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", model.Id);
                    checkCmd.Parameters.AddWithValue(
                        "@name",
                        model.CategoryName.Trim());

                    int exists =
                        Convert.ToInt32(
                            await checkCmd.ExecuteScalarAsync());

                    if (exists > 0)
                    {
                        return BadRequest(new
                        {
                            status = false,
                            message = "Category already exists"
                        });
                    }
                }

                // ✅ INSERT
                if (model.Id == 0)
                {
                    int lastCode = 0;

                    string codeQuery = @"
                SELECT code
                FROM tbl_item_category
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

                    // Generate next code
                    string newCode =
                        (lastCode + 1).ToString("D3");

                    // Insert query
                    string insertQuery = @"
                INSERT INTO tbl_item_category
                (code, name)
                VALUES
                (@code, @name)";

                    using (var insertCmd =
                           new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue(
                            "@code",
                            newCode);

                        insertCmd.Parameters.AddWithValue(
                            "@name",
                            model.CategoryName.Trim());

                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new
                    {
                        status = true,
                        message = "Category added successfully",
                        code = newCode
                    });
                }

                // ✅ UPDATE
                else
                {
                    string updateQuery = @"
                UPDATE tbl_item_category
                SET name = @name
                WHERE id = @id";

                    using (var updateCmd =
                           new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue(
                            "@name",
                            model.CategoryName.Trim());

                        updateCmd.Parameters.AddWithValue(
                            "@id",
                            model.Id);

                        int affected =
                            await updateCmd.ExecuteNonQueryAsync();

                        if (affected == 0)
                        {
                            return NotFound(new
                            {
                                status = false,
                                message = "Category not found"
                            });
                        }
                    }

                    return Ok(new
                    {
                        status = true,
                        message = "Category updated successfully"
                    });
                }
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

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id)
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

                // ✅ Check category exists
                string checkQuery = @"
        SELECT COUNT(*)
        FROM tbl_item_category
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
                            message = "Category not found"
                        });
                    }
                }

                // ❌ Check if category is used in menu items
                string usageQuery = @"
        SELECT COUNT(*)
        FROM tbl_menu_item
        WHERE category_id = @id";

                using (var usageCmd = new MySqlCommand(usageQuery, conn))
                {
                    usageCmd.Parameters.AddWithValue("@id", id);

                    int usedCount = Convert.ToInt32(await usageCmd.ExecuteScalarAsync());

                    if (usedCount > 0)
                    {
                        return Json(new
                        {
                            status = false,
                            message = "Cannot delete category. It is used in menu items."
                        });
                    }
                }

                // ✅ Delete category
                string deleteQuery = @"
        DELETE FROM tbl_item_category
        WHERE id = @id";

                using (var deleteCmd = new MySqlCommand(deleteQuery, conn))
                {
                    deleteCmd.Parameters.AddWithValue("@id", id);

                    await deleteCmd.ExecuteNonQueryAsync();
                }

                return Json(new
                {
                    status = true,
                    message = "Category deleted successfully"
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
