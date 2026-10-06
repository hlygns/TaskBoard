using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Common.Interfaces;

public record AccessToken(string Token, DateTime ExpiresAt);

public record GeneratedRefreshToken(string Token, string TokenHash, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);
    GeneratedRefreshToken CreateRefreshToken();

    // Tahmin edilemez, URL'de kullanılabilir rastgele değer (davet linkleri vb.).
    string GenerateSecureToken();
    string HashToken(string token);
}
