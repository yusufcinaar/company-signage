using CompanySignage.Application.DTOs.Player;
using CompanySignage.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompanySignage.API.Controllers;

[ServiceFilter(typeof(CompanySignage.API.Security.DeviceAuthorizationFilter))]
[ApiController]
[Route("api/player/{screenCode}")]
public class PlayerApiController : ControllerBase
{
    private readonly IPlayerService _playerService;

    public PlayerApiController(IPlayerService playerService)
    {
        _playerService = playerService;
    }

    [HttpGet("configuration")]
    public async Task<IActionResult> GetConfiguration(string screenCode)
    {
        var config = await _playerService.GetConfigurationAsync(screenCode);
        if (config == null) return NotFound(new { error = "Ekran bulunamadı." });
        return Ok(config);
    }

    [HttpGet("current-publication")]
    public async Task<IActionResult> GetCurrentPublication(string screenCode)
    {
        var pub = await _playerService.GetCurrentPublicationAsync(screenCode);
        if (pub == null) return NotFound(new { error = "Ekran bulunamadı." });
        return Ok(pub);
    }

    [HttpGet("playlist")]
    public async Task<IActionResult> GetPlaylist(string screenCode)
    {
        var pub = await _playerService.GetCurrentPublicationAsync(screenCode);
        if (pub == null) return NotFound(new { error = "Ekran bulunamadı." });
        if (pub.AssignmentType != Domain.Enums.AssignmentType.Playlist)
            return Ok(new { items = new List<object>() });
        return Ok(new { items = pub.PlaylistItems, version = pub.PlaylistVersion });
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat(string screenCode, [FromBody] HeartbeatDto dto)
    {
        if (dto.ScreenCode != screenCode)
            dto.ScreenCode = screenCode;
        var result = await _playerService.HeartbeatAsync(screenCode, dto);
        if (!result) return NotFound(new { error = "Ekran bulunamadı." });
        return Ok(new { success = true });
    }

    [HttpPost("download-completed")]
    public async Task<IActionResult> DownloadCompleted(string screenCode, [FromBody] DownloadCompletedDto dto)
    {
        if (dto.ScreenCode != screenCode)
            dto.ScreenCode = screenCode;
        await _playerService.DownloadCompletedAsync(screenCode, dto);
        return Ok(new { success = true });
    }

    [HttpPost("error")]
    public async Task<IActionResult> ReportError(string screenCode, [FromBody] PlayerErrorDto dto)
    {
        if (dto.ScreenCode != screenCode)
            dto.ScreenCode = screenCode;
        await _playerService.ReportErrorAsync(screenCode, dto);
        return Ok(new { success = true });
    }
}
