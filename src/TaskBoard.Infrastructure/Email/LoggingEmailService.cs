using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.Infrastructure.Email;

// Geliştirme için: mail göndermek yerine içeriği API terminaline yazar.
// Hangfire adımında gerçek SMTP gönderen bir uygulamayla değiştirilecek; Application katmanı değişmeyecek.
public class LoggingEmailService(IConfiguration configuration, ILogger<LoggingEmailService> logger) : IEmailService
{
    private readonly string _clientUrl = configuration["ClientApp:Url"] ?? "http://localhost:5173";

    public Task SendBoardInvitationAsync(
        string toEmail, string inviterName, string boardName, string invitationToken, CancellationToken ct = default)
    {
        var link = $"{_clientUrl}/invitations/{invitationToken}";

        logger.LogInformation(
            """

            ================== DAVET MAİLİ ==================
            Kime : {ToEmail}
            Konu : {InviterName} seni "{BoardName}" panosuna davet etti
            Link : {Link}
            =================================================
            """,
            toEmail, inviterName, boardName, link);

        return Task.CompletedTask;
    }
}
