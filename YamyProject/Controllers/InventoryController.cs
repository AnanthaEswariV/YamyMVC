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

        #endregion

    }
}
