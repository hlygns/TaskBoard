using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Cards;

namespace TaskBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class CardsController(ICardService cardService) : ControllerBase
{
    [HttpGet("cards/{cardId:guid}")]
    public Task<CardDetailDto> Get(Guid cardId, CancellationToken ct) =>
        cardService.GetAsync(cardId, ct);

    [HttpPut("cards/{cardId:guid}")]
    public Task<CardDetailDto> Update(Guid cardId, UpdateCardRequest request, CancellationToken ct) =>
        cardService.UpdateAsync(cardId, request, ct);

    [HttpPut("cards/{cardId:guid}/move")]
    public async Task<IActionResult> Move(Guid cardId, MoveCardRequest request, CancellationToken ct)
    {
        await cardService.MoveAsync(cardId, request, ct);
        return NoContent();
    }

    [HttpDelete("cards/{cardId:guid}")]
    public async Task<IActionResult> Delete(Guid cardId, CancellationToken ct)
    {
        await cardService.DeleteAsync(cardId, ct);
        return NoContent();
    }

    [HttpPut("cards/{cardId:guid}/complete")]
    public async Task<IActionResult> SetCompleted(Guid cardId, SetCardCompletedRequest request, CancellationToken ct)
    {
        await cardService.SetCompletedAsync(cardId, request.Completed, ct);
        return NoContent();
    }

    [HttpPut("cards/{cardId:guid}/archive")]
    public async Task<IActionResult> SetArchived(Guid cardId, SetCardArchivedRequest request, CancellationToken ct)
    {
        await cardService.SetArchivedAsync(cardId, request.Archived, ct);
        return NoContent();
    }

    [HttpPut("cards/{cardId:guid}/labels")]
    public async Task<IActionResult> SetLabels(Guid cardId, SetCardLabelsRequest request, CancellationToken ct)
    {
        await cardService.SetLabelsAsync(cardId, request.LabelIds, ct);
        return NoContent();
    }

    [HttpPost("cards/{cardId:guid}/checklist")]
    public Task<ChecklistItemDto> AddChecklistItem(Guid cardId, AddChecklistItemRequest request, CancellationToken ct) =>
        cardService.AddChecklistItemAsync(cardId, request, ct);

    [HttpPatch("checklist/{itemId:guid}")]
    public Task<ChecklistItemDto> UpdateChecklistItem(Guid itemId, UpdateChecklistItemRequest request, CancellationToken ct) =>
        cardService.UpdateChecklistItemAsync(itemId, request, ct);

    [HttpDelete("checklist/{itemId:guid}")]
    public async Task<IActionResult> DeleteChecklistItem(Guid itemId, CancellationToken ct)
    {
        await cardService.DeleteChecklistItemAsync(itemId, ct);
        return NoContent();
    }

    [HttpPost("cards/{cardId:guid}/comments")]
    public Task<CommentDto> AddComment(Guid cardId, AddCommentRequest request, CancellationToken ct) =>
        cardService.AddCommentAsync(cardId, request, ct);

    [HttpDelete("comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid commentId, CancellationToken ct)
    {
        await cardService.DeleteCommentAsync(commentId, ct);
        return NoContent();
    }
}
