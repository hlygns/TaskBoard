using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

// Kart içindeki alt görev ("Taslak yaz", "Görselleri ekle"...). Kart yüzünde "2/5" olarak görünür.
public class ChecklistItem : BaseEntity
{
    public required string Text { get; set; }
    public bool IsDone { get; set; }
    public double Position { get; set; }

    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;
}
