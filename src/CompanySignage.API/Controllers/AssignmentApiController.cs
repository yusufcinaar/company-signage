using CompanySignage.Application.DTOs.Assignment;
using CompanySignage.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompanySignage.API.Controllers;

[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
[AutoValidateAntiforgeryToken]
[ApiController]
[Route("api/assignments")]
public class AssignmentApiController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;

    public AssignmentApiController(IAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var assignments = await _assignmentService.GetAllAsync();
        return Ok(assignments);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentDto dto)
    {
        try { return Ok(await _assignmentService.CreateAsync(dto)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAssignmentDto dto)
    {
        try
        {
            var assignment = await _assignmentService.UpdateAsync(id, dto);
            if (assignment == null) return NotFound();
            return Ok(assignment);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _assignmentService.DeleteAsync(id);
        if (!result) return NotFound();
        return Ok(new { success = true });
    }

    [HttpPost("send-now")]
    public async Task<IActionResult> SendNow([FromBody] SendNowDto dto)
    {
        var result = await _assignmentService.SendNowAsync(dto);
        if (!result) return BadRequest(new { error = "Gönderim başarısız." });
        return Ok(new { success = true });
    }
}
