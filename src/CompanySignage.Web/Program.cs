using System.Text.Json.Serialization;
using CompanySignage.API.Hubs;
using CompanySignage.Application.Interfaces;
using CompanySignage.Application.Services;
using CompanySignage.Infrastructure.BackgroundJobs;
using CompanySignage.Infrastructure.Data;
using CompanySignage.Infrastructure.FileStorage;
using Microsoft.AspNetCore.Authentication.Cookies;

using CompanySignage.API.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<DeviceAuthorizationFilter>();
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");
var maxUploadMb = builder.Configuration.GetValue<long?>("Uploads:MaxFileSizeMb") ?? 200;
var maxUploadBytes = maxUploadMb * 1024 * 1024;
var maxRequestBodyBytes = checked(maxUploadBytes + 1024 * 1024);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o => o.MultipartBodyLengthLimit = maxRequestBodyBytes);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = maxRequestBodyBytes);

// Windows Service olarak calisma destegi
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "CompanySignage Server";
});

// MVC panel + API controller'ları (API assembly'sinden) tek host üzerinde
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddApplicationPart(typeof(CompanySignage.API.Controllers.PlayerApiController).Assembly);

builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=CompanySignagePublic;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=True";

connectionString = ConfigureLocalSqlEncryption(connectionString);

builder.Services.AddSingleton<IDbContextFactory>(new SignageDbContextFactory(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IScreenService, ScreenService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IPlaylistService, PlaylistService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<ISignalRService, CompanySignage.API.Services.SignalRService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddSingleton<CompanySignage.Web.Services.BackupManager>();
builder.Services.AddHostedService<CompanySignage.Web.Services.BackupHostedService>();

// Background: heartbeat'i geçen ekranları offline işaretle
builder.Services.AddHostedService<HeartbeatMonitorService>();

// Cookie tabanlı yönetici oturumu
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "CompanySignage.Auth";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Veritabanını başlat (migration + seed)
using (var scope = app.Services.CreateScope())
{
    var adminPassword = builder.Configuration["AdminSetup:InitialPassword"] ?? "Admin123!";
    using var dbContext = new SignageDbContext(connectionString);
    await DbInitializer.InitializeAsync(dbContext, adminPassword);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// Upload klasörünü /uploads adresinden sun
var uploadFolder = builder.Configuration["FileStorage:UploadFolder"] ?? @"C:\CompanySignagePublic\Server\Uploads";
if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
app.UseSignageMediaAuthorization();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadFolder),
    RequestPath = "/uploads"
});



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<SignageHub>("/signageHub");

app.Run();
static string ConfigureLocalSqlEncryption(string connectionString)
{
    var sql = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
    var dataSource = sql.DataSource.Trim().ToLowerInvariant();
    var isLocal = dataSource is "." or "(local)" or "localhost" or "127.0.0.1" or "::1"
        || dataSource.StartsWith(@".\")
        || dataSource.StartsWith(@"(local)\")
        || dataSource.StartsWith(@"localhost\")
        || dataSource.StartsWith(@"127.0.0.1\")
        || dataSource.StartsWith(@"(localdb)\");

    if (isLocal)
    {
        // Eski yerel SQLEXPRESS/LocalDB örnekleri TLS desteklemeyebilir.
        // Yalnızca aynı bilgisayardaki bağlantıda şifreleme kapatılır.
        sql["Encrypt"] = false;
    }

    return sql.ConnectionString;
}
