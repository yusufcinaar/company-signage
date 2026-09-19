using CompanySignage.Application.DTOs.Assignment;
using CompanySignage.Application.DTOs.Playlist;
using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanySignage.Web.Controllers;

[Authorize]
public class PlaylistController : Controller
{
    private readonly IPlaylistService _playlistService;
    private readonly IMediaService _mediaService;
    private readonly IScreenService _screenService;
    private readonly IAssignmentService _assignmentService;

    public PlaylistController(
        IPlaylistService playlistService,
        IMediaService mediaService,
        IScreenService screenService,
        IAssignmentService assignmentService)
    {
        _playlistService = playlistService;
        _mediaService = mediaService;
        _screenService = screenService;
        _assignmentService = assignmentService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _playlistService.GetAllAsync());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePlaylistDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            ViewData["Error"] = "Oynatma listesi adı zorunludur.";
            return View(dto);
        }

        var playlist = await _playlistService.CreateAsync(dto);
        TempData["Success"] = "Oynatma listesi oluşturuldu. İçerikleri ekleyip ardından ekranlara yayınlayın.";
        return RedirectToAction("Edit", new { id = playlist.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var playlist = await _playlistService.GetByIdAsync(id);
        if (playlist == null) return NotFound();
        await PopulateEditViewDataAsync();
        return View(playlist);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, UpdatePlaylistDto dto)
    {
        var result = await _playlistService.UpdateAsync(id, dto);
        if (result == null) return NotFound();
        TempData["Success"] = "Oynatma listesi güncellendi.";
        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id, List<int>? screenIds)
    {
        var playlist = await _playlistService.GetByIdAsync(id);
        if (playlist == null) return NotFound();

        if (!playlist.IsActive)
        {
            TempData["Error"] = "Pasif bir oynatma listesi yayınlanamaz.";
            return RedirectToAction("Edit", new { id });
        }

        if (playlist.Items.Count == 0)
        {
            TempData["Error"] = "Yayınlamadan önce listeye en az bir içerik ekleyin.";
            return RedirectToAction("Edit", new { id });
        }

        if (screenIds == null || screenIds.Count == 0)
        {
            TempData["Error"] = "En az bir ekran seçmelisiniz.";
            return RedirectToAction("Edit", new { id });
        }

        await _playlistService.IncrementVersionAsync(id);
        var published = await _assignmentService.SendNowAsync(new SendNowDto
        {
            ScreenIds = screenIds.Distinct().ToList(),
            PlaylistId = id,
            AssignmentType = AssignmentType.Playlist
        });

        if (!published)
        {
            TempData["Error"] = "Yayın yapılamadı. Seçilen ekranların ve listenin aktif olduğunu kontrol edin.";
            return RedirectToAction("Edit", new { id });
        }

        TempData["Success"] = $"“{playlist.Name}” oynatma listesi {screenIds.Distinct().Count()} ekrana yayınlandı.";
        return RedirectToAction("Edit", new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _playlistService.DeleteAsync(id);
        TempData[deleted ? "Success" : "Error"] = deleted
            ? "Oynatma listesi silindi. Bu listeye bağlı ekran yayınları durduruldu."
            : "Oynatma listesi bulunamadı.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int id, AddPlaylistItemDto dto)
    {
        var result = await _playlistService.AddItemAsync(id, dto);
        TempData[result == null ? "Error" : "Success"] =
            result == null ? "İçerik listeye eklenemedi." : "İçerik listeye eklendi.";
        return Redirect($"/Playlist/Edit/{id}#step1");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItemDuration(int id, int itemId, int displayDuration)
    {
        var updated = await _playlistService.UpdateItemDurationAsync(id, itemId, displayDuration);
        TempData[updated ? "Success" : "Error"] = updated
            ? "Görselin gösterim süresi kaydedildi."
            : "Gösterim süresi güncellenemedi.";
        return Redirect($"/Playlist/Edit/{id}#step2");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int itemId, int playlistId)
    {
        await _playlistService.RemoveItemAsync(itemId);
        TempData["Success"] = "İçerik listeden çıkarıldı.";
        return Redirect($"/Playlist/Edit/{playlistId}#step2");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveItem(int id, int itemId, string direction)
    {
        var playlist = await _playlistService.GetByIdAsync(id);
        if (playlist == null) return NotFound();

        var itemIds = playlist.Items
            .OrderBy(item => item.OrderNumber)
            .Select(item => item.Id)
            .ToList();
        var currentIndex = itemIds.IndexOf(itemId);
        var targetIndex = string.Equals(direction, "up", StringComparison.OrdinalIgnoreCase)
            ? currentIndex - 1
            : currentIndex + 1;

        if (currentIndex >= 0 && targetIndex >= 0 && targetIndex < itemIds.Count)
        {
            (itemIds[currentIndex], itemIds[targetIndex]) = (itemIds[targetIndex], itemIds[currentIndex]);
            await _playlistService.ReorderItemsAsync(id, itemIds);
        }

        return Redirect($"/Playlist/Edit/{id}#step2");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(int id, [FromBody] ReorderPlaylistItemsDto dto)
    {
        var result = await _playlistService.ReorderItemsAsync(id, dto.ItemIds);
        return Json(new { success = result });
    }

    private async Task PopulateEditViewDataAsync()
    {
        ViewData["MediaFiles"] = await _mediaService.GetAllAsync();
        ViewData["Screens"] = await _screenService.GetAllAsync();
    }
}
