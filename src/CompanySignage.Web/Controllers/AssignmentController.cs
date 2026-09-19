using CompanySignage.Application.DTOs.Assignment;
using CompanySignage.Application.Interfaces;
using CompanySignage.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanySignage.Web.Controllers;

[Authorize]
public class AssignmentController : Controller
{
    private readonly IAssignmentService _assignmentService;
    private readonly IScreenService _screenService;
    private readonly IMediaService _mediaService;
    private readonly IPlaylistService _playlistService;

    public AssignmentController(
        IAssignmentService assignmentService,
        IScreenService screenService,
        IMediaService mediaService,
        IPlaylistService playlistService)
    {
        _assignmentService = assignmentService;
        _screenService = screenService;
        _mediaService = mediaService;
        _playlistService = playlistService;
    }

    public async Task<IActionResult> Index()
    {
        var assignments = await _assignmentService.GetAllAsync();
        return View(assignments);
    }

    [HttpGet]
    public async Task<IActionResult> SendNow(int? screenId = null)
    {
        ViewData["Screens"] = await _screenService.GetAllAsync();
        ViewData["MediaFiles"] = await _mediaService.GetAllAsync();
        ViewData["Playlists"] = await _playlistService.GetAllAsync();
        ViewData["PreselectedScreenId"] = screenId;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNow(List<int> screenIds, string contentType, int? mediaFileId, int? playlistId)
    {
        if (!screenIds.Any())
        {
            TempData["Error"] = "En az bir ekran seçmelisiniz.";
            return RedirectToAction("SendNow");
        }

        var dto = new SendNowDto { ScreenIds = screenIds };

        if (contentType == "playlist" && playlistId.HasValue)
        {
            dto.PlaylistId = playlistId;
            dto.AssignmentType = AssignmentType.Playlist;
        }
        else if (contentType == "media" && mediaFileId.HasValue)
        {
            dto.MediaFileId = mediaFileId;
            dto.AssignmentType = AssignmentType.SingleMedia;
        }
        else
        {
            TempData["Error"] = "Bir içerik veya playlist seçmelisiniz.";
            return RedirectToAction("SendNow");
        }

        var sent = await _assignmentService.SendNowAsync(dto);
        if (!sent)
        {
            TempData["Error"] = "İçerik gönderilemedi. Ekranın aktif, içeriğin geçerli olduğundan emin olun.";
            return RedirectToAction("SendNow");
        }

        TempData["Success"] = $"İçerik {screenIds.Distinct().Count()} ekrana gönderildi. Çevrimiçi ekranlarda yayın hemen yenilenecek.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _assignmentService.DeleteAsync(id);
        TempData["Success"] = "Yayın ataması silindi.";
        return RedirectToAction("Index");
    }
}
