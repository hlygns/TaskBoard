using TaskBoard.Application.Notifications;

namespace TaskBoard.Application.Common.Interfaces;

// Mail içeriği, link oluşturma ve gönderme şekli (SMTP, kuyruk) Infrastructure'ın işi;
// Application sadece "şu kişiye davet / hatırlatma / özet maili gönder" der.
public interface IEmailService
{
    Task SendBoardInvitationAsync(
        string toEmail, string inviterName, string boardName, string invitationToken, CancellationToken ct = default);

    Task SendDueDateReminderAsync(DueDateReminderEmail email, CancellationToken ct = default);

    Task SendDailyDigestAsync(DailyDigestEmail email, CancellationToken ct = default);
}
