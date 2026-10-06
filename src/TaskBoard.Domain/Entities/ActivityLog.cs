using TaskBoard.Domain.Common;
using TaskBoard.Domain.Enums;

namespace TaskBoard.Domain.Entities;

// Geçmiş kaydı silinen kartlardan bağımsız yaşamalı; bu yüzden kart/sütun için
// foreign key yerine sadece Id ve o anki ad (snapshot) tutuyoruz.
public class ActivityLog : BaseEntity
{
    public ActivityType Type { get; set; }

    public Guid BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? EntityId { get; set; }

    // Ek bilgiler JSON olarak: {"cardTitle":"Login sayfası","fromColumn":"Yapılacak","toColumn":"Bitti"}
    // PostgreSQL'de jsonb kolonu olarak saklanacak.
    public string? Metadata { get; set; }
}
