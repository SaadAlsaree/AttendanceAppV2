using Domain.Entities.Devices;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Devices;

internal sealed class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices", Schemas.Default);

        builder.Property(d => d.Username)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.Location)
            .HasMaxLength(200);

        builder.Property(d => d.IpAddress)
            .HasMaxLength(45)  // IPv6 max length
            .IsRequired();

        builder.Property(d => d.DeviceId)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true);

        builder.Property(d => d.LastConnected)
            .HasColumnType("timestamp with time zone");

        // Relationships
        builder.HasOne(d => d.Organization)
            .WithMany()
            .HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);


        // Indexes
        builder.HasIndex(d => d.DeviceId).IsUnique();
        builder.HasIndex(d => d.IpAddress);

        builder.HasIndex(d => d.OrganizationId);
    }
}
