using System.Security.Claims;
using CompanySignage.Application.DTOs.Auth;
using CompanySignage.Application.Interfaces;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CompanySignage.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthApiController(IAuthService auth, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("csrf")]
    public IActionResult Csrf()
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await auth.LoginAsync(dto, HttpContext.Connection.RemoteIpAddress?.ToString());
        if (!result.Success) return Unauthorized(new { success = false, message = result.ErrorMessage });
        var user = await auth.GetUserAsync(dto.Username);
        var claims = new List<Claim> { new(ClaimTypes.Name, dto.Username), new(ClaimTypes.Role, result.Role ?? "Operator") };
        if (user.HasValue) claims.Add(new(ClaimTypes.NameIdentifier, user.Value.userId.ToString()));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { success = true });
    }
}
