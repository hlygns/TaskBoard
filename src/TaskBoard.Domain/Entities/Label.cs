using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

// Panoya ait renkli etiket (Bug, Frontend, Okul...). Bir karta birden fazla etiket takılabilir.
public class Label : BaseEntity
{
    public required string Name { get; set; }

    // Arayüzdeki sabit paletten bir renk adı: "red", "green", "sky"...
    public required string Color { get; set; }

    public Guid BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public ICollection<CardLabel> Cards { get; set; } = new List<CardLabel>();
}
