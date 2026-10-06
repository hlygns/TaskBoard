using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Common.Interfaces;

public record AccessToken(string Token, DateTime ExpiresAt);

public record GeneratedRefreshToken(string Token, string TokenHash, DateTime ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user);
    GeneratedRefreshToken CreateRefreshToken();
    string HashToken(string token);
}
