using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace TaskBoard.Infrastructure.Email;

public record EmailMessage(string ToEmail, string ToName, string Subject, string HtmlBody);

// Maili gerçekten gönderen katman. Hangfire kuyruğundaki iş bunu çağırır.
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    // Boşsa SMTP kullanılmaz, mailler sadece log'a yazılır.
    public string? Host { get; init; }
    public int Port { get; init; } = 587;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UseSsl { get; init; }
    public string FromEmail { get; init; } = "noreply@taskboard.local";
    public string FromName { get; init; } = "TaskBoard";
}

public class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromEmail));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToEmail));
        mime.Subject = message.Subject;
        mime.Body = new BodyBuilder { HtmlBody = message.HtmlBody }.ToMessageBody();

        var host = _options.Host ?? throw new InvalidOperationException("Smtp:Host ayarlanmamış.");

        using var client = new SmtpClient();
        await client.ConnectAsync(host, _options.Port,
            _options.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);
        if (!string.IsNullOrEmpty(_options.Username))
            await client.AuthenticateAsync(_options.Username, _options.Password ?? "", ct);
        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);

        logger.LogInformation("Mail gönderildi: {To} - {Subject}", message.ToEmail, message.Subject);
    }
}

// SMTP ayarı yoksa (ör. Mailpit çalışmıyorken) mail içeriğini terminale yazar.
public class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        logger.LogInformation(
            """

            ===================== MAİL =====================
            Kime : {To}
            Konu : {Subject}
            {Body}
            ================================================
            """,
            message.ToEmail, message.Subject, message.HtmlBody);
        return Task.CompletedTask;
    }
}
