using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

public class BoardMemberConfiguration : IEntityTypeConfiguration<BoardMember>
{
    public void Configure(EntityTypeBuilder<BoardMember> builder)
    {
        // Bileşik birincil anahtar: aynı kullanıcı aynı panoya iki kez eklenemez.
        builder.HasKey(m => new { m.BoardId, m.UserId });

        builder.Property(m => m.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(m => m.Board)
            .WithMany(b => b.Members)
            .HasForeignKey(m => m.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
            .WithMany(u => u.BoardMemberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
