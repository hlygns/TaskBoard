namespace TaskBoard.Application.Cards;

public interface ICardService
{
    Task<CardSummaryDto> CreateAsync(Guid columnId, CreateCardRequest request, CancellationToken ct = default);
    Task<CardDetailDto> GetAsync(Guid cardId, CancellationToken ct = default);
    Task<CardDetailDto> UpdateAsync(Guid cardId, UpdateCardRequest request, CancellationToken ct = default);
    Task MoveAsync(Guid cardId, MoveCardRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid cardId, CancellationToken ct = default);

    Task SetCompletedAsync(Guid cardId, bool completed, CancellationToken ct = default);
    Task SetArchivedAsync(Guid cardId, bool archived, CancellationToken ct = default);
    Task SetLabelsAsync(Guid cardId, IReadOnlyList<Guid> labelIds, CancellationToken ct = default);

    Task<ChecklistItemDto> AddChecklistItemAsync(Guid cardId, AddChecklistItemRequest request, CancellationToken ct = default);
    Task<ChecklistItemDto> UpdateChecklistItemAsync(Guid itemId, UpdateChecklistItemRequest request, CancellationToken ct = default);
    Task DeleteChecklistItemAsync(Guid itemId, CancellationToken ct = default);

    Task<CommentDto> AddCommentAsync(Guid cardId, AddCommentRequest request, CancellationToken ct = default);
    Task DeleteCommentAsync(Guid commentId, CancellationToken ct = default);
}
