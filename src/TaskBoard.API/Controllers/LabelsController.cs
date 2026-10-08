using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Labels;

namespace TaskBoard.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class LabelsController(ILabelService labelService) : ControllerBase
{
    [HttpPost("boards/{boardId:guid}/labels")]
    public Task<LabelDto> Create(Guid boardId, SaveLabelRequest request, CancellationToken ct) =>
        labelService.CreateAsync(boardId, request, ct);

    [HttpPut("labels/{labelId:guid}")]
    public Task<LabelDto> Update(Guid labelId, SaveLabelRequest request, CancellationToken ct) =>
        labelService.UpdateAsync(labelId, request, ct);

    [HttpDelete("labels/{labelId:guid}")]
    public async Task<IActionResult> Delete(Guid labelId, CancellationToken ct)
    {
        await labelService.DeleteAsync(labelId, ct);
        return NoContent();
    }
}
