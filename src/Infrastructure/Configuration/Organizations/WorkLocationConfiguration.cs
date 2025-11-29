using Domain.Entities.Organizations;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Organizations;

internal sealed class WorkLocationConfiguration : IEntityTypeConfiguration<WorkLocation>
{
    public void Configure(EntityTypeBuilder<WorkLocation> builder)
    {
        builder.ToTable("WorkLocations", Schemas.Default);

        builder.Property(w => w.Name)
            .HasMaxLength(100)
            .IsRequired();


        builder.Property(w => w.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(w => w.Description)
            .HasMaxLength(500);

        builder.Property(w => w.WifiSSID)
            .HasMaxLength(100);

        builder.Property(w => w.BeaconId)
            .HasMaxLength(100);

        // Relationships
        builder.HasOne(w => w.Organization)
            .WithMany()
            .HasForeignKey(w => w.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(w => new { w.OrganizationId, w.Name }).IsUnique();
        builder.HasIndex(w => new { w.Latitude, w.Longitude });
        builder.HasIndex(w => w.IsActive);
    }
}
