using System.Text.Json.Serialization;
using CompanySignage.API.Hubs;
using CompanySignage.API.Services;
using CompanySignage.Application.Interfaces;
using CompanySignage.Application.Services;
using CompanySignage.Infrastructure.BackgroundJobs;
using CompanySignage.Infrastructure.Data;
using CompanySignage.Infrastructure.FileStorage;

using CompanySignage.API.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<DeviceAuthorizationFilter>();
builder.Services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");

builder.Services.AddControllersWithViews().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSignalR();
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\MSSQLLocalDB;Database=CompanySignagePublic;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

builder.Services.AddSingleton<IDbContextFactory>(new SignageDbContextFactory(connectionString));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IScreenService, ScreenService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IPlaylistService, PlaylistService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<ISignalRService, SignalRService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

builder.Services.AddHostedService<HeartbeatMonitorService>();



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var adminPassword = builder.Configuration["AdminSetup:InitialPassword"] ?? "Admin123!";
    using var dbContext = new SignageDbContext(connectionString);
    await DbInitializer.InitializeAsync(dbContext, adminPassword);
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

var uploadFolder = builder.Configuration["FileStorage:UploadFolder"] ?? @"C:\CompanySignagePublic\Server\Uploads";
if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
app.UseSignageMediaAuthorization();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadFolder),
    RequestPath = "/uploads"
});

app.MapControllers();
app.MapHub<SignageHub>("/signageHub");

app.Run();
