using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Tasks;

namespace TaskBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api/me/tasks")]
public class TasksController(IMyTasksService myTasks) : ControllerBase
{
    // Tüm panolardaki açık işlerim (Görevlerim ekranı).
    [HttpGet]
    public Task<IReadOnlyList<MyTaskDto>> Get(CancellationToken ct) => myTasks.GetAsync(ct);
}
