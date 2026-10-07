using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.Application.Notifications;

// Saatte bir çalışır: son tarihi bugün ya da yarın olan, atanmış ve henüz hatırlatılmamış kartlar için
// atanan kişiye TEK bir mail (tüm kartları listeli) gönderir.
public class DueDateReminderJob(IAppDbContext db, IEmailService emailService, TimeProvider time)
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var todayStart = now.Date;
        var until = todayStart.AddDays(2);

        var cards = await db.Cards
            .Include(c => c.Assignee)
            .Include(c => c.Column).ThenInclude(col => col.Board)
            .Where(c => c.AssigneeId != null
                        && c.DueDate >= todayStart && c.DueDate < until
                        && c.DueReminderSentAt == null
                        && !CompletedColumns.Names.Contains(c.Column.Name.ToLower()))
            .ToListAsync(ct);

        foreach (var group in cards.GroupBy(c => c.AssigneeId))
        {
            var assignee = group.First().Assignee!;
            var items = group
                .OrderBy(c => c.DueDate)
                .Select(c => new EmailCardItem(c.Column.BoardId, c.Column.Board.Name, c.Column.Name, c.Title, c.DueDate!.Value))
                .ToList();

            // Mail kuyruğa alınır (Hangfire); SMTP hatası olursa kuyruk kendisi tekrar dener.
            await emailService.SendDueDateReminderAsync(new DueDateReminderEmail(assignee.Email, assignee.FullName, items), ct);

            foreach (var card in group)
                card.DueReminderSentAt = now;
        }

        // Önce kuyruğa alıp sonra işaretliyoruz: arada çökerse en kötü ihtimalle hatırlatma iki kez gider,
        // hiç gitmemesinden iyidir (at-least-once).
        await db.SaveChangesAsync(ct);
    }
}
