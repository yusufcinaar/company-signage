using CompanySignage.Application.DTOs.Screen;
using CompanySignage.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanySignage.Web.Controllers;

[Authorize]
public class ScreenController : Controller
{
    private readonly IScreenService _screenService;
    private readonly IWebHostEnvironment _environment;

    public ScreenController(IScreenService screenService, IWebHostEnvironment environment)
    {
        _screenService = screenService;
        _environment = environment;
    }

    public async Task<IActionResult> Index()
    {
        var screens = await _screenService.GetAllAsync();
        return View(screens);
    }

    [HttpGet]
    public IActionResult DownloadInstaller()
    {
        const string fileName = "CompanySignage-Ekran-Kurulum.zip";
        var packagePath = Path.Combine(_environment.ContentRootPath, "Packages", fileName);
        if (!System.IO.File.Exists(packagePath))
            return NotFound("Ekran kurulum paketi henüz oluşturulmamış.");

        return PhysicalFile(packagePath, "application/zip", fileName, enableRangeProcessing: true);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateScreenDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.ScreenCode))
        {
            ViewData["Error"] = "Ekran adı ve kodu zorunludur.";
            return View(dto);
        }

        if (string.IsNullOrWhiteSpace(dto.DeviceToken))
            dto.DeviceToken = Guid.NewGuid().ToString();

        await _screenService.CreateAsync(dto);
        TempData["Success"] = $"Ekran oluşturuldu. Cihaz tokenı: {dto.DeviceToken} — Bu tokenı player kurulumunda kullanın, tekrar gösterilmeyecektir.";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var screen = await _screenService.GetByIdAsync(id);
        if (screen == null) return NotFound();
        return View(screen);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateScreenDto dto)
    {
        var result = await _screenService.UpdateAsync(id, dto);
        if (result == null) return NotFound();
        TempData["Success"] = "Ekran güncellendi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _screenService.DeleteAsync(id);
        TempData["Success"] = "Ekran silindi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Refresh(int id)
    {
        await _screenService.RefreshAsync(id);
        TempData["Success"] = "Yenileme komutu gönderildi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Stop(int id)
    {
        await _screenService.StopAsync(id);
        TempData["Success"] = "Durdurma komutu gönderildi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restart(int id)
    {
        await _screenService.RestartAsync(id);
        TempData["Success"] = "Yeniden başlatma komutu gönderildi.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearCache(int id)
    {
        await _screenService.ClearCacheAsync(id);
        TempData["Success"] = "Cache temizleme komutu gönderildi.";
        return RedirectToAction("Index");
    }
}
