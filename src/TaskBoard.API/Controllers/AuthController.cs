using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.API.Controllers;

public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, ICurrentUser currentUser) : ControllerBase
{
    private const string RefreshTokenCookie = "refreshToken";

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        return ToResponse(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return ToResponse(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken ct)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie]
            ?? throw new UnauthorizedException("Oturum bulunamadı, lütfen giriş yapın.");

        var result = await authService.RefreshAsync(refreshToken, ct);
        return ToResponse(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        if (Request.Cookies[RefreshTokenCookie] is { } refreshToken)
            await authService.LogoutAsync(refreshToken, ct);

        Response.Cookies.Delete(RefreshTokenCookie, CookieOptions());
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) =>
        await authService.GetUserAsync(currentUser.Id, ct);

    private AuthResponse ToResponse(AuthResult result)
    {
        // Refresh token'ı httpOnly cookie'de tutuyoruz: JavaScript okuyamaz, böylece
        // XSS saldırısında çalınamaz. Access token ise kısa ömürlü ve React'te bellekte tutulacak.
        var options = CookieOptions();
        options.Expires = result.RefreshTokenExpiresAt;
        Response.Cookies.Append(RefreshTokenCookie, result.RefreshToken, options);

        return new AuthResponse(result.AccessToken, result.AccessTokenExpiresAt, result.User);
    }

    private static CookieOptions CookieOptions() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth"
    };
}
