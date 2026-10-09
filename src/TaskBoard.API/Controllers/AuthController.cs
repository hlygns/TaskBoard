using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TaskBoard.Application.Auth;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.API.Controllers;

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt,
    UserDto User,
    // Sadece mobil istemciye döner; web'de refresh token httpOnly cookie'de kalır.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? RefreshToken = null);

// Mobil istemci refresh token'ı gövdede gönderir.
public record RefreshTokenRequest(string? RefreshToken);

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService, ICurrentUser currentUser) : ControllerBase
{
    private const string RefreshTokenCookie = "refreshToken";

    // Web tarayıcısı refresh token'ı httpOnly cookie'de tutar (JavaScript okuyamaz → XSS'e karşı güvenli).
    // Mobil uygulamada cookie yönetimi zahmetli ve HTTPS'siz ev ağında "Secure" cookie hiç gönderilmez;
    // bu yüzden mobil istemci "X-Client: mobile" başlığıyla gelir, token'ı gövdede alır ve
    // telefonun şifreli deposunda (iOS Keychain / Android Keystore) saklar.
    private bool IsMobileClient => Request.Headers["X-Client"] == "mobile";

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
    public async Task<ActionResult<AuthResponse>> Refresh(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? body, CancellationToken ct)
    {
        var refreshToken = ReadRefreshToken(body)
            ?? throw new UnauthorizedException("Oturum bulunamadı, lütfen giriş yapın.");

        var result = await authService.RefreshAsync(refreshToken, ct);
        return ToResponse(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshTokenRequest? body, CancellationToken ct)
    {
        if (ReadRefreshToken(body) is { } refreshToken)
            await authService.LogoutAsync(refreshToken, ct);

        if (!IsMobileClient)
            Response.Cookies.Delete(RefreshTokenCookie, CookieOptions());
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct) =>
        await authService.GetUserAsync(currentUser.Id, ct);

    private string? ReadRefreshToken(RefreshTokenRequest? body) =>
        IsMobileClient ? body?.RefreshToken : Request.Cookies[RefreshTokenCookie];

    private AuthResponse ToResponse(AuthResult result)
    {
        if (IsMobileClient)
            return new AuthResponse(result.AccessToken, result.AccessTokenExpiresAt, result.User, result.RefreshToken);

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
