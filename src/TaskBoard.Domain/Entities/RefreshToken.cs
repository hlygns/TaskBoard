using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    // Token rotation: yenilenen token'ın yerine geçen token. Çalınmış token tespiti için kullanılır.
    public string? ReplacedByTokenHash { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
