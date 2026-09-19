using CompanySignage.Application.Interfaces;
using CompanySignage.API.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace CompanySignage.API.Services;

public class SignalRService : ISignalRService
{
    private readonly IHubContext<SignageHub> _hubContext;

    public SignalRService(IHubContext<SignageHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToScreenAsync(string screenCode, string method, object payload)
    {
        await _hubContext.Clients.Group(NormalizeScreenCode(screenCode)).SendAsync(method, payload);
    }

    public async Task SendToAllScreensAsync(string method, object payload)
    {
        await _hubContext.Clients.All.SendAsync(method, payload);
    }

    public async Task SendToScreensAsync(List<string> screenCodes, string method, object payload)
    {
        var tasks = screenCodes
            .Select(NormalizeScreenCode)
            .Distinct()
            .Select(code => _hubContext.Clients.Group(code).SendAsync(method, payload));
        await Task.WhenAll(tasks);
    }

    private static string NormalizeScreenCode(string screenCode)
        => screenCode.Trim().ToUpperInvariant();
}
