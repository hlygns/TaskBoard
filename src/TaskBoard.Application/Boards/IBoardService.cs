namespace TaskBoard.Application.Boards;

public interface IBoardService
{
    Task<IReadOnlyList<BoardSummaryDto>> GetMyBoardsAsync(CancellationToken ct = default);
    Task<BoardDetailDto> GetAsync(Guid boardId, CancellationToken ct = default);
    Task<IReadOnlyList<Cards.ArchivedCardDto>> GetArchivedCardsAsync(Guid boardId, CancellationToken ct = default);
    Task<BoardDetailDto> CreateAsync(CreateBoardRequest request, CancellationToken ct = default);
    Task UpdateAsync(Guid boardId, UpdateBoardRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid boardId, CancellationToken ct = default);

    // Kendini çıkarmak = panodan ayrılmak. Başkasını çıkarmak sadece sahibin yetkisinde.
    Task RemoveMemberAsync(Guid boardId, Guid userId, CancellationToken ct = default);
}
