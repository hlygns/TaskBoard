using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Activities;

namespace TaskBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/boards/{boardId:guid}/activities")]
public class ActivitiesController(IActivityService activityService) : ControllerBase
{
    // İlk sayfa: GET /api/boards/{id}/activities
    // Sonraki sayfa: ?before=<son kaydın createdAt değeri>
    [HttpGet]
    public Task<IReadOnlyList<ActivityDto>> Get(Guid boardId, DateTime? before, CancellationToken ct, int limit = 30) =>
        activityService.GetAsync(boardId, before, limit, ct);
}
