using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.Application.Common;

// Decorator: panoyu değiştiren her işlem zaten IBoardNotifier'a haber veriyor. Cache temizliğini
// buraya koyunca hiçbir servis "cache'i silmeyi unutamaz".
//
// Sıra önemli: ÖNCE cache silinir, SONRA canlı bildirim gider. Tersi olsaydı bildirimi alan
// istemci panoyu hemen tekrar çekip henüz silinmemiş eski cache'i okuyabilirdi.
public class CacheInvalidatingBoardNotifier(IBoardCache cache, IBoardNotifier inner) : IBoardNotifier
{
    public async Task NotifyAsync(Guid boardId, BoardEvent boardEvent, CancellationToken ct = default)
    {
        await cache.InvalidateAsync(boardId, ct);
        await inner.NotifyAsync(boardId, boardEvent, ct);
    }
}
