using Microsoft.EntityFrameworkCore;
using TaskBoard.Application.Common.Interfaces;
using TaskBoard.Application.Labels;
using TaskBoard.Application.Notifications;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Tasks;

public record MyTaskDto(
    Guid CardId,
    string Title,
    Guid BoardId,
    string BoardName,
    string ColumnName,
    DateTime? DueDate,
    CardPriority Priority,
    int ChecklistDone,
    int ChecklistTotal,
    IReadOnlyList<LabelDto> Labels,
    bool AssignedToMe);

public interface IMyTasksService
{
    Task<IReadOnlyList<MyTaskDto>> GetAsync(CancellationToken ct = default);
}

// "Görevlerim": tüm panolardaki açık işlerim tek listede.
//   - Son tarihi olan ve sorumlusu ben olan kartlar (atanmış ya da sahibi olduğum panoda atanmamış),
//   - Son tarihi olmasa da bana açıkça atanmış kartlar.
// Gruplama (Gecikmiş / Bugün / Bu hafta...) kullanıcının saat dilimine göre arayüzde yapılır.
public class MyTasksService(IAppDbContext db, ICurrentUser currentUser) : IMyTasksService
{
    private const int Limit = 300;

    public async Task<IReadOnlyList<MyTaskDto>> GetAsync(CancellationToken ct = default)
    {
        var me = currentUser.Id;

        return await db.Cards
            .Where(Responsibility.IsOpen)
            .WithResponsible()
            .Where(x => x.ResponsibleId == me && (x.Card.DueDate != null || x.Card.AssigneeId == me))
            // Panodan çıkarılmış olabilirim; sadece hâlâ üyesi olduğum panolar.
            .Where(x => x.Card.Column.Board.Members.Any(m => m.UserId == me))
            .OrderBy(x => x.Card.DueDate == null).ThenBy(x => x.Card.DueDate).ThenByDescending(x => x.Card.Priority)
            .Take(Limit)
            .Select(x => new MyTaskDto(
                x.Card.Id,
                x.Card.Title,
                x.Card.Column.BoardId,
                x.Card.Column.Board.Name,
                x.Card.Column.Name,
                x.Card.DueDate,
                x.Card.Priority,
                x.Card.ChecklistItems.Count(i => i.IsDone),
                x.Card.ChecklistItems.Count,
                x.Card.Labels.Select(l => new LabelDto(l.Label.Id, l.Label.Name, l.Label.Color)).ToList(),
                x.Card.AssigneeId == me))
            .ToListAsync(ct);
    }
}
