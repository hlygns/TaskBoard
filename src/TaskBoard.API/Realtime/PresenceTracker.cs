namespace TaskBoard.API.Realtime;

public record OnlineUser(Guid UserId, string FullName);

// Hangi panoda hangi bağlantıların (sekmelerin) açık olduğunu bellekte tutar.
// Bir kullanıcının iki sekmesi açıksa iki bağlantısı olur; listede yine tek kişi görünür.
//
// Sınır: Bellekte durduğu için tek sunucuda çalışır. API birden fazla kopya halinde çalışırsa
// (yatay ölçekleme) bu bilgi Redis'e taşınmalı; SignalR mesajları için de Redis backplane gerekir.
public class PresenceTracker
{
    private readonly Dictionary<Guid, Dictionary<string, OnlineUser>> _boards = new();
    private readonly Lock _lock = new();

    public IReadOnlyList<OnlineUser> Join(Guid boardId, string connectionId, OnlineUser user)
    {
        lock (_lock)
        {
            if (!_boards.TryGetValue(boardId, out var connections))
                _boards[boardId] = connections = new Dictionary<string, OnlineUser>();

            connections[connectionId] = user;
            return Snapshot(connections);
        }
    }

    public IReadOnlyList<OnlineUser>? Leave(Guid boardId, string connectionId)
    {
        lock (_lock)
        {
            if (!_boards.TryGetValue(boardId, out var connections) || !connections.Remove(connectionId))
                return null;

            if (connections.Count == 0)
                _boards.Remove(boardId);

            return Snapshot(connections);
        }
    }

    // Bağlantı koptuğunda (sekme kapandı, internet gitti) bulunduğu tüm panolardan çıkar.
    public List<(Guid BoardId, IReadOnlyList<OnlineUser> Users)> LeaveAll(string connectionId)
    {
        lock (_lock)
        {
            var changed = new List<(Guid, IReadOnlyList<OnlineUser>)>();
            foreach (var (boardId, connections) in _boards.ToList())
            {
                if (!connections.Remove(connectionId)) continue;
                if (connections.Count == 0) _boards.Remove(boardId);
                changed.Add((boardId, Snapshot(connections)));
            }
            return changed;
        }
    }

    private static IReadOnlyList<OnlineUser> Snapshot(Dictionary<string, OnlineUser> connections) =>
        connections.Values.DistinctBy(u => u.UserId).OrderBy(u => u.FullName).ToList();
}
