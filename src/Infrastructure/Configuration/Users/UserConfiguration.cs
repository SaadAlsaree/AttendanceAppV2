using Domain.Entities.Users;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", Schemas.Default);

        builder.Property(u => u.UserLogin)
            .HasMaxLength(50)
            .IsRequired()
            .IsUnicode(true);

        builder.Property(u => u.Username)
            .HasMaxLength(100)
            .IsRequired()
            .IsUnicode(true);



        builder.Property(u => u.PasswordHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Relationships
        builder.HasOne(u => u.OrganizationalUnit)
            .WithMany(o => o.Users)
            .HasForeignKey(u => u.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(u => u.Site)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.SiteId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(u => u.UserLogin).IsUnique();
        builder.HasIndex(u => u.SiteId);

    }
}
