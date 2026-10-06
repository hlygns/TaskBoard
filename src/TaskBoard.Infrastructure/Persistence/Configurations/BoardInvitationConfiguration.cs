using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

public class BoardInvitationConfiguration : IEntityTypeConfiguration<BoardInvitation>
{
    public void Configure(EntityTypeBuilder<BoardInvitation> builder)
    {
        builder.Property(i => i.Email).HasMaxLength(256);
        builder.Property(i => i.TokenHash).HasMaxLength(128);
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(i => i.TokenHash).IsUnique();

        builder.HasOne(i => i.Board)
            .WithMany(b => b.Invitations)
            .HasForeignKey(i => i.BoardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.InvitedBy)
            .WithMany()
            .HasForeignKey(i => i.InvitedByUserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
