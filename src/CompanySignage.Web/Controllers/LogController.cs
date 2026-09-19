using CompanySignage.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanySignage.Web.Controllers;

[Authorize]
public class LogController : Controller
{
    private readonly ILogService _logService;

    public LogController(ILogService logService)
    {
        _logService = logService;
    }

    public async Task<IActionResult> Index()
    {
        var logs = await _logService.GetRecentAsync(200);
        return View(logs);
    }
}
