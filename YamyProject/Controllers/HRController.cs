using Microsoft.AspNetCore.Mvc;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace YamyProject.Controllers
{
    [Route("HR/[action]")]
    public class HRController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly MySqlConnection _connection;
        public HRController(IHttpClientFactory httpClientFactory, IConfiguration config, ApplicationDbContext applicationDbContext)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _connection = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            _applicationDbContext = applicationDbContext;
        }
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


    }
}
