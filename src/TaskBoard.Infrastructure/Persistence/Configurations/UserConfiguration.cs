using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskBoard.Domain.Entities;

namespace TaskBoard.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.FullName).HasMaxLength(100);

        // Aynı mail ile iki hesap açılamaz; ayrıca girişte mail ile arama hızlanır.
        builder.HasIndex(u => u.Email).IsUnique();
    }
}
