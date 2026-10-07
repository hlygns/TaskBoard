using System.Text.Json;
using StackExchange.Redis;

namespace TaskBoard.API.Realtime;

public record OnlineUser(Guid UserId, string FullName);

// Hangi panoda hangi bağlantıların (sekmelerin) açık olduğunu tutar.
// Bir kullanıcının iki sekmesi açıksa iki bağlantısı olur; listede yine tek kişi görünür.
public interface IPresenceTracker
{
    Task<IReadOnlyList<OnlineUser>> JoinAsync(Guid boardId, string connectionId, OnlineUser user);

    // Bağlantı o panoda değilse null.
    Task<IReadOnlyList<OnlineUser>?> LeaveAsync(Guid boardId, string connectionId);

    // Bağlantı koptuğunda (sekme kapandı, internet gitti) bulunduğu tüm panolardan çıkar.
    Task<List<(Guid BoardId, IReadOnlyList<OnlineUser> Users)>> LeaveAllAsync(string connectionId);
}

internal static class PresenceList
{
    public static IReadOnlyList<OnlineUser> Normalize(IEnumerable<OnlineUser> users) =>
        users.DistinctBy(u => u.UserId).OrderBy(u => u.FullName).ToList();
}

// Tek sunucu için: bilgi API'nin belleğinde. Redis ayarlı değilse kullanılır.
public class InMemoryPresenceTracker : IPresenceTracker
{
    private readonly Dictionary<Guid, Dictionary<string, OnlineUser>> _boards = new();
    private readonly Lock _lock = new();

    public Task<IReadOnlyList<OnlineUser>> JoinAsync(Guid boardId, string connectionId, OnlineUser user)
    {
        lock (_lock)
        {
            if (!_boards.TryGetValue(boardId, out var connections))
                _boards[boardId] = connections = new Dictionary<string, OnlineUser>();

            connections[connectionId] = user;
            return Task.FromResult(PresenceList.Normalize(connections.Values));
        }
    }

    public Task<IReadOnlyList<OnlineUser>?> LeaveAsync(Guid boardId, string connectionId)
    {
        lock (_lock)
        {
            if (!_boards.TryGetValue(boardId, out var connections) || !connections.Remove(connectionId))
                return Task.FromResult<IReadOnlyList<OnlineUser>?>(null);

            if (connections.Count == 0)
                _boards.Remove(boardId);

            return Task.FromResult<IReadOnlyList<OnlineUser>?>(PresenceList.Normalize(connections.Values));
        }
    }

    public Task<List<(Guid BoardId, IReadOnlyList<OnlineUser> Users)>> LeaveAllAsync(string connectionId)
    {
        lock (_lock)
        {
            var changed = new List<(Guid, IReadOnlyList<OnlineUser>)>();
            foreach (var (boardId, connections) in _boards.ToList())
            {
                if (!connections.Remove(connectionId)) continue;
                if (connections.Count == 0) _boards.Remove(boardId);
                changed.Add((boardId, PresenceList.Normalize(connections.Values)));
            }
            return Task.FromResult(changed);
        }
    }
}

// Birden fazla API sunucusu için: bilgi Redis'te, tüm sunucular aynı listeyi görür.
//   presence:board:{boardId}  → hash  { bağlantıId: kullanıcı(JSON) }
//   presence:conn:{bağlantıId} → set   { boardId, ... }  (bağlantı koptuğunda hangi panolardan çıkacağını bilmek için)
//
// Sunucu aniden kapanırsa (OnDisconnected çalışmaz) kayıtlar kalabilir; bu yüzden anahtarlara
// süre (TTL) konur, en geç bu süre sonunda kendiliğinden silinir.
public class RedisPresenceTracker(IConnectionMultiplexer redis) : IPresenceTracker
{
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(12);
    private IDatabase Db => redis.GetDatabase();

    private static RedisKey BoardKey(Guid boardId) => $"presence:board:{boardId}";
    private static RedisKey ConnectionKey(string connectionId) => $"presence:conn:{connectionId}";

    public async Task<IReadOnlyList<OnlineUser>> JoinAsync(Guid boardId, string connectionId, OnlineUser user)
    {
        var batch = Db.CreateBatch();
        var tasks = new Task[]
        {
            batch.HashSetAsync(BoardKey(boardId), connectionId, JsonSerializer.Serialize(user)),
            batch.KeyExpireAsync(BoardKey(boardId), Ttl),
            batch.SetAddAsync(ConnectionKey(connectionId), boardId.ToString()),
            batch.KeyExpireAsync(ConnectionKey(connectionId), Ttl)
        };
        batch.Execute();
        await Task.WhenAll(tasks);

        return await ReadAsync(boardId);
    }

    public async Task<IReadOnlyList<OnlineUser>?> LeaveAsync(Guid boardId, string connectionId)
    {
        await Db.SetRemoveAsync(ConnectionKey(connectionId), boardId.ToString());
        if (!await Db.HashDeleteAsync(BoardKey(boardId), connectionId))
            return null;

        return await ReadAsync(boardId);
    }

    public async Task<List<(Guid BoardId, IReadOnlyList<OnlineUser> Users)>> LeaveAllAsync(string connectionId)
    {
        var boards = await Db.SetMembersAsync(ConnectionKey(connectionId));
        await Db.KeyDeleteAsync(ConnectionKey(connectionId));

        var changed = new List<(Guid, IReadOnlyList<OnlineUser>)>();
        foreach (var value in boards)
        {
            var boardId = Guid.Parse(value.ToString());
            if (await Db.HashDeleteAsync(BoardKey(boardId), connectionId))
                changed.Add((boardId, await ReadAsync(boardId)));
        }
        return changed;
    }

    private async Task<IReadOnlyList<OnlineUser>> ReadAsync(Guid boardId)
    {
        var values = await Db.HashValuesAsync(BoardKey(boardId));
        return PresenceList.Normalize(values.Select(v => JsonSerializer.Deserialize<OnlineUser>(v.ToString())!));
    }
}
