using Domain.Entities.Organizations;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Organizations;

internal sealed class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays", Schemas.Default);

        builder.Property(h => h.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Relationships
        builder.HasOne(h => h.Organization)
            .WithMany()
            .HasForeignKey(h => h.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(h => new { h.OrganizationId, h.Date, h.IsRecurring }).IsUnique();
        builder.HasIndex(h => h.Date);
        builder.HasIndex(h => h.IsRecurring);
    }
}
