using Microsoft.EntityFrameworkCore;

namespace YamyProject.Services.Implementations
    {
    public class CustomerEditeService : IEditeCustomerService
        {
        private readonly YamyDbContext _context;
        private readonly IListServices _ListServices;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGlobalService _GlobalService;

        public CustomerEditeService(
            YamyDbContext context,
            IListServices listServices,
            IHttpContextAccessor httpContextAccessor,
            IGlobalService globalService)
            {
            _context = context;
            _ListServices = listServices;
            _httpContextAccessor = httpContextAccessor;
            _GlobalService = globalService;
            }

        public async Task<CustomerViewModel> GetCustomerFormDataAsync(int? id)
            {
            var vm = new CustomerViewModel
                {
                };

            if (id.HasValue)
                {

               
                vm.Customer = await _context.TblCustomers
                    .FirstOrDefaultAsync(c => c.Id == id.Value) ?? new TblCustomer();
                if (vm.Customer.Balance > 0)
                    vm.OpeningType = "Debit";
                else
                    vm.OpeningType = "Credit";
                vm.Customer.Balance = Math.Abs((decimal)vm.Customer.Balance);
                vm.Transactions = await _context.TblTransactions
                        .FirstOrDefaultAsync(t => t.HumId == id.Value && t.Type == "Customer Opening Balance")
                        ?? new TblTransaction();
                }

            return vm;
            }

        public async Task<CustomerViewModel> GetCreateCustomerFormAsync()
            {
            var vm = new CustomerViewModel
                {
                Customer = new TblCustomer(),

                Transactions = new TblTransaction() // empty for new customer
                };
            return vm;
            }

        public string GenerateNextCustomerCode()
            {
            int lastCode = _context.TblCustomers
                .Select(v => (int?)v.Code)
                .Max() ?? 0;

            int next = lastCode + 1;
            return next.ToString("D5");
            }

        public async Task SaveCustomerAsync(CustomerViewModel model)
            {
            var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;

            // NEW CUSTOMER
            if (model.Customer.Id == 0)
                {
                var code = GenerateNextCustomerCode();
                var Balance = model.Customer.Balance;
                if (model.OpeningType == "Credit")
                    Balance = Balance * -1;

                var customerRow = new TblCustomer
                    {
                    Date = model.Customer.Date,
                    Name = model.Customer.Name,
                    Code = int.Parse(code),
                    CatId = model.Customer.CatId,
                    Balance = Balance,
                    Mobile = model.Customer.Mobile,
                    WorkPhone = model.Customer.WorkPhone,
                    MainPhone = model.Customer.MainPhone,
                    Email = model.Customer.Email,
                    Ccemail = model.Customer.Ccemail,
                    Country = model.Customer.Country,
                    City = model.Customer.City,
                    Region = model.Customer.Region,
                    BuildingName = model.Customer.BuildingName,
                    Website = model.Customer.Website,
                    AccountId = model.Customer.AccountId,
                    Trn = model.Customer.Trn,
                    FaciltyName = model.Customer.FaciltyName,
                    Active = model.Customer.Active,
                    CreatedBy = userId,
                    CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                    State = 0,
                    ProjectId = model.Customer.ProjectId,
                    ProjectSite = model.Customer.ProjectSite,
                    };

                _context.TblCustomers.Add(customerRow);
                await _context.SaveChangesAsync();

                // 🔹 Make sure opening balance entries are actually created
                await ProcessOpeningBalanceTransactions(customerRow, model.OpeningType);

                // If LogAudit is async Task, keep await. If it's void/sync, remove await.
                await _GlobalService.LogAudit(
                    userId,
                    "INSERT",
                    "Customer Center",
                    customerRow.Id,
                    "New Customer Created - Code: " + customerRow.Code + ", Name: " + customerRow.Name
                );
                }
            else // UPDATE EXISTING CUSTOMER
                {
                var customerRow = await _context.TblCustomers.FindAsync(model.Customer.Id);
                if (customerRow == null)
                    {
                    //      throw new InvalidOperationException("Customer not found.");
                    }
                else
                    {
                    var Balance = model.Customer.Balance;
                    if (model.OpeningType == "Credit")
                        Balance = Balance * -1;

                    customerRow.Name = model.Customer.Name;
                    customerRow.Code = model.Customer.Code;
                    customerRow.CatId = model.Customer.CatId;
                    customerRow.Balance = Balance;
                    customerRow.Mobile = model.Customer.Mobile;
                    customerRow.Email = model.Customer.Email;
                    customerRow.Country = model.Customer.Country;
                    customerRow.City = model.Customer.City;
                    customerRow.Region = model.Customer.Region;
                    customerRow.BuildingName = model.Customer.BuildingName;
                    customerRow.AccountId = model.Customer.AccountId;
                    customerRow.Trn = model.Customer.Trn;
                    customerRow.FaciltyName = model.Customer.FaciltyName;
                    // Note: this logic is as you wrote it; adjust if needed.
                    customerRow.Active = model.Customer.Active == 1 ? 0 : -1;
                    customerRow.State = model.Customer.State;
                    customerRow.ProjectId = model.Customer.ProjectId;
                    customerRow.ProjectSite = model.Customer.ProjectSite;

                    await _context.SaveChangesAsync();

                    // Remove old opening balance entries
                    var journalDetails = await _context.TblTransactions
                        .Where(d => d.TransactionId == model.Customer.Id &&
                                    d.Type == "Customer Opening Balance")
                        .ToListAsync();

                    _context.TblTransactions.RemoveRange(journalDetails);
                    await _context.SaveChangesAsync();

                    // Recreate opening balance entries
                    await ProcessOpeningBalanceTransactions(model.Customer, model.OpeningType);

                    await _GlobalService.LogAudit(
                        userId,
                        "UPDATE",
                        "Customer Center",
                        customerRow.Id,
                        "Customer UPDATE - Code: " + customerRow.Code + ", Name: " + customerRow.Name);
                    }
                }
            }

        public async Task ProcessOpeningBalanceTransactions(TblCustomer customer, string OpeningType)
            {
            var accountId = customer.AccountId;

            // 1) get default opening balance equity account from settings
            var openingBalanceAccount = await _ListServices.DefaultAccountsSet("Opening Balance Equity");
            var openingBalanceEquity = openingBalanceAccount.ToString();

            // 2) fallback to chart of accounts if setting is 0
            if (openingBalanceEquity == "0")
                {
                var result = await _context.TblCoaLevel4s
                    .FirstOrDefaultAsync(a => a.Name == "Opening Balance Equity");

                if (result == null)
                    {
                    throw new InvalidOperationException(
                        "Opening Balance Equity account not found in TblCoaLevel4s.");
                    }

                openingBalanceEquity = result.Id.ToString();
                }

            if (customer.Balance != null && customer.Balance != 0)
                {
                var userId = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                var nowDate = DateOnly.FromDateTime(DateTime.UtcNow);
                var balance = Math.Abs((decimal)customer.Balance!);
            //    var openingBalanceEquity = await _ListServices.DefaultAccountsSet("Opening Balance Equity");

                // NOTE:
                // Make sure model.Debit / model.Credit are being set correctly from the UI.
                if (OpeningType== "Debit")
                    {
                    // Dr: Opening Balance Equity | Cr: Customer
                    await AddTransactionEntry(
                        customer.Date,
                        int.Parse(openingBalanceEquity),
                        balance,
                        0,
                        customer.Id,
                        0,
                        "Customer Opening Balance",
                        "OPENING BALANCE",
                        "Opening Balance Equity - Customer Code - " + customer.Code,
                        userId,
                        nowDate,
                        ""
                    );

                    await AddTransactionEntry(
                        customer.Date,
                        accountId,
                        0,
                        balance,
                        customer.Id,
                        customer.Id,
                        "Customer Opening Balance",
                        "OPENING BALANCE",
                        "Opening Balance - Customer Code - " + customer.Code,
                        userId,
                        nowDate,
                        ""
                    );
                    }
                else if (OpeningType== "Credit")
                    {
                    // Dr: Customer | Cr: Opening Balance Equity
                    await AddTransactionEntry(
                        customer.Date,
                        int.Parse(openingBalanceEquity),
                        0,
                        balance,
                        customer.Id,
                        0,
                        "Customer Opening Balance",
                        "OPENING BALANCE",
                        "Opening Balance Equity - Customer Code - " + customer.Code,
                        userId,
                        nowDate,
                        ""
                    );

                    await AddTransactionEntry(
                        customer.Date,
                        accountId,
                        balance,
                        0,
                        customer.Id,
                        customer.Id,
                        "Customer Opening Balance",
                        "OPENING BALANCE",
                        "Opening Balance - Customer Code - " + customer.Code,
                        userId,
                        nowDate,
                        ""
                    );
                    }
                }
            }

        public async Task AddTransactionEntry(DateOnly? date,int? accountId,decimal debit,decimal credit,int transactionId,int humId,string type,string voucherName,string description,int createdBy,DateOnly createdDate,string voucherNo)
            {
            try
                {
                var effectiveDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);

                var trx = new TblTransaction
                    {
                    Date = effectiveDate,
                    AccountId = accountId,
                    Debit = debit,
                    Credit = credit,
                    TransactionId = transactionId,
                    HumId = humId,
                    TType = voucherName,
                    Type = type,
                    Description = description,
                    CreatedBy = createdBy,
                    VoucherNo = voucherNo,
                    CreatedDate = createdDate,
                    State = 0
                    };

                _context.TblTransactions.Add(trx);
                await _context.SaveChangesAsync();
                }
            catch (Exception ex)
                {
                // This will surface any FK / required field / mapping error
                throw new InvalidOperationException("Error inserting transaction entry: " + ex.Message, ex);
                }
            }
        }
    }
