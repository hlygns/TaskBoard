using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities;

public class Board : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }

    public ICollection<BoardMember> Members { get; set; } = new List<BoardMember>();
    public ICollection<Column> Columns { get; set; } = new List<Column>();
    public ICollection<BoardInvitation> Invitations { get; set; } = new List<BoardInvitation>();
    public ICollection<ActivityLog> Activities { get; set; } = new List<ActivityLog>();
    public ICollection<Label> Labels { get; set; } = new List<Label>();
}
