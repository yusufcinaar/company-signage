using CompanySignage.Application.DTOs.Screen;
using CompanySignage.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompanySignage.API.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
[ApiController]
[Route("api/screens")]
public class ScreenApiController : ControllerBase
{
    private readonly IScreenService _screenService;

    public ScreenApiController(IScreenService screenService)
    {
        _screenService = screenService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var screens = await _screenService.GetAllAsync();
        return Ok(screens);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var screen = await _screenService.GetByIdAsync(id);
        if (screen == null) return NotFound();
        return Ok(screen);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScreenDto dto)
    {
        var screen = await _screenService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = screen.Id }, screen);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateScreenDto dto)
    {
        var screen = await _screenService.UpdateAsync(id, dto);
        if (screen == null) return NotFound();
        return Ok(screen);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _screenService.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPost("{id}/refresh")]
    public async Task<IActionResult> Refresh(int id)
    {
        var result = await _screenService.RefreshAsync(id);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPost("{id}/stop")]
    public async Task<IActionResult> Stop(int id)
    {
        var result = await _screenService.StopAsync(id);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPost("{id}/restart")]
    public async Task<IActionResult> Restart(int id)
    {
        var result = await _screenService.RestartAsync(id);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPost("{id}/clear-cache")]
    public async Task<IActionResult> ClearCache(int id)
    {
        var result = await _screenService.ClearCacheAsync(id);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }
}
