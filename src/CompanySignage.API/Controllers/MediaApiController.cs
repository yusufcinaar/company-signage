using CompanySignage.Application.DTOs.Media;
using CompanySignage.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompanySignage.API.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
[ApiController]
[Route("api/media")]
public class MediaApiController : ControllerBase
{
    private readonly IMediaService _mediaService;

    public MediaApiController(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var media = await _mediaService.GetAllAsync();
        return Ok(media);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var media = await _mediaService.GetByIdAsync(id);
        if (media == null) return NotFound();
        return Ok(media);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(500 * 1024 * 1024)]
    public async Task<IActionResult> Upload([FromForm] string name, [FromForm] string? description, [FromForm] int displayDuration, [FromForm] bool soundEnabled, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Dosya seçilmedi." });

        using var stream = file.OpenReadStream();
        try
        {
            var media = await _mediaService.UploadAsync(stream, file.FileName, file.ContentType, file.Length, name, description, displayDuration, soundEnabled);
            return Ok(media);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMediaDto dto)
    {
        var media = await _mediaService.UpdateAsync(id, dto);
        if (media == null) return NotFound();
        return Ok(media);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _mediaService.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }
}
