using TaskBoard.Domain.Common;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Domain.Entities;

// Davet edilen kişi henüz kayıtlı olmayabilir, bu yüzden UserId değil Email tutuyoruz.
public class BoardInvitation : BaseEntity
{
    public required string Email { get; set; }
    public required string TokenHash { get; set; }
    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;
    public DateTime ExpiresAt { get; set; }

    public Guid BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public Guid InvitedByUserId { get; set; }
    public User InvitedBy { get; set; } = null!;
}
