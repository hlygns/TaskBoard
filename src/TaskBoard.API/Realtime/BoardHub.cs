using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;
using TaskBoard.Application.Common;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.API.Realtime;

// İstemci pano sayfasını açınca JoinBoard ile o panonun "grubuna" katılır;
// panodaki değişiklikler sadece o gruba gönderilir.
[Authorize]
public class BoardHub(IAppDbContext db, IPresenceTracker presence) : Hub
{
    public static string GroupName(Guid boardId) => $"board:{boardId}";

    public async Task JoinBoard(Guid boardId)
    {
        var user = CurrentUser();

        // Sadece panonun üyeleri gruba katılabilir; değilse NotFoundException → istemciye hata.
        await db.EnsureMemberAsync(boardId, user.UserId, Context.ConnectionAborted);

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(boardId));
        var online = await presence.JoinAsync(boardId, Context.ConnectionId, user);
        await Clients.Group(GroupName(boardId)).SendAsync("PresenceChanged", boardId, online);
    }

    public async Task LeaveBoard(Guid boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(boardId));
        if (await presence.LeaveAsync(boardId, Context.ConnectionId) is { } online)
            await Clients.Group(GroupName(boardId)).SendAsync("PresenceChanged", boardId, online);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var (boardId, online) in await presence.LeaveAllAsync(Context.ConnectionId))
            await Clients.Group(GroupName(boardId)).SendAsync("PresenceChanged", boardId, online);

        await base.OnDisconnectedAsync(exception);
    }

    private OnlineUser CurrentUser()
    {
        var principal = Context.User!;
        return new OnlineUser(
            Guid.Parse(principal.FindFirst(JwtRegisteredClaimNames.Sub)!.Value),
            principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? "");
    }
}
