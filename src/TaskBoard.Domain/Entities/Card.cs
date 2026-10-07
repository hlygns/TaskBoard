using TaskBoard.Domain.Common;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Domain.Entities;

public class Card : BaseEntity
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public CardPriority Priority { get; set; } = CardPriority.Medium;

    // Son tarih hatırlatma maili gönderildiyse zamanı. Hatırlatma işi saatte bir çalışır;
    // bu alan sayesinde aynı karta iki kez mail gitmez. Son tarih değişince sıfırlanır.
    public DateTime? DueReminderSentAt { get; set; }

    // Kesirli sıra numarası: kart iki kartın arasına bırakılınca (önceki + sonraki) / 2 olur.
    // Böylece taşıma işlemi diğer kartları güncellemeden tek satırı değiştirir.
    public double Position { get; set; }

    public Guid ColumnId { get; set; }
    public Column Column { get; set; } = null!;

    public Guid? AssigneeId { get; set; }
    public User? Assignee { get; set; }

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
