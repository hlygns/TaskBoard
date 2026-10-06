namespace TaskBoard.Application.Invitations;

public interface IInvitationService
{
    // Pano sahibinin işlemleri
    Task<IReadOnlyList<InvitationDto>> GetPendingAsync(Guid boardId, CancellationToken ct = default);
    Task<InvitationDto> InviteAsync(Guid boardId, InviteMemberRequest request, CancellationToken ct = default);
    Task CancelAsync(Guid boardId, Guid invitationId, CancellationToken ct = default);

    // Davet edilen kişinin işlemleri (mail'deki linkteki token ile)
    Task<InvitationPreviewDto> GetByTokenAsync(string token, CancellationToken ct = default);
    Task<AcceptInvitationResult> AcceptAsync(string token, CancellationToken ct = default);
    Task DeclineAsync(string token, CancellationToken ct = default);
}
