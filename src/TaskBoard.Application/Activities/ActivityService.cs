using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Cards;
using TaskBoard.Application.Common;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.Application.Activities;

public interface IActivityService
{
    Task<IReadOnlyList<ActivityDto>> GetAsync(Guid boardId, DateTime? before, int limit, CancellationToken ct = default);
}

public class ActivityService(IAppDbContext db, ICurrentUser currentUser) : IActivityService
{
    public const int MaxLimit = 100;

    // Sayfalama "before" imleciyle (keyset pagination): "şu andan önceki 30 kayıt".
    // OFFSET'in aksine sayfa ne kadar derin olursa olsun index'ten (board_id, created_at) doğrudan okunur
    // ve arada yeni kayıt eklenince sayfalar kaymaz.
    public async Task<IReadOnlyList<ActivityDto>> GetAsync(
        Guid boardId, DateTime? before, int limit, CancellationToken ct = default)
    {
        await db.EnsureMemberAsync(boardId, currentUser.Id, ct);
        limit = Math.Clamp(limit, 1, MaxLimit);
        var cursor = before is { } b ? DateTime.SpecifyKind(b, DateTimeKind.Utc) : (DateTime?)null;

        var rows = await db.ActivityLogs
            .Where(a => a.BoardId == boardId && (cursor == null || a.CreatedAt < cursor))
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new { a.Id, a.Type, a.UserId, a.User.FullName, a.Metadata, a.CreatedAt })
            .ToListAsync(ct);

        // JSON çözümleme SQL'e çevrilemez; satırlar geldikten sonra bellekte yapılır.
        return rows
            .Select(r => new ActivityDto(
                r.Id,
                r.Type,
                new MemberRefDto(r.UserId, r.FullName),
                r.Metadata is null
                    ? []
                    : JsonSerializer.Deserialize<Dictionary<string, string?>>(r.Metadata, ActivityRecorder.JsonOptions) ?? [],
                r.CreatedAt))
            .ToList();
    }
}
