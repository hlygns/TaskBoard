namespace TaskBoard.Application.Cards;

public interface ICardService
{
    Task<CardSummaryDto> CreateAsync(Guid columnId, CreateCardRequest request, CancellationToken ct = default);
    Task<CardDetailDto> GetAsync(Guid cardId, CancellationToken ct = default);
    Task<CardDetailDto> UpdateAsync(Guid cardId, UpdateCardRequest request, CancellationToken ct = default);
    Task MoveAsync(Guid cardId, MoveCardRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid cardId, CancellationToken ct = default);

    Task<CommentDto> AddCommentAsync(Guid cardId, AddCommentRequest request, CancellationToken ct = default);
    Task DeleteCommentAsync(Guid commentId, CancellationToken ct = default);
}
