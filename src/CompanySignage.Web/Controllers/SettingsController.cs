using System.Security.Claims;
using CompanySignage.Application.DTOs.Auth;
using CompanySignage.Application.Interfaces;
using CompanySignage.Web.Models;
using CompanySignage.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace CompanySignage.Web.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly BackupManager _backupManager;

    public SettingsController(IAuthService authService, IConfiguration configuration, BackupManager backupManager)
    {
        _authService = authService;
        _configuration = configuration;
        _backupManager = backupManager;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["UploadFolder"] = _configuration["FileStorage:UploadFolder"] ?? @"C:\CompanySignagePublic\Server\Uploads";
        ViewData["BaseUrl"] = _configuration["FileStorage:BaseUrl"] ?? "http://localhost:5000";
        ViewData["BackupSettings"] = await _backupManager.LoadAsync();

        var connectionString = _configuration.GetConnectionString("DefaultConnection") ?? "";
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            ViewData["SqlServer"] = builder.DataSource;
            ViewData["DatabaseName"] = builder.InitialCatalog;
            ViewData["SqlEncrypted"] = builder.Encrypt.ToString();
        }
        catch
        {
            ViewData["SqlServer"] = "-";
            ViewData["DatabaseName"] = "-";
            ViewData["SqlEncrypted"] = "-";
        }
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBackupSettings(string backupFolder, bool autoBackupEnabled, string backupTime, int retentionDays)
    {
        try
        {
            var existing = await _backupManager.LoadAsync();
            existing.BackupFolder = backupFolder.Trim();
            existing.AutoBackupEnabled = autoBackupEnabled;
            existing.BackupTime = backupTime;
            existing.RetentionDays = retentionDays;
            await _backupManager.SaveAsync(existing);
            TempData["Success"] = "Yedekleme ayarları kaydedildi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RunBackup()
    {
        var result = await _backupManager.RunBackupAsync();
        if (result.Success) TempData["Success"] = result.Message;
        else TempData["Error"] = "Yedekleme başarısız: " + result.Message;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword)
        {
            TempData["Error"] = "Yeni şifreler eşleşmiyor.";
            return RedirectToAction("Index");
        }
        if (newPassword.Length < 8)
        {
            TempData["Error"] = "Yeni şifre en az 8 karakter olmalıdır.";
            return RedirectToAction("Index");
        }
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            TempData["Error"] = "Oturum bilgisi okunamadı. Lütfen yeniden giriş yapın.";
            return RedirectToAction("Index");
        }
        var result = await _authService.ChangePasswordAsync(userId, new ChangePasswordDto
        {
            CurrentPassword = currentPassword,
            NewPassword = newPassword
        });
        TempData[result ? "Success" : "Error"] = result ? "Şifre başarıyla değiştirildi." : "Mevcut şifre hatalı.";
        return RedirectToAction("Index");
    }
}
