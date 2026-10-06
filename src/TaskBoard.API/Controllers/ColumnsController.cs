using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Boards;
using TaskBoard.Application.Cards;
using TaskBoard.Application.Columns;

namespace TaskBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class ColumnsController(IColumnService columnService, ICardService cardService) : ControllerBase
{
    [HttpPost("boards/{boardId:guid}/columns")]
    public Task<ColumnDto> Create(Guid boardId, CreateColumnRequest request, CancellationToken ct) =>
        columnService.CreateAsync(boardId, request, ct);

    [HttpPut("columns/{columnId:guid}")]
    public async Task<IActionResult> Rename(Guid columnId, RenameColumnRequest request, CancellationToken ct)
    {
        await columnService.RenameAsync(columnId, request, ct);
        return NoContent();
    }

    [HttpPut("columns/{columnId:guid}/move")]
    public async Task<IActionResult> Move(Guid columnId, MoveColumnRequest request, CancellationToken ct)
    {
        await columnService.MoveAsync(columnId, request, ct);
        return NoContent();
    }

    [HttpDelete("columns/{columnId:guid}")]
    public async Task<IActionResult> Delete(Guid columnId, CancellationToken ct)
    {
        await columnService.DeleteAsync(columnId, ct);
        return NoContent();
    }

    [HttpPost("columns/{columnId:guid}/cards")]
    public Task<CardSummaryDto> CreateCard(Guid columnId, CreateCardRequest request, CancellationToken ct) =>
        cardService.CreateAsync(columnId, request, ct);
}
