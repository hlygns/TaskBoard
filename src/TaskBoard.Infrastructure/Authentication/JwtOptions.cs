namespace TaskBoard.Infrastructure.Authentication;

// appsettings.json'daki "Jwt" bölümüne karşılık gelir.
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }

    // En az 32 karakter. Asla repoya gerçek değer yazılmaz; production'da ortam değişkeninden gelir.
    public required string Secret { get; init; }

    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
}
