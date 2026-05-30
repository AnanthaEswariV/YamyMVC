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
                    return Json(new { status = false, message = "Session expired" });

                string query = @"
        SELECT m.*, c.name AS category_name
        FROM tbl_menu_item m
        LEFT JOIN tbl_item_category c ON c.id = m.category_id
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
                        isActive = reader["is_active"],
                        image = reader["image"]?.ToString()
                    });
                }

                return Json(new { status = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveMenuItem([FromForm] MenuItemRequest model)
        {
            try
            {
                if (model == null)
                    return Json(new { status = false, message = "Invalid request" });

                using var conn = await OpenConnectionAsync();
                if (conn == null)
                    return Json(new { status = false, message = "Session expired" });

                // ================= DUPLICATE CHECK =================
                string duplicateQuery = @"
        SELECT COUNT(*) 
        FROM tbl_menu_item
        WHERE id <> @id
        AND category_id = @categoryId
        AND subcategory_name = @name";

                using (var cmd = new MySqlCommand(duplicateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", model.Id);
                    cmd.Parameters.AddWithValue("@categoryId", model.CategoryId);
                    cmd.Parameters.AddWithValue("@name", model.SubCategoryName.Trim());

                    if (Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0)
                        return Json(new { status = false, message = "Already exists" });
                }

                // ================= MEAL TIMES (FIXED) =================
                string mealTimes = model.MealTimesRaw ?? "[]";

                // ================= IMAGE UPLOAD =================
                string imageName = null;

                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/menu");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    imageName = $"{Guid.NewGuid()}{Path.GetExtension(model.ImageFile.FileName)}";

                    string path = Path.Combine(folder, imageName);

                    using var stream = new FileStream(path, FileMode.Create);
                    await model.ImageFile.CopyToAsync(stream);
                }

                // ================= INSERT =================
                if (model.Id == 0)
                {
                    string codeQuery = @"SELECT IFNULL(MAX(CAST(code AS UNSIGNED)),0) FROM tbl_menu_item";
                    int lastCode = 0;

                    using (var cmd = new MySqlCommand(codeQuery, conn))
                        lastCode = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    string newCode = (lastCode + 1).ToString("D3");

                    string insertQuery = @"
            INSERT INTO tbl_menu_item
            (code, category_id, subcategory_name, meal_times, price, is_active, image)
            VALUES
            (@code, @categoryId, @name, @mealTimes, @price, @isActive, @image)";

                    using var cmdIns = new MySqlCommand(insertQuery, conn);

                    cmdIns.Parameters.AddWithValue("@code", newCode);
                    cmdIns.Parameters.AddWithValue("@categoryId", model.CategoryId);
                    cmdIns.Parameters.AddWithValue("@name", model.SubCategoryName);
                    cmdIns.Parameters.AddWithValue("@mealTimes", mealTimes);
                    cmdIns.Parameters.AddWithValue("@price", model.Price);
                    cmdIns.Parameters.AddWithValue("@isActive", model.IsActive);
                    cmdIns.Parameters.AddWithValue("@image", imageName);

                    await cmdIns.ExecuteNonQueryAsync();

                    return Json(new { status = true, message = "Added successfully" });
                }

                // ================= UPDATE =================
                string updateQuery = imageName != null
                ? @"UPDATE tbl_menu_item SET 
            category_id=@categoryId,
            subcategory_name=@name,
            meal_times=@mealTimes,
            price=@price,
            is_active=@isActive,
            image=@image
            WHERE id=@id"
                : @"UPDATE tbl_menu_item SET 
            category_id=@categoryId,
            subcategory_name=@name,
            meal_times=@mealTimes,
            price=@price,
            is_active=@isActive
            WHERE id=@id";

                using var cmdUp = new MySqlCommand(updateQuery, conn);

                cmdUp.Parameters.AddWithValue("@id", model.Id);
                cmdUp.Parameters.AddWithValue("@categoryId", model.CategoryId);
                cmdUp.Parameters.AddWithValue("@name", model.SubCategoryName);
                cmdUp.Parameters.AddWithValue("@mealTimes", mealTimes);
                cmdUp.Parameters.AddWithValue("@price", model.Price);
                cmdUp.Parameters.AddWithValue("@isActive", model.IsActive);

                if (imageName != null)
                    cmdUp.Parameters.AddWithValue("@image", imageName);

                await cmdUp.ExecuteNonQueryAsync();

                return Json(new { status = true, message = "Updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
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

        #region Table CRUD

        public IActionResult Table()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetRestaurantTables()
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
        SELECT *
        FROM tbl_restaurant_table
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
                        code = reader["code"]?.ToString(),
                        tableName = reader["table_name"]?.ToString(),
                        capacity = reader["capacity"],
                        location = reader["location"]?.ToString(),
                        statusText = reader["status"]?.ToString(),
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

        [HttpPost]
        public async Task<IActionResult> SaveRestaurantTable(TableRequest model)
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

                // DUPLICATE CHECK
                string duplicateQuery = @"
        SELECT COUNT(*)
        FROM tbl_restaurant_table
        WHERE id <> @id
        AND table_name = @tableName";

                using (var checkCmd = new MySqlCommand(duplicateQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@id", model.Id);
                    checkCmd.Parameters.AddWithValue("@tableName", model.TableName);

                    int exists =
                        Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                    if (exists > 0)
                    {
                        return Json(new
                        {
                            status = false,
                            message = "Table already exists"
                        });
                    }
                }

                // INSERT
                if (model.Id == 0)
                {
                    string codeQuery = @"
            SELECT IFNULL(MAX(CAST(code AS UNSIGNED)),0)
            FROM tbl_restaurant_table";

                    int lastCode = 0;

                    using (var cmd = new MySqlCommand(codeQuery, conn))
                    {
                        lastCode =
                            Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    string newCode = (lastCode + 1).ToString("D3");

                    string insertQuery = @"
            INSERT INTO tbl_restaurant_table
            (
                code,
                table_name,
                capacity,
                location,
                status,
                is_active
            )
            VALUES
            (
                @code,
                @tableName,
                @capacity,
                @location,
                @status,
                @isActive
            )";

                    using var insertCmd =
                        new MySqlCommand(insertQuery, conn);

                    insertCmd.Parameters.AddWithValue("@code", newCode);
                    insertCmd.Parameters.AddWithValue("@tableName", model.TableName);
                    insertCmd.Parameters.AddWithValue("@capacity", model.Capacity);
                    insertCmd.Parameters.AddWithValue("@location", model.Location);
                    insertCmd.Parameters.AddWithValue("@status", model.Status);
                    insertCmd.Parameters.AddWithValue("@isActive", model.IsActive);

                    await insertCmd.ExecuteNonQueryAsync();

                    return Json(new
                    {
                        status = true,
                        message = "Table added successfully"
                    });
                }

                // UPDATE
                string updateQuery = @"
        UPDATE tbl_restaurant_table
        SET
            table_name=@tableName,
            capacity=@capacity,
            location=@location,
            status=@status,
            is_active=@isActive
        WHERE id=@id";

                using var updateCmd =
                    new MySqlCommand(updateQuery, conn);

                updateCmd.Parameters.AddWithValue("@id", model.Id);
                updateCmd.Parameters.AddWithValue("@tableName", model.TableName);
                updateCmd.Parameters.AddWithValue("@capacity", model.Capacity);
                updateCmd.Parameters.AddWithValue("@location", model.Location);
                updateCmd.Parameters.AddWithValue("@status", model.Status);
                updateCmd.Parameters.AddWithValue("@isActive", model.IsActive);

                await updateCmd.ExecuteNonQueryAsync();

                return Json(new
                {
                    status = true,
                    message = "Table updated successfully"
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
        public async Task<IActionResult> DeleteRestaurantTable(int id)
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
        DELETE FROM tbl_restaurant_table
        WHERE id=@id";

                using var cmd = new MySqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();

                return Json(new
                {
                    status = true,
                    message = "Table deleted successfully"
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

        #region Orders

        public IActionResult Orders()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableTables()
        {
            try
            {
                using var conn = await OpenConnectionAsync();

                string query = @"
        SELECT id, table_name
        FROM tbl_restaurant_table
        WHERE is_active = 1";

                List<object> data = new();

                using var cmd = new MySqlCommand(query, conn);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    data.Add(new
                    {
                        id = reader["id"],
                        tableName = reader["table_name"]?.ToString()
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
        [HttpGet]
        public async Task<IActionResult> GetOrderMenuItems()
        {
            try
            {
                using var conn = await OpenConnectionAsync();

                string query = @"
        SELECT 
            id,
            subcategory_name,
            price,
            image
        FROM tbl_menu_item
        WHERE is_active = 1";

                List<object> data = new();

                using var cmd = new MySqlCommand(query, conn);

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    data.Add(new
                    {
                        id = reader["id"],
                        name = reader["subcategory_name"]?.ToString(),
                        price = reader["price"],
                        image = reader["image"]?.ToString()
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
        public async Task<IActionResult> SaveOrder(
    [FromBody] OrderRequest model)
        {
            try
            {
                using var conn = await OpenConnectionAsync();

                using var transaction =
                    await conn.BeginTransactionAsync();

                // ORDER NUMBER
                string orderNo = "";

                string orderQuery =
                @"SELECT IFNULL(MAX(id),0)+1 FROM tbl_order";

                using (var cmd =
                    new MySqlCommand(orderQuery, conn))
                {
                    cmd.Transaction =
                        (MySqlTransaction)transaction;

                    int nextId =
                        Convert.ToInt32(
                            await cmd.ExecuteScalarAsync());

                    orderNo = "ORD" + nextId.ToString("D5");
                }

                // INSERT ORDER
                string insertOrder = @"
        INSERT INTO tbl_order
        (
            order_no,
            table_id,
            customer_name,
            customer_mobile,
            total_amount,
            discount_amount,
            tax_amount,
            grand_total,
            payment_status,
            order_status
        )
        VALUES
        (
            @orderNo,
            @tableId,
            @customerName,
            @customerMobile,
            @totalAmount,
            @discountAmount,
            @taxAmount,
            @grandTotal,
            @paymentStatus,
            @orderStatus
        );

        SELECT LAST_INSERT_ID();";

                int orderId = 0;

                using (var cmd =
                    new MySqlCommand(insertOrder, conn))
                {
                    cmd.Transaction =
                        (MySqlTransaction)transaction;

                    cmd.Parameters.AddWithValue("@orderNo", orderNo);
                    cmd.Parameters.AddWithValue("@tableId", model.TableId);
                    cmd.Parameters.AddWithValue("@customerName", model.CustomerName);
                    cmd.Parameters.AddWithValue("@customerMobile", model.CustomerMobile);
                    cmd.Parameters.AddWithValue("@totalAmount", model.TotalAmount);
                    cmd.Parameters.AddWithValue("@discountAmount", model.DiscountAmount);
                    cmd.Parameters.AddWithValue("@taxAmount", model.TaxAmount);
                    cmd.Parameters.AddWithValue("@grandTotal", model.GrandTotal);
                    cmd.Parameters.AddWithValue("@paymentStatus", "Pending");
                    cmd.Parameters.AddWithValue("@orderStatus", "Running");

                    orderId =
                        Convert.ToInt32(
                            await cmd.ExecuteScalarAsync());
                }

                // INSERT ITEMS
                foreach (var item in model.Items)
                {
                    string detailQuery = @"
            INSERT INTO tbl_order_details
            (
                order_id,
                menu_item_id,
                item_name,
                price,
                qty,
                amount
            )
            VALUES
            (
                @orderId,
                @menuItemId,
                @itemName,
                @price,
                @qty,
                @amount
            )";

                    using var detailCmd =
                        new MySqlCommand(detailQuery, conn);

                    detailCmd.Transaction =
                        (MySqlTransaction)transaction;

                    detailCmd.Parameters.AddWithValue("@orderId", orderId);
                    detailCmd.Parameters.AddWithValue("@menuItemId", item.MenuItemId);
                    detailCmd.Parameters.AddWithValue("@itemName", item.ItemName);
                    detailCmd.Parameters.AddWithValue("@price", item.Price);
                    detailCmd.Parameters.AddWithValue("@qty", item.Qty);
                    detailCmd.Parameters.AddWithValue("@amount", item.Amount);

                    await detailCmd.ExecuteNonQueryAsync();
                }

                // UPDATE TABLE STATUS
                string updateTable = @"
        UPDATE tbl_restaurant_table
        SET status='Occupied'
        WHERE id=@tableId";

                using (var tableCmd =
                    new MySqlCommand(updateTable, conn))
                {
                    tableCmd.Transaction =
                        (MySqlTransaction)transaction;

                    tableCmd.Parameters.AddWithValue(
                        "@tableId",
                        model.TableId);

                    await tableCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return Json(new
                {
                    status = true,
                    message = "Order saved successfully"
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

        [HttpGet]
        public async Task<IActionResult> GetOrderCustomers()
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
            id,
            code,
            name,
            mobile
        FROM tbl_customer
        WHERE active = 0
        ORDER BY name";

                List<object> data = new();

                using var cmd =
                    new MySqlCommand(query, conn);

                using var reader =
                    await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    data.Add(new
                    {
                        id = reader["id"],
                        code = reader["code"]?.ToString(),
                        name = reader["name"]?.ToString(),
                        mobile = reader["mobile"]?.ToString()
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

        #endregion
    }
}
