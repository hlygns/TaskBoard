namespace TaskBoard.Application.Common.Interfaces;

// Mail içeriği ve link oluşturma Infrastructure'ın işi; Application sadece "davet maili gönder" der.
public interface IEmailService
{
    Task SendBoardInvitationAsync(
        string toEmail, string inviterName, string boardName, string invitationToken, CancellationToken ct = default);
}
