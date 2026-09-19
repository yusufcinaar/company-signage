using CompanySignage.Application.DTOs.Media;
using CompanySignage.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CompanySignage.Application.DTOs.Assignment;
using CompanySignage.Domain.Enums;

namespace CompanySignage.Web.Controllers;

[Authorize]
public class MediaController : Controller
{
    private readonly IMediaService _mediaService;
    private readonly IScreenService _screenService;
    private readonly IAssignmentService _assignmentService;
    private readonly long _maxUploadBytes;

    public MediaController(
        IMediaService mediaService,
        IScreenService screenService,
        IAssignmentService assignmentService,
        IConfiguration configuration)
    {
        _mediaService = mediaService;
        _screenService = screenService;
        _assignmentService = assignmentService;
        _maxUploadBytes = (configuration.GetValue<long?>("Uploads:MaxFileSizeMb") ?? 200) * 1024 * 1024;
    }

    public async Task<IActionResult> Index() => View(await _mediaService.GetAllAsync());

    [HttpGet]
    public async Task<IActionResult> Upload()
    {
        await PopulateUploadViewDataAsync();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int screenId, string name, string? description,
        int displayDuration, bool soundEnabled, IFormFile file)
    {
        var screen = await _screenService.GetByIdAsync(screenId);
        if (screen == null || !screen.IsActive)
            return await UploadErrorAsync("Lütfen geçerli ve aktif bir ekran seçin.");

        if (file == null || file.Length == 0)
            return await UploadErrorAsync("Lütfen bir dosya seçin.");

        if (file.Length > _maxUploadBytes)
            return await UploadErrorAsync($"Dosya boyutu en fazla {_maxUploadBytes / 1024 / 1024} MB olabilir.");

        if (string.IsNullOrWhiteSpace(name))
            name = Path.GetFileNameWithoutExtension(file.FileName);
        if (displayDuration <= 0) displayDuration = 10;

        try
        {
            using var stream = file.OpenReadStream();
            var media = await _mediaService.UploadAsync(stream, file.FileName, file.ContentType,
                file.Length, name, description, displayDuration, soundEnabled);

            var sent = await _assignmentService.SendNowAsync(new SendNowDto
            {
                ScreenIds = new List<int> { screenId },
                MediaFileId = media.Id,
                AssignmentType = AssignmentType.SingleMedia
            });

            TempData["Success"] = sent
                ? $"İçerik kaydedildi ve {screen.Name} ekranına gönderildi."
                : "İçerik kaydedildi ancak ekrana gönderilemedi. İçerik Gönder sayfasından tekrar deneyebilirsiniz.";
            return RedirectToAction("Index");
        }
        catch (ArgumentException ex)
        {
            return await UploadErrorAsync(ex.Message);
        }
    }

    private async Task<IActionResult> UploadErrorAsync(string message)
    {
        ViewData["Error"] = message;
        await PopulateUploadViewDataAsync();
        return View("Upload");
    }

    private async Task PopulateUploadViewDataAsync()
    {
        ViewData["MaxFileSizeMb"] = _maxUploadBytes / 1024 / 1024;
        ViewData["Screens"] = await _screenService.GetAllAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var media = await _mediaService.GetByIdAsync(id);
        return media == null ? NotFound() : View(media);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateMediaDto dto)
    {
        var result = await _mediaService.UpdateAsync(id, dto);
        if (result == null) return NotFound();
        TempData["Success"] = "İçerik güncellendi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediaService.DeleteAsync(id);
        TempData["Success"] = "İçerik silindi.";
        return RedirectToAction("Index");
    }
}
