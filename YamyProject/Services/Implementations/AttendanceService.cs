namespace YamyProject.Services.Implementations
    {
    public class AttendanceService(YamyDbContext context): IAttendanceService
        {
        private readonly YamyDbContext _context = context;
        public async Task<SaveResultViewModel> SaveAttendanceAsync(DefaultEmplowyeeViewModel request)
            {         
            int year = request.SelectedYear;
            int month = request.SelectedMonth;

            // check if month already exists (like the COUNT(*) query)
            bool exists = await _context.TblSettingAttendances
                .AnyAsync(a => a.Date.Value.Year == year && a.Date.Value.Month == month);

            if (exists)
                {
                return new SaveResultViewModel
                    {
                    Success = false,
                    Message = $"Attendance data for {month}/{year} already exists in the database!"
                    };
                }

            var regex = new System.Text.RegularExpressions.Regex(@"^\d{1,2}:\d{2}$");

            foreach (var row in request.AttendanceRows)
                {
                if (string.IsNullOrWhiteSpace(row.Date.ToString()) || string.IsNullOrWhiteSpace(row.DayName.ToString()))
                    continue;

                if (!regex.IsMatch(row.TimeIn.ToString()) || !regex.IsMatch(row.TimeOut.ToString()))
                    {
                    return new SaveResultViewModel
                        {
                        Success = false,
                        Message = "Invalid IN or OUT time format. Please enter time as HH:mm (e.g. 08:00)."
                        };
                    }

                if (!DateOnly.TryParse(row.Date.ToString(), out var attendanceDate))
                    {
                    return new SaveResultViewModel
                        {
                        Success = false,
                        Message = $"Invalid date value: {row.Date}"
                        };
                    }

                var entity = new TblSettingAttendance
                    {
                    Date = attendanceDate,
                    Day = row.DayName,
                    Timein = TimeOnly.Parse(row.TimeIn.ToString()), // same as original: add seconds
                    Timeout = TimeOnly.Parse(row.TimeOut.ToString()),
                    State = row.State
                    };

                _context.TblSettingAttendances.Add(entity);
                }

            await _context.SaveChangesAsync();

            return new SaveResultViewModel
                {
                Success = true,
                Message = "Attendance data saved successfully!"
                };
            }   

    public async Task<DefaultAccountViewModel> GetListAsync()
            {
            var defaultAccounts = await _context.TblCoaConfigs
                .AsNoTracking()
                .Select(c => new
                    {
                    c.AccountId,
                    c.Category
                    })
                .ToListAsync();

            var vm = new DefaultAccountViewModel();

            foreach (var a in defaultAccounts)
                {
                if (a.Category == "Default Account For Cash")
                    vm.DefaultCashAccountId = a.AccountId;
                else if (a.Category == "Petty Cash Account")
                    vm.PettyCashAccountId = a.AccountId;
                else if (a.Category == "Opening Balance")
                    vm.OpeningBalanceAccountId = a.AccountId;
                else if (a.Category == "Opening Balance Equity")
                    vm.OpeningBalanceEquityAccountId = a.AccountId;
                else if (a.Category == "PDC Receivable")
                    vm.PdcReceivableId = a.AccountId;
                else if (a.Category == "PDC Receivable Return")
                    vm.PdcReceivableReturnId = a.AccountId;
                else if (a.Category == "PDC Receivable Hold")
                    vm.PdcReceivableHoldId = a.AccountId;
                else if (a.Category == "PDC Payable")
                    vm.PdcPayableId = a.AccountId;
                else if (a.Category == "PDC Payable Return")
                    vm.PdcPayableReturnId = a.AccountId;
                else if (a.Category == "PDC Payable Hold")
                    vm.PdcPayableHoldId = a.AccountId;
                else if (a.Category == "Prepaid Expense Debit Account")
                    vm.PrepaidExpenseDebitAccountId = a.AccountId;
                else if (a.Category == "Prepaid Expense Credit Account")
                    vm.PrepaidExpenseCreditAccountId = a.AccountId;
                else if (a.Category == "Fixed Asset Debit Account")
                    vm.FixedAssetDebitAccountId = a.AccountId;
                else if (a.Category == "Fixed Asset Credit Account")
                    vm.FixedAssetCreditAccountId = a.AccountId;
                else if (a.Category == "Vendor")
                    vm.VendorId = a.AccountId;
                else if (a.Category == "Vat Input")
                    vm.VatInputId = a.AccountId;
                else if (a.Category == "Purchase Payment Cash Method")
                    vm.PurchasePaymentCashMethodId = a.AccountId;
                else if (a.Category == "Purchase")
                    vm.PurchaseInvoiceId = a.AccountId;
                else if (a.Category == "PurchaseReturn")
                    vm.PurchaseReturnInvoiceId = a.AccountId;
                else if (a.Category == "Inventory")
                    vm.InventoryId = a.AccountId;
                else if (a.Category == "COGS")
                    vm.ItemCogsId = a.AccountId;
                else if (a.Category == "Item Damage")
                    vm.InventoryDamageId = a.AccountId;
                else if (a.Category == "Stock Settlement")
                    vm.StockSettlementId = a.AccountId;
                else if (a.Category == "Customer")
                    vm.CustomerId = a.AccountId;
                else if (a.Category == "Invoice Payment Cash Method")
                    vm.InvoicePaymentCashMethodId = a.AccountId;
                else if (a.Category == "Vat Output")
                    vm.VatOutputId = a.AccountId;
                else if (a.Category == "Sales")
                    vm.SalesInvoiceId = a.AccountId;
                else if (a.Category == "SalesReturn")
                    vm.SalesReturnInvoiceId = a.AccountId;
                else if (a.Category == "Accrued Salaries")
                    vm.AccruedSalariesId = a.AccountId;
                else if (a.Category == "Salaries")
                    vm.SalariesId = a.AccountId;
                else if (a.Category == "Acroal Leave Salary")
                    vm.AccrualLeaveSalaryId = a.AccountId;
                else if (a.Category == "Employee Receivable")
                    vm.EmployeeReceivableId = a.AccountId;
                else if (a.Category == "Gratuit")
                    vm.GratuityId = a.AccountId;
                else if (a.Category == "End of Service Debit")
                    vm.EosDebitId = a.AccountId;
                else if (a.Category == "End of Service Credit")
                    vm.EosCreditId = a.AccountId;
                else if (a.Category == "Leave Salary Debit")
                    vm.LeaveSalaryDebitId = a.AccountId;
                else if (a.Category == "Leave Salary Credit")
                    vm.LeaveSalaryCreditId = a.AccountId;
                else if (a.Category == "Default Account For Bank")
                    vm.DefaultBankAccountId = a.AccountId;
                }

            return vm;
            }

        }
    }
