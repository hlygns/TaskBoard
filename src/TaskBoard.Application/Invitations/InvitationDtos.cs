using System.ComponentModel.DataAnnotations;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Invitations;

public record InviteMemberRequest([Required, EmailAddress, MaxLength(256)] string Email);

// Pano sahibinin gördüğü bekleyen davet listesi.
public record InvitationDto(Guid Id, string Email, string InvitedByName, DateTime CreatedAt, DateTime ExpiresAt);

// Davet linkine tıklayan kişinin gördüğü özet. Giriş yapmadan da görülebilir.
public record InvitationPreviewDto(
    string BoardName,
    string InvitedByName,
    string Email,
    InvitationStatus Status,
    DateTime ExpiresAt);

public record AcceptInvitationResult(Guid BoardId);
