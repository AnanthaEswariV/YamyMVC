using System.Globalization;

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

        #region HR Login

        public IActionResult Index()
        {
            return View();
        }

        #endregion

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
                insertCmd.Parameters.AddWithValue("@country_of_issue", model.CountryOfIssue ?? 0);
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

                var newId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());
                return Ok(new { status = true, message = "Employee added successfully", code = newCode , id= newId, name= model.Name.Trim() });
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
                            id, code, name, city_id, address, birth_day, Social_Status, Social_Insurance_Number, Email, EmergencyName,
                            EmergencyAddress, EmergencyPhone, Relation, BasicSalary, bank_id, Iban_Number, Bank_account_Number, EmiratesIDFileNumber, 
                            EmiratesIDIssuingAuthority, EmiratesIDIssueDate, EmiratesIDExpiryDate, PassportNumber, CountryOfIssue,
                            PassportIssueDate, PassportExpiryDate, WorkContractNumber, Position_Id, Department_Id, WorkDays, workinghours, ContractIssueDate,
                            ContractExpiryDate, ResidencyFileNumber, ResidencyIssuingAuthority, ResidencyIssueDate, ResidencyExpiryDate, account_id, Accrued_Salaries_id,
                            Employee_Recivable_Id, Acroal_Leave_Salary_Id, Gratuit_Id, Petty_Cash_Id,
                            phone, email, active,
                            BasicSalary, HousingAllowance, TransportationAllowance, Other
                        FROM tbl_employee";

                if (filter.Equals("active", StringComparison.OrdinalIgnoreCase))
                    query += " WHERE Active = 1";
                else if (filter.Equals("inactive", StringComparison.OrdinalIgnoreCase))
                    query += " WHERE Active = 0";

                query += " ORDER BY id DESC";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var employees = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var employee = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        employee[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    employees.Add(employee);
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
                updateCmd.Parameters.AddWithValue("@country_of_issue", model.CountryOfIssue ?? 0);
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

        [HttpGet]
        public async Task<IActionResult> GetCountryOfIssue()
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


            // 3️⃣ Combine countries with their cities
            var data = countries.Select(c => new
            {
                id = c.Id,
                name = c.Name,
            }).ToList();

            return Json(data);
        }

        [HttpGet]
        public async Task<IActionResult> GetAccruedSalaries()
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

                var items = new List<object>();
                using (var cmd = new MySqlCommand("SELECT id, code, name FROM tbl_coa_level_4 ORDER BY code", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        items.Add(new
                        {
                            Id = reader.GetInt32("id"),
                            Code = reader.GetInt32("code"),
                            Name = reader.GetString("name")
                        });
                    }
                }

                return Json(items);
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoiceByCustomerId(int id)
        {
            if (id <= 0)
                return Json(new { status = false, message = "Invalid customer ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config["ConnectionStrings:DefaultDatabase"]
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            WITH CTE AS (
                SELECT 
                    t.id,
                    t.transaction_id AS `InvoiceId`,
                    ROW_NUMBER() OVER (ORDER BY t.date, t.id) AS SN,
                    t.voucher_no AS `VoucherNo`,
                    t.date AS `Date`,
                    CONCAT(ta.code, ' - ', ta.name) AS `AccountName`,
                    t.type AS `Type`,
                    CASE
                        WHEN t.type = 'Employee Salary Payment' THEN IF(t.debit = 0, t.credit, t.debit)
                        WHEN t.type = 'Petty Cash' THEN IF(t.debit = 0, t.credit, t.debit)
                        WHEN t.type = 'Check Cancel (Employee)' THEN t.credit
                        WHEN t.type = 'Employee Salary' THEN IF(t.debit = 0, t.credit, t.debit)
                        WHEN t.type = 'Employee Loan Payment' THEN IF(t.debit = 0, t.credit, t.debit)
                        WHEN t.type = 'Employee Petty Cash Payment' THEN IF(t.debit = 0, t.credit, t.debit)
                        ELSE 0
                    END AS `Amount`,
                    CASE
                        WHEN t.type = 'Employee Salary Payment' THEN IF(t.debit = 0, t.credit, t.debit)
                        WHEN t.type = 'Petty Cash' THEN IF(t.debit = 0, t.credit, t.debit)
                        WHEN t.type = 'Employee Loan Payment' THEN IF(t.debit = 0, t.credit, t.debit)
                        WHEN t.type = 'Employee Petty Cash Payment' THEN IF(t.debit = 0, t.credit, t.debit)
                        WHEN t.type = 'Check Cancel (Employee)' THEN -t.credit
                        ELSE 0
                    END AS `PaidAmt`
                FROM tbl_transaction t
                INNER JOIN tbl_coa_level_4 ta ON t.account_id = ta.id
               WHERE t.hum_id = @id AND t.state = 0
                  AND (
                      t.type LIKE 'Employee%' OR 
                      t.type = 'Employee Loan Payment' OR 
                      t.type = 'Employee Salary Payment' OR 
                      t.type = 'Petty Cash' OR 
                      t.type = 'Check Cancel (Employee)' OR
                      t.type = 'Employee Petty Cash Payment' OR 
                      t.type LIKE 'Employee Salary'
                  )
            ),
            Running_Balance AS (
                SELECT *,
                    SUM(`Amount` - `PaidAmt`) OVER (ORDER BY `Date`, id ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS `Balance`
                FROM CTE
            )
            SELECT 
                `id`,
                `InvoiceId`,
                `SN` as `SN`,
                `VoucherNo`,
                `Date`,
                `AccountName`,
                `Type`,
                `Amount`,
                `PaidAmt`,
                `Balance`
            FROM Running_Balance
            ORDER BY `Date`, id;";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();

                var invoices = new List<object>();

                while (await reader.ReadAsync())
                {
                    var invoice = new
                    {
                        Id = reader.GetInt32("id"),
                        InvoiceId = reader["InvoiceId"]?.ToString(),
                        SN = reader.GetInt32("SN"),
                        VoucherNo = reader["VoucherNo"]?.ToString(),
                        Date = reader["Date"] as DateTime? != null
                            ? ((DateTime)reader["Date"]).ToString("yyyy-MM-dd")
                            : null,
                        AccountName = reader["AccountName"]?.ToString(),
                        Type = reader["Type"]?.ToString(),
                        Amount = reader["Amount"] as decimal? ?? 0,
                        PaidAmt = reader["PaidAmt"] as decimal? ?? 0,
                        Balance = reader["Balance"] as decimal? ?? 0
                    };
                    invoices.Add(invoice);
                }

                if (invoices.Count == 0)
                    return Json(new { status = false, message = "No invoices found for this customer" });

                return Ok(new { status = true, data = invoices });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region HRMS Dashboard

        public IActionResult HRMS()
        {
            return View();
        }

        #endregion

        #region Employee List
         
        public IActionResult EmployeeList()
        {
            return View();
        }

        #endregion

        #region Attendance 

        public IActionResult AttendanceSheet()
        {
            return View();
        }
        private TimeSpan ParseFlexibleTime(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return TimeSpan.MinValue; // indicate "no value" or invalid

            input = input.Trim();

            if (TimeSpan.TryParse(input, out var ts))
                return ts;

            if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dt))
                return dt.TimeOfDay;

            if (double.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out double dbl))
            {
                if (dbl >= 0 && dbl < 1)
                    return TimeSpan.FromDays(dbl);

                return TimeSpan.FromDays(dbl % 1);
            }

            // Not parseable
            return TimeSpan.MinValue;
        }

        //[HttpPost]
        //public async Task<IActionResult> SaveAttendance([FromBody] AttendanceSaveRequest model)
        //{
        //    if (model == null || model.AttendanceRows == null || model.AttendanceRows.Count == 0)
        //        return Json(new { status = false, message = "Invalid attendance data" });

        //    if (!model.IsAllDataSucceed)
        //        return Json(new { status = false, message = "Check All Data Required First..." });

        //    int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
        //    if (userId <= 0)
        //        return Json(new { status = false, message = "User not logged in" });

        //    try
        //    {
        //        var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
        //        {
        //            Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
        //        };

        //        using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
        //        await conn.OpenAsync();
        //        using var tran = await conn.BeginTransactionAsync();

        //        var firstRow = model.AttendanceRows.First();
        //        string empCode = firstRow.EmpId;
        //        DateTime workDate = firstRow.WorkDate;

        //        // --- Duplicate Check ---
        //        var dupQuery = @"SELECT COUNT(*) 
        //         FROM tbl_attendancesheet 
        //         WHERE code = @code 
        //           AND DAY(WorkDate) = @day 
        //           AND MONTH(WorkDate) = @month 
        //           AND YEAR(WorkDate) = @year";

        //        using (var cmdDup = new MySqlCommand(dupQuery, conn, tran))
        //        {
        //            cmdDup.Parameters.AddWithValue("@code", empCode);
        //            cmdDup.Parameters.AddWithValue("@day", workDate.Day);
        //            cmdDup.Parameters.AddWithValue("@month", workDate.Month);
        //            cmdDup.Parameters.AddWithValue("@year", workDate.Year);

        //            var exists = Convert.ToInt32(await cmdDup.ExecuteScalarAsync()) > 0;
        //            if (exists)
        //            {
        //                await tran.RollbackAsync();
        //                return BadRequest(new { status = false, message = $"Attendance already saved for {workDate:yyyy-MM-dd}" });
        //            }
        //        }


        //        // --- Check Missing Time In/Out ---
        //        foreach (var row in model.AttendanceRows)
        //        {
        //            if ((string.IsNullOrWhiteSpace(row.TimeIn) || string.IsNullOrWhiteSpace(row.TimeOut)) && row.Status.ToUpper() == "P")
        //            {
        //                await tran.RollbackAsync();
        //                return BadRequest(new
        //                {
        //                    status = false,
        //                    message = $"Date {row.WorkDate:yyyy-MM-dd} does not contain time, please check it."
        //                });
        //            }
        //        }

        //        // --- Start Salary Record Insert ---
        //        DateTime endOfMonth = new DateTime(workDate.Year, workDate.Month, DateTime.DaysInMonth(workDate.Year, workDate.Month));
        //        var salaryInsertQuery = @"
        //    INSERT INTO tbl_attendance_salary (date, emp_code, created_by, created_date)
        //    VALUES (@date, @emp_code, @created_by, @created_date);
        //    SELECT LAST_INSERT_ID();";

        //        decimal attendanceSalaryId;
        //        using (var cmdSalary = new MySqlCommand(salaryInsertQuery, conn, tran))
        //        {
        //            cmdSalary.Parameters.AddWithValue("@emp_code", empCode);
        //            cmdSalary.Parameters.AddWithValue("@date", endOfMonth);
        //            cmdSalary.Parameters.AddWithValue("@created_by", userId);
        //            cmdSalary.Parameters.AddWithValue("@created_date", DateTime.Now);
        //            attendanceSalaryId = Convert.ToDecimal(await cmdSalary.ExecuteScalarAsync());
        //        }

        //        // --- Fetch deduction config ---
        //        TimeSpan defaultTimeIn = TimeSpan.Zero;
        //        decimal deductionRate = 0;
        //        using (var cmdCfg = new MySqlCommand("SELECT delaytime, latearrivaldeduction FROM tbl_setting_deduction_config LIMIT 1", conn, tran))
        //        using (var reader = await cmdCfg.ExecuteReaderAsync())
        //        {
        //            if (await reader.ReadAsync())
        //            {
        //                defaultTimeIn = TimeSpan.Parse(reader["delaytime"].ToString());
        //                deductionRate = Convert.ToDecimal(reader["latearrivaldeduction"]);
        //            }
        //        }

        //        // --- Fetch employee salary ---
        //        decimal basicSalary = 0, housingAllowance = 0, transportationAllowance = 0, otherAllowance = 0, totalSalary = 0;
        //        int workDays = 30;
        //        DateTime contractIssueDate = DateTime.Now;

        //        using (var cmdEmp = new MySqlCommand(
        //            "SELECT BasicSalary, HousingAllowance, TransportationAllowance, Other, WorkDays, contractIssueDate FROM tbl_employee WHERE code = @code",
        //            conn, tran))
        //        {
        //            cmdEmp.Parameters.AddWithValue("@code", empCode);
        //            using var rdr = await cmdEmp.ExecuteReaderAsync();
        //            if (await rdr.ReadAsync())
        //            {
        //                workDays = Convert.ToInt32(rdr["WorkDays"]);
        //                basicSalary = Convert.ToDecimal(rdr["BasicSalary"]);
        //                housingAllowance = Convert.ToDecimal(rdr["HousingAllowance"]);
        //                transportationAllowance = Convert.ToDecimal(rdr["TransportationAllowance"]);
        //                otherAllowance = Convert.ToDecimal(rdr["Other"]);
        //                contractIssueDate = rdr["contractIssueDate"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(rdr["contractIssueDate"]);
        //                totalSalary = basicSalary + housingAllowance + transportationAllowance + otherAllowance;
        //            }
        //        }

        //        int totalAbsence = 0, totalDelayMinutes = 0;
        //        decimal totalDeduction = 0, dailyDeduction = 0;
        //        int workingDays = model.AttendanceRows.Count(r => r.Status.ToUpper() is "P" or "V");

        //        // --- Generate Ref_Code ---
        //        int ref_no;
        //        var refResult = await new MySqlCommand(
        //            "SELECT Ref_Code FROM tbl_attendancesheet WHERE YEAR(WorkDate)=@y AND MONTH(WorkDate)=@m LIMIT 1",
        //            conn, tran)
        //        {
        //            Parameters = { new("@y", workDate.Year), new("@m", workDate.Month) }
        //        }.ExecuteScalarAsync();

        //        if (refResult != null && refResult != DBNull.Value && Convert.ToInt32(refResult) > 0)
        //            ref_no = Convert.ToInt32(refResult);
        //        else
        //        {
        //            var maxRes = await new MySqlCommand("SELECT MAX(Ref_Code) FROM tbl_attendancesheet", conn, tran).ExecuteScalarAsync();
        //            ref_no = ((maxRes != DBNull.Value && maxRes != null) ? Convert.ToInt32(maxRes) : 600000) + 1;
        //        }

        //        // --- Insert Attendance Rows ---
        //        foreach (var row in model.AttendanceRows)
        //        {
        //            dailyDeduction = 0;
        //            TimeSpan timeInSpan = TimeSpan.Zero;
        //            TimeSpan timeOutSpan = TimeSpan.Zero;

        //            // Absent employee
        //            if (row.Status.ToUpper() == "A")
        //            {
        //                dailyDeduction = Math.Round(totalSalary / workingDays, 3);
        //                totalDeduction += dailyDeduction;
        //                totalAbsence++;
        //            }

        //            // Present employee - parse time
        //            if (row.Status.ToUpper() == "P")
        //            {
        //                string timeInStr = row.TimeIn?.Trim() ?? "00:00";
        //                string timeOutStr = row.TimeOut?.Trim() ?? "00:00";

        //                // Parse TimeIn
        //                timeInSpan = ParseFlexibleTime(timeInStr);
        //                if (timeInSpan == TimeSpan.MinValue)
        //                {
        //                    await tran.RollbackAsync();
        //                    return BadRequest(new { status = false, message = $"Invalid TimeIn format: {timeInStr} for date {row.WorkDate:yyyy-MM-dd}. Expected formats: HH:mm (24-hour) or h.mm AM/PM" });
        //                }

        //                // Parse TimeOut
        //                timeOutSpan = ParseFlexibleTime(timeOutStr);
        //                if (timeOutSpan == TimeSpan.MinValue)
        //                {
        //                    await tran.RollbackAsync();
        //                    return BadRequest(new { status = false, message = $"Invalid TimeOut format: {timeOutStr} for date {row.WorkDate:yyyy-MM-dd}. Expected formats: HH:mm (24-hour) or h.mm AM/PM" });
        //                }

        //                // Calculate delay and deduction
        //                int delayMinutes = timeInSpan > defaultTimeIn ? (int)(timeInSpan - defaultTimeIn).TotalMinutes : 0;
        //                decimal deduction = delayMinutes * deductionRate;

        //                totalDelayMinutes += delayMinutes;
        //                totalDeduction += deduction;
        //            }

        //            // Insert into MySQL
        //            var insertRow = @"
        //        INSERT INTO tbl_attendancesheet 
        //        (attendance_salary_id, code, WorkDate, TimeIn, TimeOut, DayOfWeek, Status, Reference, Ref_Code)
        //        VALUES (@salaryId, @code, @workDate, @timeIn, @timeOut, @dow, @status, @ref, @refCode);";

        //            using var cmdRow = new MySqlCommand(insertRow, conn, tran);
        //            cmdRow.Parameters.AddWithValue("@salaryId", attendanceSalaryId);
        //            cmdRow.Parameters.AddWithValue("@code", empCode);
        //            cmdRow.Parameters.AddWithValue("@workDate", row.WorkDate);
        //            cmdRow.Parameters.AddWithValue("@timeIn", timeInSpan.ToString(@"hh\:mm\:ss"));
        //            cmdRow.Parameters.AddWithValue("@timeOut", timeOutSpan.ToString(@"hh\:mm\:ss"));
        //            cmdRow.Parameters.AddWithValue("@dow", row.DayOfWeek);
        //            cmdRow.Parameters.AddWithValue("@status", row.Status);
        //            cmdRow.Parameters.AddWithValue("@ref", workDate.ToString("yyMM"));
        //            cmdRow.Parameters.AddWithValue("@refCode", ref_no);

        //            await cmdRow.ExecuteNonQueryAsync();
        //        }

        //        // --- Calculate Loan ---
        //        var loanCmd = new MySqlCommand(
        //            "SELECT SUM(amount) FROM tbl_loan WHERE YEAR(loandates)=@y AND MONTH(loandates)=@m AND employeeId=@e",
        //            conn, tran);
        //        loanCmd.Parameters.AddWithValue("@y", workDate.Year);
        //        loanCmd.Parameters.AddWithValue("@m", workDate.Month);
        //        loanCmd.Parameters.AddWithValue("@e", empCode);
        //        var loanVal = await loanCmd.ExecuteScalarAsync();
        //        decimal loanAmount = (loanVal != DBNull.Value && loanVal != null) ? Convert.ToDecimal(loanVal) : 0;

        //        decimal totalAdditions = basicSalary + housingAllowance + transportationAllowance + otherAllowance;
        //        decimal totalDeductions = totalDeduction + loanAmount;
        //        decimal netSalary = totalAdditions - totalDeductions;

        //        // --- Update Salary Summary ---
        //        var updateSalary = @"
        //    UPDATE tbl_attendance_salary SET
        //        absence_days=@ad,
        //        total_absence=@ta,
        //        delay_minutes=@dm,
        //        total_delay=@td,
        //        total_loan=@loan,
        //        net_salary=@net,
        //        pay=0,
        //        `change`=@net
        //    WHERE id=@id;";
        //        using (var cmdUp = new MySqlCommand(updateSalary, conn, tran))
        //        {
        //            cmdUp.Parameters.AddWithValue("@ad", totalAbsence);
        //            cmdUp.Parameters.AddWithValue("@ta", totalAbsence * dailyDeduction);
        //            cmdUp.Parameters.AddWithValue("@dm", totalDelayMinutes);
        //            cmdUp.Parameters.AddWithValue("@td", totalDelayMinutes * deductionRate);
        //            cmdUp.Parameters.AddWithValue("@loan", loanAmount);
        //            cmdUp.Parameters.AddWithValue("@net", netSalary);
        //            cmdUp.Parameters.AddWithValue("@id", attendanceSalaryId);
        //            await cmdUp.ExecuteNonQueryAsync();
        //        }

        //        // --- Insert Leave Salary ---
        //        decimal leaveDays = ((decimal)workDays - totalAbsence) / 365 * 30;
        //        decimal leaveAmount = (basicSalary * 12 / 365) * leaveDays;
        //        using (var cmdLeave = new MySqlCommand(@"
        //    INSERT INTO tbl_leave_salary(date,code,name,Reference,description,leave_days,credit,created_by,created_date)
        //    VALUES(@date,@code,@name,@ref,@desc,@ld,@credit,@by,@cd);", conn, tran))
        //        {
        //            cmdLeave.Parameters.AddWithValue("@date", workDate.ToString("MMM-yy"));
        //            cmdLeave.Parameters.AddWithValue("@code", empCode);
        //            cmdLeave.Parameters.AddWithValue("@name", model.EmpName);
        //            cmdLeave.Parameters.AddWithValue("@ref", workDate.ToString("yyMM"));
        //            cmdLeave.Parameters.AddWithValue("@desc", "Leave Salary");
        //            cmdLeave.Parameters.AddWithValue("@ld", Math.Round(leaveDays, 3));
        //            cmdLeave.Parameters.AddWithValue("@credit", Math.Round(leaveAmount, 3));
        //            cmdLeave.Parameters.AddWithValue("@by", model.CreatedBy);
        //            cmdLeave.Parameters.AddWithValue("@cd", DateTime.Now);
        //            await cmdLeave.ExecuteNonQueryAsync();
        //        }

        //        // --- Insert EOS ---
        //        int yearsOfService = (int)((DateTime.Now - contractIssueDate).TotalDays / 365);
        //        decimal eosDays = (yearsOfService < 5 ? (workDays - totalAbsence) / 365m * 21 : (workDays - totalAbsence) / 365m * 30);
        //        decimal eosAmount = (basicSalary * 12 / 365m) * eosDays;
        //        using (var cmdEos = new MySqlCommand(@"
        //    INSERT INTO tbl_end_of_service(date,code,name,Reference,description,debit,leave_days,credit,created_by,created_date)
        //    VALUES(@date,@code,@name,@ref,@desc,0,@ld,@credit,@by,@cd);", conn, tran))
        //        {
        //            cmdEos.Parameters.AddWithValue("@date", workDate.ToString("MMM-yy"));
        //            cmdEos.Parameters.AddWithValue("@code", empCode);
        //            cmdEos.Parameters.AddWithValue("@name", model.EmpName);
        //            cmdEos.Parameters.AddWithValue("@ref", workDate.ToString("yyMM"));
        //            cmdEos.Parameters.AddWithValue("@desc", "End Of Service");
        //            cmdEos.Parameters.AddWithValue("@ld", Math.Round(eosDays, 3));
        //            cmdEos.Parameters.AddWithValue("@credit", Math.Round(eosAmount, 3));
        //            cmdEos.Parameters.AddWithValue("@by", userId);
        //            cmdEos.Parameters.AddWithValue("@cd", DateTime.Now);
        //            await cmdEos.ExecuteNonQueryAsync();
        //        }

        //        await tran.CommitAsync();
        //        return Ok(new { status = true, message = "Employee Month Saved Successfully" });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new { status = false, message = ex.Message });
        //    }
        //}

        [HttpPost]
        public async Task<IActionResult> SaveAttendance([FromBody] AttendanceSaveRequest model)
        {
            if (model == null || model.AttendanceRows == null || model.AttendanceRows.Count == 0)
                return BadRequest(new { status = false, message = "Invalid request or no attendance rows provided." });

            // Basic validation: ensure all required fields present
            if (model.AttendanceRows.Any(r => string.IsNullOrWhiteSpace(r.EmpId)))
                return BadRequest(new { status = false, message = "Employee ID missing in one or more rows." });

            // Determine user id (session or provided)
            int userId = HttpContext.Session.GetInt32("UserId") ?? model.CreatedBy;
            if (userId <= 0)
                return Unauthorized(new { status = false, message = "User not logged in." });

            // Build connection string using session DB name if provided
            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
            };

            using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();

            using (var tx = await conn.BeginTransactionAsync())
            {
                try
                {
                    // 1) Validate: if any 'P' (present) row has empty time in/out -> fail
                    foreach (var row in model.AttendanceRows)
                    {
                        if (row.Status?.ToUpper() == "P")
                        {
                            if (string.IsNullOrWhiteSpace(row.TimeIn) || string.IsNullOrWhiteSpace(row.TimeOut))
                            {
                                await tx.RollbackAsync();
                                return BadRequest(new { status = false, message = $"Date {row.WorkDate:yyyy-MM-dd} does not contain time in/out for employee {row.EmpId}." });
                            }
                        }

                    }

                    //// We use the month/year of the first row (same logic as WinForms)
                    //var firstRow = model.AttendanceRows[0];
                    //int month = firstRow.WorkDate.Month;
                    //int year = firstRow.WorkDate.Year;
                    //DateTime endOfMonthDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                    //string checkDuplicateSql = "SELECT 1 FROM tbl_attendancesheet WHERE code=@code AND YEAR(WorkDate)=@year AND MONTH(WorkDate)=@month LIMIT 1;";
                    //using (var cmdDup = new MySqlCommand(checkDuplicateSql, conn, (MySqlTransaction)tx))
                    //{
                    //    cmdDup.Parameters.AddWithValue("@code", firstRow.EmpId);
                    //    cmdDup.Parameters.AddWithValue("@year", year);
                    //    cmdDup.Parameters.AddWithValue("@month", month);
                    //    var dupExists = await cmdDup.ExecuteScalarAsync();
                    //    if (dupExists != null)
                    //    {
                    //        await tx.RollbackAsync();
                    //        return Conflict(new { status = false, message = "Employee Month already saved before" });
                    //    }
                    //}

                    // Get first row month/year
                    var firstRow = model.AttendanceRows[0];
                    int month = firstRow.WorkDate.Month;
                    int year = firstRow.WorkDate.Year;
                    DateTime endOfMonthDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));

                    // Start and end of month
                    DateTime startOfMonth = new DateTime(year, month, 1);
                    DateTime endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                    // Get existing dates for that employee in that month
                    string getExistingSql = @"
    SELECT WorkDate
    FROM tbl_attendancesheet
    WHERE code = @code
    AND WorkDate >= @startDate
    AND WorkDate <= @endDate;";

                    var existingDates = new HashSet<DateTime>();

                    using (var cmd = new MySqlCommand(getExistingSql, conn, (MySqlTransaction)tx))
                    {
                        cmd.Parameters.Add("@code", MySqlDbType.VarChar).Value = firstRow.EmpId;
                        cmd.Parameters.Add("@startDate", MySqlDbType.Date).Value = startOfMonth;
                        cmd.Parameters.Add("@endDate", MySqlDbType.Date).Value = endOfMonth;

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                existingDates.Add(reader.GetDateTime("WorkDate").Date);
                            }
                        }
                    }
                    var newRows = model.AttendanceRows
                        .Where(r => !existingDates.Contains(r.WorkDate.Date))
                        .ToList();
                    if (newRows.Count == 0)
                    {
                        await tx.RollbackAsync();
                        return Conflict(new
                        {
                            status = false,
                            message = "Attendance already uploaded for this employee and month."
                        });
                    }


                    // 3) Insert into tbl_attendance_salary and get id (attendanceSalaryId) and compute refr like winforms
                    string insertAttendanceSalarySql = @"INSERT INTO `tbl_attendance_salary`(date, emp_code, created_by, created_date)
                                                        VALUES(@date,@emp_code,@created_by,@created_date);
                                                        SELECT LAST_INSERT_ID();";
                    long attendanceSalaryId;
                    using (var cmd = new MySqlCommand(insertAttendanceSalarySql, conn, (MySqlTransaction)tx))
                    {
                        cmd.Parameters.AddWithValue("@date", endOfMonthDate);
                        cmd.Parameters.AddWithValue("@emp_code", firstRow.EmpId);
                        cmd.Parameters.AddWithValue("@created_by", userId);
                        cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                        var scalar = await cmd.ExecuteScalarAsync();
                        attendanceSalaryId = Convert.ToInt64(scalar);
                    }

                    DateTime parseDateR = firstRow.WorkDate;
                    string refr = (parseDateR.Year % 100).ToString() + $"{parseDateR.Month:D2}";

                 
                    // 4) Determine ref_no logic (existing Ref_Code for that month or max+1)
                    object refNoResult;
                    using (var cmd = new MySqlCommand(@"SELECT Ref_Code FROM tbl_attendancesheet 
                                                       WHERE YEAR(WorkDate)=@year AND MONTH(WorkDate)=@month LIMIT 1", conn, (MySqlTransaction)tx))
                    {
                        cmd.Parameters.AddWithValue("@year", year);
                        cmd.Parameters.AddWithValue("@month", month);
                        refNoResult = await cmd.ExecuteScalarAsync();
                    }

                    int ref_no;
                    if (refNoResult != null && refNoResult != DBNull.Value && int.TryParse(refNoResult.ToString(), out int parsedRef) && parsedRef > 0)
                    {
                        ref_no = parsedRef;
                    }
                    else
                    {
                        object maxResult;
                        using (var cmd = new MySqlCommand("SELECT MAX(Ref_Code) FROM tbl_attendancesheet", conn, (MySqlTransaction)tx))
                        {
                            maxResult = await cmd.ExecuteScalarAsync();
                        }
                        int max = (maxResult != null && maxResult != DBNull.Value && int.TryParse(maxResult.ToString(), out int mm)) ? mm : 600000;
                        ref_no = max + 1;
                    }

                    // 5) Load settings: defaultTimeIn and deductionRate from tbl_setting_deduction_config (like WinForms)
                    TimeSpan defaultTimeIn = TimeSpan.Zero;
                    decimal deductionRate = 0m;
                    using (var cmd = new MySqlCommand("SELECT delaytime, latearrivaldeduction FROM tbl_setting_deduction_config LIMIT 1", conn, (MySqlTransaction)tx))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            defaultTimeIn = TimeSpan.Parse(reader["delaytime"].ToString());
                            deductionRate = Convert.ToDecimal(reader["latearrivaldeduction"]);
                        }
                        reader.Close();
                    }

                    // 6) Load employee salary data: basicSalary, allowances, workDays, contractIssueDate
                    decimal basicSalary = 0m, housingAllowance = 0m, transportationAllowance = 0m, otherAllowance = 0m;
                    int workDays = 30;
                    DateTime? contractIssueDate = null;

                    using (var cmd = new MySqlCommand(@"
        SELECT BasicSalary, HousingAllowance, TransportationAllowance,
               Other, WorkDays, ContractIssueDate
        FROM tbl_employee 
        WHERE code = @code LIMIT 1", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@code", firstRow.EmpId);

                        using var rdr = await cmd.ExecuteReaderAsync();

                        if (await rdr.ReadAsync())
                        {
                            workDays = Convert.ToInt32(rdr["WorkDays"]);
                            basicSalary = Convert.ToDecimal(rdr["BasicSalary"]);
                            housingAllowance = Convert.ToDecimal(rdr["HousingAllowance"]);
                            transportationAllowance = Convert.ToDecimal(rdr["TransportationAllowance"]);
                            otherAllowance = Convert.ToDecimal(rdr["Other"]);
                            if (!rdr.IsDBNull(rdr.GetOrdinal("ContractIssueDate")))
                            {
                                contractIssueDate = rdr.GetDateTime(rdr.GetOrdinal("ContractIssueDate"));
                            }
                        }

                    }



                    decimal totalSalary = basicSalary + housingAllowance + transportationAllowance + otherAllowance;

                    // compute workingDays as count of P or V like original
                    int workingDaysCount = model.AttendanceRows.Count(r => !string.IsNullOrWhiteSpace(r.Status) &&
                                                                            (r.Status.ToUpper() == "P" || r.Status.ToUpper() == "V"));

                    // Initialize accumulators
                    decimal totalDeduction = 0m;
                    int totalAbsence = 0;
                    int totalDelayMinutes = 0;
                    decimal dailyDeduction = 0m;
                    decimal deductionAmount = 0m;
                    int idOfLastInsertedAttendanceRow = 0;
                    int totalPrecentDays = 0;

                    // 7) Loop through rows and insert into tbl_attendancesheet and compute deductions
                    string insertAttendanceRowSql = @"
                        INSERT INTO tbl_attendancesheet 
                        (attendance_salary_id, code, WorkDate, TimeIn, TimeOut, DayOfWeek, Status, Reference, Ref_Code) 
                        VALUES(@attendance_salary_id, @code, @WorkDate, @TimeIn, @TimeOut, @DayOfWeek, @Status, @Reference, @Ref_Code);
                        SELECT LAST_INSERT_ID();";

                    foreach (var row in model.AttendanceRows)
                    {
                        string status = (row.Status ?? "").ToUpper();

                        if (status == "A")
                        {
                            // daily deduction is totalSalary / workingDaysCount
                            dailyDeduction = workingDaysCount > 0 ? Math.Round(totalSalary / workingDaysCount, 3) : 0m;
                            totalDeduction += dailyDeduction;
                            totalAbsence++;
                        }
                        else if (status == "P")
                        {
                            // Find setting applicable for this date (original code used tbl_setting_attendance; logic: WHERE DATE >= @date AND DATE <= @date AND state=1)
                            decimal settingDeductionRate = deductionRate;
                            using (var cmd = new MySqlCommand(@"SELECT (SELECT latearrivaldeduction FROM tbl_setting_deduction_config) AS deduction
                                                              FROM tbl_setting_attendance
                                                              WHERE DATE >= @date AND DATE <= @date AND state=1 LIMIT 1", conn, (MySqlTransaction)tx))
                            {
                                cmd.Parameters.AddWithValue("@date", row.WorkDate);
                                var s = await cmd.ExecuteScalarAsync();
                                if (s != null && s != DBNull.Value)
                                {
                                    settingDeductionRate = Convert.ToDecimal(s);
                                }
                            }

                            // compute delay minutes
                            if (!string.IsNullOrWhiteSpace(row.TimeIn) && TimeSpan.TryParse(row.TimeIn, out TimeSpan actualTimeIn))
                            {
                                int delayMinutes = (actualTimeIn > defaultTimeIn) ? (int)(actualTimeIn - defaultTimeIn).TotalMinutes : 0;
                                totalDelayMinutes += delayMinutes;
                                deductionAmount = delayMinutes * settingDeductionRate;
                                totalDeduction += deductionAmount;
                            }

                            totalPrecentDays++;
                        }

                        // before inserting each row:
                        TimeSpan? parsedTimeIn = null;
                        TimeSpan? parsedTimeOut = null;

                        // only parse/require when status = "P" (present) — adjust based on your validation rules
                        if (!string.IsNullOrWhiteSpace(row.TimeIn))
                        {
                            var tIn = ParseFlexibleTime(row.TimeIn);
                            if (tIn == TimeSpan.MinValue)
                            {
                                await tx.RollbackAsync();
                                return BadRequest(new { status = false, message = $"Invalid TimeIn format: '{row.TimeIn}' for date {row.WorkDate:yyyy-MM-dd}." });
                            }
                            parsedTimeIn = tIn;
                        }

                        if (!string.IsNullOrWhiteSpace(row.TimeOut))
                        {
                            var tOut = ParseFlexibleTime(row.TimeOut);
                            if (tOut == TimeSpan.MinValue)
                            {
                                await tx.RollbackAsync();
                                return BadRequest(new { status = false, message = $"Invalid TimeOut format: '{row.TimeOut}' for date {row.WorkDate:yyyy-MM-dd}." });
                            }
                            parsedTimeOut = tOut;
                        }
                        // Insert attendance row
                        using (var cmd = new MySqlCommand(insertAttendanceRowSql, conn, (MySqlTransaction)tx))
                        {
                            cmd.Parameters.AddWithValue("@attendance_salary_id", attendanceSalaryId);
                            cmd.Parameters.AddWithValue("@code", row.EmpId ?? (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@WorkDate", row.WorkDate.ToString("yyyy-MM-dd"));
                            cmd.Parameters.AddWithValue("@TimeIn", parsedTimeIn.HasValue ? parsedTimeIn.Value.ToString(@"hh\:mm\:ss") : (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@TimeOut", parsedTimeOut.HasValue ? parsedTimeOut.Value.ToString(@"hh\:mm\:ss") : (object)DBNull.Value);
                            cmd.Parameters.AddWithValue("@DayOfWeek", row.DayOfWeek ?? "");
                            cmd.Parameters.AddWithValue("@Status", row.Status ?? "");
                            cmd.Parameters.AddWithValue("@Reference", refr);
                            cmd.Parameters.AddWithValue("@Ref_Code", ref_no.ToString());
                            var insertedScalar = await cmd.ExecuteScalarAsync();
                            idOfLastInsertedAttendanceRow = Convert.ToInt32(insertedScalar);
                        }
                    } // foreach rows

                    // 8) Determine ss_no for attendance_salary record
                    int ss_no;
                    using (var cmd = new MySqlCommand(@"SELECT ss_no FROM tbl_attendance_salary WHERE YEAR(date)=@year AND MONTH(date)=@month LIMIT 1", conn, (MySqlTransaction)tx))
                    {
                        cmd.Parameters.AddWithValue("@year", year);
                        cmd.Parameters.AddWithValue("@month", month);
                        var res = await cmd.ExecuteScalarAsync();
                        if (res != null && res != DBNull.Value && int.TryParse(res.ToString(), out int r))
                        {
                            ss_no = r;
                        }
                        else
                        {
                            object maxResult;
                            using (var cmd2 = new MySqlCommand("SELECT MAX(ss_no) FROM tbl_attendance_salary", conn, (MySqlTransaction)tx))
                            {
                                maxResult = await cmd2.ExecuteScalarAsync();
                            }
                            int max = (maxResult != null && maxResult != DBNull.Value && int.TryParse(maxResult.ToString(), out int mmx)) ? mmx : 0;
                            ss_no = max + 1;
                        }
                    }

                    // 9) Get employee loan for the month/year
                    decimal loanAmount = await GetEmployeeLoanAsync(conn, tx, firstRow.EmpId, month, year);

                    // 10) totals and net salary calculation
                    decimal totalDeductions = totalDeduction + loanAmount;
                    decimal totalAdditions = basicSalary + housingAllowance + transportationAllowance + otherAllowance;
                    decimal netSalary = totalAdditions - totalDeductions;

                    // 11) Update tbl_attendance_salary with computed values
                    using (var cmd = new MySqlCommand(@"UPDATE tbl_attendance_salary SET absence_days=@absence_days,
                                                                 total_absence=@total_absence,
                                                                 delay_minutes=@delay_minutes,
                                                                 total_delay=@total_delay,
                                                                 total_loan=@total_loan,
                                                                 net_salary=@net_salary,
                                                                 pay=0,
                                                                 `change`=@net_salary,
                                                                 ss_no=@ss_no
                                                          WHERE id=@id", conn, (MySqlTransaction)tx))
                    {
                        cmd.Parameters.AddWithValue("@id", attendanceSalaryId);
                        cmd.Parameters.AddWithValue("@absence_days", totalAbsence);
                        cmd.Parameters.AddWithValue("@total_absence", totalAbsence * (workingDaysCount > 0 ? Math.Round(totalSalary / workingDaysCount, 3) : 0m));
                        cmd.Parameters.AddWithValue("@delay_minutes", totalDelayMinutes);
                        cmd.Parameters.AddWithValue("@total_delay", totalDelayMinutes * deductionRate);
                        cmd.Parameters.AddWithValue("@total_loan", loanAmount);
                        cmd.Parameters.AddWithValue("@net_salary", netSalary);
                        cmd.Parameters.AddWithValue("@ss_no", ss_no);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // 12) Insert journals for salary (two entries)
                    await InsertJournalEntriesForSalaryAsync(conn, tx, netSalary, idOfLastInsertedAttendanceRow, firstRow.EmpId, userId, firstRow.WorkDate);

                    // 13) Save Leave Salary (LS)
                    await SaveLeaveSalaryAsync(conn, tx, firstRow, workDays, totalAbsence, basicSalary, userId, idOfLastInsertedAttendanceRow);

                    // 14) Save End Of Service (EOS)
                    await SaveEndOfServiceAsync(conn, tx, firstRow, workDays, totalAbsence, basicSalary, contractIssueDate.Value, userId, idOfLastInsertedAttendanceRow);

                    // Commit transaction
                    await tx.CommitAsync();

                    
                    return Ok(new { status = true, message = "Employee Month Saved Successfully" });
                }
                catch (Exception ex)
                {
                    try { await tx.RollbackAsync(); } catch { }
                    return StatusCode(500, new { status = false, message = ex.Message });
                }
            }
        }

        private async Task<decimal> GetEmployeeLoanAsync(MySqlConnection conn, MySqlTransaction tx, string empCode, int month, int year)
        {
            if (string.IsNullOrEmpty(empCode)) return 0m;
            using var cmd = new MySqlCommand(@"SELECT SUM(amount) AS totalLoan FROM tbl_loan
                                               WHERE YEAR(loandates)=@year AND MONTH(loandates)=@month AND employeeId=@code", conn, (MySqlTransaction)tx);
            cmd.Parameters.AddWithValue("@year", year);
            cmd.Parameters.AddWithValue("@month", month);
            cmd.Parameters.AddWithValue("@code", empCode);
            var res = await cmd.ExecuteScalarAsync();
            if (res == null || res == DBNull.Value) return 0m;
            return Convert.ToDecimal(res);
        }

        private async Task<int> GetAccountIdByCategoryAsync(MySqlConnection conn, MySqlTransaction tx, string category)
        {
            using var cmd = new MySqlCommand("SELECT account_id FROM tbl_coa_config WHERE category=@category LIMIT 1", conn, (MySqlTransaction)tx);
            cmd.Parameters.AddWithValue("@category", category);
            var res = await cmd.ExecuteScalarAsync();
            if (res == null || res == DBNull.Value) return 0;
            return Convert.ToInt32(res);
        }

        private async Task InsertJournalEntriesForSalaryAsync(MySqlConnection conn, MySqlTransaction tx, decimal netSalary, int attendanceRowId, string empCode, int userId, DateTime workDate)
        {
            // Get accrued salary account
            int accruedSalaryAccountId = await GetAccountIdByCategoryAsync(conn, tx, "Accrued Salaries");
            if (accruedSalaryAccountId == 0)
                throw new Exception("No Default Account Set for 'Accrued Salaries' Please Add It");

            // Get employee internal id (hum id)
            object empIdResult;
            using (var cmd = new MySqlCommand("SELECT id FROM tbl_employee WHERE code=@code LIMIT 1", conn, (MySqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@code", empCode);
                empIdResult = await cmd.ExecuteScalarAsync();
            }
            string employeeId = empIdResult?.ToString() ?? "0";

            // Get employee account id from tbl_coa_config category "Salaries"
            object empAccountIdResult;
            using (var cmd = new MySqlCommand("SELECT account_id FROM tbl_coa_config WHERE category=@cat LIMIT 1", conn, (MySqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@cat", "Salaries");
                empAccountIdResult = await cmd.ExecuteScalarAsync();
            }
            string empAccId = empAccountIdResult?.ToString() ?? "0";

            string description = "Employee Salary";
            string refr = (workDate.Year % 100).ToString() + $"{workDate.Month:D2}";
            string voucherNote = "Salary Sheet NO. " + refr;

            // Debit the employee account (credit is zero)
            await AddTransactionEntryAsync(conn, tx, DateTime.Now.Date, empAccId, netSalary, 0, attendanceRowId.ToString(), employeeId, description, "Salary", voucherNote, userId, DateTime.Now.Date, "");

            // Credit the accrued salary account
            await AddTransactionEntryAsync(conn, tx, DateTime.Now.Date, accruedSalaryAccountId.ToString(), 0, netSalary, attendanceRowId.ToString(), "0", description, "Salary", voucherNote, userId, DateTime.Now.Date, "");
        }

        private async Task SaveLeaveSalaryAsync(MySqlConnection conn, MySqlTransaction tx, AttendanceRow firstRow, int workDays, int totalAbsence, decimal basicSalary, int userId, int attendanceId)
        {
            int debitAccountId = await GetAccountIdByCategoryAsync(conn, tx, "Leave Salary Debit");
            int creditAccountId = await GetAccountIdByCategoryAsync(conn, tx, "Leave Salary Credit");

            DateTime parseDate = firstRow.WorkDate;
            string dateText = parseDate.ToString("MMM-yy");
            string refr = (parseDate.Year % 100).ToString() + $"{parseDate.Month:D2}";
            decimal leaveDays = ((decimal)workDays - (decimal)totalAbsence) / 365m * 30m;
            decimal leaveAmount = ((decimal)basicSalary * 12m) / 365m * leaveDays;
            string voucherNote = "Leave Salary " + refr;
            string description = "Leave Salary";

            // Insert leave salary record
            using (var cmd = new MySqlCommand(@"INSERT INTO tbl_leave_salary(date, code, name, Reference, description, leave_days, credit, created_by, created_date)
                                              VALUES(@date, @code, @name, @Reference, @description, @leave_days, @credit, @created_by, @created_date)", conn, (MySqlTransaction)tx))
            {
                cmd.Parameters.AddWithValue("@date", dateText);
                cmd.Parameters.AddWithValue("@code", firstRow.EmpId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Reference", refr);
                cmd.Parameters.AddWithValue("@name", firstRow.EmpName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@leave_days", Math.Round(leaveDays, 3));
                cmd.Parameters.AddWithValue("@credit", Math.Round(leaveAmount, 3));
                cmd.Parameters.AddWithValue("@created_by", userId);
                cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                await cmd.ExecuteNonQueryAsync();
            }

           
            // Insert transactions
            await AddTransactionEntryAsync(conn, tx, DateTime.Now.Date, debitAccountId.ToString(), leaveAmount, 0, attendanceId.ToString(), firstRow.EmpId, description, "Leave Salary", voucherNote, userId, DateTime.Now.Date, "");
            await AddTransactionEntryAsync(conn, tx, DateTime.Now.Date, creditAccountId.ToString(), 0, leaveAmount, attendanceId.ToString(), "0", description, "Leave Salary", voucherNote, userId, DateTime.Now.Date, "");
        }

        private async Task SaveEndOfServiceAsync(
     MySqlConnection conn,
     MySqlTransaction tx,
     AttendanceRow firstRow,
     int workDays,
     int totalAbsence,
     decimal basicSalary,
     DateTime contractIssueDate,
     int userId,
     int attendanceId)
        {
            int debitAccountId = await GetAccountIdByCategoryAsync(conn, tx, "End of Service Debit");
            int creditAccountId = await GetAccountIdByCategoryAsync(conn, tx, "End of Service Credit");

            DateTime workDate = firstRow.WorkDate;

            // SAME AS WINDOWS VERSION: "MMM-yy"
            string dateText = workDate.ToString("MMM-yy");

            // SAME FORMAT AS WINDOWS: YY0MM
            //string refr = (workDate.Year % 100).ToString("D2") + workDate.Month.ToString("D2");
            string refr = $"{workDate.Year % 100:D2}0{workDate.Month:D2}";

            // (parseDate.Year % 100) + $"0{month:D2}";
            // SAME FORMULA: Convert total days to years

            DateTime now = DateTime.Now;

            int yearsOfService = now.Year - contractIssueDate.Year;

            if (now < contractIssueDate.AddYears(yearsOfService))
            {
                yearsOfService--;
            }

            //int yearsOfService = (int)((DateTime.Now - contractIssueDate).TotalDays / 365);

            // SAME FORMULA AS WINDOWS
            decimal netWorkedDays = (decimal)workDays - (decimal)totalAbsence;

            decimal endOfServiceDays = (yearsOfService < 5)
    ? (((decimal)workDays - (decimal)totalAbsence) / 365m * 21m)
    : (((decimal)workDays - (decimal)totalAbsence) / 365m * 30m);

            // EXACT Windows formula for amount
            decimal endOfServiceAmount = ((basicSalary * 12m) / 365m) * endOfServiceDays;

            // ROUND only when inserting into DB, not before
            decimal roundedEndOfServiceAmount = Math.Round(endOfServiceAmount, 3);

            // SAME FORMULA AS WINDOWS:
            // decimal endOfServiceAmount = ((basicSalary * 12m) / 365m) * endOfServiceDays;

            string description = "End Of Service";
            string voucherNote = "End Of Services NO. " + refr;

            // Insert EOS record
            using (var cmd = new MySqlCommand(@"
        INSERT INTO tbl_end_of_service
        (`date`, `code`, `name`, `Reference`, `description`, `debit`, `leave_days`, `credit`, `created_by`, `created_date`)
        VALUES
        (@date, @code, @name, @Reference, @description, @debit, @leave_days, @credit, @created_by, @created_date)",
                conn, tx))
            {
                cmd.Parameters.AddWithValue("@date", dateText);
                cmd.Parameters.AddWithValue("@code", firstRow.EmpId ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@name", firstRow.EmpName ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Reference", refr);
                cmd.Parameters.AddWithValue("@description", description);
                cmd.Parameters.AddWithValue("@debit", 0);
                cmd.Parameters.AddWithValue("@leave_days", Math.Round(endOfServiceDays, 3));
                cmd.Parameters.AddWithValue("@credit", Math.Round(endOfServiceAmount, 3));
                cmd.Parameters.AddWithValue("@created_by", userId);
                cmd.Parameters.AddWithValue("@created_date", DateTime.Now.Date);
                await cmd.ExecuteNonQueryAsync();
            }

            // JOURNAL ENTRY (same as Windows)
            await AddTransactionEntryAsync(conn, tx,DateTime.Now.Date,debitAccountId.ToString(),endOfServiceAmount, 0, attendanceId.ToString(),
                firstRow.EmpId,description,"End Of Services",voucherNote, userId, DateTime.Now.Date, "");

            await AddTransactionEntryAsync(conn, tx, DateTime.Now.Date, creditAccountId.ToString(), 0, endOfServiceAmount, attendanceId.ToString(),
                "0",description, "End Of Services", voucherNote,userId,
                DateTime.Now.Date,
                "");
        }

        private async Task AddTransactionEntryAsync(MySqlConnection conn, MySqlTransaction tx,
                                                    DateTime date, string accountId, decimal debit, decimal credit,
                                                    string transactionId, string humId, string type, string voucher_name,
                                                    string description, int createdBy, DateTime createdDate, string voucherNo)
        {
            using var cmd = new MySqlCommand(@"INSERT INTO tbl_transaction
            (date, account_id, debit, credit, transaction_id, hum_id, t_type, type, description, created_by, created_date, state, voucher_no)
            VALUES (@date, @accountId, @debit, @credit, @transactionId, @hum_id, @tType, @type, @description, @createdBy, @createdDate, 0, @voucher_no)", conn, (MySqlTransaction)tx);
            cmd.Parameters.AddWithValue("@date", date);
            cmd.Parameters.AddWithValue("@accountId", accountId);
            cmd.Parameters.AddWithValue("@debit", debit);
            cmd.Parameters.AddWithValue("@credit", credit);
            cmd.Parameters.AddWithValue("@transactionId", transactionId);
            cmd.Parameters.AddWithValue("@type", type);
            cmd.Parameters.AddWithValue("@tType", voucher_name);
            cmd.Parameters.AddWithValue("@hum_id", humId);
            cmd.Parameters.AddWithValue("@description", description);
            cmd.Parameters.AddWithValue("@createdBy", createdBy);
            cmd.Parameters.AddWithValue("@createdDate", createdDate);
            cmd.Parameters.AddWithValue("@voucher_no", voucherNo);
            await cmd.ExecuteNonQueryAsync();
        }


        [HttpGet]
        public async Task<IActionResult> GetAttendance(string? employeeName = null, string? month = null, string? year = null)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY a.WorkDate) AS Sn,
                a.code AS EmployeeCode,
                e.name AS EmployeeName,
                DATE_FORMAT(a.WorkDate, '%Y-%m-%d') AS WorkDate,
                TIME_FORMAT(a.TimeIn, '%h:%i %p') AS TimeIn,
                TIME_FORMAT(a.TimeOut, '%h:%i %p') AS TimeOut,
                a.DayOfWeek,
                a.Status
            FROM tbl_attendancesheet a
            INNER JOIN tbl_employee e ON a.code = e.code
            WHERE 1 = 1";

                var parameters = new List<MySqlParameter>();

                // Filter by employee name (case-insensitive)
                if (!string.IsNullOrEmpty(employeeName))
                {
                    query += " AND LOWER(e.name) = LOWER(@empName)";
                    parameters.Add(new MySqlParameter("@empName", employeeName.Trim()));
                }

                // Filter by month and year
                if (!string.IsNullOrEmpty(month) && !string.IsNullOrEmpty(year))
                {
                    int monthInt = int.Parse(month.TrimStart('0'));
                    int yearInt = int.Parse(year);

                    query += " AND MONTH(a.WorkDate) = @month AND YEAR(a.WorkDate) = @year";
                    parameters.Add(new MySqlParameter("@month", monthInt));
                    parameters.Add(new MySqlParameter("@year", yearInt));
                }

                query += " ORDER BY a.WorkDate";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                var attendanceList = new List<object>();
                await using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    attendanceList.Add(new
                    {
                        Sn = reader["Sn"],
                        EmployeeCode = reader["EmployeeCode"],
                        EmployeeName = reader["EmployeeName"],
                        WorkDate = reader["WorkDate"],
                        TimeIn = reader["TimeIn"],
                        TimeOut = reader["TimeOut"],
                        DayOfWeek = reader["DayOfWeek"],
                        Status = reader["Status"]
                    });
                }

                return Ok(new { status = true, data = attendanceList });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAttendance(string employeeName, string month, string year)
        {
            if (string.IsNullOrWhiteSpace(employeeName) ||
                string.IsNullOrWhiteSpace(month) ||
                string.IsNullOrWhiteSpace(year))
                return BadRequest(new { status = false, message = "Invalid input" });

            int monthInt = ParseMonthToInt(month);
            int yearInt = ParseYearToInt(year);

            if (monthInt == 0 || yearInt == 0)
                return BadRequest(new { status = false, message = "Invalid month or year" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(
                    _config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();
                await using var tx = await conn.BeginTransactionAsync();

                // 1️⃣ Get Employee Code
                string empCode;
                using (var cmd = new MySqlCommand(
                    "SELECT code FROM tbl_employee WHERE LOWER(name)=LOWER(@name) LIMIT 1",
                    conn, (MySqlTransaction)tx))
                {
                    cmd.Parameters.AddWithValue("@name", employeeName.Trim());
                    var result = await cmd.ExecuteScalarAsync();

                    if (result == null)
                    {
                        await tx.RollbackAsync();
                        return NotFound(new { status = false, message = "Employee not found" });
                    }

                    empCode = result.ToString();
                }

                // 2️⃣ Get Salary IDs
                var salaryIds = new List<long>();

                using (var cmd = new MySqlCommand(@"
SELECT DISTINCT attendance_salary_id
FROM tbl_attendancesheet
WHERE code=@code
AND MONTH(WorkDate)=@month
AND YEAR(WorkDate)=@year",
                conn, (MySqlTransaction)tx))
                {
                    cmd.Parameters.AddWithValue("@code", empCode);
                    cmd.Parameters.AddWithValue("@month", monthInt);
                    cmd.Parameters.AddWithValue("@year", yearInt);

                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        if (!reader.IsDBNull(0))
                            salaryIds.Add(reader.GetInt64(0));
                    }
                }

                if (salaryIds.Count == 0)
                {
                    await tx.RollbackAsync();
                    return Ok(new { status = false, message = "No attendance found" });
                }

                string ids = string.Join(",", salaryIds);

                // 3️⃣ Delete Attendance Sheet
                using (var cmd = new MySqlCommand(@"
DELETE FROM tbl_attendancesheet
WHERE code=@code
AND MONTH(WorkDate)=@month
AND YEAR(WorkDate)=@year",
                conn, (MySqlTransaction)tx))
                {
                    cmd.Parameters.AddWithValue("@code", empCode);
                    cmd.Parameters.AddWithValue("@month", monthInt);
                    cmd.Parameters.AddWithValue("@year", yearInt);

                    await cmd.ExecuteNonQueryAsync();
                }

                // 4️⃣ Delete Salary
                if (!string.IsNullOrEmpty(ids))
                {
                    using var cmd = new MySqlCommand(
                        $"DELETE FROM tbl_attendance_salary WHERE id IN ({ids})",
                        conn, (MySqlTransaction)tx);

                    await cmd.ExecuteNonQueryAsync();
                }

                // 5️⃣ Delete Transactions
                if (!string.IsNullOrEmpty(ids))
                {
                    using var cmd = new MySqlCommand(
                        $"DELETE FROM tbl_transaction WHERE transaction_id IN ({ids})",
                        conn, (MySqlTransaction)tx);

                    await cmd.ExecuteNonQueryAsync();
                }

                // Build pattern to match stored format e.g. "مارس-26"
                string datePattern = $"{MonthToArabic(monthInt)}-{yearInt % 100:D2}";

                // 6️⃣ Delete Leave Salary
                using (var cmd = new MySqlCommand(@"
    DELETE FROM tbl_leave_salary
    WHERE code=@code AND date=@datePattern",
                    conn, (MySqlTransaction)tx))
                {
                    cmd.Parameters.AddWithValue("@code", empCode);
                    cmd.Parameters.AddWithValue("@datePattern", datePattern);
                    await cmd.ExecuteNonQueryAsync();
                }

                // 7️⃣ Delete End Of Service
                using (var cmd = new MySqlCommand(@"
    DELETE FROM tbl_end_of_service
    WHERE code=@code AND date=@datePattern",
                    conn, (MySqlTransaction)tx))
                {
                    cmd.Parameters.AddWithValue("@code", empCode);
                    cmd.Parameters.AddWithValue("@datePattern", datePattern);
                    await cmd.ExecuteNonQueryAsync();
                }
                await tx.CommitAsync();

                return Ok(new { status = true, message = "Attendance deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }
        private static int ParseMonthToInt(string input)
        {
            var part = input?.Split('-')[0].Trim() ?? "";
            if (int.TryParse(part.TrimStart('0'), out int n) && n is >= 1 and <= 12) return n;

            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["يناير"] = 1,
                ["فبراير"] = 2,
                ["مارس"] = 3,
                ["أبريل"] = 4,
                ["ابريل"] = 4,
                ["مايو"] = 5,
                ["يونيو"] = 6,
                ["يوليو"] = 7,
                ["أغسطس"] = 8,
                ["اغسطس"] = 8,
                ["سبتمبر"] = 9,
                ["أكتوبر"] = 10,
                ["اكتوبر"] = 10,
                ["نوفمبر"] = 11,
                ["ديسمبر"] = 12,
                ["january"] = 1,
                ["february"] = 2,
                ["march"] = 3,
                ["april"] = 4,
                ["may"] = 5,
                ["june"] = 6,
                ["july"] = 7,
                ["august"] = 8,
                ["september"] = 9,
                ["october"] = 10,
                ["november"] = 11,
                ["december"] = 12
            };

            return map.TryGetValue(part, out int v) ? v : 0;
        }

        private static int ParseYearToInt(string input)
        {
            var part = input?.Split('-').Last().Trim() ?? "";
            if (!int.TryParse(part, out int y)) return DateTime.Now.Year;
            return y < 100 ? y + 2000 : y;
        }

        private static string MonthToArabic(int month) => month switch
        {
            1 => "يناير",
            2 => "فبراير",
            3 => "مارس",
            4 => "أبريل",
            5 => "مايو",
            6 => "يونيو",
            7 => "يوليو",
            8 => "أغسطس",
            9 => "سبتمبر",
            10 => "أكتوبر",
            11 => "نوفمبر",
            12 => "ديسمبر",
            _ => ""
        };
        #endregion

        #region Salary

        public IActionResult SalarySheet()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetSalarySheet(int month, int year)
        {
            try
            {
                // Validate month and year
                if (month < 1 || month > 12)
                {
                    return BadRequest(new { status = false, message = "Invalid month. Must be between 1 and 12." });
                }

                if (year < 2000 || year > 2050)
                {
                    return BadRequest(new { status = false, message = "Invalid year. Must be between 2000 and 2050." });
                }

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Get Reference Code
                string refCode = "";
                try
                {
                    string refQuery = @"SELECT CONCAT(Ref_Code, ' - ', YEAR(WorkDate)) AS Ref_Code 
                               FROM tbl_attendancesheet 
                               WHERE YEAR(WorkDate) = @year AND MONTH(WorkDate) = @month 
                               LIMIT 1";

                    await using (var cmd = new MySqlCommand(refQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@year", year);
                        cmd.Parameters.AddWithValue("@month", month);
                        var refResult = await cmd.ExecuteScalarAsync();
                        refCode = (refResult != null && refResult != DBNull.Value) ? refResult.ToString() : "";
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                // Get SS Number
                string ssNo = "";
                try
                {
                    string ssNoQuery = @"SELECT ss_no FROM tbl_attendance_salary 
                                WHERE YEAR(date) = @year AND MONTH(date) = @month 
                                LIMIT 1";

                    await using (var cmd = new MySqlCommand(ssNoQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@year", year);
                        cmd.Parameters.AddWithValue("@month", month);
                        var ssResult = await cmd.ExecuteScalarAsync();
                        ssNo = (ssResult != null && ssResult != DBNull.Value) ? ssResult.ToString() : "";
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                // Get Max Work Date
                string dated = "";
                try
                {
                    string dateQuery = @"SELECT MAX(WorkDate) AS dated FROM tbl_attendancesheet 
                                WHERE YEAR(WorkDate) = @year AND MONTH(WorkDate) = @month 
                                LIMIT 1";

                    await using (var cmd = new MySqlCommand(dateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@year", year);
                        cmd.Parameters.AddWithValue("@month", month);
                        var dateResult = await cmd.ExecuteScalarAsync();
                        if (dateResult != null && dateResult != DBNull.Value)
                        {
                            DateTime workDate = Convert.ToDateTime(dateResult);
                            dated = workDate.ToString("dd-MM-yyyy");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }

                // Get Employee Data - First load all employees into memory
                var employees = new List<Dictionary<string, object>>();

                try
                {
                    string employeeQuery = @"SELECT 
                                    ROW_NUMBER() OVER(ORDER BY e.code) AS SN, 
                                    e.code,
                                    e.name,
                                    e.basicSalary,
                                    e.HousingAllowance,
                                    e.TransportationAllowance,
                                    e.Other,
                                    p.name AS PositionName
                                    FROM tbl_employee e
                                    LEFT JOIN tbl_position p ON e.Position_id = p.id
                                    WHERE e.state = 0";

                    await using (var empCmd = new MySqlCommand(employeeQuery, conn))
                    {
                        await using var empReader = await empCmd.ExecuteReaderAsync();

                        while (await empReader.ReadAsync())
                        {
                            var employee = new Dictionary<string, object>
                            {
                                ["SN"] = Convert.ToInt32(empReader["SN"]),
                                ["code"] = empReader["code"].ToString(),
                                ["name"] = empReader["name"].ToString(),
                                ["PositionName"] = empReader["PositionName"]?.ToString() ?? "",
                                ["basicSalary"] = empReader["basicSalary"] != DBNull.Value
                                    ? Convert.ToDecimal(empReader["basicSalary"]) : 0m,
                                ["HousingAllowance"] = empReader["HousingAllowance"] != DBNull.Value
                                    ? Convert.ToDecimal(empReader["HousingAllowance"]) : 0m,
                                ["TransportationAllowance"] = empReader["TransportationAllowance"] != DBNull.Value
                                    ? Convert.ToDecimal(empReader["TransportationAllowance"]) : 0m,
                                ["Other"] = empReader["Other"] != DBNull.Value
                                    ? Convert.ToDecimal(empReader["Other"]) : 0m
                            };
                            employees.Add(employee);
                        }
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { status = false, message = "Error loading employees: " + ex.Message });
                }

                // Now process each employee
                var salaryData = new List<Dictionary<string, object>>();

                foreach (var employee in employees)
                {
                    try
                    {
                        string empCode = employee["code"].ToString();
                        string empName = employee["name"].ToString();
                        string positionName = employee["PositionName"].ToString();
                        int serialNumber = Convert.ToInt32(employee["SN"]);

                        decimal basicSalary = Convert.ToDecimal(employee["basicSalary"]);
                        decimal housingAllowance = Convert.ToDecimal(employee["HousingAllowance"]);
                        decimal transportAllowance = Convert.ToDecimal(employee["TransportationAllowance"]);
                        decimal other = Convert.ToDecimal(employee["Other"]);

                        

                        string workDays = "";
                        int refId = 0;
                        bool hasAttendance = false;

                        try
                        {
                            string attendanceQuery = @"SELECT DayOfWeek, Ref_Code FROM tbl_attendancesheet 
                                              WHERE YEAR(WorkDate) = @year 
                                              AND MONTH(WorkDate) = @month 
                                              AND code = @id
                                              LIMIT 1";

                            await using (var attCmd = new MySqlCommand(attendanceQuery, conn))
                            {
                                attCmd.Parameters.AddWithValue("@year", year);
                                attCmd.Parameters.AddWithValue("@month", month);
                                attCmd.Parameters.AddWithValue("@id", empCode);

                                await using var attReader = await attCmd.ExecuteReaderAsync();

                                if (await attReader.ReadAsync())
                                {
                                    hasAttendance = true;
                                    workDays = attReader["DayOfWeek"]?.ToString() ?? "";
                                    refId = attReader["Ref_Code"] != DBNull.Value
                                        ? Convert.ToInt32(attReader["Ref_Code"]) : 0;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        // Skip if no attendance record
                        if (!hasAttendance)
                            continue;

                        // Calculate allowance
                        decimal allowance = housingAllowance + transportAllowance;

                        // Get absence days
                        int absDays = 0;
                        try
                        {
                            absDays = await GetAbsenceDays(conn, empCode, year, month);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        // Get total loan
                        decimal totalLoan = 0;
                        try
                        {
                            totalLoan = await GetTotalLoan(conn, empCode, year, month);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        // Get total delay
                        decimal totalDelay = 0;
                        try
                        {
                            totalDelay = await GetTotalDelay(conn, empCode, year, month);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        // Get total work absence
                        decimal totalWorkAbsence = 0;
                        try
                        {
                            totalWorkAbsence = await GetTotalAbsence(conn, empCode, year, month);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        // Get salary adjustments
                        decimal additions = 0;
                        try
                        {
                            additions = await GetSalaryAdjustment(conn, empCode, refId, "Additions");
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        decimal salik = 0;
                        try
                        {
                            salik = await GetSalaryAdjustment(conn, empCode, refId, "Salik");
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        decimal otherDeductions = 0;
                        try
                        {
                            otherDeductions = await GetSalaryAdjustment(conn, empCode, refId, "othersDeductions");
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        // Calculate totals
                        decimal totalEarnings = basicSalary + allowance + other + additions;
                        decimal totalDeductions = totalLoan + totalDelay + totalWorkAbsence + salik + otherDeductions;
                        decimal netSalary = totalEarnings - totalDeductions;

                        var row = new Dictionary<string, object>
                        {
                            ["SN"] = serialNumber,
                            ["EmployeeCode"] = empCode,
                            ["EmployeeName"] = empName,
                            ["Position"] = positionName,
                            ["WorkDays"] = workDays,
                            ["AbsenceDays"] = absDays,
                            ["BasicSalary"] = basicSalary.ToString("N2"),
                            ["Allowance"] = allowance.ToString("N2"),
                            ["Other"] = other.ToString("N2"),
                            ["Additions"] = additions.ToString("N2"),
                            ["TotalEarnings"] = totalEarnings.ToString("N2"),
                            ["EmployeeAdvance"] = totalLoan.ToString("N2"),
                            ["Delay"] = totalDelay.ToString("N2"),
                            ["WorkAbsence"] = totalWorkAbsence.ToString("N2"),
                            ["Salik"] = salik.ToString("N2"),
                            ["OtherDeductions"] = otherDeductions.ToString("N2"),
                            ["TotalDeductions"] = totalDeductions.ToString("N2"),
                            ["NetSalary"] = netSalary.ToString("N2")
                        };

                        salaryData.Add(row);
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }

                var response = new
                {
                    status = true,
                    refCode = refCode,
                    ssNo = ssNo,
                    date = dated,
                    month = month.ToString("D2"),
                    year = year.ToString(),
                    data = salaryData
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        private async Task<int> GetAbsenceDays(MySqlConnection conn, string empCode, int year, int month)
        {
            try
            {
                string query = @"SELECT COUNT(attendance_salary_id) AS count 
                        FROM tbl_attendancesheet 
                        WHERE YEAR(WorkDate) = @year 
                        AND MONTH(WorkDate) = @month 
                        AND attendance_salary_id = @code 
                        AND status = 'A'";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@month", month);
                cmd.Parameters.AddWithValue("@code", empCode);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync() && reader["count"] != DBNull.Value)
                {
                    return Convert.ToInt32(reader["count"]);
                }
                return 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<decimal> GetTotalLoan(MySqlConnection conn, string empCode, int year, int month)
        {
            try
            {
                string query = @"SELECT SUM(amount) AS totalLoan 
                        FROM tbl_loan 
                        WHERE YEAR(loandates) = @year 
                        AND MONTH(loandates) = @month 
                        AND employeeId = @code";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@month", month);
                cmd.Parameters.AddWithValue("@code", empCode);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync() && reader["totalLoan"] != DBNull.Value)
                {
                    return Convert.ToDecimal(reader["totalLoan"]);
                }
                return 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<decimal> GetTotalDelay(MySqlConnection conn, string empCode, int year, int month)
        {
            try
            {
                string query = @"SELECT total_delay FROM tbl_attendance_salary 
                        WHERE YEAR(date) = @year 
                        AND MONTH(date) = @month 
                        AND emp_code = @code";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@month", month);
                cmd.Parameters.AddWithValue("@code", empCode);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync() && reader["total_delay"] != DBNull.Value)
                {
                    return Convert.ToDecimal(reader["total_delay"]);
                }
                return 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<decimal> GetTotalAbsence(MySqlConnection conn, string empCode, int year, int month)
        {
            try
            {
                string query = @"SELECT total_absence FROM tbl_attendance_salary 
                        WHERE YEAR(date) = @year 
                        AND MONTH(date) = @month 
                        AND emp_code = @code";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@year", year);
                cmd.Parameters.AddWithValue("@month", month);
                cmd.Parameters.AddWithValue("@code", empCode);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync() && reader["total_absence"] != DBNull.Value)
                {
                    return Convert.ToDecimal(reader["total_absence"]);
                }
                return 0;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<decimal> GetSalaryAdjustment(MySqlConnection conn, string empCode, int refId, string adjustmentType)
        {
            try
            {
                string query = @"SELECT SUM(amount) FROM tbl_salary_adjustments 
                        WHERE code = @code 
                        AND ref_id = @id 
                        AND adjustment_type = @adjustment_type";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@code", empCode);
                cmd.Parameters.AddWithValue("@adjustment_type", adjustmentType);
                cmd.Parameters.AddWithValue("@id", refId);

                var result = await cmd.ExecuteScalarAsync();
                return (result != null && result != DBNull.Value) ? Convert.ToDecimal(result) : 0m;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Leave Salary

        public IActionResult LeaveSalary()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetLeaveSalary()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                CODE AS 'Employee Code',
                NAME AS 'Employee Name',
                SUM(leave_days) AS 'Leave Salary Days',
                SUM(credit) AS 'Leave Salary Amount',
                SUM(debit) / (SUM(credit) / SUM(leave_days)) AS 'L.S Used Days',
                SUM(leave_days) - (SUM(debit) / (SUM(credit) / SUM(leave_days))) AS 'L.S Remaining Days',
                SUM(debit) AS 'L.S Received Amount',
                SUM(credit) - SUM(debit) AS 'L.S Remaining Amount'
            FROM tbl_leave_salary
            GROUP BY CODE, NAME;
        ";

                var result = new List<Dictionary<string, object>>();

                await using (var cmd = new MySqlCommand(query, conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>
                        {
                            ["EmployeeCode"] = reader["Employee Code"]?.ToString(),
                            ["EmployeeName"] = reader["Employee Name"]?.ToString(),
                            ["LeaveSalaryDays"] = reader["Leave Salary Days"] != DBNull.Value
                                ? Convert.ToDecimal(reader["Leave Salary Days"]).ToString("N2")
                                : "0.00",
                            ["LeaveSalaryAmount"] = reader["Leave Salary Amount"] != DBNull.Value
                                ? Convert.ToDecimal(reader["Leave Salary Amount"]).ToString("N2")
                                : "0.00",
                            ["LSUsedDays"] = reader["L.S Used Days"] != DBNull.Value
                                ? Convert.ToDecimal(reader["L.S Used Days"]).ToString("N2")
                                : "0.00",
                            ["LSRemainingDays"] = reader["L.S Remaining Days"] != DBNull.Value
                                ? Convert.ToDecimal(reader["L.S Remaining Days"]).ToString("N2")
                                : "0.00",
                            ["LSReceivedAmount"] = reader["L.S Received Amount"] != DBNull.Value
                                ? Convert.ToDecimal(reader["L.S Received Amount"]).ToString("N2")
                                : "0.00",
                            ["LSRemainingAmount"] = reader["L.S Remaining Amount"] != DBNull.Value
                                ? Convert.ToDecimal(reader["L.S Remaining Amount"]).ToString("N2")
                                : "0.00"
                        };

                        result.Add(row);
                    }
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        public IActionResult LeaveSalaryStatement()
        {
            return View();
        }


        [HttpGet]
        public async Task<IActionResult> GetLeaveSalaryStatement(string id)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY id) AS SN, 
                code AS EmployeeCode, 
                name AS EmployeeName, 
                Reference, 
                description, 
                leave_days, 
                debit, 
                credit,
                SUM(credit - debit) OVER (PARTITION BY code ORDER BY id) AS Balance
            FROM tbl_leave_salary
            WHERE code = @code
            ORDER BY id";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@code", id);

                var result = new List<Dictionary<string, object>>();

                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        result.Add(new Dictionary<string, object>
                        {
                            ["SN"] = reader["SN"]?.ToString(),
                            ["EmployeeCode"] = reader["EmployeeCode"]?.ToString(),
                            ["EmployeeName"] = reader["EmployeeName"]?.ToString(),
                            ["Reference"] = reader["Reference"]?.ToString(),
                            ["Description"] = reader["description"]?.ToString(),

                            ["LeaveDays"] = reader["leave_days"] != DBNull.Value
                                ? Convert.ToDecimal(reader["leave_days"]).ToString("N2")
                                : "0.00",

                            ["Debit"] = reader["debit"] != DBNull.Value
                                ? Convert.ToDecimal(reader["debit"]).ToString("N2")
                                : "0.00",

                            ["Credit"] = reader["credit"] != DBNull.Value
                                ? Convert.ToDecimal(reader["credit"]).ToString("N2")
                                : "0.00",

                            ["Balance"] = reader["Balance"] != DBNull.Value
                                ? Convert.ToDecimal(reader["Balance"]).ToString("N2")
                                : "0.00"
                        });
                    }
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }


        #endregion

        #region End Of Service

        public IActionResult EndOfService()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetEndOfService()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                CODE AS 'Employee Code',
                NAME AS 'Employee Name',
                SUM(leave_days) AS 'End Of Service Days',
                SUM(credit) AS 'End Of Service Amount',
                SUM(leave_days) - (SUM(debit) / (SUM(credit) / SUM(leave_days))) AS 'EOS Remaining Days',
                SUM(debit) AS 'EOS Received Amount',
                SUM(credit) - SUM(debit) AS 'EOS Remaining Amount'
            FROM tbl_end_of_service
            GROUP BY CODE, NAME;
        ";

                var result = new List<Dictionary<string, object>>();

                await using (var cmd = new MySqlCommand(query, conn))
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>
                        {
                            ["EmployeeCode"] = reader["Employee Code"]?.ToString(),
                            ["EmployeeName"] = reader["Employee Name"]?.ToString(),

                            ["EndOfServiceDays"] = reader["End Of Service Days"] != DBNull.Value
                                ? Convert.ToDecimal(reader["End Of Service Days"]).ToString("N2")
                                : "0.00",

                            ["EndOfServiceAmount"] = reader["End Of Service Amount"] != DBNull.Value
                                ? Convert.ToDecimal(reader["End Of Service Amount"]).ToString("N2")
                                : "0.00",

                            ["EOSRemainingDays"] = reader["EOS Remaining Days"] != DBNull.Value
                                ? Convert.ToDecimal(reader["EOS Remaining Days"]).ToString("N2")
                                : "0.00",

                            ["EOSReceivedAmount"] = reader["EOS Received Amount"] != DBNull.Value
                                ? Convert.ToDecimal(reader["EOS Received Amount"]).ToString("N2")
                                : "0.00",

                            ["EOSRemainingAmount"] = reader["EOS Remaining Amount"] != DBNull.Value
                                ? Convert.ToDecimal(reader["EOS Remaining Amount"]).ToString("N2")
                                : "0.00"
                        };

                        result.Add(row);
                    }
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        public IActionResult EndOfServiceStatement()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetEndOfServiceStatement(string id)
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName")
                               ?? _config.GetConnectionString("DefaultDatabase")
                };

                await using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY id) AS SN, 
                code AS EmployeeCode, 
                name AS EmployeeName, 
                Reference, 
                description, 
                leave_days, 
                debit, 
                credit,
                SUM(credit - debit) OVER (PARTITION BY code ORDER BY id) AS Balance
            FROM tbl_end_of_service
            WHERE code = @code
            ORDER BY id;
        ";

                await using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@code", id);

                var result = new List<Dictionary<string, object>>();

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new Dictionary<string, object>
                    {
                        ["SN"] = reader["SN"]?.ToString(),
                        ["EmployeeCode"] = reader["EmployeeCode"]?.ToString(),
                        ["EmployeeName"] = reader["EmployeeName"]?.ToString(),
                        ["Reference"] = reader["Reference"]?.ToString(),
                        ["Description"] = reader["description"]?.ToString(),

                        ["LeaveDays"] = reader["leave_days"] != DBNull.Value
                            ? Convert.ToDecimal(reader["leave_days"]).ToString("N2")
                            : "0.00",

                        ["Debit"] = reader["debit"] != DBNull.Value
                            ? Convert.ToDecimal(reader["debit"]).ToString("N2")
                            : "0.00",

                        ["Credit"] = reader["credit"] != DBNull.Value
                            ? Convert.ToDecimal(reader["credit"]).ToString("N2")
                            : "0.00",

                        ["Balance"] = reader["Balance"] != DBNull.Value
                            ? Convert.ToDecimal(reader["Balance"]).ToString("N2")
                            : "0.00"
                    });
                }

                return Ok(new { status = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Loan

        public IActionResult Loan()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateLoan([FromBody] LoanRequestDto model)
        {
            if (model == null)
                return BadRequest(new { status = false, message = "Invalid request data" });

            if (model.RequestAmount <= 0 || model.Installments <= 0)
                return BadRequest(new { status = false, message = "Invalid amount or installments" });

            var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
            {
                Database = HttpContext.Session.GetString("DatabaseName") ??
                           _config.GetConnectionString("DefaultDatabase")
            };

            using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
            await conn.OpenAsync();
            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // Generate new loan code
                int nextCodeNumber = 10001;
                string code = "LO" + nextCodeNumber;

                var getCodeCmd = new MySqlCommand(
                    "SELECT code FROM tbl_loan ORDER BY CAST(SUBSTRING(code, 3) AS UNSIGNED) DESC LIMIT 1;",
                    conn, transaction);

                using var reader = await getCodeCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var lastCode = reader.GetString("code");
                    if (lastCode.StartsWith("LO"))
                    {
                        int num = int.Parse(lastCode.Substring(2));
                        nextCodeNumber = num + 1;
                        code = "LO" + nextCodeNumber;
                    }
                }
                await reader.CloseAsync();

                // Insert schedule + loan rows
                foreach (var item in model.Schedule)
                {
                    var insertLoanCmd = new MySqlCommand(@"
                INSERT INTO tbl_loan 
                    (code, LoanDate, EmployeeID, EmployeeName, RequestAmount, Installments, 
                     StartDate, EndDate, loanDates, Months, Description, Amount, 
                     debit_account_id, credit_account_id, `change`) 
                VALUES
                    (@code, @loanDate, @employeeID, @employeeName, @requestAmount, @installments,
                     @startDate, @endDate, @loanDates, @months, @description, @amount,
                     @debitAccount, @creditAccount, @changeValue);
            ", conn, transaction);

                    insertLoanCmd.Parameters.AddWithValue("@code", code);
                    insertLoanCmd.Parameters.AddWithValue("@loanDate", model.RequestDate.ToString("yyyy-MM-dd"));
                    insertLoanCmd.Parameters.AddWithValue("@employeeID", model.EmployeeCode);
                    insertLoanCmd.Parameters.AddWithValue("@employeeName", model.EmployeeName);
                    insertLoanCmd.Parameters.AddWithValue("@requestAmount", model.RequestAmount);
                    insertLoanCmd.Parameters.AddWithValue("@installments", model.Installments);
                    insertLoanCmd.Parameters.AddWithValue("@startDate", model.StartDate.ToString("yyyy-MM-dd"));
                    insertLoanCmd.Parameters.AddWithValue("@endDate", model.EndDate.ToString("yyyy-MM-dd"));
                    insertLoanCmd.Parameters.AddWithValue("@loanDates", item.Date.ToString("yyyy-MM-dd"));
                    insertLoanCmd.Parameters.AddWithValue("@months", item.Month);
                    insertLoanCmd.Parameters.AddWithValue("@description", model.Description);
                    insertLoanCmd.Parameters.AddWithValue("@amount", item.Amount);
                    insertLoanCmd.Parameters.AddWithValue("@debitAccount", model.DebitAccountId);
                    insertLoanCmd.Parameters.AddWithValue("@creditAccount", model.CreditAccountId);
                    insertLoanCmd.Parameters.AddWithValue("@changeValue", model.RequestAmount);

                    var insertedId = Convert.ToInt32(await insertLoanCmd.ExecuteScalarAsync());

                    // JOURNAL: Debit
                    var insertJournalDebit = new MySqlCommand(@"
                INSERT INTO tbl_transaction
                    (`date`, account_id, debit, credit, transaction_id, hum_id, 
                     t_type, `type`, description, created_by, created_date, 
                     state, voucher_no)
                VALUES
                    (@date, @accountId, @debit, @credit, @transactionId, @humId,
                     @voucherType, @type, @description, @createdBy, @createdDate,
                     0, @voucherNo);
            ", conn, transaction);

                    insertJournalDebit.Parameters.AddWithValue("@date", model.RequestDate);
                    insertJournalDebit.Parameters.AddWithValue("@accountId", model.DebitAccountId);
                    insertJournalDebit.Parameters.AddWithValue("@debit", model.RequestAmount);
                    insertJournalDebit.Parameters.AddWithValue("@credit", 0);
                    insertJournalDebit.Parameters.AddWithValue("@transactionId", insertedId);
                    insertJournalDebit.Parameters.AddWithValue("@humId", model.EmployeeCode);
                    insertJournalDebit.Parameters.AddWithValue("@voucherType", "Loan Request");
                    insertJournalDebit.Parameters.AddWithValue("@type", "Employee LOAN");
                    insertJournalDebit.Parameters.AddWithValue("@description", $"Loan Request {code}");
                    insertJournalDebit.Parameters.AddWithValue("@createdBy", 0);
                    insertJournalDebit.Parameters.AddWithValue("@createdDate", DateTime.UtcNow);
                    insertJournalDebit.Parameters.AddWithValue("@voucherNo", code);

                    await insertJournalDebit.ExecuteNonQueryAsync();

                    // JOURNAL: Credit
                    var insertJournalCredit = new MySqlCommand(@"
                INSERT INTO tbl_transaction
                    (`date`, account_id, debit, credit, transaction_id, hum_id, 
                     t_type, `type`, description, created_by, created_date, 
                     state, voucher_no)
                VALUES
                    (@date, @accountId, @debit, @credit, @transactionId, @humId,
                     @voucherType, @type, @description, @createdBy, @createdDate,
                     0, @voucherNo);
            ", conn, transaction);

                    insertJournalCredit.Parameters.AddWithValue("@date", model.RequestDate);
                    insertJournalCredit.Parameters.AddWithValue("@accountId", model.CreditAccountId);
                    insertJournalCredit.Parameters.AddWithValue("@debit", 0);
                    insertJournalCredit.Parameters.AddWithValue("@credit", model.RequestAmount);
                    insertJournalCredit.Parameters.AddWithValue("@transactionId", insertedId);
                    insertJournalCredit.Parameters.AddWithValue("@humId", model.EmployeeCode);
                    insertJournalCredit.Parameters.AddWithValue("@voucherType", "Loan Request");
                    insertJournalCredit.Parameters.AddWithValue("@type", "Employee LOAN");
                    insertJournalCredit.Parameters.AddWithValue("@description", $"Loan Request {code}");
                    insertJournalCredit.Parameters.AddWithValue("@createdBy", 0);
                    insertJournalCredit.Parameters.AddWithValue("@createdDate", DateTime.UtcNow);
                    insertJournalCredit.Parameters.AddWithValue("@voucherNo", code);

                    await insertJournalCredit.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();

                return Ok(new
                {
                    status = true,
                    message = "Loan and journal entries saved",
                    code = code
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeLoan(string employeeCode)
        {
            try
            {
                if (string.IsNullOrEmpty(employeeCode))
                    return BadRequest(new { status = false, message = "Employee code is required" });

                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var loanQuery = @"
            WITH loan_data AS (
                SELECT 
                    l.id, 
                    l.code, 
                    l.LoanDate, 
                    l.EmployeeID, 
                    e.name AS EmployeeName, 
                    l.RequestAmount, 
                    l.Installments, 
                    l.StartDate, 
                    l.EndDate, 
                    l.Description, 
                    l.Amount AS MonthlyAmount, 
                    l.`change`,
                    ROW_NUMBER() OVER (PARTITION BY l.code ORDER BY l.id) AS rn
                FROM tbl_loan l
                JOIN tbl_employee e ON e.code = l.EmployeeID
                WHERE l.EmployeeID = @employeeCode
            )
            SELECT 
                id,
                code,
                LoanDate,
                EmployeeID,
                EmployeeName,
                RequestAmount,
                Installments,
                StartDate,
                EndDate,
                Description,
                MonthlyAmount,
                `change`,
                ROW_NUMBER() OVER (ORDER BY LoanDate) AS rn
            FROM loan_data
            WHERE rn = 1;";

                using var loanCmd = new MySqlCommand(loanQuery, conn);
                loanCmd.Parameters.AddWithValue("@employeeCode", employeeCode);

                var loanReader = await loanCmd.ExecuteReaderAsync();
                var loans = new List<object>();
                decimal finalBalance = 0;

                while (await loanReader.ReadAsync())
                {
                    finalBalance = loanReader.GetDecimal("RequestAmount");

                    loans.Add(new
                    {
                        Date = loanReader["LoanDate"],
                        Code = loanReader["Code"],
                        LoanDate = loanReader["LoanDate"],
                        EmployeeName = loanReader["EmployeeName"],
                        Type = "Loan",
                        LoanAmount = loanReader["RequestAmount"],
                        Installments = loanReader["Installments"],
                        StartDate = loanReader["StartDate"],
                        EndDate = loanReader["EndDate"],
                        MonthlyAmount = loanReader["MonthlyAmount"],
                        Balance = finalBalance,
                    });
                }

                loanReader.Close();

                if (loans.Count > 0)
                {
                    loans.Add(new
                    {
                        Date = (object)null,
                        Id = (object)null,
                        Type = "Total",
                        Description = (object)null,
                        ReceivedAmount = (object)null,
                        PayAmount = (object)null,
                        Balance = finalBalance
                    });
                }

                return Ok(new
                {
                    status = true,
                    loanRecords = loans
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        #endregion

        #region Final Settlement

        public IActionResult FinalSettlement()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetFinalSettlement()
        {
            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                var query = @"
            SELECT 
                e.id,
                e.code AS EmpCode, 
                e.name AS EmployeeName,
                tf.DateCommencement, 
                tf.DateLastWork,
                tf.TotalSalary, 
                tf.TotalAdditions, 
                tf.TotalDeductions,
                tf.NetAccruals
            FROM tbl_final_settlement tf
            INNER JOIN tbl_employee e ON tf.emp_id = e.id";

                using var cmd = new MySqlCommand(query, conn);
                using var reader = await cmd.ExecuteReaderAsync();

                var settlements = new List<object>();

                while (await reader.ReadAsync())
                {
                    settlements.Add(new
                    {
                        Id = reader["id"],
                        EmpCode = reader["EmpCode"],
                        EmployeeName = reader["EmployeeName"],
                        DateCommencement = reader["DateCommencement"],
                        DateLastWork = reader["DateLastWork"],
                        TotalSalary = reader["TotalSalary"],
                        TotalAdditions = reader["TotalAdditions"],
                        TotalDeductions = reader["TotalDeductions"],
                        NetAccruals = reader["NetAccruals"]
                    });
                }

                return Ok(new
                {
                    status = true,
                    finalSettlements = settlements
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeFinalSettlementData(int employeeId)
        {
            if (employeeId <= 0)
                return Json(new { status = false, message = "Invalid employee ID" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();


                // ---------------------------------------------------------
                // 1) GET EMPLOYEE DETAILS (salary info + employee code)
                // ---------------------------------------------------------
                var empQuery = @"SELECT code, BasicSalary, HousingAllowance, TransportationAllowance, Other
                         FROM tbl_employee WHERE id = @id";

                string empCode = "";
                decimal basicSalary = 0, housing = 0, transport = 0, other = 0;

                using (var cmd = new MySqlCommand(empQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@id", employeeId);

                    using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        empCode = r["code"]?.ToString();
                        basicSalary = r["BasicSalary"] == DBNull.Value ? 0 : Convert.ToDecimal(r["BasicSalary"]);
                        housing = r["HousingAllowance"] == DBNull.Value ? 0 : Convert.ToDecimal(r["HousingAllowance"]);
                        transport = r["TransportationAllowance"] == DBNull.Value ? 0 : Convert.ToDecimal(r["TransportationAllowance"]);
                        other = r["Other"] == DBNull.Value ? 0 : Convert.ToDecimal(r["Other"]);
                    }
                    else
                    {
                        return Json(new { status = false, message = "Employee not found." });
                    }
                }


                // ---------------------------------------------------------
                // 2) GET LEAVE SALARY
                // ---------------------------------------------------------
                decimal leaveSalary = 0;

                using (var cmd = new MySqlCommand(
                    "SELECT SUM(credit) - SUM(debit) AS Sum FROM tbl_leave_salary WHERE code=@code", conn))
                {
                    cmd.Parameters.AddWithValue("@code", empCode);
                    var val = await cmd.ExecuteScalarAsync();
                    leaveSalary = val == DBNull.Value ? 0 : Convert.ToDecimal(val);
                }


                // ---------------------------------------------------------
                // 3) GET END OF SERVICE
                // ---------------------------------------------------------
                decimal eosAmount = 0;

                using (var cmd = new MySqlCommand(
                     "SELECT SUM(credit) - SUM(debit) AS EOS FROM tbl_end_of_service WHERE code=@code", conn))
                {
                    cmd.Parameters.AddWithValue("@code", empCode);
                    var val = await cmd.ExecuteScalarAsync();
                    eosAmount = val == DBNull.Value ? 0 : Convert.ToDecimal(val);
                }


                // ---------------------------------------------------------
                // 4) GET LOANS
                // ---------------------------------------------------------
                decimal loanAmount = 0;

                using (var cmd = new MySqlCommand(
                    "SELECT SUM(amount) FROM tbl_loan WHERE EmployeeID=@id AND loanDates >= CURDATE()", conn))
                {
                    cmd.Parameters.AddWithValue("@id", empCode);
                    var val = await cmd.ExecuteScalarAsync();
                    loanAmount = val == DBNull.Value ? 0 : Convert.ToDecimal(val);
                }


                // ---------------------------------------------------------
                // 5) CALCULATE TOTAL SALARY
                // ---------------------------------------------------------
                decimal totalSalary = basicSalary + housing + transport + other;


                // ---------------------------------------------------------
                // 6) CALCULATE (totalSalary / 31) * 29  (your formula)
                // ---------------------------------------------------------
                decimal salaryCalculatedValue = (totalSalary / 31) * 29;


                // ---------------------------------------------------------
                // 7) TOTAL ADDITIONS = salary + leaveSalary + eos + otherAdditions
                // ---------------------------------------------------------
                decimal totalAdditions = salaryCalculatedValue + leaveSalary + eosAmount;


                // ---------------------------------------------------------
                // 8) NET ACCRUALS = totalAdditions - loanAmount
                // ---------------------------------------------------------
                decimal netAccruals = totalAdditions - loanAmount;


                // ---------------------------------------------------------
                // FINAL RESPONSE
                // ---------------------------------------------------------
                return Ok(new
                {
                    status = true,
                    employee = new
                    {
                        EmployeeId = employeeId,
                        EmployeeCode = empCode,
                        BasicSalary = basicSalary,
                        HousingAllowance = housing,
                        TransportationAllowance = transport,
                        OtherAllowance = other,
                        LeaveSalary = leaveSalary,
                        EndOfService = eosAmount,
                        LoanAmount = loanAmount,
                        TotalSalary = totalSalary,
                        SalaryCalculatedValue = Math.Round(salaryCalculatedValue, 2),
                        TotalAdditions = Math.Round(totalAdditions, 2),
                        NetAccruals = Math.Round(netAccruals, 2)
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveFinalSettlement([FromBody] FinalSettlementSaveRequest model)
        {
            if (model == null)
                return Json(new { status = false, message = "Invalid data." });

            if (string.IsNullOrWhiteSpace(model.EmployeeName))
                return Json(new { status = false, message = "Please choose an employee first." });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();
                using var tran = await conn.BeginTransactionAsync();


                string checkEmp = @"SELECT COUNT(*) FROM tbl_final_settlement WHERE emp_id = @emp_id";

                using (var cmdCheck = new MySqlCommand(checkEmp, conn, tran))
                {
                    cmdCheck.Parameters.AddWithValue("@emp_id", model.EmployeeId);

                    int count = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());

                    if (count > 0)
                    {
                        return Json(new { status = false, message = "Final Settlement already exists for this employee." });
                    }
                }


                string code = "FS-0001";
                var dupQuery = @"SELECT code FROM tbl_final_settlement 
                         ORDER BY CAST(SUBSTRING_INDEX(code, '-', -1) AS UNSIGNED) DESC LIMIT 1";

                using (var cmdDup = new MySqlCommand(dupQuery, conn, tran))
                using (var reader = await cmdDup.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync() && !string.IsNullOrWhiteSpace(reader["code"].ToString()))
                    {
                        code = "FS-000" + (int.Parse(reader["code"].ToString().Replace("FS-", "")) + 1);
                    }
                }

                // --- Insert Final Settlement Record ---
                var insertQuery = @"
            INSERT INTO tbl_final_settlement
            (code, Date, emp_id, DateCommencement, DateLastWork, TotalSalary, TotalAdditions, TotalDeductions, NetAccruals)
            VALUES (@code, @Date, @emp_id, @DateCommencement, @DateLastWork, @TotalSalary, @TotalAdditions, @TotalDeductions, @NetAccruals);
            SELECT LAST_INSERT_ID();";

                int refId;
                using (var cmdInsert = new MySqlCommand(insertQuery, conn, tran))
                {
                    cmdInsert.Parameters.AddWithValue("@code", code);
                    cmdInsert.Parameters.AddWithValue("@Date", model.Date);
                    cmdInsert.Parameters.AddWithValue("@emp_id", model.EmployeeId);
                    cmdInsert.Parameters.AddWithValue("@DateCommencement", model.DateCommencement);
                    cmdInsert.Parameters.AddWithValue("@DateLastWork", model.DateLastWork);
                    cmdInsert.Parameters.AddWithValue("@TotalSalary", model.TotalSalary);
                    cmdInsert.Parameters.AddWithValue("@TotalAdditions", model.TotalAdditions);
                    cmdInsert.Parameters.AddWithValue("@TotalDeductions", model.TotalDeductions);
                    cmdInsert.Parameters.AddWithValue("@NetAccruals", model.NetAccruals);

                    refId = Convert.ToInt32(await cmdInsert.ExecuteScalarAsync());
                }

                await tran.CommitAsync();

                return Json(new { status = true, message = "Final Settlement saved successfully." });
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFinalSettlement(int id)
        {
            if (id <= 0)
                return Json(new { status = false, message = "Invalid settlement ID." });

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (userId <= 0)
                return Json(new { status = false, message = "User not logged in" });

            try
            {
                var connStrBuilder = new MySqlConnectionStringBuilder(_config.GetConnectionString("DefaultConnection"))
                {
                    Database = HttpContext.Session.GetString("DatabaseName") ?? _config.GetConnectionString("DefaultDatabase")
                };

                using var conn = new MySqlConnection(connStrBuilder.ConnectionString);
                await conn.OpenAsync();

                // Check if record exists
                string checkSql = "SELECT emp_id FROM tbl_final_settlement WHERE id = @id";
                int empId = 0;

                using (var cmd = new MySqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result == null)
                        return Json(new { status = false, message = "Final settlement not found." });

                    empId = Convert.ToInt32(result);
                }

                // Perform soft delete
                string updateSql = "UPDATE tbl_final_settlement SET state = -1 WHERE id = @id";
                using (var cmd = new MySqlCommand(updateSql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    await cmd.ExecuteNonQueryAsync();
                }

                return Ok(new { status = true, message = "Final Settlement deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(500, new { status = false, message = ex.Message });
            }
        }

        #endregion
    }
}
