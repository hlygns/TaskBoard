using TaskBoard.Application.Boards;

namespace TaskBoard.Application.Common.Interfaces;

// Pano detayının (sütunlar + kart özetleri) önbelleği. Pano ekranı en sık çağrılan istek;
// her açılışta 3 ayrı SQL sorgusu yerine Redis'ten tek okuma.
public interface IBoardCache
{
    Task<BoardDetailDto?> GetAsync(Guid boardId, CancellationToken ct = default);
    Task SetAsync(Guid boardId, BoardDetailDto board, CancellationToken ct = default);
    Task InvalidateAsync(Guid boardId, CancellationToken ct = default);
}

// Redis ayarlanmamışsa: önbellek yok, her istek veritabanına gider.
public class NoBoardCache : IBoardCache
{
    public Task<BoardDetailDto?> GetAsync(Guid boardId, CancellationToken ct = default) => Task.FromResult<BoardDetailDto?>(null);
    public Task SetAsync(Guid boardId, BoardDetailDto board, CancellationToken ct = default) => Task.CompletedTask;
    public Task InvalidateAsync(Guid boardId, CancellationToken ct = default) => Task.CompletedTask;
}
