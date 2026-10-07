using System.Globalization;
using System.Net;
using System.Text;
using TaskBoard.Application.Notifications;

namespace TaskBoard.Infrastructure.Email;

// Basit HTML mail şablonları. Mail istemcileri harici CSS desteklemediği için stiller satır içi.
// Kullanıcıdan gelen her metin (kart başlığı, pano adı...) HTML-encode edilir: mail içine kod enjekte edilemesin.
public class EmailTemplates(string clientUrl)
{
    private static readonly CultureInfo Turkish = new("tr-TR");

    public EmailMessage Invitation(string toEmail, string inviterName, string boardName, string token)
    {
        var link = $"{clientUrl}/invitations/{Uri.EscapeDataString(token)}";
        var body = $"""
            <p><strong>{E(inviterName)}</strong> seni <strong>{E(boardName)}</strong> panosuna davet etti.</p>
            <p>{Button(link, "Daveti görüntüle")}</p>
            <p style="color:#64748b;font-size:13px">Davet 7 gün geçerlidir. Bu daveti beklemiyorsan bu maili yok sayabilirsin.</p>
            """;
        return new EmailMessage(toEmail, toEmail, $"{inviterName} seni \"{boardName}\" panosuna davet etti", Layout(body));
    }

    public EmailMessage DueDateReminder(DueDateReminderEmail email)
    {
        var body = $"""
            <p>Merhaba {E(email.ToName)},</p>
            <p>Sana atanmış şu kartların son tarihi yaklaşıyor:</p>
            {CardList(email.Cards)}
            """;
        var subject = email.Cards.Count == 1
            ? $"Hatırlatma: \"{email.Cards[0].CardTitle}\" kartının son tarihi yaklaşıyor"
            : $"Hatırlatma: {email.Cards.Count} kartın son tarihi yaklaşıyor";
        return new EmailMessage(email.ToEmail, email.ToName, subject, Layout(body));
    }

    public EmailMessage DailyDigest(DailyDigestEmail email)
    {
        var sb = new StringBuilder($"<p>Günaydın {E(email.ToName)}, işte bugünün özeti:</p>");

        if (email.Overdue.Count > 0)
            sb.Append($"""<h3 style="color:#b91c1c;margin:24px 0 8px">Gecikmiş ({email.Overdue.Count})</h3>""")
              .Append(CardList(email.Overdue));
        if (email.DueSoon.Count > 0)
            sb.Append($"""<h3 style="margin:24px 0 8px">Yakında ({email.DueSoon.Count})</h3>""")
              .Append(CardList(email.DueSoon));
        if (email.ActivityCountLast24h > 0)
            sb.Append($"<p style=\"margin-top:24px\">Son 24 saatte panolarında ekip arkadaşların <strong>{email.ActivityCountLast24h}</strong> değişiklik yaptı.</p>");

        sb.Append($"<p>{Button(clientUrl + "/boards", "Panolarıma git")}</p>");
        return new EmailMessage(email.ToEmail, email.ToName, "TaskBoard günlük özetin", Layout(sb.ToString()));
    }

    private string CardList(IEnumerable<EmailCardItem> cards)
    {
        var rows = string.Concat(cards.Select(c => $"""
            <tr>
              <td style="padding:8px 0;border-bottom:1px solid #e2e8f0">
                <a href="{clientUrl}/boards/{c.BoardId}" style="color:#4f46e5;text-decoration:none;font-weight:600">{E(c.CardTitle)}</a>
                <div style="color:#64748b;font-size:13px">{E(c.BoardName)} · {E(c.ColumnName)}</div>
              </td>
              <td style="padding:8px 0;border-bottom:1px solid #e2e8f0;text-align:right;white-space:nowrap;color:#334155">
                {c.DueDate.ToString("d MMMM", Turkish)}
              </td>
            </tr>
            """));
        return $"""<table style="width:100%;border-collapse:collapse">{rows}</table>""";
    }

    private static string Button(string href, string text) =>
        $"""<a href="{href}" style="display:inline-block;margin-top:16px;background:#4f46e5;color:#fff;padding:10px 18px;border-radius:6px;text-decoration:none;font-weight:600">{E(text)}</a>""";

    private static string Layout(string content) => $"""
        <div style="font-family:Segoe UI,Arial,sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1e293b;font-size:15px;line-height:1.5">
          <div style="font-weight:700;font-size:18px;margin-bottom:16px">TaskBoard</div>
          {content}
        </div>
        """;

    private static string E(string text) => WebUtility.HtmlEncode(text);
}
