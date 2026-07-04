using Domain.Entities.Organizations;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Organizations;

internal sealed class EmployeeWeeklyShiftConfiguration : IEntityTypeConfiguration<EmployeeWeeklyShift>
{
    public void Configure(EntityTypeBuilder<EmployeeWeeklyShift> builder)
    {
        builder.ToTable("EmployeeWeeklyShifts", Schemas.Default);

        // Relationships
        builder.HasOne(w => w.Employee)
            .WithMany(e => e.WeeklyShifts)
            .HasForeignKey(w => w.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(w => w.Shift)
            .WithMany()
            .HasForeignKey(w => w.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(w => new { w.EmployeeId, w.DayOfWeek }).IsUnique();
        builder.HasIndex(w => w.ShiftId);
    }
}
