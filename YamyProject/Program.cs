var builder = WebApplication.CreateBuilder(args);
var ConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

QuestPDF.Settings.License = LicenseType.Community;


builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("ApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7280/");
    client.DefaultRequestHeaders.Accept.Clear();
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowRestaurant", policy =>
    {
        policy.WithOrigins("https://localhost:7290")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});


builder.Services.AddLocalization(options => options.ResourcesPath = "");
builder.Services.AddMvc().AddViewLocalization();

var cultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
builder.Services.Configure<RequestLocalizationOptions>(o => {
    o.DefaultRequestCulture = new RequestCulture("en");
    o.SupportedCultures = cultures;
    o.SupportedUICultures = cultures;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(24);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;

});

builder.Services.AddMvc()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options => {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(YamyProject.SharedResource)); 
    });


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
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "YamyProjectAuth";
});
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".YamyProject.Session";
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
app.UseRequestLocalization();
app.UseSession();
app.UseCors("AllowRestaurant");
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();