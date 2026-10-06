using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

public class ColumnConfiguration : IEntityTypeConfiguration<Column>
{
    public void Configure(EntityTypeBuilder<Column> builder)
    {
        // "column" SQL'de ayrılmış kelime; tabloyu "board_columns" diye adlandırıyoruz.
        builder.ToTable("board_columns");

        builder.Property(c => c.Name).HasMaxLength(100);

        // Pano açılırken sütunlar "WHERE board_id = ? ORDER BY position" ile çekilir.
        builder.HasIndex(c => new { c.BoardId, c.Position });

        builder.HasOne(c => c.Board)
            .WithMany(b => b.Columns)
            .HasForeignKey(c => c.BoardId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
