using TaskBoard.Domain.Enums;

namespace TaskBoard.Domain.Entities;

// Kullanıcı–pano ara tablosu. Birincil anahtar (BoardId, UserId) bileşik anahtarıdır,
// bu yüzden BaseEntity'den türemiyor.
public class BoardMember
{
    public Guid BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public BoardRole Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
