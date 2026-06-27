using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using YamyRestaurant.Models;


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
    WHERE is_active = 1
    AND status = 'Available'";

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

                return Json(new { status = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
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
                        subcategory_name as name,
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
                        name = reader["name"]?.ToString(),
                        price = reader["price"],
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
        public async Task<IActionResult> SaveOrder([FromBody] OrderRequest model)
        {
            try
            {
                using var conn = await OpenConnectionAsync();
                using var transaction = await conn.BeginTransactionAsync();

                // 1. PREVENT BOOKING AN OCCUPIED TABLE (must run BEFORE any insert)
                if (model.TableId.HasValue && model.TableId > 0)
                {
                    string checkTable = @"
                SELECT status 
                FROM tbl_restaurant_table 
                WHERE id = @tableId";

                    using var checkCmd = new MySqlCommand(checkTable, conn);
                    checkCmd.Transaction = (MySqlTransaction)transaction;
                    checkCmd.Parameters.AddWithValue("@tableId", model.TableId);
                    var tableStatus = (await checkCmd.ExecuteScalarAsync())?.ToString();

                    if (tableStatus != "Available")
                    {
                        await transaction.RollbackAsync();
                        return Json(new { status = false, message = "This table is already occupied. Please select another table." });
                    }
                }

                // 2. GENERATE ORDER NUMBER
                string orderNo = "";
                string orderQuery = @"SELECT IFNULL(MAX(id),0)+1 FROM tbl_order";

                using (var cmd = new MySqlCommand(orderQuery, conn))
                {
                    cmd.Transaction = (MySqlTransaction)transaction;
                    int nextId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    orderNo = "ORD" + nextId.ToString("D5");
                }

                // 3. INSERT ORDER
                string insertOrder = @"
            INSERT INTO tbl_order
            (
                order_no,
                order_type,
                table_id,
                customer_name,
                customer_mobile,
                total_amount,
                discount_amount,
                tax_amount,
                grand_total,
                payment_status,
                order_status,
                created_date
            )
            VALUES
            (
                @orderNo,
                @orderType,
                @tableId,
                @customerName,
                @customerMobile,
                @totalAmount,
                @discountAmount,
                @taxAmount,
                @grandTotal,
                @paymentStatus,
                @orderStatus,
                @createdDate
            );
            SELECT LAST_INSERT_ID();";

                int orderId = 0;

                using (var cmd = new MySqlCommand(insertOrder, conn))
                {
                    cmd.Transaction = (MySqlTransaction)transaction;
                    cmd.Parameters.AddWithValue("@orderNo", orderNo);
                    cmd.Parameters.AddWithValue("@orderType", model.OrderType);
                    cmd.Parameters.AddWithValue("@tableId", model.TableId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@customerName", model.CustomerName ?? "");
                    cmd.Parameters.AddWithValue("@customerMobile", model.CustomerMobile ?? "");
                    cmd.Parameters.AddWithValue("@totalAmount", model.TotalAmount);
                    cmd.Parameters.AddWithValue("@discountAmount", model.DiscountAmount);
                    cmd.Parameters.AddWithValue("@taxAmount", model.TaxAmount);
                    cmd.Parameters.AddWithValue("@grandTotal", model.GrandTotal);
                    cmd.Parameters.AddWithValue("@kitchenStatus", "New");
                    cmd.Parameters.AddWithValue("@paymentStatus", "Pending");
                    cmd.Parameters.AddWithValue("@orderStatus", "Running");
                    cmd.Parameters.AddWithValue("@createdDate", DateTime.Now);

                    orderId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // 4. INSERT ORDER ITEMS
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

                    using var detailCmd = new MySqlCommand(detailQuery, conn);
                    detailCmd.Transaction = (MySqlTransaction)transaction;
                    detailCmd.Parameters.AddWithValue("@orderId", orderId);
                    detailCmd.Parameters.AddWithValue("@menuItemId", item.MenuItemId);
                    detailCmd.Parameters.AddWithValue("@itemName", item.ItemName);
                    detailCmd.Parameters.AddWithValue("@price", item.Price);
                    detailCmd.Parameters.AddWithValue("@qty", item.Qty);
                    detailCmd.Parameters.AddWithValue("@amount", item.Amount);

                    await detailCmd.ExecuteNonQueryAsync();
                }

                // 5. MARK TABLE AS OCCUPIED (this was missing!)
                if (model.TableId.HasValue && model.TableId > 0)
                {
                    string updateTable = @"
                UPDATE tbl_restaurant_table
                SET status = 'Occupied'
                WHERE id = @tableId";

                    using var tableCmd = new MySqlCommand(updateTable, conn);
                    tableCmd.Transaction = (MySqlTransaction)transaction;
                    tableCmd.Parameters.AddWithValue("@tableId", model.TableId);
                    await tableCmd.ExecuteNonQueryAsync();
                }

                // 6. COMMIT
                await transaction.CommitAsync();

                return Json(new
                {
                    status = true,
                    message = "Order saved successfully",
                    orderId = orderId,
                    orderNo = orderNo
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                using var conn = await OpenConnectionAsync();

                string query = @"
                    SELECT 
                        o.id,
                        o.order_no as orderNo,
                        o.customer_name as customerName,
                        o.customer_mobile as customerMobile,
                        o.total_amount as totalAmount,
                        o.grand_total as grandTotal,
                        o.payment_status as paymentStatus,
                        o.order_status as orderStatus,
                        o.order_type as orderType,
                        t.table_name as tableName,
                        COUNT(od.id) as itemCount
                    FROM tbl_order o
                    LEFT JOIN tbl_restaurant_table t ON o.table_id = t.id
                    LEFT JOIN tbl_order_details od ON o.id = od.order_id
                    GROUP BY o.id, o.order_no, o.customer_name, o.customer_mobile, o.total_amount, 
                             o.grand_total, o.payment_status, o.order_status, o.order_type, t.table_name
                    ORDER BY o.id DESC
                    LIMIT 100";

                List<object> data = new();

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    data.Add(new
                    {
                        id = reader["id"],
                        orderNo = reader["orderNo"]?.ToString(),
                        customerName = reader["customerName"]?.ToString(),
                        customerMobile = reader["customerMobile"]?.ToString(),
                        totalAmount = reader["totalAmount"],
                        grandTotal = reader["grandTotal"],
                        paymentStatus = reader["paymentStatus"]?.ToString(),
                        orderStatus = reader["orderStatus"]?.ToString(),
                        orderType = reader["orderType"]?.ToString() ?? "DineIn",
                        tableName = reader["tableName"]?.ToString() ?? "N/A",
                        itemCount = reader["itemCount"]
                    });
                }

                return Json(new { status = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            try
            {
                using var conn = await OpenConnectionAsync();

                // Get order header
                string orderQuery = @"
                    SELECT 
                        o.id,
                        o.order_no as orderNo,
                        o.customer_name as customerName,
                        o.customer_mobile as customerMobile,
                        o.total_amount as totalAmount,
                        o.discount_amount as discountAmount,
                        o.tax_amount as taxAmount,
                        o.grand_total as grandTotal,
                        o.payment_status as paymentStatus,
                        o.order_status as orderStatus,
                        o.order_type as orderType,
                        t.table_name as tableName
                    FROM tbl_order o
                    LEFT JOIN tbl_restaurant_table t ON o.table_id = t.id
                    WHERE o.id = @orderId";

                object orderData = null;

                using (var cmd = new MySqlCommand(orderQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@orderId", id);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        orderData = new
                        {
                            id = reader["id"],
                            orderNo = reader["orderNo"]?.ToString(),
                            customerName = reader["customerName"]?.ToString(),
                            customerMobile = reader["customerMobile"]?.ToString(),
                            totalAmount = reader["totalAmount"],
                            discountAmount = reader["discountAmount"],
                            taxAmount = reader["taxAmount"],
                            grandTotal = reader["grandTotal"],
                            paymentStatus = reader["paymentStatus"]?.ToString(),
                            orderStatus = reader["orderStatus"]?.ToString(),
                            orderType = reader["orderType"]?.ToString(),
                            tableName = reader["tableName"]?.ToString()
                        };
                    }
                }

                // Get order items
                string itemsQuery = @"
                    SELECT 
                        id,
                        menu_item_id as menuItemId,
                        item_name as itemName,
                        price,
                        qty,
                        amount
                    FROM tbl_order_details
                    WHERE order_id = @orderId";

                List<object> items = new();

                using (var cmd = new MySqlCommand(itemsQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@orderId", id);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        items.Add(new
                        {
                            id = reader["id"],
                            menuItemId = reader["menuItemId"],
                            itemName = reader["itemName"]?.ToString(),
                            price = reader["price"],
                            qty = reader["qty"],
                            amount = reader["amount"]
                        });
                    }
                }

                return Json(new
                {
                    status = true,
                    data = new { order = orderData, items }
                });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusRequest model)
        {
            try
            {
                using var conn = await OpenConnectionAsync();

                // RULE: cannot complete unless payment is Paid
                if (model.Status == "Completed")
                {
                    string payQuery = "SELECT payment_status FROM tbl_order WHERE id = @orderId";
                    using var payCmd = new MySqlCommand(payQuery, conn);
                    payCmd.Parameters.AddWithValue("@orderId", model.OrderId);
                    var paymentStatus = (await payCmd.ExecuteScalarAsync())?.ToString();

                    if (paymentStatus != "Paid")
                    {
                        return Json(new { status = false, message = "Order can only be completed after payment is marked as Paid." });
                    }
                }

                string query = @"
            UPDATE tbl_order
            SET order_status = @status
            WHERE id = @orderId";

                int affected;
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@status", model.Status);
                    cmd.Parameters.AddWithValue("@orderId", model.OrderId);
                    affected = await cmd.ExecuteNonQueryAsync();
                }

                if (affected == 0)
                    return Json(new { status = false, message = "Order not found" });

                // FREE THE TABLE ON COMPLETE / CANCEL
                if (model.Status == "Completed" || model.Status == "Cancelled")
                {
                    string freeTable = @"
                UPDATE tbl_restaurant_table t
                INNER JOIN tbl_order o ON o.table_id = t.id
                SET t.status = 'Available'
                WHERE o.id = @orderId";

                    using var freeCmd = new MySqlCommand(freeTable, conn);
                    freeCmd.Parameters.AddWithValue("@orderId", model.OrderId);
                    await freeCmd.ExecuteNonQueryAsync();
                }

                return Json(new { status = true, message = "Order status updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }
        [HttpPost]
        public async Task<IActionResult> DeleteOrder([FromBody] DeleteOrderRequest model)
        {
            try
            {
                using var conn = await OpenConnectionAsync();
                using var transaction = await conn.BeginTransactionAsync();

                // Delete order details
                string deleteDetailsQuery = "DELETE FROM tbl_order_details WHERE order_id = @orderId";
                using (var cmd = new MySqlCommand(deleteDetailsQuery, conn))
                {
                    cmd.Transaction = (MySqlTransaction)transaction;
                    cmd.Parameters.AddWithValue("@orderId", model.OrderId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // Delete order
                string deleteOrderQuery = "DELETE FROM tbl_order WHERE id = @orderId";
                using (var cmd = new MySqlCommand(deleteOrderQuery, conn))
                {
                    cmd.Transaction = (MySqlTransaction)transaction;
                    cmd.Parameters.AddWithValue("@orderId", model.OrderId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return Json(new { status = true, message = "Order deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
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
                    return Json(new { status = false, message = "Session expired" });
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

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

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

                return Json(new { status = true, data });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveCustomer([FromBody] CustomerRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid request" });

            // Basic validations
            if (string.IsNullOrWhiteSpace(model.Name))
                return Json(new { status = false, message = "Please enter customer name" });

            if (model.CategoryId == null || model.CategoryId <= 0)
                return Json(new { status = false, message = "Please select customer category" });

            if (model.CountryId == null || model.CountryId <= 0)
                return Json(new { status = false, message = "Please select country" });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in" });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            try
            {
                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check duplicate customer
                var dupQuery = "SELECT id FROM tbl_customer WHERE name=@name";
                using (var dupCmd = new MySqlCommand(dupQuery, conn))
                {
                    dupCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    var existingId = await dupCmd.ExecuteScalarAsync();
                    if (existingId != null && (model.Id == 0 || Convert.ToInt32(existingId) != model.Id))
                    {
                        return Json(new { status = false, message = "Customer already exists. Enter another name." });
                    }
                }

                string formattedCode = model.Code;

                if (model.Id == 0) // INSERT
                {
                    // Generate next customer code
                    int lastCode = 0;
                    var codeQuery = "SELECT MAX(CAST(code AS UNSIGNED)) FROM tbl_customer";
                    using (var codeCmd = new MySqlCommand(codeQuery, conn))
                    {
                        var result = await codeCmd.ExecuteScalarAsync();
                        if (result != DBNull.Value && result != null)
                            lastCode = Convert.ToInt32(result);
                    }
                    formattedCode = (lastCode + 1).ToString("D5");

                    // Insert customer
                    var insertQuery = @"INSERT INTO tbl_customer 
                        (code, name, cat_id, mobile, email, country, active, created_by, created_date)
                        VALUES(@code, @name, @cat_id, @mobile, @email, @country, @active, @created_by, @created_date);
                        SELECT LAST_INSERT_ID();";

                    using var insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@code", formattedCode);
                    insertCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    insertCmd.Parameters.AddWithValue("@cat_id", model.CategoryId);
                    insertCmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
                    insertCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                    insertCmd.Parameters.AddWithValue("@country", model.CountryId);
                    insertCmd.Parameters.AddWithValue("@active", model.Active ? 0 : -1);
                    insertCmd.Parameters.AddWithValue("@created_by", userId);
                    insertCmd.Parameters.AddWithValue("@created_date", DateTime.Now);

                    var customerId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                    return Json(new
                    {
                        status = true,
                        message = "Customer added successfully",
                        customerId = customerId,
                        code = formattedCode
                    });
                }
                else // UPDATE
                {
                    var updateQuery = @"UPDATE tbl_customer SET 
                            code=@code, name=@name, cat_id=@cat_id, mobile=@mobile, email=@email,
                            country=@country, active=@active
                        WHERE id=@id";

                    using var updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@code", model.Code);
                    updateCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    updateCmd.Parameters.AddWithValue("@cat_id", model.CategoryId);
                    updateCmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
                    updateCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                    updateCmd.Parameters.AddWithValue("@country", model.CountryId);
                    updateCmd.Parameters.AddWithValue("@active", model.Active ? 0 : -1);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);

                    await updateCmd.ExecuteNonQueryAsync();

                    return Json(new { status = true, message = "Customer updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrder([FromBody] UpdateOrderRequest model)
        {
            try
            {
                using var conn = await OpenConnectionAsync();

                // RULE: Completed only allowed when payment is Paid
                if (model.OrderStatus == "Completed" && model.PaymentStatus != "Paid")
                {
                    return Json(new { status = false, message = "Order can only be completed after payment is marked as Paid." });
                }

                string query = @"
            UPDATE tbl_order
            SET 
                customer_name = @customerName,
                customer_mobile = @customerMobile,
                order_status = @orderStatus,
                payment_status = @paymentStatus,
                discount_amount = @discountAmount,
                grand_total = @grand_total,
                tax_amount = @taxAmount
            WHERE id = @orderId";

                int affected;
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@customerName", model.CustomerName ?? "");
                    cmd.Parameters.AddWithValue("@customerMobile", model.CustomerMobile ?? "");
                    cmd.Parameters.AddWithValue("@orderStatus", model.OrderStatus);
                    cmd.Parameters.AddWithValue("@paymentStatus", model.PaymentStatus);
                    cmd.Parameters.AddWithValue("@discountAmount", model.DiscountAmount);
                    cmd.Parameters.AddWithValue("@taxAmount", model.TaxAmount);
                    cmd.Parameters.AddWithValue("@grand_total", model.GrandTotal);
                    cmd.Parameters.AddWithValue("@orderId", model.OrderId);

                    affected = await cmd.ExecuteNonQueryAsync();
                }

                if (affected == 0)
                    return Json(new { status = false, message = "Order not found" });

                // FREE THE TABLE WHEN ORDER IS COMPLETED OR CANCELLED
                if (model.OrderStatus == "Completed" || model.OrderStatus == "Cancelled")
                {
                    string freeTable = @"
                UPDATE tbl_restaurant_table t
                INNER JOIN tbl_order o ON o.table_id = t.id
                SET t.status = 'Available'
                WHERE o.id = @orderId";

                    using var freeCmd = new MySqlCommand(freeTable, conn);
                    freeCmd.Parameters.AddWithValue("@orderId", model.OrderId);
                    await freeCmd.ExecuteNonQueryAsync();
                }

                return Json(new { status = true, message = "Order updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TransferTable([FromBody] TransferTableRequest model)
        {
            try
            {
                using var conn = await OpenConnectionAsync();
                using var transaction = await conn.BeginTransactionAsync();

                // 1. Get the order + its current table, and make sure it's a running dine-in order
                int? oldTableId = null;
                string orderStatus = "";
                string orderType = "";

                string getOrder = @"SELECT table_id, order_status, order_type FROM tbl_order WHERE id = @orderId";
                using (var cmd = new MySqlCommand(getOrder, conn))
                {
                    cmd.Transaction = (MySqlTransaction)transaction;
                    cmd.Parameters.AddWithValue("@orderId", model.OrderId);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        oldTableId = reader["table_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["table_id"]);
                        orderStatus = reader["order_status"]?.ToString() ?? "";
                        orderType = reader["order_type"]?.ToString() ?? "";
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        return Json(new { status = false, message = "Order not found" });
                    }
                }

                if (orderType != "DineIn")
                {
                    await transaction.RollbackAsync();
                    return Json(new { status = false, message = "Only dine-in orders can be transferred to a table." });
                }
                if (orderStatus != "Running")
                {
                    await transaction.RollbackAsync();
                    return Json(new { status = false, message = "Only running orders can be transferred." });
                }
                if (oldTableId == model.NewTableId)
                {
                    await transaction.RollbackAsync();
                    return Json(new { status = false, message = "The order is already on that table." });
                }

                // 2. Make sure the target table is free
                string checkTarget = @"SELECT status FROM tbl_restaurant_table WHERE id = @tableId";
                using (var cmd = new MySqlCommand(checkTarget, conn))
                {
                    cmd.Transaction = (MySqlTransaction)transaction;
                    cmd.Parameters.AddWithValue("@tableId", model.NewTableId);
                    var targetStatus = (await cmd.ExecuteScalarAsync())?.ToString();
                    if (targetStatus == null)
                    {
                        await transaction.RollbackAsync();
                        return Json(new { status = false, message = "Target table not found" });
                    }
                    if (targetStatus != "Available")
                    {
                        await transaction.RollbackAsync();
                        return Json(new { status = false, message = "That table is already occupied. Pick a free one." });
                    }
                }

                // 3. Point the order at the new table
                string moveOrder = @"UPDATE tbl_order SET table_id = @newTableId WHERE id = @orderId";
                using (var cmd = new MySqlCommand(moveOrder, conn))
                {
                    cmd.Transaction = (MySqlTransaction)transaction;
                    cmd.Parameters.AddWithValue("@newTableId", model.NewTableId);
                    cmd.Parameters.AddWithValue("@orderId", model.OrderId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 4. Occupy the new table
                string occupyNew = @"UPDATE tbl_restaurant_table SET status = 'Occupied' WHERE id = @newTableId";
                using (var cmd = new MySqlCommand(occupyNew, conn))
                {
                    cmd.Transaction = (MySqlTransaction)transaction;
                    cmd.Parameters.AddWithValue("@newTableId", model.NewTableId);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 5. Free the old table (only if no other running order still uses it)
                if (oldTableId.HasValue && oldTableId.Value > 0)
                {
                    string freeOld = @"
                UPDATE tbl_restaurant_table
                SET status = 'Available'
                WHERE id = @oldTableId
                AND NOT EXISTS (
                    SELECT 1 FROM tbl_order
                    WHERE table_id = @oldTableId AND order_status = 'Running'
                )";
                    using var cmd = new MySqlCommand(freeOld, conn);
                    cmd.Transaction = (MySqlTransaction)transaction;
                    cmd.Parameters.AddWithValue("@oldTableId", oldTableId.Value);
                    await cmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return Json(new { status = true, message = "Order transferred to the new table." });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        public IActionResult Kitchen()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetKitchenOrders()
        {
            try
            {
                using var conn = await OpenConnectionAsync();

                // Only orders still relevant to the kitchen: running and not yet served
                string query = @"
            SELECT 
                o.id,
                o.order_no       AS orderNo,
                o.order_type     AS orderType,
                o.kitchen_status AS kitchenStatus,
                o.created_date   AS createdDate,
                t.table_name     AS tableName
            FROM tbl_order o
            LEFT JOIN tbl_restaurant_table t ON o.table_id = t.id
            WHERE o.order_status = 'Running'
              AND o.kitchen_status <> 'Served'
            ORDER BY o.id ASC";

                var orders = new List<dynamic>();
                var ids = new List<int>();

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        int id = Convert.ToInt32(reader["id"]);
                        ids.Add(id);
                        orders.Add(new
                        {
                            id,
                            orderNo = reader["orderNo"]?.ToString(),
                            orderType = reader["orderType"]?.ToString(),
                            kitchenStatus = reader["kitchenStatus"]?.ToString(),
                            createdDate = reader["createdDate"] == DBNull.Value
                                ? (DateTime?)null : Convert.ToDateTime(reader["createdDate"]),
                            tableName = reader["tableName"]?.ToString() ?? "N/A",
                            items = new List<object>()
                        });
                    }
                }

                // Pull the items for those orders in one query
                if (ids.Count > 0)
                {
                    string inList = string.Join(",", ids);
                    string itemsQuery = $@"
                SELECT order_id AS orderId, item_name AS itemName, qty
                FROM tbl_order_details
                WHERE order_id IN ({inList})
                ORDER BY id ASC";

                    using var cmd = new MySqlCommand(itemsQuery, conn);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        int oid = Convert.ToInt32(reader["orderId"]);
                        var ord = orders.FirstOrDefault(o => o.id == oid);
                        if (ord != null)
                        {
                            ((List<object>)ord.items).Add(new
                            {
                                itemName = reader["itemName"]?.ToString(),
                                qty = reader["qty"]
                            });
                        }
                    }
                }

                return Json(new { status = true, data = orders });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateKitchenStatus([FromBody] KitchenStatusRequest model)
        {
            try
            {
                var allowed = new[] { "New", "Preparing", "Ready", "Served" };
                if (!allowed.Contains(model.Status))
                    return Json(new { status = false, message = "Invalid kitchen status" });

                using var conn = await OpenConnectionAsync();

                string query = @"UPDATE tbl_order SET kitchen_status = @status WHERE id = @orderId";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@status", model.Status);
                cmd.Parameters.AddWithValue("@orderId", model.OrderId);

                int affected = await cmd.ExecuteNonQueryAsync();
                if (affected == 0)
                    return Json(new { status = false, message = "Order not found" });

                return Json(new { status = true, message = "Kitchen status updated" });
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.Message });
            }
        }


        #endregion






    }
}
