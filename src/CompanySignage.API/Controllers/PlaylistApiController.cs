using CompanySignage.Application.DTOs.Playlist;
using CompanySignage.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompanySignage.API.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
[ApiController]
[Route("api/playlists")]
public class PlaylistApiController : ControllerBase
{
    private readonly IPlaylistService _playlistService;

    public PlaylistApiController(IPlaylistService playlistService)
    {
        _playlistService = playlistService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var playlists = await _playlistService.GetAllAsync();
        return Ok(playlists);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var playlist = await _playlistService.GetByIdAsync(id);
        if (playlist == null) return NotFound();
        return Ok(playlist);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlaylistDto dto)
    {
        var playlist = await _playlistService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = playlist.Id }, playlist);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePlaylistDto dto)
    {
        var playlist = await _playlistService.UpdateAsync(id, dto);
        if (playlist == null) return NotFound();
        return Ok(playlist);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _playlistService.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPost("{id}/items")]
    public async Task<IActionResult> AddItem(int id, [FromBody] AddPlaylistItemDto dto)
    {
        var playlist = await _playlistService.AddItemAsync(id, dto);
        if (playlist == null) return NotFound();
        return Ok(playlist);
    }

    [HttpDelete("items/{itemId}")]
    public async Task<IActionResult> RemoveItem(int itemId)
    {
        var result = await _playlistService.RemoveItemAsync(itemId);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPut("{id}/reorder")]
    public async Task<IActionResult> Reorder(int id, [FromBody] ReorderPlaylistItemsDto dto)
    {
        var result = await _playlistService.ReorderItemsAsync(id, dto.ItemIds);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }
}
