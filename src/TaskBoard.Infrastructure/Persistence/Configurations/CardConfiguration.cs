using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.Property(c => c.Title).HasMaxLength(200);
        builder.Property(c => c.Description).HasMaxLength(5000);
        builder.Property(c => c.Priority).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(c => new { c.ColumnId, c.Position });

        // Hangfire hatırlatmaları son tarihi yaklaşan kartları bu index ile bulacak.
        builder.HasIndex(c => c.DueDate);

        builder.HasOne(c => c.Column)
            .WithMany(col => col.Cards)
            .HasForeignKey(c => c.ColumnId)
            .OnDelete(DeleteBehavior.Cascade);

        // Atanan kişi silinirse kart silinmez, sadece ataması kalkar.
        builder.HasOne(c => c.Assignee)
            .WithMany()
            .HasForeignKey(c => c.AssigneeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
