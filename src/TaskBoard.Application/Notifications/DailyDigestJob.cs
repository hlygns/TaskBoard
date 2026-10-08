using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.Application.Notifications;

// Her sabah çalışır. Her kullanıcıya: sorumlu olduğu gecikmiş kartları, önümüzdeki 3 gün içinde son tarihi olan
// kartları ve son 24 saatte panolarında başkalarının yaptığı hareket sayısını gönderir.
// Anlatacak bir şeyi olmayan kullanıcıya mail gitmez.
public class DailyDigestJob(IAppDbContext db, IEmailService emailService, TimeProvider time)
{
    private const int DueSoonDays = 3;

    public async Task RunAsync(CancellationToken ct)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var todayStart = now.Date;
        var soonUntil = todayStart.AddDays(DueSoonDays);
        var since = now.AddHours(-24);

        // Sorumlu kişi: atanan kişi, yoksa pano sahibi (bkz. Responsibility).
        var cards = await db.Cards
            .Where(Responsibility.IsOpen)
            .Where(c => c.DueDate != null && c.DueDate < soonUntil)
            .WithResponsible()
            .Where(x => x.ResponsibleId != null)
            .Select(x => new
            {
                AssigneeId = x.ResponsibleId!.Value,
                Item = new EmailCardItem(x.Card.Column.BoardId, x.Card.Column.Board.Name, x.Card.Column.Name,
                    x.Card.Title, x.Card.DueDate!.Value)
            })
            .ToListAsync(ct);

        // Kullanıcının üyesi olduğu panolarda, başkalarının son 24 saatteki hareket sayısı (tek GROUP BY sorgusu).
        var activityCounts = await (
                from member in db.BoardMembers
                join activity in db.ActivityLogs on member.BoardId equals activity.BoardId
                where activity.CreatedAt >= since && activity.UserId != member.UserId
                group activity by member.UserId into g
                select new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, ct);

        var cardsByUser = cards.ToLookup(c => c.AssigneeId, c => c.Item);
        var userIds = cardsByUser.Select(g => g.Key).Union(activityCounts.Keys).ToList();

        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.FullName })
            .ToListAsync(ct);

        foreach (var user in users)
        {
            var items = cardsByUser[user.Id].OrderBy(i => i.DueDate).ToList();
            var digest = new DailyDigestEmail(
                user.Email,
                user.FullName,
                Overdue: items.Where(i => i.DueDate < todayStart).ToList(),
                DueSoon: items.Where(i => i.DueDate >= todayStart).ToList(),
                ActivityCountLast24h: activityCounts.GetValueOrDefault(user.Id));

            await emailService.SendDailyDigestAsync(digest, ct);
        }
    }
}
