using Microsoft.AspNetCore.Mvc;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace YamyProject.Controllers
{
    [Route("HR/[action]")]
    public class HRController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly YamyDbContext _applicationDbContext;
        private readonly MySqlConnection _connection;
        public HRController(IHttpClientFactory httpClientFactory, IConfiguration config, YamyDbContext applicationDbContext)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _connection = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            _applicationDbContext = applicationDbContext;
        }

        #region Employee
        public IActionResult Employee()
        {
            return View();
        }

        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> SaveEmployee([FromBody] EmployeeRequest model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter employee name" });

            try
            {

                // Build connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check duplicate by Name
                var checkQuery = "SELECT COUNT(*) FROM tbl_employee WHERE name = @name";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name);
                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

                    if (exists && model.Id == 0)
                        return BadRequest(new { status = false, message = "Employee name already exists. Enter another name." });
                }
                // Generate new employee code
                string newCode = await GenerateNextEmployeeCode(conn);

                
                // Helper function for nullable values
                object DbValue<T>(T? value, object defaultValue = null) where T : struct
                    => value.HasValue ? value.Value : (defaultValue ?? DBNull.Value);

                // Convert DateOnly? to DateTime? inline
                object DbDate(DateOnly? date) => date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue) : (object)DBNull.Value;

                var insertQuery = @"
            INSERT INTO tbl_employee 
            (Code, Name, Birth_Day, Social_Status, City_Id, Address, Phone, Email, 
             Social_Insurance_Number, EmergencyName, EmergencyAddress, EmergencyPhone, Relation,
             PassportNumber, CountryOfIssue, PassportIssueDate, PassportExpiryDate,
             WorkContractNumber, Position_Id, Department_Id, WorkContractType, WorkDays, 
             WorkingHours, ContractIssueDate, ContractExpiryDate,
             ResidencyFileNumber, ResidencyIssuingAuthority, ResidencyIssueDate, ResidencyExpiryDate,
             EmiratesIDFileNumber, EmiratesIDIssuingAuthority, EmiratesIDIssueDate, EmiratesIDExpiryDate,
             BasicSalary, HousingAllowance, TransportationAllowance, Other,
             Account_Id, Bank_Id, Iban_Number, Bank_Account_Number,
             Employee_Recivable_Id, Accrued_Salaries_Id, Acroal_Leave_Salary_Id, Gratuit_Id, Petty_Cash_Id,
             Active, State)
            VALUES
            (@code, @name, @birth_day, @social_status, @city_id, @address, @phone, @email,
             @social_insurance_number, @emergency_name, @emergency_address, @emergency_phone, @relation,
             @passport_number, @country_of_issue, @passport_issue_date, @passport_expiry_date,
             @work_contract_number, @position_id, @department_id, @work_contract_type, @work_days,
             @working_hours, @contract_issue_date, @contract_expiry_date,
             @residency_file_number, @residency_issuing_authority, @residency_issue_date, @residency_expiry_date,
             @emirates_id_file_number, @emirates_id_issuing_authority, @emirates_id_issue_date, @emirates_id_expiry_date,
             @basic_salary, @housing_allowance, @transportation_allowance, @other,
             @account_id, @bank_id, @iban_number, @bank_account_number,
             @employee_recivable_id, @accrued_salaries_id, @acroal_leave_salary_id, @gratuit_id, @petty_cash_id,
             @active, 0)";

                using var insertCmd = new MySqlCommand(insertQuery, conn);

                insertCmd.Parameters.AddWithValue("@code", newCode);
                insertCmd.Parameters.AddWithValue("@name", model.Name ?? "");
                insertCmd.Parameters.AddWithValue("@birth_day", model.BirthDay);
                insertCmd.Parameters.AddWithValue("@social_status", model.SocialStatus ?? "");
                insertCmd.Parameters.AddWithValue("@city_id", model.CityId ?? 0);
                insertCmd.Parameters.AddWithValue("@address", model.Address ?? "");
                insertCmd.Parameters.AddWithValue("@phone", model.Phone ?? "");
                insertCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                insertCmd.Parameters.AddWithValue("@social_insurance_number", model.SocialInsuranceNumber ?? "");
                insertCmd.Parameters.AddWithValue("@emergency_name", model.EmergencyName ?? "");
                insertCmd.Parameters.AddWithValue("@emergency_address", model.EmergencyAddress ?? "");
                insertCmd.Parameters.AddWithValue("@emergency_phone", model.EmergencyPhone ?? "");
                insertCmd.Parameters.AddWithValue("@relation", model.Relation ?? "");
                insertCmd.Parameters.AddWithValue("@passport_number", model.PassportNumber ?? "");
                insertCmd.Parameters.AddWithValue("@country_of_issue", model.CountryOfIssue ?? "");
                insertCmd.Parameters.AddWithValue("@passport_issue_date", model.PassportIssueDate);
                insertCmd.Parameters.AddWithValue("@passport_expiry_date", model.PassportExpiryDate);
                insertCmd.Parameters.AddWithValue("@work_contract_number", model.WorkContractNumber ?? "");
                insertCmd.Parameters.AddWithValue("@position_id", model.PositionId ?? 0);
                insertCmd.Parameters.AddWithValue("@department_id", model.DepartmentId ?? 0);
                insertCmd.Parameters.AddWithValue("@work_contract_type", model.WorkContractType ?? "");
                insertCmd.Parameters.AddWithValue("@work_days", model.WorkDays ?? 0);
                insertCmd.Parameters.AddWithValue("@working_hours", model.Workinghours ?? 0);
                insertCmd.Parameters.AddWithValue("@contract_issue_date",model.ContractIssueDate);
                insertCmd.Parameters.AddWithValue("@contract_expiry_date",model.ContractExpiryDate);
                insertCmd.Parameters.AddWithValue("@residency_file_number", model.ResidencyFileNumber ?? "");
                insertCmd.Parameters.AddWithValue("@residency_issuing_authority", model.ResidencyIssuingAuthority ?? "");
                insertCmd.Parameters.AddWithValue("@residency_issue_date", model.ResidencyIssueDate);
                insertCmd.Parameters.AddWithValue("@residency_expiry_date",model.ResidencyExpiryDate);
                insertCmd.Parameters.AddWithValue("@emirates_id_file_number", model.EmiratesIdfileNumber ?? "");
                insertCmd.Parameters.AddWithValue("@emirates_id_issuing_authority", model.EmiratesIdissuingAuthority ?? "");
                insertCmd.Parameters.AddWithValue("@emirates_id_issue_date",model.EmiratesIdissueDate);
                insertCmd.Parameters.AddWithValue("@emirates_id_expiry_date",model.EmiratesIdexpiryDate);
                insertCmd.Parameters.AddWithValue("@basic_salary", model.BasicSalary ?? 0);
                insertCmd.Parameters.AddWithValue("@housing_allowance", model.HousingAllowance ?? 0);
                insertCmd.Parameters.AddWithValue("@transportation_allowance", model.TransportationAllowance ?? 0);
                insertCmd.Parameters.AddWithValue("@other", model.Other ?? 0);
                insertCmd.Parameters.AddWithValue("@account_id", model.AccountId ?? 0);
                insertCmd.Parameters.AddWithValue("@bank_id", model.BankId ?? 0);
                insertCmd.Parameters.AddWithValue("@iban_number", model.IbanNumber ?? "");
                insertCmd.Parameters.AddWithValue("@bank_account_number", model.BankAccountNumber ?? "");
                insertCmd.Parameters.AddWithValue("@employee_recivable_id", model.EmployeeRecivableId ?? 0);
                insertCmd.Parameters.AddWithValue("@accrued_salaries_id", model.AccruedSalariesId ?? 0);
                insertCmd.Parameters.AddWithValue("@acroal_leave_salary_id", model.AcroalLeaveSalaryId ?? 0);
                insertCmd.Parameters.AddWithValue("@gratuit_id", model.GratuitId ?? 0);
                insertCmd.Parameters.AddWithValue("@petty_cash_id", model.PettyCashId ?? 0);
                insertCmd.Parameters.AddWithValue("@active", model.Active != 0 ? 1 : 0);

                await insertCmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Employee added successfully", code = newCode });
            }
            catch(Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task<string> GenerateNextEmployeeCode(MySqlConnection conn)
        {
            int code;

            var query = "SELECT MAX(CAST(Code AS UNSIGNED)) AS lastCode FROM tbl_employee";
            using (var cmd = new MySqlCommand(query, conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync() && reader["lastCode"] != DBNull.Value)
                    code = Convert.ToInt32(reader["lastCode"]) + 1;
                else
                    code = 30001; // first code
            }

            return code.ToString("D5"); // always 5 digits
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployees([FromQuery] string filter = "all")
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config["ConnectionStrings:DefaultDatabase"]
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"SELECT 
                            id, code, name, phone, email, active,
                            BasicSalary, HousingAllowance, TransportationAllowance, Other
                         FROM tbl_employee";

                if (filter.Equals("active", StringComparison.OrdinalIgnoreCase))
                    query += " WHERE Active = 1";
                else if (filter.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                    query += " WHERE Active = 0";

                query += " ORDER BY id DESC";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var employees = new List<object>();
                while (await reader.ReadAsync())
                {
                    employees.Add(new
                    {
                        id = reader["id"],
                        code = reader["code"],
                        name = reader["name"],
                        phone = reader["phone"],
                        email = reader["email"],
                        active = Convert.ToInt32(reader["active"]) == 1,
                        basicSalary = reader["BasicSalary"] != DBNull.Value ? Convert.ToDecimal(reader["BasicSalary"]) : 0,
                        housingAllowance = reader["HousingAllowance"] != DBNull.Value ? Convert.ToDecimal(reader["HousingAllowance"]) : 0,
                        transportationAllowance = reader["TransportationAllowance"] != DBNull.Value ? Convert.ToDecimal(reader["TransportationAllowance"]) : 0,
                        other = reader["Other"] != DBNull.Value ? Convert.ToDecimal(reader["Other"]) : 0
                    });
                }

                return Ok(new { status = true, data = employees });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [IgnoreAntiforgeryToken]
        [HttpPost]
        public async Task<IActionResult> EditEmployee([FromBody] EmployeeRequest model)
        {
            if (model == null || model.Id <= 0)
                return BadRequest(new { status = false, message = "Invalid request or missing employee Id" });

            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { status = false, message = "Please enter employee name" });

            try
            {
                // Build connection string
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check duplicate by Name (ignore current Id)
                var checkQuery = "SELECT COUNT(*) FROM tbl_employee WHERE name = @name AND id != @id";
                using (var checkCmd = new MySqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@name", model.Name);
                    checkCmd.Parameters.AddWithValue("@id", model.Id);
                    var exists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;

                    if (exists)
                        return BadRequest(new { status = false, message = "Employee name already exists. Enter another name." });
                }

                // Update query
                var updateQuery = @"
            UPDATE tbl_employee SET
                Name = @name,
                Birth_Day = @birth_day,
                Social_Status = @social_status,
                City_Id = @city_id,
                Address = @address,
                Phone = @phone,
                Email = @email,
                Social_Insurance_Number = @social_insurance_number,
                EmergencyName = @emergency_name,
                EmergencyAddress = @emergency_address,
                EmergencyPhone = @emergency_phone,
                Relation = @relation,
                PassportNumber = @passport_number,
                CountryOfIssue = @country_of_issue,
                PassportIssueDate = @passport_issue_date,
                PassportExpiryDate = @passport_expiry_date,
                WorkContractNumber = @work_contract_number,
                Position_Id = @position_id,
                Department_Id = @department_id,
                WorkContractType = @work_contract_type,
                WorkDays = @work_days,
                WorkingHours = @working_hours,
                ContractIssueDate = @contract_issue_date,
                ContractExpiryDate = @contract_expiry_date,
                ResidencyFileNumber = @residency_file_number,
                ResidencyIssuingAuthority = @residency_issuing_authority,
                ResidencyIssueDate = @residency_issue_date,
                ResidencyExpiryDate = @residency_expiry_date,
                EmiratesIDFileNumber = @emirates_id_file_number,
                EmiratesIDIssuingAuthority = @emirates_id_issuing_authority,
                EmiratesIDIssueDate = @emirates_id_issue_date,
                EmiratesIDExpiryDate = @emirates_id_expiry_date,
                BasicSalary = @basic_salary,
                HousingAllowance = @housing_allowance,
                TransportationAllowance = @transportation_allowance,
                Other = @other,
                Account_Id = @account_id,
                Bank_Id = @bank_id,
                Iban_Number = @iban_number,
                Bank_Account_Number = @bank_account_number,
                Employee_Recivable_Id = @employee_recivable_id,
                Accrued_Salaries_Id = @accrued_salaries_id,
                Acroal_Leave_Salary_Id = @acroal_leave_salary_id,
                Gratuit_Id = @gratuit_id,
                Petty_Cash_Id = @petty_cash_id,
                Active = @active
            WHERE Id = @id";

                using var updateCmd = new MySqlCommand(updateQuery, conn);

                // Map parameters
                updateCmd.Parameters.AddWithValue("@id", model.Id);
                updateCmd.Parameters.AddWithValue("@name", model.Name ?? "");
                updateCmd.Parameters.AddWithValue("@birth_day", model.BirthDay);
                updateCmd.Parameters.AddWithValue("@social_status", model.SocialStatus ?? "");
                updateCmd.Parameters.AddWithValue("@city_id", model.CityId ?? 0);
                updateCmd.Parameters.AddWithValue("@address", model.Address ?? "");
                updateCmd.Parameters.AddWithValue("@phone", model.Phone ?? "");
                updateCmd.Parameters.AddWithValue("@email", model.Email ?? "");
                updateCmd.Parameters.AddWithValue("@social_insurance_number", model.SocialInsuranceNumber ?? "");
                updateCmd.Parameters.AddWithValue("@emergency_name", model.EmergencyName ?? "");
                updateCmd.Parameters.AddWithValue("@emergency_address", model.EmergencyAddress ?? "");
                updateCmd.Parameters.AddWithValue("@emergency_phone", model.EmergencyPhone ?? "");
                updateCmd.Parameters.AddWithValue("@relation", model.Relation ?? "");
                updateCmd.Parameters.AddWithValue("@passport_number", model.PassportNumber ?? "");
                updateCmd.Parameters.AddWithValue("@country_of_issue", model.CountryOfIssue ?? "");
                updateCmd.Parameters.AddWithValue("@passport_issue_date", model.PassportIssueDate);
                updateCmd.Parameters.AddWithValue("@passport_expiry_date", model.PassportExpiryDate);
                updateCmd.Parameters.AddWithValue("@work_contract_number", model.WorkContractNumber ?? "");
                updateCmd.Parameters.AddWithValue("@position_id", model.PositionId ?? 0);
                updateCmd.Parameters.AddWithValue("@department_id", model.DepartmentId ?? 0);
                updateCmd.Parameters.AddWithValue("@work_contract_type", model.WorkContractType ?? "");
                updateCmd.Parameters.AddWithValue("@work_days", model.WorkDays ?? 0);
                updateCmd.Parameters.AddWithValue("@working_hours", model.Workinghours ?? 0);
                updateCmd.Parameters.AddWithValue("@contract_issue_date", model.ContractIssueDate);
                updateCmd.Parameters.AddWithValue("@contract_expiry_date", model.ContractExpiryDate);
                updateCmd.Parameters.AddWithValue("@residency_file_number", model.ResidencyFileNumber ?? "");
                updateCmd.Parameters.AddWithValue("@residency_issuing_authority", model.ResidencyIssuingAuthority ?? "");
                updateCmd.Parameters.AddWithValue("@residency_issue_date", model.ResidencyIssueDate);
                updateCmd.Parameters.AddWithValue("@residency_expiry_date", model.ResidencyExpiryDate);
                updateCmd.Parameters.AddWithValue("@emirates_id_file_number", model.EmiratesIdfileNumber ?? "");
                updateCmd.Parameters.AddWithValue("@emirates_id_issuing_authority", model.EmiratesIdissuingAuthority ?? "");
                updateCmd.Parameters.AddWithValue("@emirates_id_issue_date", model.EmiratesIdissueDate);
                updateCmd.Parameters.AddWithValue("@emirates_id_expiry_date", model.EmiratesIdexpiryDate);
                updateCmd.Parameters.AddWithValue("@basic_salary", model.BasicSalary ?? 0);
                updateCmd.Parameters.AddWithValue("@housing_allowance", model.HousingAllowance ?? 0);
                updateCmd.Parameters.AddWithValue("@transportation_allowance", model.TransportationAllowance ?? 0);
                updateCmd.Parameters.AddWithValue("@other", model.Other ?? 0);
                updateCmd.Parameters.AddWithValue("@account_id", model.AccountId ?? 0);
                updateCmd.Parameters.AddWithValue("@bank_id", model.BankId ?? 0);
                updateCmd.Parameters.AddWithValue("@iban_number", model.IbanNumber ?? "");
                updateCmd.Parameters.AddWithValue("@bank_account_number", model.BankAccountNumber ?? "");
                updateCmd.Parameters.AddWithValue("@employee_recivable_id", model.EmployeeRecivableId ?? 0);
                updateCmd.Parameters.AddWithValue("@accrued_salaries_id", model.AccruedSalariesId ?? 0);
                updateCmd.Parameters.AddWithValue("@acroal_leave_salary_id", model.AcroalLeaveSalaryId ?? 0);
                updateCmd.Parameters.AddWithValue("@gratuit_id", model.GratuitId ?? 0);
                updateCmd.Parameters.AddWithValue("@petty_cash_id", model.PettyCashId ?? 0);
                updateCmd.Parameters.AddWithValue("@active", model.Active != 0 ? 1 : 0);

                await updateCmd.ExecuteNonQueryAsync();

                return Ok(new { status = true, message = "Employee updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCountryCities()
        {
            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName")
                           ?? _config["ConnectionStrings:DefaultDatabase"]
            };

            using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            // 1️⃣ Get countries
            var countries = new List<(int Id, string Name)>();
            using (var cmd = new MySqlCommand("SELECT id, name FROM tbl_country ORDER BY name", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    countries.Add((reader.GetInt32("id"), reader.GetString("name")));
                }
            }

            // 2️⃣ Get cities
            var cities = new List<(int Id, string Name, int CountryId)>();
            using (var cmd = new MySqlCommand("SELECT id, name, country_id FROM tbl_city ORDER BY name", conn))
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    cities.Add((reader.GetInt32("id"), reader.GetString("name"), reader.GetInt32("country_id")));
                }
            }

            // 3️⃣ Combine countries with their cities
            var data = countries.Select(c => new
            {
                id = c.Id,
                name = c.Name,
                cities = cities.Where(city => city.CountryId == c.Id)
                               .Select(city => new { id = city.Id, name = city.Name })
                               .ToList()
            }).ToList();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeById(int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid employee ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config["ConnectionStrings:DefaultDatabase"]
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT * FROM tbl_employee WHERE Id = @id";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();

                if (!reader.Read())
                    return NotFound(new { status = false, message = "Employee not found" });

                var emp = new
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader["Name"]?.ToString(),
                    CityId = reader["City_Id"] as int? ?? 0,
                    SocialStatus = reader["Social_Status"]?.ToString(),
                    Address = reader["Address"]?.ToString(),
                    Phone = reader["Phone"]?.ToString(),
                    Email = reader["Email"]?.ToString(),
                    BirthDay = reader["Birth_Day"] as DateTime? != null
                   ? ((DateTime)reader["Birth_Day"]).ToString("yyyy-MM-dd")
                   : null,
                    SocialInsuranceNumber = reader["Social_Insurance_Number"]?.ToString(),
                    BasicSalary = reader["BasicSalary"] as decimal? ?? 0,
                    HousingAllowance = reader["HousingAllowance"] as decimal? ?? 0,
                    TransportationAllowance = reader["TransportationAllowance"] as decimal? ?? 0,
                    Other = reader["Other"] as decimal? ?? 0,
                    EmergencyName = reader["EmergencyName"]?.ToString(),
                    EmergencyAddress = reader["EmergencyAddress"]?.ToString(),
                    EmergencyPhone = reader["EmergencyPhone"]?.ToString(),
                    Relation = reader["Relation"]?.ToString(),

                    PassportNumber = reader["PassportNumber"]?.ToString(),
                    CountryOfIssue = reader["CountryOfIssue"]?.ToString(),
                    PassportIssueDate = reader["PassportIssueDate"] as DateTime? != null
                           ? ((DateTime)reader["PassportIssueDate"]).ToString("yyyy-MM-dd")
                           : null,
                    PassportExpiryDate = reader["PassportExpiryDate"] as DateTime? != null
                           ? ((DateTime)reader["PassportExpiryDate"]).ToString("yyyy-MM-dd")
                           : null,

                    ResidencyFileNumber = reader["ResidencyFileNumber"]?.ToString(),
                    ResidencyIssuingAuthority = reader["ResidencyIssuingAuthority"]?.ToString(),
                    ResidencyIssueDate = reader["ResidencyIssueDate"] as DateTime? != null
                           ? ((DateTime)reader["ResidencyIssueDate"]).ToString("yyyy-MM-dd")
                           : null,
                    ResidencyExpiryDate = reader["ResidencyExpiryDate"] as DateTime? != null
                           ? ((DateTime)reader["ResidencyExpiryDate"]).ToString("yyyy-MM-dd")
                           : null,

                    ContractNumber = reader["WorkContractNumber"]?.ToString(),
                    DepartmentId = reader["Department_Id"] as int? ?? 0,
                    ContractType = reader["WorkContractType"]?.ToString(),
                    PositionId = reader["Position_Id"] as int? ?? 0,
                    WorkDays = reader["WorkDays"] as int? ?? 0,
                    WorkingHours = reader["WorkingHours"] as int? ?? 0,

                    ContractIssueDate = reader["ContractIssueDate"] as DateTime? != null
                           ? ((DateTime)reader["ContractIssueDate"]).ToString("yyyy-MM-dd")
                           : null,
                    ContractExpiryDate = reader["ContractExpiryDate"] as DateTime? != null
                           ? ((DateTime)reader["ContractExpiryDate"]).ToString("yyyy-MM-dd")
                           : null,

                    AccruedSalariesId = reader["Accrued_Salaries_Id"] as int? ?? 0,
                    EmployeeRecivableId = reader["Employee_Recivable_Id"] as int? ?? 0,
                    AcroalLeaveSalaryId = reader["Acroal_Leave_Salary_Id"] as int? ?? 0,
                    GratuitId = reader["Gratuit_Id"] as int? ?? 0,
                    Active = reader["Active"] as int? ?? 1
                };


                return Ok(emp);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteEmployee([FromBody] int id)
        {
            if (id <= 0)
                return BadRequest(new { status = false, message = "Invalid employee ID" });

            try
            {
                // Build connection string dynamically based on session or default
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config["ConnectionStrings:DefaultDatabase"]
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Delete query
                var query = "DELETE FROM tbl_employee WHERE Id = @Id";

                // Execute query
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Id", id);
                int affectedRows = await cmd.ExecuteNonQueryAsync();

                if (affectedRows > 0)
                    return Ok(new { status = true, message = "Employee deleted successfully" });
                else
                    return NotFound(new { status = false, message = "Employee not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

    }
}
