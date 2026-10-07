using Hangfire;
using Microsoft.Extensions.Configuration;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Notifications;

namespace TaskBoard.Infrastructure.Email;

// Maili hazırlar ve Hangfire kuyruğuna atar; asıl gönderimi arka planda IEmailSender yapar.
// Faydası: HTTP isteği SMTP sunucusunu beklemez, SMTP geçici olarak çökerse Hangfire otomatik tekrar dener.
public class EmailService(IBackgroundJobClient jobs, IConfiguration configuration) : IEmailService
{
    private readonly EmailTemplates _templates = new(configuration["ClientApp:Url"] ?? "http://localhost:5173");

    public Task SendBoardInvitationAsync(
        string toEmail, string inviterName, string boardName, string invitationToken, CancellationToken ct = default) =>
        Enqueue(_templates.Invitation(toEmail, inviterName, boardName, invitationToken));

    public Task SendDueDateReminderAsync(DueDateReminderEmail email, CancellationToken ct = default) =>
        Enqueue(_templates.DueDateReminder(email));

    public Task SendDailyDigestAsync(DailyDigestEmail email, CancellationToken ct = default) =>
        Enqueue(_templates.DailyDigest(email));

    private Task Enqueue(EmailMessage message)
    {
        // Hangfire, CancellationToken parametresini çalışma anında kendi token'ıyla değiştirir.
        jobs.Enqueue<IEmailSender>(sender => sender.SendAsync(message, CancellationToken.None));
        return Task.CompletedTask;
    }
}
