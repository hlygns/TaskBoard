using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.Metadata).HasColumnType("jsonb");

        // Aktivite akışı "bu panonun son 50 olayı" diye sorgulanır.
        builder.HasIndex(a => new { a.BoardId, a.CreatedAt });

        builder.HasOne(a => a.Board)
            .WithMany(b => b.Activities)
            .HasForeignKey(a => a.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
