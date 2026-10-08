using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

public class ChecklistItemConfiguration : IEntityTypeConfiguration<ChecklistItem>
{
    public void Configure(EntityTypeBuilder<ChecklistItem> builder)
    {
        builder.Property(i => i.Text).HasMaxLength(300);
        builder.HasIndex(i => new { i.CardId, i.Position });

        builder.HasOne(i => i.Card)
            .WithMany(c => c.ChecklistItems)
            .HasForeignKey(i => i.CardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
