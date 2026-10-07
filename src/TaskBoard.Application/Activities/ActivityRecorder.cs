using System.Text.Json;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Activities;

public static class ActivityRecorder
{
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Aktivite kaydı sadece change tracker'a eklenir; asıl değişiklikle AYNI SaveChanges'ta,
    // yani aynı transaction'da yazılır. Böylece "kart taşındı ama kaydı yok" (ya da tersi) olamaz.
    //
    // data: o anki adlar (kart başlığı, sütun adı...). Kart sonradan silinse ya da adı değişse bile
    // geçmiş kaydı doğru kalsın diye ilişki yerine anlık kopya (snapshot) tutuyoruz → jsonb kolon.
    public static void LogActivity(
        this IAppDbContext db, Guid boardId, Guid userId, ActivityType type, Guid? entityId = null, object? data = null)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            BoardId = boardId,
            UserId = userId,
            Type = type,
            EntityId = entityId,
            Metadata = data is null ? null : JsonSerializer.Serialize(data, JsonOptions)
        });
    }
}
