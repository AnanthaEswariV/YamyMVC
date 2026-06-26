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
                    cmd.Parameters.AddWithValue("@orderType",model.OrderType);
                    cmd.Parameters.AddWithValue( "@tableId", model.TableId ?? (object)DBNull.Value);
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

            if (model.OpeningBalanceDate.HasValue &&
    model.OpeningBalanceDate.Value.Date > DateTime.Now.Date)
            {
                return Json(new { status = false, message = "Opening balance date must be today or earlier" });
            }

            if (!string.IsNullOrWhiteSpace(model.TRN))
            {
                if (model.TRN.Length < 3 || model.TRN.Length > 15)
                {
                    return Json(new { status = false, message = "TRN must be between 3 and 15 characters" });
                }
            }


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
                if (model.Id == 0) // Insert
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
                    var projectSite = string.Join(",", model.ProjectSites);
                    var insertQuery = @"INSERT INTO tbl_customer 
        (code, NAME, Cat_id, Balance, DATE, main_phone, work_phone, mobile, email, ccemail, website, 
        country, city, region, building_name, account_id, trn, facilty_name, active, created_by, created_date, state, project_site)
        VALUES(@code,@name,@cat_id,@balance,@date,@main_phone,@work_phone,@mobile,@email,@ccemail,@website,
        @country,@city,@region,@building_name,@account_id,@trn,@facilty_name,@active,@created_by,@created_date,0,@project_site);
        SELECT LAST_INSERT_ID();";

                    using var insertCmd = new MySqlCommand(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@code", formattedCode);
                    insertCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    insertCmd.Parameters.AddWithValue("@cat_id", model.CategoryId);
                    decimal balance = (model.Debit ?? 0) - (model.Credit ?? 0);
                    insertCmd.Parameters.AddWithValue("@balance", balance);
                    insertCmd.Parameters.AddWithValue("@date", model.OpeningBalanceDate);
                    insertCmd.Parameters.AddWithValue("@main_phone", model.MainPhone ?? "");
                    insertCmd.Parameters.AddWithValue("@work_phone", model.WorkPhone ?? "");
                    insertCmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
                    insertCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                    insertCmd.Parameters.AddWithValue("@ccemail", model.CCEmail ?? "");
                    insertCmd.Parameters.AddWithValue("@website", model.Website ?? "");
                    insertCmd.Parameters.AddWithValue("@country", model.CountryId);
                    insertCmd.Parameters.AddWithValue("@city", model.CityId ?? 0);
                    insertCmd.Parameters.AddWithValue("@region", model.Region ?? "");
                    insertCmd.Parameters.AddWithValue("@building_name", model.BuildingName ?? "");
                    insertCmd.Parameters.AddWithValue("@account_id", model.AccountId ?? 0);
                    insertCmd.Parameters.AddWithValue("@trn", model.TRN ?? "");
                    insertCmd.Parameters.AddWithValue("@facilty_name", model.FacilityName ?? "");
                    insertCmd.Parameters.AddWithValue("@active", model.Active ? 0 : -1);
                    insertCmd.Parameters.AddWithValue("@created_by", userId);
                    insertCmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    insertCmd.Parameters.AddWithValue("@project_site", projectSite);

                    var customerId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

                    // Process opening balance
                    await ProcessOpeningBalanceAsync(conn, customerId, formattedCode, userId, model);

                    return Json(new { status = true, message = "Customer added successfully", code = formattedCode });
                }
                else // Update
                {

                    var projectSite = string.Join(",", model.ProjectSites);
                    var updateQuery = @"UPDATE tbl_customer SET 
                            code=@code, NAME=@name, Cat_id=@cat_id, DATE=@date, main_phone=@main_phone,
                            work_phone=@work_phone, mobile=@mobile, email=@email, ccemail=@ccemail, website=@website,
                            country=@country, city=@city, region=@region,  project_site=@project_site,
                            building_name=@building_name, account_id=@account_id, trn=@trn, facilty_name=@facilty_name,
                            active=@active, Balance=@balance
                        WHERE id=@id";

                    using var updateCmd = new MySqlCommand(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@code", model.Code);
                    updateCmd.Parameters.AddWithValue("@name", model.Name.Trim());
                    updateCmd.Parameters.AddWithValue("@cat_id", model.CategoryId);
                    updateCmd.Parameters.AddWithValue("@date", model.OpeningBalanceDate);
                    updateCmd.Parameters.AddWithValue("@main_phone", model.MainPhone ?? "");
                    updateCmd.Parameters.AddWithValue("@work_phone", model.WorkPhone ?? "");
                    updateCmd.Parameters.AddWithValue("@mobile", model.Mobile ?? "");
                    updateCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                    updateCmd.Parameters.AddWithValue("@ccemail", model.CCEmail ?? "");
                    updateCmd.Parameters.AddWithValue("@website", model.Website ?? "");
                    updateCmd.Parameters.AddWithValue("@country", model.CountryId);
                    updateCmd.Parameters.AddWithValue("@city", model.CityId ?? 0);
                    updateCmd.Parameters.AddWithValue("@region", model.Region ?? "");
                    updateCmd.Parameters.AddWithValue("@building_name", model.BuildingName ?? "");
                    updateCmd.Parameters.AddWithValue("@account_id", model.AccountId ?? 0);
                    updateCmd.Parameters.AddWithValue("@trn", model.TRN ?? "");
                    updateCmd.Parameters.AddWithValue("@facilty_name", model.FacilityName ?? "");
                    updateCmd.Parameters.AddWithValue("@active", model.Active ? 0 : -1);
                    decimal balance = (model.Debit ?? 0) - (model.Credit ?? 0);
                    updateCmd.Parameters.AddWithValue("@balance", balance);
                    updateCmd.Parameters.AddWithValue("@project_site", projectSite);
                    updateCmd.Parameters.AddWithValue("@id", model.Id);

                    int affected = await updateCmd.ExecuteNonQueryAsync();

                    return Json(new { status = true, message = "Customer updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }
        private async Task ProcessOpeningBalanceAsync(MySqlConnection conn, int customerId, string formattedCode, int userId, CustomerRequest model)
        {
            decimal debitAmount = model.Debit ?? 0;
            decimal creditAmount = model.Credit ?? 0;
            string accountId = model.AccountId?.ToString() ?? "0";

            // Get Opening Balance Equity account ID
            string openingBalanceEquity = await SelectDefaultLevelAccountAsync(conn, "Opening Balance Equity");
            if (string.IsNullOrWhiteSpace(openingBalanceEquity) || openingBalanceEquity == "0")
            {
                var cmd = new MySqlCommand("SELECT id FROM tbl_coa_level_4 WHERE name='Opening Balance Equity'", conn);
                var result = await cmd.ExecuteScalarAsync();
                if (result != null && result != DBNull.Value)
                    openingBalanceEquity = result.ToString();
            }

            if (string.IsNullOrWhiteSpace(openingBalanceEquity) || openingBalanceEquity == "0")
                throw new Exception("Cannot make opening balance without Opening Balance Equity account");

            DateTime transactionDate = model.OpeningBalanceDate ?? DateTime.Now;

            // Mimic your synchronous logic

            if (creditAmount != 0)
            {
                // Credit transaction: 
                // 1) Opening Balance Equity - Debit = creditAmount, Credit=0
                await AddTransactionEntryAsync(
                    conn, transactionDate, openingBalanceEquity,
                    creditAmount.ToString(), "0",
                    customerId.ToString(), "0",
                    "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance Equity - Customer Code - {formattedCode}",
                    userId, DateTime.Now, "");

                // 2) Customer Account - Debit=0, Credit=creditAmount
                await AddTransactionEntryAsync(
                    conn, transactionDate, openingBalanceEquity,
                    "0", creditAmount.ToString(),
                    customerId.ToString(), customerId.ToString(),
                    "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance - Customer Code - {formattedCode}",
                    userId, DateTime.Now, "");
            }

            if (debitAmount != 0)
            {
                // Debit transaction:
                // 1) Opening Balance Equity - Debit=0, Credit=debitAmount
                await AddTransactionEntryAsync(
                    conn, transactionDate, openingBalanceEquity,
                    "0", debitAmount.ToString(),
                    customerId.ToString(), "0",
                    "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance Equity - Customer Code - {formattedCode}",
                    userId, DateTime.Now, "");

                // 2) Customer Account - Debit=debitAmount, Credit=0
                await AddTransactionEntryAsync(
                    conn, transactionDate, openingBalanceEquity,
                    debitAmount.ToString(), "0",
                    customerId.ToString(), customerId.ToString(),
                    "Customer Opening Balance", "OPENING BALANCE",
                    $"Opening Balance - Customer Code - {formattedCode}",
                    userId, DateTime.Now, "");
            }
        }

        public static async Task AddTransactionEntryAsync(MySqlConnection conn, DateTime date, string accountId, string debit, string credit,
               string transactionId, string humId, string type, string voucherName, string description,
               int createdBy, DateTime createdDate, string voucherNo)
        {
            var query = @"INSERT INTO tbl_transaction 
            (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no) 
            VALUES (@date, @accountId, @debit, @credit, @transactionId, @hum_id, @tType, @type, @description, @createdBy, @createdDate, 0, @voucher_no)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transactionId", transactionId);
            cmd.Parameters.AddWithValue("@hum_id", humId);
            cmd.Parameters.AddWithValue("@tType", voucherName);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@createdBy", createdBy);
            cmd.Parameters.AddWithValue("@createdDate", createdDate);
            cmd.Parameters.AddWithValue("@voucher_no", voucherNo);

            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<string> SelectDefaultLevelAccountAsync(MySqlConnection conn, string accountName)
        {
            var cmd = new MySqlCommand("SELECT id FROM tbl_coa_level_4 WHERE name=@name LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@name", accountName);
            var result = await cmd.ExecuteScalarAsync();
            return result?.ToString() ?? "0";
        }

        #endregion
    }
}
