using Domain.Entities.Organizations;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Organizations;

internal sealed class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts", Schemas.Default);

        builder.Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.ShiftType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();



        // Indexes

        builder.HasIndex(s => s.ShiftType);
        builder.HasIndex(s => s.IsActive);
    }
}
