using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Boards;
using TaskBoard.Application.Invitations;

namespace TaskBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/boards")]
public class BoardsController(IBoardService boardService, IInvitationService invitationService) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<BoardSummaryDto>> GetMyBoards(CancellationToken ct) =>
        boardService.GetMyBoardsAsync(ct);

    [HttpGet("{boardId:guid}")]
    public Task<BoardDetailDto> Get(Guid boardId, CancellationToken ct) =>
        boardService.GetAsync(boardId, ct);

    [HttpPost]
    public async Task<ActionResult<BoardDetailDto>> Create(CreateBoardRequest request, CancellationToken ct)
    {
        var board = await boardService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { boardId = board.Id }, board);
    }

    [HttpPut("{boardId:guid}")]
    public async Task<IActionResult> Update(Guid boardId, UpdateBoardRequest request, CancellationToken ct)
    {
        await boardService.UpdateAsync(boardId, request, ct);
        return NoContent();
    }

    [HttpDelete("{boardId:guid}")]
    public async Task<IActionResult> Delete(Guid boardId, CancellationToken ct)
    {
        await boardService.DeleteAsync(boardId, ct);
        return NoContent();
    }

    [HttpDelete("{boardId:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid boardId, Guid userId, CancellationToken ct)
    {
        await boardService.RemoveMemberAsync(boardId, userId, ct);
        return NoContent();
    }

    [HttpGet("{boardId:guid}/invitations")]
    public Task<IReadOnlyList<InvitationDto>> GetInvitations(Guid boardId, CancellationToken ct) =>
        invitationService.GetPendingAsync(boardId, ct);

    [HttpPost("{boardId:guid}/invitations")]
    public Task<InvitationDto> Invite(Guid boardId, InviteMemberRequest request, CancellationToken ct) =>
        invitationService.InviteAsync(boardId, request, ct);

    [HttpDelete("{boardId:guid}/invitations/{invitationId:guid}")]
    public async Task<IActionResult> CancelInvitation(Guid boardId, Guid invitationId, CancellationToken ct)
    {
        await invitationService.CancelAsync(boardId, invitationId, ct);
        return NoContent();
    }
}
