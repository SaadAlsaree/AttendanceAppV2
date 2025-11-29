using Domain.Entities.Organizations;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Organizations;

internal sealed class OrganizationalUnitConfiguration : IEntityTypeConfiguration<OrganizationalUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationalUnit> builder)
    {
        builder.ToTable("OrganizationalUnits", Schemas.Default);

        builder.Property(u => u.UnitName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.UnitCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(u => u.UnitDescription)
            .HasMaxLength(500);

        builder.Property(u => u.Email)
            .HasMaxLength(256);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(u => u.Address)
            .HasMaxLength(500);

        builder.Property(u => u.PostalCode)
            .HasMaxLength(20);

        builder.Property(u => u.UnitLogo)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(u => u.ParentUnit)
            .WithMany(u => u.ChildUnits)
            .HasForeignKey(u => u.ParentUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Manager)
            .WithMany(e => e.ManagedUnits)
            .HasForeignKey(u => u.ManagerId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(u => u.UnitCode).IsUnique();
        builder.HasIndex(u => u.UnitName);
        builder.HasIndex(u => u.ParentUnitId);
    }
}
