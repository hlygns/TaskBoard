using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class Comment : BaseEntity
{
    public required string Content { get; set; }

    public Guid CardId { get; set; }
    public Card Card { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
}
