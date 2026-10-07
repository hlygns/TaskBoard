using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.JsonWebTokens;
using TaskBoard.Application.Common.Interfaces;

namespace TaskBoard.API.Realtime;

// IBoardNotifier'ın SignalR uygulaması: olayı panonun grubuna yayınlar.
public class SignalRBoardNotifier(
    IHubContext<BoardHub> hub,
    IHttpContextAccessor httpContextAccessor,
    ILogger<SignalRBoardNotifier> logger) : IBoardNotifier
{
    // İstemci, değişikliği yapan isteğe kendi SignalR bağlantı kimliğini bu başlıkla ekler.
    // Böylece olay ona geri gönderilmez: ekranını zaten kendisi güncelledi.
    public const string ConnectionIdHeader = "X-Connection-Id";

    public async Task NotifyAsync(Guid boardId, BoardEvent boardEvent, CancellationToken ct = default)
    {
        var http = httpContextAccessor.HttpContext;
        var actor = new
        {
            userId = http?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
            fullName = http?.User.FindFirst(JwtRegisteredClaimNames.Name)?.Value
        };
        var senderConnectionId = http?.Request.Headers[ConnectionIdHeader].ToString();

        var clients = string.IsNullOrEmpty(senderConnectionId)
            ? hub.Clients.Group(BoardHub.GroupName(boardId))
            : hub.Clients.GroupExcept(BoardHub.GroupName(boardId), senderConnectionId);

        try
        {
            await clients.SendAsync("BoardEvent", boardId, boardEvent, actor, ct);
        }
        catch (Exception ex)
        {
            // Değişiklik veritabanına zaten kaydedildi; canlı bildirim gitmedi diye isteği başarısız saymıyoruz.
            logger.LogWarning(ex, "Pano olayı gönderilemedi: {BoardId} {Type}", boardId, boardEvent.Type);
        }
    }
}
