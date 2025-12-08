namespace YamyProject.Services.Implementations
    {
    public class VendorService(YamyDbContext context, IHttpContextAccessor httpContextAccessor, IGlobalService GlobalService) :IVendorService
        {
        private readonly YamyDbContext _context = context;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
        private readonly IGlobalService _GlobalService = GlobalService;
        public  string GenerateNextCode()
            {
            //var prefix = 00000; // Prefix for Credit Note
            //var lastCodeValue =  _context.TblVendors
            //   .Select(s => s.Code)
            //   .DefaultIfEmpty(0)
            //   .Max();
            //if (lastCodeValue!=null)
            //    {
            //    prefix = lastCodeValue + 1;
            //    }
            //else
            //    {
            //    prefix = prefix + 1;
            //    }
            //return prefix.ToString("D5");
            int lastCode = _context.TblVendors
            .Select(v => (int?)v.Code) 
            .Max() ?? 0;              

            int next = lastCode + 1;
            return next.ToString("D5");
            }
        public async Task CreateVendorOrSubcontractorAcync(VendorSubContactViewModel  Model)
            {
               // int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var strategy = _context.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _context.Database.BeginTransactionAsync();
                try
                    {
                    var vendor =await _context.TblVendors.FindAsync(Model.Name);
                    if(vendor!=null)
                        {
                        
                        }
                    var  Balance = 0.00m;
                    if (Model.Debit != 0m)
                        {
                        Balance = Model.Debit;
                        }
                    if (Model.Credit != 0m)
                        {
                        Balance = Model.Credit;
                        }
                        var Codes = GenerateNextCode();
                    var code=Codes.ToString();
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
                        Email= Model.Email,
                        Ccemail=Model.EmailCC,
                        Website=Model.Website,
                        Country=Model.CountryId.ToString(),
                        City=Model.CityId.ToString(),
                        ProjectId=Model.ProjectId,
                        ProjectSite=Model.ProjectId.ToString(),
                        Region=Model.Region,
                        BuildingName=Model.BulidingNumber,
                        AccountId=Model.AccountId,
                        Trn=Model.TRN,
                        FaciltyName=Model.FaciltyName,
                        Active = Model.IsActive ? 1:0,
                        CreatedBy= _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0,
                        CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow),
                        State=0
                        };
                    _context.TblVendors.Add(Vendors);
                    _context.SaveChanges();
                    //Add Loger
                    _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Add Vendor", "Vendor Center", vendor.Id, "Added Vendor: " + Model.Name);
                    var openingBalanceEquity =await _GlobalService.SelectDefaultLevelAccount("Opening Balance Equity");

                    if (Model.Credit != 0)
                        {
                        await AddTransactionEntry(Model.Date, int.Parse(openingBalanceEquity), Model.Credit, 0, Model.Id, 0, "Subcontractor Opening Balance", "OPENING BALANCE", "Opening Balance Equity - Subcontractor Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                        await AddTransactionEntry(Model.Date, Model.AccountId, 0, Model.Credit, Model.Id, Model.Id, "Subcontractor Opening Balance", "OPENING BALANCE", "Account Payable  - Subcontractor Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                        }
                    else if (Model.Debit != 0)
                        {
                        await AddTransactionEntry(Model.Date, int.Parse(openingBalanceEquity), 0, Model.Debit, Model.Id, 0, "Subcontractor Opening Balance", "OPENING BALANCE", "Opening Balance Equity - Subcontractor Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                        await AddTransactionEntry(Model.Date, Model.AccountId, Model.Debit, 0, Model.Id, Model.Id, "Subcontractor Opening Balance", "OPENING BALANCE", "Account Payable  - Subcontractor Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
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
                    var vendor = await _context.TblVendors.FindAsync(Model.Name);
                    if (vendor != null)
                        {

                        }
                    var Balance = 0.00m;
                    if (Model.Debit != 0m)
                        {
                        Balance = Model.Debit;
                        }
                    if (Model.Credit != 0m)
                        {
                        Balance = Model.Credit;
                        }
                 //  Balance=Model.OpeningAmount;


                    var vendors =await _context.TblVendors.FindAsync(Model.Id);
                    if (vendors == null)
                        { }
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
                        vendors.Country = Model.CountryId.ToString();
                        vendors.City = Model.CityId.ToString();
                        vendors.ProjectId = Model.ProjectId;
                        vendors.ProjectSite = Model.ProjectId.ToString();
                        vendors.Region = Model.Region;
                        vendors.BuildingName = Model.BulidingNumber;
                        vendors.AccountId = Model.AccountId;
                        vendors.Trn = Model.TRN;
                        vendors.FaciltyName = Model.FaciltyName;
                        vendors.Active = Model.IsActive ? 1 : 0;
//vendors.CreatedBy = _httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0;
                      //  vendors.CreatedDate = DateOnly.FromDateTime(DateTime.UtcNow);
                        vendors.State = 0;
                        
                    _context.TblVendors.Update(vendors);

                    await _context.SaveChangesAsync();
                    await _context.TblTransactions
                    .Where(t => t.TransactionId == vendor.Id && t.Type == "Vendor Opening Balance")
                    .ExecuteDeleteAsync();
                    //Get the Defolte account
                    //Addloger 
                    _GlobalService.LogAudit(_httpContextAccessor.HttpContext.Session.GetInt32("UserId") ?? 0, "Update Vendor", "Vendor Center", vendor.Id, "Updated Vendor: " + Model.Name);
                    var openingBalanceEquity = await _GlobalService.SelectDefaultLevelAccount("Opening Balance Equity");

                    //Get the login User
                    if (Model.Credit != 0)
                        {
                        await AddTransactionEntry(Model.Date, int.Parse(openingBalanceEquity), Model.Credit, 0, Model.Id, 0, "Subcontractor Opening Balance", "OPENING BALANCE", "Opening Balance Equity - Subcontractor Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                        await AddTransactionEntry(Model.Date, Model.AccountId, 0, Model.Credit, Model.Id, Model.Id, "Subcontractor Opening Balance", "OPENING BALANCE", "Account Payable  - Subcontractor Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                        }
                    else if(Model.Debit!=0)
                        {
                        await AddTransactionEntry(Model.Date, int.Parse(openingBalanceEquity), 0, Model.Debit,  Model.Id, 0, "Subcontractor Opening Balance", "OPENING BALANCE", "Opening Balance Equity - Subcontractor Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
                        await AddTransactionEntry(Model.Date, Model.AccountId,  Model.Debit,0, Model.Id, Model.Id, "Subcontractor Opening Balance", "OPENING BALANCE", "Account Payable  - Subcontractor Code - " + Model.Code, 1, DateOnly.FromDateTime(DateTime.Now));
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
                VoucherNo = voucher_name,
                CreatedDate = createdDate,
                State = 0
                };
            _context.TblTransactions.Add(trx);
            await _context.SaveChangesAsync();
            }
        }
    }