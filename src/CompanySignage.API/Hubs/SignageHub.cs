using Microsoft.AspNetCore.SignalR;

namespace CompanySignage.API.Hubs;

public class SignageHub : Hub
{
    private readonly ILogger<SignageHub> _logger;
    private readonly CompanySignage.Application.Interfaces.IDbContextFactory _factory;

    public SignageHub(ILogger<SignageHub> logger, CompanySignage.Application.Interfaces.IDbContextFactory factory)
    {
        _logger = logger;
        _factory = factory;
    }

    public async Task JoinScreenGroup(string screenCode)
    {
        var groupName = NormalizeScreenCode(screenCode);
        if (!string.Equals(Context.Items["screen"] as string, groupName, StringComparison.Ordinal))
            throw new HubException("Ekran kimligi dogrulanmadi.");
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} joined group {ScreenCode}", Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("JoinedGroup", new { screenCode = groupName, success = true });
    }

    public async Task LeaveScreenGroup(string screenCode)
    {
        var groupName = NormalizeScreenCode(screenCode);
        if (!string.Equals(Context.Items["screen"] as string, groupName, StringComparison.Ordinal))
            throw new HubException("Ekran kimligi dogrulanmadi.");
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} left group {ScreenCode}", Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        var http = Context.GetHttpContext();
        if (http == null || !await CompanySignage.API.Security.DeviceCredentials.ValidateAsync(http, _factory))
        {
            Context.Abort();
            throw new HubException("Cihaz kimligi dogrulanmadi.");
        }
        Context.Items["screen"] = http.Request.Headers[CompanySignage.API.Security.DeviceCredentials.CodeHeader].ToString().Trim().ToUpperInvariant();
        _logger.LogInformation("New connection: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Connection disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    private static string NormalizeScreenCode(string screenCode)
        => screenCode.Trim().ToUpperInvariant();
}
