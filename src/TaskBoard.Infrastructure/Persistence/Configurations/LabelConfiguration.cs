using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

public class LabelConfiguration : IEntityTypeConfiguration<Label>
{
    public void Configure(EntityTypeBuilder<Label> builder)
    {
        builder.Property(l => l.Name).HasMaxLength(40);
        builder.Property(l => l.Color).HasMaxLength(20);

        // Aynı panoda aynı adla iki etiket olmasın.
        builder.HasIndex(l => new { l.BoardId, l.Name }).IsUnique();

        builder.HasOne(l => l.Board)
            .WithMany(b => b.Labels)
            .HasForeignKey(l => l.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
