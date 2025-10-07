
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
builder.Services.AddScoped<IEditeCustomerService, CustomerEditeService>();
builder.Services.AddScoped<ISalesService, SalesService>();
builder.Services.AddScoped<ISalesServices, SalesServices>();
builder.Services.AddScoped<IListServices, ListServices>();
builder.Services.AddScoped<ISalesCenterService, SalesCenterService>();
builder.Services.AddScoped<IStockSettlementService, StockSettlementService>();
builder.Services.AddHttpClient<IMicroserviceClient, MicroserviceClient>();
builder.Services.AddHttpClient<IMicroserviceClientt, MicroserviceClientt>();


builder.Services.AddScoped<ISalesClientService, SalesClientService>();



builder.Services.AddScoped<IItemStockSettlementService, ItemStockSettlementService>();
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



// ? Register Session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24); // session timeout
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
// Add DbContext with SQL Server
builder.Services.AddDbContext<YamyDbContext>(options =>
    options.UseMySql(ConnectionString,
        ServerVersion.AutoDetect(ConnectionString)
    )
);
builder.Services.AddAutoMapper(typeof(MappingProfile));
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();
app.UseSession();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=CompanyList}/{id?}");

app.Run();
