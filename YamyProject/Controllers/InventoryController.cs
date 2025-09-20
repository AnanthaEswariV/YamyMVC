using Microsoft.AspNetCore.Mvc;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace YamyProject.Controllers
{
    [Route("Inventory/[action]")]
    public class InventoryController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly YamyDbContext _applicationDbContext;
        private readonly MySqlConnection _connection;
        public InventoryController(IHttpClientFactory httpClientFactory, IConfiguration config, YamyDbContext applicationDbContext)
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
        [HttpPost]
        public async Task<IActionResult> SaveWarehouse([FromBody] WarehouseRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter warehouse name" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                //var userId = model.CreadedBy; // Get UserId from request
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                // Get connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // 🔎 Check duplicate
                var checkQuery = "SELECT COUNT(*) FROM tbl_warehouse WHERE name = @name";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name);
                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

                    if (exists && model.Id == 0)
                        return BadRequest(new { status = false, message = "Warehouse name already exists. Enter another name." });
                }


                if (model.Id == 0) // INSERT
                {
                    // Generate next warehouse code
                    int lastCodeNum = 0;
                    var getCodeQuery = @"SELECT Code FROM tbl_warehouse WHERE Code LIKE 'WH-%' ORDER BY CAST(SUBSTRING(Code, 4) AS UNSIGNED) DESC LIMIT 1";
                    using (var codeCmd = new MySqlCommand(getCodeQuery, conn))
                    using (var reader = await codeCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync() && reader["Code"] != DBNull.Value)
                            lastCodeNum = int.Parse(reader["Code"].ToString().Substring(3));
                    }

                    string newCode = "WH-" + (lastCodeNum + 1).ToString("D4");

                    // Insert
                    var insertQuery = @"INSERT INTO tbl_warehouse (Code, Name, emp_id, City, building_name, account_id, State, created_by, created_date) 
                                VALUES (@code, @name, @emp_id, @city, @building_name, @account_id, 0, @created_by, @created_date)";
                    using (var insertCmd = new MySqlCommand(insertQuery, conn))
                    {
                        insertCmd.Parameters.AddWithValue("@code", newCode);
                        insertCmd.Parameters.AddWithValue("@name", model.Name);
                        insertCmd.Parameters.AddWithValue("@emp_id", model.EmpId ?? 0);
                        insertCmd.Parameters.AddWithValue("@city", model.City ?? "");
                        insertCmd.Parameters.AddWithValue("@building_name", model.BuildingName ?? "");
                        insertCmd.Parameters.AddWithValue("@account_id", model.AccountId ?? 0);
                        insertCmd.Parameters.AddWithValue("@created_by", userId);
                        insertCmd.Parameters.AddWithValue("@created_date", DateTime.Now);

                        await insertCmd.ExecuteNonQueryAsync();
                    }

                    return Ok(new { status = true, message = "Warehouse added successfully", code = newCode });
                }
                else // UPDATE
                {
                    var updateQuery = @"UPDATE tbl_warehouse 
                                SET Name=@name, emp_id=@emp_id, City=@city, building_name=@building_name, account_id=@account_id 
                                WHERE Id=@id";
                    using (var updateCmd = new MySqlCommand(updateQuery, conn))
                    {
                        updateCmd.Parameters.AddWithValue("@name", model.Name);
                        updateCmd.Parameters.AddWithValue("@emp_id", model.EmpId ?? 0);
                        updateCmd.Parameters.AddWithValue("@city", model.City ?? "");
                        updateCmd.Parameters.AddWithValue("@building_name", model.BuildingName ?? "");
                        updateCmd.Parameters.AddWithValue("@account_id", model.AccountId ?? 0);
                        updateCmd.Parameters.AddWithValue("@id", model.Id);

                        int affected = await updateCmd.ExecuteNonQueryAsync();
                        if (affected == 0)
                            return NotFound(new { status = false, message = "Warehouse not found" });
                    }

                    return Ok(new { status = true, message = "Warehouse updated successfully" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> EditWarehouse([FromBody] WarehouseRequest model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { status = false, message = "Invalid warehouse data" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter warehouse name" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Optional: Check duplicate name excluding current Id
                var checkQuery = "SELECT COUNT(*) FROM tbl_warehouse WHERE Name=@name AND Id<>@id";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@name", model.Name);
                checkCmd.Parameters.AddWithValue("@id", model.Id);
                var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
                if (exists)
                    return BadRequest(new { status = false, message = "Warehouse name already exists. Enter another name." });

                var updateQuery = @"UPDATE tbl_warehouse
                            SET Name=@name, emp_id=@emp_id, City=@city, building_name=@building_name, account_id=@account_id
                            WHERE Id=@id";

                using var updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@name", model.Name);
                updateCmd.Parameters.AddWithValue("@emp_id", model.EmpId ?? 0);
                updateCmd.Parameters.AddWithValue("@city", model.City ?? "");
                updateCmd.Parameters.AddWithValue("@building_name", model.BuildingName ?? "");
                updateCmd.Parameters.AddWithValue("@account_id", model.AccountId ?? 0);
                updateCmd.Parameters.AddWithValue("@id", model.Id);

                int affected = await updateCmd.ExecuteNonQueryAsync();
                if (affected == 0)
                    return NotFound(new { status = false, message = "Warehouse not found" });

                return Ok(new { status = true, message = "Warehouse updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid Id" });

            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if warehouse has associated items or transactions
                var checkQuery = @"
            SELECT CASE 
                     WHEN EXISTS(SELECT 1 FROM tbl_items_warehouse WHERE warehouse_id = @id)
                       OR EXISTS(SELECT 1 FROM tbl_item_transaction WHERE warehouse_id = @id)
                     THEN 1
                     ELSE 0
                   END AS exists_flag;";

                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@id", id);
                var result = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                if (result > 0)
                {
                    return BadRequest(new { status = false, message = "Cannot delete this warehouse because it has associated items or transactions." });
                }

                // Delete warehouse
                var deleteQuery = "DELETE FROM tbl_warehouse WHERE Id=@id";
                using var deleteCmd = new MySqlCommand(deleteQuery, conn);
                deleteCmd.Parameters.AddWithValue("@id", id);

                int affected = await deleteCmd.ExecuteNonQueryAsync();
                if (affected > 0)
                    return Ok(new { status = true, message = "Warehouse deleted successfully" });

                return NotFound(new { status = false, message = "Warehouse not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWarehouses()
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"SELECT Id, Code, Name, emp_id AS EmpId, City, building_name AS BuildingName, account_id AS AccountId
                         FROM tbl_warehouse
                         ORDER BY Id DESC";
                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var warehouses = new List<object>();
                while (await reader.ReadAsync())
                {
                    warehouses.Add(new
                    {
                        id = reader.GetInt32("Id"),
                        display = $"{reader.GetString("Code")} - {reader.GetString("Name")}",
                        Code = reader["Code"],
                        Name = reader["Name"],
                        EmpId = reader["EmpId"],
                        City = reader["City"],
                        BuildingName = reader["BuildingName"],
                        AccountId = reader["AccountId"],
                    });
                }


                return Ok(new { status = true, data = warehouses });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllEmployees()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config["ConnectionStrings:DefaultDatabase"]
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = "SELECT Id, Name FROM tbl_employee ORDER BY Name";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var employees = new List<object>();
                while (await reader.ReadAsync())
                {
                    employees.Add(new
                    {
                        id = reader.GetInt32("Id"),
                        name = reader["Name"]?.ToString()
                    });
                }

                return Ok(employees);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWarehouseItems(int warehouseId)
        {
            try
            {

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"
            SELECT 
                w.id, 
                w.item_id,
                CONCAT(i.code, ' - ', i.name) AS ItemName,
                i.barcode,
                i.cost_price AS CostPrice,
                i.sales_price AS SalesPrice,
                w.qty AS QTY,
                i.type
            FROM tbl_items_warehouse w
            INNER JOIN tbl_items i ON w.item_id = i.id
            WHERE w.warehouse_id = @id";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", warehouseId);

                using var reader = await cmd.ExecuteReaderAsync();
                var items = new List<object>();

                while (await reader.ReadAsync())
                {
                    items.Add(new
                    {
                        id = reader.GetInt32("id"),
                        item_id = reader.GetInt32("item_id"),
                        itemName = reader.GetString("ItemName"),
                        barcode = reader["barcode"]?.ToString(),
                        costPrice = reader.GetDecimal("CostPrice"),
                        salesPrice = reader.GetDecimal("SalesPrice"),
                        qty = reader.GetDecimal("QTY"),
                        type = reader["type"]?.ToString()
                    });
                }

                return Ok(new { status = true, data = items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Warehouse Trasnfer
        public IActionResult WarehouseTransfer()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> GetWarehouseItemsFrom(int warehouseId)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"
            SELECT 
                i.id AS item_id,
                CONCAT(i.code, ' - ', i.name) AS ItemName,
                IFNULL(SUM(t.qty_in - t.qty_out), 0) AS QTY
            FROM tbl_item_transaction t
            INNER JOIN tbl_items i ON t.item_id = i.id
            WHERE i.active = 0
              AND t.warehouse_id = @id
            GROUP BY i.id, i.code, i.name
            HAVING QTY > 0";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", warehouseId);

                using var reader = await cmd.ExecuteReaderAsync();
                var items = new List<object>();

                while (await reader.ReadAsync())
                {
                    items.Add(new
                    {
                        item_id = reader.GetInt32("item_id"),
                        itemName = reader.GetString("ItemName"),
                        qty = reader.GetDecimal("QTY")
                    });
                }

                return Ok(new { status = true, data = items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetWarehouseItemsTo(int warehouseId)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"
            SELECT 
                i.id AS item_id,
                CONCAT(i.code, ' - ', i.name) AS ItemName,
                w.qty AS QTY
            FROM tbl_items_warehouse w
            INNER JOIN tbl_items i ON w.item_id = i.id
            WHERE i.active = 0
              AND w.warehouse_id = @id";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", warehouseId);

                using var reader = await cmd.ExecuteReaderAsync();
                var items = new List<object>();

                while (await reader.ReadAsync())
                {
                    items.Add(new
                    {
                        item_id = reader.GetInt32("item_id"),
                        itemName = reader.GetString("ItemName"),
                        qty = reader.GetDecimal("QTY")
                    });
                }

                return Ok(new { status = true, data = items });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        [HttpPost]
        public async Task<IActionResult> TransferItem([FromBody] TransferRequest request)
        {
            if (request == null || request.SourceWarehouseId <= 0 || request.TargetWarehouseId <= 0
                || request.ItemId <= 0 || request.Qty <= 0)
            {
                return BadRequest(new { status = false, message = "Invalid request data" });
            }

            if (request.SourceWarehouseId == request.TargetWarehouseId)
                return BadRequest(new { status = false, message = "Source and Target warehouses cannot be the same" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();
                using var transaction = await conn.BeginTransactionAsync();

                try
                {

                    var checkQtyCmd = new MySqlCommand(@"
    SELECT IFNULL(SUM(qty_in - qty_out), 0) AS availableQty
    FROM tbl_item_transaction
    WHERE warehouse_id = @warehouse_id AND item_id = @item_id", conn, (MySqlTransaction)transaction);
                    checkQtyCmd.Parameters.AddWithValue("@warehouse_id", request.SourceWarehouseId);
                    checkQtyCmd.Parameters.AddWithValue("@item_id", request.ItemId);

                    var availableQtyObj = await checkQtyCmd.ExecuteScalarAsync();
                    decimal availableQty = availableQtyObj == DBNull.Value ? 0 : Convert.ToDecimal(availableQtyObj);

                    if (availableQty < request.Qty)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new { status = false, message = "Quantity entered is greater than available stock" });
                    }
               
                    int invId = 0;

                    // 2️⃣ Insert/update target warehouse stock
                    var checkDestCmd = new MySqlCommand(@"
                SELECT id FROM tbl_items_warehouse 
                WHERE warehouse_id = @warehouse_id AND item_id = @item_id", conn, (MySqlTransaction)transaction);
                    checkDestCmd.Parameters.AddWithValue("@warehouse_id", request.TargetWarehouseId);
                    checkDestCmd.Parameters.AddWithValue("@item_id", request.ItemId);

                    var destIdObj = await checkDestCmd.ExecuteScalarAsync();
                    if (destIdObj != null)
                    {
                        invId = Convert.ToInt32(destIdObj);
                        var updateDestCmd = new MySqlCommand(@"
                    UPDATE tbl_items_warehouse SET qty = qty + @qty WHERE id = @id", conn, (MySqlTransaction)transaction);
                        updateDestCmd.Parameters.AddWithValue("@qty", request.Qty);
                        updateDestCmd.Parameters.AddWithValue("@id", invId);
                        await updateDestCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        var insertDestCmd = new MySqlCommand(@"
                    INSERT INTO tbl_items_warehouse (warehouse_id, item_id, qty) 
                    VALUES (@warehouse_id, @item_id, @qty); SELECT LAST_INSERT_ID();", conn, (MySqlTransaction)transaction);
                        insertDestCmd.Parameters.AddWithValue("@warehouse_id", request.TargetWarehouseId);
                        insertDestCmd.Parameters.AddWithValue("@item_id", request.ItemId);
                        insertDestCmd.Parameters.AddWithValue("@qty", request.Qty);
                        invId = Convert.ToInt32(await insertDestCmd.ExecuteScalarAsync());
                    }

                    // 3️⃣ Deduct stock from source warehouse
                    var deductSourceCmd = new MySqlCommand(@"
                UPDATE tbl_items_warehouse SET qty = qty - @qty 
                WHERE warehouse_id = @warehouse_id AND item_id = @item_id", conn, (MySqlTransaction)transaction);
                    deductSourceCmd.Parameters.AddWithValue("@qty", request.Qty);
                    deductSourceCmd.Parameters.AddWithValue("@warehouse_id", request.SourceWarehouseId);
                    deductSourceCmd.Parameters.AddWithValue("@item_id", request.ItemId);
                    await deductSourceCmd.ExecuteNonQueryAsync();

                    // 4️⃣ Insert warehouse transaction log
                    var insertTransCmd = new MySqlCommand(@"
                INSERT INTO tbl_item_warehouse_transaction 
                (`date`, `warehouse_from`, `warehouse_to`, `item_id`, `qty`, `description`, `created_by`, `created_date`)
                VALUES (@date, @warehouse_from, @warehouse_to, @item_id, @qty, @description, @created_by, @created_date)", conn, (MySqlTransaction)transaction);
                    insertTransCmd.Parameters.AddWithValue("@date", DateTime.Now);
                    insertTransCmd.Parameters.AddWithValue("@warehouse_from", request.SourceWarehouseId);
                    insertTransCmd.Parameters.AddWithValue("@warehouse_to", request.TargetWarehouseId);
                    insertTransCmd.Parameters.AddWithValue("@item_id", request.ItemId);
                    insertTransCmd.Parameters.AddWithValue("@qty", request.Qty);
                    insertTransCmd.Parameters.AddWithValue("@description", "Item Transfer");
                    insertTransCmd.Parameters.AddWithValue("@created_by", request.UserId);
                    insertTransCmd.Parameters.AddWithValue("@created_date", DateTime.Now);
                    await insertTransCmd.ExecuteNonQueryAsync();

                    // 5️⃣ Add cost_price & sales_price transactions (like addItemTransaction)
                    // Get last transaction prices
                    decimal costPrice = 0, salesPrice = 0;
                    var getPriceCmd = new MySqlCommand(@"
                SELECT cost_price, sales_price FROM tbl_item_transaction 
                WHERE warehouse_id = @warehouse_id AND item_id = @item_id 
                ORDER BY date DESC LIMIT 1", conn, (MySqlTransaction)transaction);
                    getPriceCmd.Parameters.AddWithValue("@warehouse_id", request.SourceWarehouseId);
                    getPriceCmd.Parameters.AddWithValue("@item_id", request.ItemId);

                    using var reader = await getPriceCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        costPrice = reader["cost_price"] != DBNull.Value ? Convert.ToDecimal(reader["cost_price"]) : 0;
                        salesPrice = reader["sales_price"] != DBNull.Value ? Convert.ToDecimal(reader["sales_price"]) : 0;
                    }
                    reader.Close();

                    // Fallback to items table if 0
                    if (costPrice == 0 || salesPrice == 0)
                    {
                        var getItemPriceCmd = new MySqlCommand(@"
                    SELECT cost_price, sales_price FROM tbl_items WHERE id = @item_id", conn, (MySqlTransaction)transaction);
                        getItemPriceCmd.Parameters.AddWithValue("@item_id", request.ItemId);
                        using var reader2 = await getItemPriceCmd.ExecuteReaderAsync();
                        if (await reader2.ReadAsync())
                        {
                            if (costPrice == 0 && reader2["cost_price"] != DBNull.Value)
                                costPrice = Convert.ToDecimal(reader2["cost_price"]);
                            if (salesPrice == 0 && reader2["sales_price"] != DBNull.Value)
                                salesPrice = Convert.ToDecimal(reader2["sales_price"]);
                        }
                        reader2.Close();
                    }

                    // Fetch warehouse code and name
                    string warehouseDisplay = "";
                    using (var warehouseCmd = new MySqlCommand("SELECT Code, Name FROM tbl_warehouse WHERE Id = @id", conn, transaction))
                    {
                        warehouseCmd.Parameters.AddWithValue("@id", request.SourceWarehouseId);
                        using (var reader3 = await warehouseCmd.ExecuteReaderAsync())
                        {
                            if (await reader3.ReadAsync())
                            {
                                warehouseDisplay = $"{reader3["Code"]} - {reader3["Name"]}";
                            }
                        }
                    }

                    // Fetch item code and name
                    string itemDisplay = "";
                    using (var itemCmd = new MySqlCommand("SELECT code, name FROM tbl_items WHERE id = @item_id", conn, transaction))
                    {
                        itemCmd.Parameters.AddWithValue("@item_id", request.ItemId);
                        using (var reader4 = await itemCmd.ExecuteReaderAsync())
                        {
                            if (await reader4.ReadAsync())
                            {
                                itemDisplay = $"{reader4["code"]} - {reader4["name"]}";
                            }
                        }
                    }

                    // Add OUT transaction
                    string outDescription = $"Warehouse Transfer No. {invId}Transferred to warehouse {warehouseDisplay} | {itemDisplay}";
                    await AddItemTransaction(conn, transaction, DateTime.Now, invId, request.ItemId, costPrice, salesPrice,
                        0, request.Qty, 0, outDescription, request.SourceWarehouseId);

                    // Add IN transaction
                    string inDescription = $"Warehouse Transfer No. {invId}Received from warehouse {warehouseDisplay} | {itemDisplay}";
                    await AddItemTransaction(conn, transaction, DateTime.Now, invId, request.ItemId, costPrice, salesPrice,
                        request.Qty, 0, request.Qty, inDescription, request.TargetWarehouseId);

                    await transaction.CommitAsync();

                    return Ok(new { status = true, message = "Transfer completed successfully" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { status = false, message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task AddItemTransaction(MySqlConnection conn, MySqlTransaction transaction,
            DateTime date, int invId, int itemId, decimal costPrice, decimal salesPrice,
            decimal qtyIn, decimal qtyOut, decimal qtyInc, string description, int warehouseId)
        {
            try
            {
                var cmd = new MySqlCommand(@"
            INSERT INTO tbl_item_transaction
            (`date`, `type`, `item_id`, `cost_price`, `qty_in`, `qty_out`, `qty_inc`, `sales_price`, `description`, `reference`, `warehouse_id`)
            VALUES
            (@date, @type, @item_id, @cost_price, @qty_in, @qty_out, @qty_inc, @sales_price, @description, @reference, @warehouse_id)", conn, transaction);
                
                cmd.Parameters.AddWithValue("@date", date);
                cmd.Parameters.AddWithValue("@type", "Warehouse Transfer");
                cmd.Parameters.AddWithValue("@item_id", itemId);
                cmd.Parameters.AddWithValue("@cost_price", costPrice.ToString("N2"));
                cmd.Parameters.AddWithValue("@qty_in", qtyIn.ToString("N2"));
                cmd.Parameters.AddWithValue("@qty_out", qtyOut.ToString("N2"));
                cmd.Parameters.AddWithValue("@qty_inc", qtyInc.ToString("N2"));
                cmd.Parameters.AddWithValue("@sales_price", salesPrice.ToString("N2"));
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@reference", invId);
                cmd.Parameters.AddWithValue("@warehouse_id", warehouseId);

                await cmd.ExecuteNonQueryAsync();
            }
            catch(Exception ex)
            {
                throw new Exception("Error in AddItemTransaction: " + ex.Message);
            }
            
        }

        [HttpGet]
        public async Task<IActionResult> GetWarehouseTransfers(int? id = null, int? itemId = null /*, int? warehouseId = null*/)
        {
            try
            {
                int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
                if (userId <= 0)
                    return Unauthorized(new { status = false, message = "User not logged in" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"
            SELECT 
                wt.Date,
                CONCAT(w1.code,' - ',w1.name) AS WarehouseFrom,
                CONCAT(w2.code,' - ',w2.name) AS WarehouseTo,
                CONCAT(i.code,' - ',i.name) AS ItemName,
                wt.qty,
                wt.description
            FROM tbl_item_warehouse_transaction wt
            INNER JOIN tbl_warehouse w1 ON wt.warehouse_from = w1.id
            INNER JOIN tbl_warehouse w2 ON wt.warehouse_to = w2.id
            INNER JOIN tbl_items i ON wt.item_id = i.id
            WHERE 1=1";

                var parameters = new List<MySqlParameter>();

                if (id.HasValue && id > 0)
                {
                    query += " AND wt.id = @id";
                    parameters.Add(new MySqlParameter("@id", id.Value));
                }

                if (itemId.HasValue && itemId > 0)
                {
                    query += " AND i.id = @itemId";
                    parameters.Add(new MySqlParameter("@itemId", itemId.Value));
                }


                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                using var reader = await cmd.ExecuteReaderAsync();
                var transfers = new List<object>();

                while (await reader.ReadAsync())
                {
                    transfers.Add(new
                    {
                        Date = reader["Date"],
                        WarehouseFrom = reader["WarehouseFrom"],
                        WarehouseTo = reader["WarehouseTo"],
                        ItemName = reader["ItemName"],
                        Qty = reader["qty"],
                        Description = reader["description"]
                    });
                }

                if (transfers.Count == 0)
                    return Ok(new { status = false, message = "No transfers available", data = new List<object>() });

                return Ok(new { status = true, data = transfers });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }




        #endregion

    }
}


