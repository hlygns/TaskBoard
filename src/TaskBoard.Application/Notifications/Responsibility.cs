using System.Linq.Expressions;
using TaskBoard.Domain.Entities;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Application.Notifications;

// "Bu kart kimin işi?" kuralı tek yerde: hatırlatma, günlük özet ve "Görevlerim" hep bunu kullanır.
//   - Karta biri atandıysa: o kişi.
//   - Atanmadıysa: pano sahibi. (Tek başına kullanımda kimse kendini karta atamaz; yine de hatırlatma gelsin.)
public static class Responsibility
{
    // Tamamlanmamış ve arşivlenmemiş kart: hatırlatılacak, listelenecek iş.
    public static readonly Expression<Func<Card, bool>> IsOpen = c => c.CompletedAt == null && c.ArchivedAt == null;

    // Sorguya "sorumlu kişi" kolonunu ekler. EF bunu SQL'e çevirir:
    //   COALESCE(assignee_id, (SELECT user_id FROM board_members WHERE role = 'Owner' ... LIMIT 1))
    // Sonraki Where/Select'ler x.Card ve x.ResponsibleId üzerinden devam edebilir.
    public static IQueryable<ResponsibleCard> WithResponsible(this IQueryable<Card> cards) =>
        cards.Select(c => new ResponsibleCard
        {
            Card = c,
            ResponsibleId = c.AssigneeId ?? c.Column.Board.Members
                .Where(m => m.Role == BoardRole.Owner)
                .Select(m => (Guid?)m.UserId)
                .FirstOrDefault()
        });
}

public sealed class ResponsibleCard
{
    public required Card Card { get; init; }
    public Guid? ResponsibleId { get; init; }
}
