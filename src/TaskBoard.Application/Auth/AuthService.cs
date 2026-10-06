using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Auth;

public class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    // Kullanıcı yok mu, şifre mi yanlış ayırt etmiyoruz: saldırgan hangi maillerin
    // kayıtlı olduğunu öğrenemesin (user enumeration).
    private const string InvalidCredentials = "E-posta veya şifre hatalı.";
    private const string InvalidRefreshToken = "Oturum geçersiz, lütfen tekrar giriş yapın.";

    public async Task<AuthResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Bu e-posta adresiyle kayıtlı bir hesap zaten var.");

        var user = new User
        {
            Email = email,
            FullName = request.FullName.Trim(),
            PasswordHash = passwordHasher.Hash(request.Password)
        };

        db.Users.Add(user);
        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !passwordHasher.Verify(user.PasswordHash, request.Password))
            throw new UnauthorizedException(InvalidCredentials);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = tokenService.HashToken(refreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (stored is null)
            throw new UnauthorizedException(InvalidRefreshToken);

        // Daha önce kullanılıp yenilenmiş bir token tekrar geldiyse, token çalınmış olabilir.
        // Güvenli tarafta kalıp kullanıcının tüm oturumlarını kapatıyoruz.
        if (stored.RevokedAt is not null)
        {
            await RevokeAllActiveTokensAsync(stored.UserId, ct);
            throw new UnauthorizedException(InvalidRefreshToken);
        }

        if (!stored.IsActive)
            throw new UnauthorizedException(InvalidRefreshToken);

        // Token rotation: her yenilemede eski token iptal edilir, yerine yenisi verilir.
        var result = await IssueTokensAsync(stored.User, ct, replacing: stored);
        return result;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var tokenHash = tokenService.HashToken(refreshToken);
        var stored = await db.RefreshTokens.SingleOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (stored is { IsActive: true })
        {
            stored.RevokedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<UserDto> GetUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");

        return ToDto(user);
    }

    private async Task<AuthResult> IssueTokensAsync(User user, CancellationToken ct, RefreshToken? replacing = null)
    {
        var accessToken = tokenService.CreateAccessToken(user);
        var refreshToken = tokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshToken.TokenHash,
            ExpiresAt = refreshToken.ExpiresAt
        });

        if (replacing is not null)
        {
            replacing.RevokedAt = DateTime.UtcNow;
            replacing.ReplacedByTokenHash = refreshToken.TokenHash;
        }

        await db.SaveChangesAsync(ct);

        return new AuthResult(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.Token,
            refreshToken.ExpiresAt,
            ToDto(user));
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static UserDto ToDto(User user) => new(user.Id, user.Email, user.FullName);
}
