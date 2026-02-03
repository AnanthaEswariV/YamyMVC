using QuestPDF.Infrastructure;
using YamyProject.Core.Consts.Mapping;
using YamyProject.Services;
using YamyProject.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);
var ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

//// Add DbContext with SQL Server
//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(ConnectionString));

// Add services to the container.

builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<IListServices, ListServices>();
builder.Services.AddHttpClient<IMicroserviceClientt, MicroserviceClientt>();
builder.Services.AddHttpClient<IMicroserviceClient, MicroserviceClient>();
builder.Services.AddScoped<IEditeCustomerService, CustomerEditeService>();
builder.Services.AddScoped<ISalesCreateService, SalesCreateService>();
builder.Services.AddScoped<ISalesQuotationCenterService, SalesQuotationCenterService>();
builder.Services.AddScoped<ISalesOrderCenterService, SalesOrderCenterService>();
builder.Services.AddScoped<ISalesProformaCenterService, SalesProformaCenterService>();
builder.Services.AddScoped<ISalesReturnService, SalesReturnService>();
builder.Services.AddScoped<IReceiptVoucherService, ReceiptVoucherService>();
builder.Services.AddScoped<ICreditNoteService, CreditNoteService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<ISalesServices, SalesServices>();
builder.Services.AddScoped<ISalesCenterService, SalesCenterService>();
builder.Services.AddScoped<IStockSettlementService, StockSettlementService>();
builder.Services.AddScoped<IVendorService, VendorService>();
builder.Services.AddScoped<IVendorsCenterService, VendorsCenterService>();
builder.Services.AddScoped<IPurchaseReturnService, PurchaseReturnService>();
builder.Services.AddScoped<IPurchaseOrdersService, PurchaseOrdersService>();
builder.Services.AddScoped<IPaymentVoucherService, PaymentVoucherService>();
builder.Services.AddScoped<IDebitNotesService, DebitNotesService>();
builder.Services.AddScoped<IAdvancePaymentService, AdvancePaymentService>();
builder.Services.AddScoped<IGeneralJournalVouchersService, GeneralJournalVouchersService>();
builder.Services.AddScoped<IIncomeSummaryServices, IncomeSummaryServices>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICustomerSummaryService, CustomerSummaryService>();
builder.Services.AddScoped<ISalesReportServices, SalesReportServices>();
builder.Services.AddScoped<IVendorSummaryService, VendorSummaryService>();
builder.Services.AddScoped<IPurchaseReportServices, PurchaseReportServices>();
builder.Services.AddScoped<IVatCorporateService, VatCorporateService>();

builder.Services.AddScoped<IGlobalService, GlobalService>();
builder.Services.AddScoped<ICurrentUserContextService, CurrentUserContextService>();

builder.Services.AddScoped<IItemStockSettlementService, ItemStockSettlementService>();

builder.Services.AddScoped<ISalesClientService, SalesClientService>();

QuestPDF.Settings.License = LicenseType.Community;
////
// Microservice HTTP client
//builder.Services.AddHttpClient<IMicroserviceClientt, MicroserviceClientt>(client =>
//{
//    client.BaseAddress = new Uri(builder.Configuration["Microservices:SettlementApi"]);
//    client.Timeout = TimeSpan.FromSeconds(10);
//});

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7280/");
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});



builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    //options.Cookie.Path = "/YamyProject"; // Virtual folder
    //options.Cookie.SecurePolicy = CookieSecurePolicy.None; // If using HTTP
    //options.Cookie.SecurePolicy = CookieSecurePolicy.None;
});


// Add DbContext with SQL Server
//builder.Services.AddDbContext<YamyDbContext>(options =>
//    options.UseMySql(ConnectionString,
//        ServerVersion.AutoDetect(ConnectionString)
//    )WWW
//);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddDbContext<YamyDbContext>((serviceProvider, options) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();
    var httpContextAccessor = serviceProvider.GetRequiredService<IHttpContextAccessor>();

    // Base connection string (without forcing a specific Database)
    var baseConnStr = config.GetConnectionString("DefaultConnection");

    // Database name from Session or fallback
    var dbNameFromSession = httpContextAccessor.HttpContext?.Session.GetString("DatabaseName");

    // you can store DefaultDatabase as a normal setting or as a connection string; adapt if needed
    var defaultDbName = config.GetConnectionString("DefaultDatabase");

    var csb = new MySqlConnectionStringBuilder(baseConnStr)
    {
        Database = string.IsNullOrWhiteSpace(dbNameFromSession)
                    ? defaultDbName
                    : dbNameFromSession
    };

    options.UseMySql(
        csb.ConnectionString,
        ServerVersion.AutoDetect(csb.ConnectionString)
    );
});

builder.Services.AddAutoMapper(typeof(MappingProfile));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=CompanyList}/{id?}");

app.Run();