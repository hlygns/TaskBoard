using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskBoard.Application.Invitations;

namespace TaskBoard.API.Controllers;

// Davet linkindeki token ile çalışan uç noktalar.
[ApiController]
[Authorize]
[Route("api/invitations")]
public class InvitationsController(IInvitationService invitationService) : ControllerBase
{
    // Linke tıklayan kişi henüz giriş yapmamış (hatta hesabı olmayabilir); daveti görebilmeli.
    [AllowAnonymous]
    [HttpGet("{token}")]
    public Task<InvitationPreviewDto> Get(string token, CancellationToken ct) =>
        invitationService.GetByTokenAsync(token, ct);

    [HttpPost("{token}/accept")]
    public Task<AcceptInvitationResult> Accept(string token, CancellationToken ct) =>
        invitationService.AcceptAsync(token, ct);

    [HttpPost("{token}/decline")]
    public async Task<IActionResult> Decline(string token, CancellationToken ct)
    {
        await invitationService.DeclineAsync(token, ct);
        return NoContent();
    }
}
