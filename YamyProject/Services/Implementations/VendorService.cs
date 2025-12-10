namespace YamyProject.Services.Implementations
    {
    public class VendorService(YamyDbContext context, IHttpContextAccessor httpContextAccessor, IGlobalService GlobalService, IListServices ListServices) :IVendorService
        {
        private readonly YamyDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IGlobalService _GlobalService = GlobalService;
        private readonly IListServices _ListServices = ListServices;

        public string GenerateNextCode()
            {
            int lastCode = _context.TblVendors
            .Select(v => (int?)v.Code) 
            .Max() ?? 0;      
            int next = lastCode + 1;
            return next.ToString("D5");
            }
        public async Task CreateVendorOrSubcontractorAcync(VendorSubContactViewModel  Model)
            {

            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    var vendor =await _context.TblVendors.FirstOrDefaultAsync(v=>v.Name==Model.Name);
                    if (vendor != null)
                        {

                        }
                    else
                        {
                        var Balance = Model.OpeningAmount;
                        if (Model.OpeningType == "Debit")
                            {
                            Balance = Balance;
                            }
                        if (Model.OpeningType == "Credit")
                            {
                            Balance = Balance * -1;
                            }
                        var Codes = GenerateNextCode();
                        var code = Codes.ToString();
                        var Vendors = new TblVendor
                            {
                            Code = int.Parse(code),
                            Name = Model.Name,
                            CatId = Model.CategoryId,
                            Balance = (decimal)Balance,
                            Date = Model.Date,
                            MainPhone = Model.MainPhone,
                            WorkPhone = Model.WorkPhone,
                            Mobile = Model.Fax,
                            Email = Model.Email,
                            Type=Model.Type,
                            Ccemail = Model.EmailCC,
                            Website = Model.Website,
                            Country = Model.CountryId.ToString(),
                            City = Model.CityId.ToString(),
                            ProjectId = Model.ProjectId,
                            ProjectSite = Model.ProjectId.ToString(),
                            Region = Model.Region,
                            BuildingName = Model.BulidingNumber,
                            AccountId = Model.AccountId,
                            Trn = Model.TRN,
                            FaciltyName = Model.FaciltyName,
                            Active = Model.IsActive ? 0 :1,
                            CreatedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0,
                            CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                            State = 0
                            };
                        _context.TblVendors.Add(Vendors);
                        await _context.SaveChangesAsync();

                        await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Add "+ Model.Type , Model.Type+" Center", Vendors.Id, "Added "+ Model.Type + ": " + Model.Name);
                        var openingBalanceEquity = await _ListServices.DefaultAccountsSet("Opening Balance Equity");

                        if (Model.OpeningType == "Credit")
                            {
                            await AddTransactionEntry(Model.Date, openingBalanceEquity, Model.OpeningAmount, 0, Vendors.Id, 0, Model.Type + " Opening Balance", "OPENING BALANCE", "Opening Balance Equity - "+ Model.Type + " Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                            await AddTransactionEntry(Model.Date, Model.AccountId, 0, Model.OpeningAmount, Vendors.Id, Vendors.Id, " Opening Balance", "OPENING BALANCE", "Account Payable  -  "+ Model.Type + " Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                            }
                        else if (Model.OpeningType == "Debit")
                            {
                            await AddTransactionEntry(Model.Date,   openingBalanceEquity, 0, Model.OpeningAmount, Vendors.Id, 0, Model.Type + " Opening Balance", "OPENING BALANCE", "Opening Balance Equity -  "+ Model.Type + " Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                            await AddTransactionEntry(Model.Date, Model.AccountId, Model.OpeningAmount, 0, Vendors.Id, Vendors.Id, Model.Type + " Opening Balance", "OPENING BALANCE", "Account Payable  -  "+ Model.Type + " Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                            }

                        }
                    await tx.CommitAsync();

                    }
                catch (Exception ex)
                    {
                    await tx.RollbackAsync();
                    throw new InvalidOperationException($"CreateVewndor failed: {ex.GetBaseException().Message}", ex);
                    }});
            }
        public async Task UpDateVendorOrSubcontractorAcync(VendorSubContactViewModel Model)
            {
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                        var Balance = Model.OpeningAmount;
                        if (Model.OpeningType == "Debit")
                            {
                            Balance = Balance;
                            }
                        if (Model.OpeningType == "Credit")
                            {
                            Balance = Balance * -1;
                            }

                        var vendors = await _context.TblVendors.FindAsync(Model.Id);
                    if (vendors == null)
                        { }
                    else
                        {
                        vendors.Code = int.Parse(Model.Code);
                        vendors.Name = Model.Name;
                        vendors.CatId = Model.CategoryId;
                        vendors.Balance = (decimal)Balance;
                        vendors.Date = Model.Date;
                        vendors.MainPhone = Model.MainPhone;
                        vendors.WorkPhone = Model.WorkPhone;
                        vendors.Mobile = Model.Fax;
                        vendors.Email = Model.Email;
                        vendors.Ccemail = Model.EmailCC;
                        vendors.Website = Model.Website;
                        vendors.Type = Model.Type;
                        vendors.Country = Model.CountryId.ToString();
                        vendors.City = Model.CityId.ToString();
                        vendors.ProjectId = Model.ProjectId;
                        vendors.ProjectSite = Model.ProjectId.ToString();
                        vendors.Region = Model.Region;
                        vendors.BuildingName = Model.BulidingNumber;
                        vendors.AccountId = Model.AccountId;
                        vendors.Trn = Model.TRN;
                        vendors.FaciltyName = Model.FaciltyName;
                        vendors.Active = Model.IsActive ? 0 : 1;
                        vendors.State = 0;

                        _context.TblVendors.Update(vendors);

                        await _context.SaveChangesAsync();
                        await _context.TblTransactions
                        .Where(t => t.TransactionId == vendors.Id && t.Type == Model.Type + " Opening Balance")
                        .ExecuteDeleteAsync();

                        await _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Update " + Model.Type, " Center", vendors.Id, "Updated " + Model.Type + ": " + Model.Name);
                        var openingBalanceEquity = await _ListServices.DefaultAccountsSet("Opening Balance Equity");

                        //Get the login User
                        if (Model.OpeningType == "Credit")
                            {
                            await AddTransactionEntry(Model.Date, openingBalanceEquity, Model.OpeningAmount, 0, Model.Id, 0, Model.Type + " Opening Balance", "OPENING BALANCE", "Opening Balance Equity -  " + Model.Type + " Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                            await AddTransactionEntry(Model.Date, Model.AccountId, 0, Model.OpeningAmount, Model.Id, Model.Id, Model.Type + " Opening Balance", "OPENING BALANCE", "Account Payable  -  " + Model.Type + " Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                            }
                        else if (Model.OpeningType == "Debit")
                            {
                            await AddTransactionEntry(Model.Date, openingBalanceEquity, 0, Model.OpeningAmount, Model.Id, 0, Model.Type + " Opening Balance", "OPENING BALANCE", "Opening Balance Equity -  " + Model.Type + " Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                            await AddTransactionEntry(Model.Date, Model.AccountId, Model.OpeningAmount, 0, Model.Id, Model.Id, Model.Type + " Opening Balance", "OPENING BALANCE", "Account Payable  -  " + Model.Type + " Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                            }
                        }
                    await tx.CommitAsync();
                    }
                catch (Exception ex)
                    {
                    await tx.RollbackAsync();
                    throw new InvalidOperationException($"CreateVewndor failed: {ex.GetBaseException().Message}", ex);
                    }
            });
        }

        public async Task AddTransactionEntry(DateOnly date, int? accountId, decimal debit, decimal credit, int transactionId, int humId, string type, string voucher_name,
               string description, int createdBy, DateOnly createdDate)
            {
            var trx = new TblTransaction
                {
                Date = date,
                AccountId = accountId,
                Debit = debit,
                Credit = credit,
                TransactionId = transactionId,
                HumId = humId,
                TType = voucher_name,
                Type = type,
                Description = description,
                CreatedBy = createdBy,
                VoucherNo = null,
                CreatedDate = createdDate,
                State = 0
                };
            _context.TblTransactions.Add(trx);
            await _context.SaveChangesAsync();
            }
        }
    }