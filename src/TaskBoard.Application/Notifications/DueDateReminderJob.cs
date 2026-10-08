using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.Application.Notifications;

// Saatte bir çalışır: son tarihi bugün ya da yarın olan, açık (tamamlanmamış, arşivlenmemiş) ve henüz
// hatırlatılmamış kartlar için sorumlu kişiye TEK bir mail (tüm kartları listeli) gönderir.
// Sorumlu kişi: atanan kişi, yoksa pano sahibi (bkz. Responsibility).
public class DueDateReminderJob(IAppDbContext db, IEmailService emailService, TimeProvider time)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var todayStart = now.Date;
        var until = todayStart.AddDays(2);

        var cards = await db.Cards
            .Where(Responsibility.IsOpen)
            .Where(c => c.DueDate >= todayStart && c.DueDate < until && c.DueReminderSentAt == null)
            .WithResponsible()
            .Select(x => new
            {
                x.Card.Id,
                RecipientId = x.ResponsibleId,
                Item = new EmailCardItem(x.Card.Column.BoardId, x.Card.Column.Board.Name, x.Card.Column.Name,
                    x.Card.Title, x.Card.DueDate!.Value)
            })
            .ToListAsync(ct);

        var recipientIds = cards.Where(c => c.RecipientId != null).Select(c => c.RecipientId!.Value).Distinct().ToList();
        var recipients = await db.Users
            .Where(u => recipientIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => new { u.Email, u.FullName }, ct);

        var sentCardIds = new List<Guid>();
        foreach (var group in cards.Where(c => c.RecipientId != null).GroupBy(c => c.RecipientId!.Value))
        {
            var recipient = recipients[group.Key];
            var items = group.Select(c => c.Item).OrderBy(i => i.DueDate).ToList();

            // Mail kuyruğa alınır (Hangfire); SMTP hatası olursa kuyruk kendisi tekrar dener.
            await emailService.SendDueDateReminderAsync(new DueDateReminderEmail(recipient.Email, recipient.FullName, items), ct);
            sentCardIds.AddRange(group.Select(c => c.Id));
        }

        // Önce kuyruğa alıp sonra işaretliyoruz: arada çökerse en kötü ihtimalle hatırlatma iki kez gider,
        // hiç gitmemesinden iyidir (at-least-once).
        if (sentCardIds.Count > 0)
            await db.Cards
                .Where(c => sentCardIds.Contains(c.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.DueReminderSentAt, now), ct);
    }
}
