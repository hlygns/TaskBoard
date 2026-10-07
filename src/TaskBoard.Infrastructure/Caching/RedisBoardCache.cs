using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using TaskBoard.Application.Boards;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.Infrastructure.Caching;

public class RedisBoardCache(IDistributedCache cache, ILogger<RedisBoardCache> logger) : IBoardCache
{
    // Değişikliklerde zaten siliniyor; bu süre sadece bir güvenlik ağı (ör. bir silme kaçarsa
    // ya da yarış durumunda eski veri yazılırsa en fazla bu kadar yaşar).
    private static readonly DistributedCacheEntryOptions Options = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    private static string Key(Guid boardId) => $"board:{boardId}:detail";

    // Cache bir hızlandırıcı; Redis'e ulaşılamazsa uygulama çökmemeli, veritabanından devam etmeli.
    public async Task<BoardDetailDto?> GetAsync(Guid boardId, CancellationToken ct = default)
    {
        try
        {
            var bytes = await cache.GetAsync(Key(boardId), ct);
            return bytes is null ? null : JsonSerializer.Deserialize<BoardDetailDto>(bytes);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis okunamadı, veritabanına gidiliyor");
            return null;
        }
    }

    public async Task SetAsync(Guid boardId, BoardDetailDto board, CancellationToken ct = default)
    {
        try
        {
            await cache.SetAsync(Key(boardId), JsonSerializer.SerializeToUtf8Bytes(board), Options, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis'e yazılamadı");
        }
    }

    public async Task InvalidateAsync(Guid boardId, CancellationToken ct = default)
    {
        try
        {
            await cache.RemoveAsync(Key(boardId), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis'ten silinemedi: {BoardId}", boardId);
        }
    }
}
