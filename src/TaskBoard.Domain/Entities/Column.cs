using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class Column : BaseEntity
{
    public required string Name { get; set; }

    // Kesirli sıra numarası: iki sütunun arasına taşırken ortalamalarını alırız.
    public double Position { get; set; }

    public Guid BoardId { get; set; }
    public Board Board { get; set; } = null!;

    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
