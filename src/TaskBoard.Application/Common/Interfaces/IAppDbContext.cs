using Microsoft.EntityFrameworkCore;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Application.Common.Interfaces;

// Application katmanı veritabanını bu arayüz üzerinden kullanır; somut AppDbContext
// Infrastructure'da kalır. Böylece testlerde gerçek veritabanı yerine başka bir uygulama verilebilir.
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Board> Boards { get; }
    DbSet<BoardMember> BoardMembers { get; }
    DbSet<BoardInvitation> BoardInvitations { get; }
    DbSet<Column> Columns { get; }
    DbSet<Card> Cards { get; }
    DbSet<Comment> Comments { get; }
    DbSet<ChecklistItem> ChecklistItems { get; }
    DbSet<Label> Labels { get; }
    DbSet<CardLabel> CardLabels { get; }
    DbSet<ActivityLog> ActivityLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
