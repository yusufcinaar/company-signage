using CompanySignage.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CompanySignage.API.Security;

public static class DeviceCredentials
{
    public const string CodeHeader = "X-Screen-Code";
    public const string TokenHeader = "X-Device-Token";

    public static async Task<bool> ValidateAsync(HttpContext http, IDbContextFactory factory, string? routeCode = null)
    {
        var code = http.Request.Headers[CodeHeader].ToString().Trim().ToUpperInvariant();
        var token = http.Request.Headers[TokenHeader].ToString();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(token) || token.Length > 512)
            return false;
        if (routeCode != null && !string.Equals(code, routeCode.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;
        using var db = factory.CreateDbContext();
        var screen = await db.Screens.AsNoTracking().FirstOrDefaultAsync(s => s.ScreenCode == code && s.IsActive);
        if (screen == null) return false;
        try { return BCrypt.Net.BCrypt.Verify(token, screen.DeviceTokenHash); }
        catch (BCrypt.Net.SaltParseException) { return false; }
    }
}

public sealed class DeviceAuthorizationFilter(IDbContextFactory factory) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var code = context.RouteData.Values["screenCode"]?.ToString();
        if (!await DeviceCredentials.ValidateAsync(context.HttpContext, factory, code))
            context.Result = new UnauthorizedResult();
    }
}

public static class MediaAuthorization
{
    public static IApplicationBuilder UseSignageMediaAuthorization(this IApplicationBuilder app)
        => app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/signageHub")
                || (context.Request.Path.StartsWithSegments("/uploads") && context.User.Identity?.IsAuthenticated != true))
            {
                var factory = context.RequestServices.GetRequiredService<IDbContextFactory>();
                if (!await DeviceCredentials.ValidateAsync(context, factory))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }
            await next(context);
        });
}
