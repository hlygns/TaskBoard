using System.ComponentModel.DataAnnotations;

namespace TaskBoard.Application.Auth;

public record RegisterRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(100)] string FullName,
    [Required, MinLength(8), MaxLength(100)] string Password);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record UserDto(Guid Id, string Email, string FullName);

// Servisin döndürdüğü sonuç. Refresh token istemciye cookie ile gideceği için
// API katmanı bunu ikiye ayırır: access token gövdeye, refresh token cookie'ye.
public record AuthResult(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User);
