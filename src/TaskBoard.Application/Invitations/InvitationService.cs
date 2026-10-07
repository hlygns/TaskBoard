using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Activities;
using TaskBoard.Application.Common;
using TaskBoard.Application.Common.Exceptions;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Invitations;

public class InvitationService(
    IAppDbContext db,
    ICurrentUser currentUser,
    ITokenService tokenService,
    IEmailService emailService,
    IBoardNotifier notifier) : IInvitationService
{
    private static readonly TimeSpan InvitationLifetime = TimeSpan.FromDays(7);

    public async Task<IReadOnlyList<InvitationDto>> GetPendingAsync(Guid boardId, CancellationToken ct = default)
    {
        await db.EnsureOwnerAsync(boardId, currentUser.Id, ct);
        var now = DateTime.UtcNow;

        return await db.BoardInvitations
            .Where(i => i.BoardId == boardId && i.Status == InvitationStatus.Pending && i.ExpiresAt > now)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvitationDto(i.Id, i.Email, i.InvitedBy.FullName, i.CreatedAt, i.ExpiresAt))
            .ToListAsync(ct);
    }

    public async Task<InvitationDto> InviteAsync(Guid boardId, InviteMemberRequest request, CancellationToken ct = default)
    {
        await db.EnsureOwnerAsync(boardId, currentUser.Id, ct);
        var email = request.Email.Trim().ToLowerInvariant();

        var alreadyMember = await db.BoardMembers
            .AnyAsync(m => m.BoardId == boardId && m.User.Email == email, ct);
        if (alreadyMember)
            throw new ConflictException("Bu kişi zaten panonun üyesi.");

        // Aynı kişiye bekleyen davet varsa eskisi silinir, yeni link gönderilir (tekrar gönder).
        await db.BoardInvitations
            .Where(i => i.BoardId == boardId && i.Email == email && i.Status == InvitationStatus.Pending)
            .ExecuteDeleteAsync(ct);

        var token = tokenService.GenerateSecureToken();
        var invitation = new BoardInvitation
        {
            BoardId = boardId,
            Email = email,
            TokenHash = tokenService.HashToken(token),
            InvitedByUserId = currentUser.Id,
            ExpiresAt = DateTime.UtcNow.Add(InvitationLifetime)
        };

        db.BoardInvitations.Add(invitation);
        db.LogActivity(boardId, currentUser.Id, ActivityType.MemberInvited, invitation.Id, new { email });
        await db.SaveChangesAsync(ct);

        var boardName = await db.Boards.Where(b => b.Id == boardId).Select(b => b.Name).SingleAsync(ct);
        var inviterName = await db.Users.Where(u => u.Id == currentUser.Id).Select(u => u.FullName).SingleAsync(ct);

        // Token'ın kendisi sadece mail'e gider; veritabanında hash'i durur.
        await emailService.SendBoardInvitationAsync(email, inviterName, boardName, token, ct);

        return new InvitationDto(invitation.Id, email, inviterName, invitation.CreatedAt, invitation.ExpiresAt);
    }

    public async Task CancelAsync(Guid boardId, Guid invitationId, CancellationToken ct = default)
    {
        await db.EnsureOwnerAsync(boardId, currentUser.Id, ct);

        var deleted = await db.BoardInvitations
            .Where(i => i.Id == invitationId && i.BoardId == boardId && i.Status == InvitationStatus.Pending)
            .ExecuteDeleteAsync(ct);

        if (deleted == 0)
            throw new NotFoundException("Davet bulunamadı.");
    }

    public async Task<InvitationPreviewDto> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        var invitation = await FindByTokenAsync(token, ct);

        return new InvitationPreviewDto(
            invitation.Board.Name,
            invitation.InvitedBy.FullName,
            invitation.Email,
            invitation.Status,
            invitation.ExpiresAt);
    }

    public async Task<AcceptInvitationResult> AcceptAsync(string token, CancellationToken ct = default)
    {
        var invitation = await FindActionableAsync(token, ct);

        var alreadyMember = await db.BoardMembers
            .AnyAsync(m => m.BoardId == invitation.BoardId && m.UserId == currentUser.Id, ct);

        if (!alreadyMember)
        {
            db.BoardMembers.Add(new BoardMember
            {
                BoardId = invitation.BoardId,
                UserId = currentUser.Id,
                Role = BoardRole.Member
            });

            var myName = await db.Users.Where(u => u.Id == currentUser.Id).Select(u => u.FullName).SingleAsync(ct);
            db.LogActivity(invitation.BoardId, currentUser.Id, ActivityType.MemberJoined, currentUser.Id,
                new { memberName = myName });
        }

        invitation.Status = InvitationStatus.Accepted;
        await db.SaveChangesAsync(ct);
        await notifier.NotifyAsync(invitation.BoardId, new BoardEvent(BoardEvent.MembersChanged), ct);

        return new AcceptInvitationResult(invitation.BoardId);
    }

    public async Task DeclineAsync(string token, CancellationToken ct = default)
    {
        var invitation = await FindActionableAsync(token, ct);

        invitation.Status = InvitationStatus.Declined;
        await db.SaveChangesAsync(ct);
    }

    private async Task<BoardInvitation> FindByTokenAsync(string token, CancellationToken ct)
    {
        var tokenHash = tokenService.HashToken(token);
        var invitation = await db.BoardInvitations
            .Include(i => i.Board)
            .Include(i => i.InvitedBy)
            .SingleOrDefaultAsync(i => i.TokenHash == tokenHash, ct)
            ?? throw new NotFoundException("Davet bulunamadı ya da iptal edilmiş.");

        // Süresi dolan davetleri ilk erişildiklerinde işaretliyoruz.
        if (invitation.Status == InvitationStatus.Pending && invitation.ExpiresAt <= DateTime.UtcNow)
        {
            invitation.Status = InvitationStatus.Expired;
            await db.SaveChangesAsync(ct);
        }

        return invitation;
    }

    // Kabul/ret için: davet hâlâ bekliyor olmalı ve giriş yapan kişiye gönderilmiş olmalı.
    private async Task<BoardInvitation> FindActionableAsync(string token, CancellationToken ct)
    {
        var invitation = await FindByTokenAsync(token, ct);

        if (invitation.Status != InvitationStatus.Pending)
            throw new ConflictException(invitation.Status switch
            {
                InvitationStatus.Accepted => "Bu davet zaten kabul edilmiş.",
                InvitationStatus.Declined => "Bu davet reddedilmiş.",
                _ => "Bu davetin süresi dolmuş."
            });

        // Link başkasına iletilse bile sadece davet edilen e-posta adresinin sahibi kabul edebilir.
        var myEmail = await db.Users.Where(u => u.Id == currentUser.Id).Select(u => u.Email).SingleAsync(ct);
        if (myEmail != invitation.Email)
            throw new ForbiddenException(
                $"Bu davet {invitation.Email} adresine gönderilmiş. Lütfen o hesapla giriş yapın.");

        return invitation;
    }
}
