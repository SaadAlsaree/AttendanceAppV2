using Domain.Entities.Organizations;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Organizations;

internal sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("Sites", Schemas.Default);

        builder.Property(s => s.SiteName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.SiteCode)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.Address)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(s => s.SiteCode).IsUnique();
        builder.HasIndex(s => s.SiteName);
    }
}
