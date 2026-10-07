using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.Application.Notifications;

// Haftada bir: 180 günden eski aktivite kayıtlarını siler; tablo sonsuza kadar büyümesin.
public class ActivityCleanupJob(IAppDbContext db, TimeProvider time)
{
    public static readonly TimeSpan Retention = TimeSpan.FromDays(180);

    public Task<int> RunAsync(CancellationToken ct)
    {
        var cutoff = time.GetUtcNow().UtcDateTime - Retention;
        return db.ActivityLogs.Where(a => a.CreatedAt < cutoff).ExecuteDeleteAsync(ct);
    }
}
