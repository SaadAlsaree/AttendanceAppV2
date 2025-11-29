using Domain.Entities.Attendance;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Attendance;

internal sealed class ScheduleDayConfiguration : IEntityTypeConfiguration<ScheduleDay>
{
    public void Configure(EntityTypeBuilder<ScheduleDay> builder)
    {
        builder.ToTable("ScheduleDays", Schemas.Default);

        builder.Property(s => s.Notes)
            .HasMaxLength(500);

        builder.Property(s => s.ScheduleDayDate)
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.AttendanceSchedule)
            .WithMany(a => a.ScheduleDays)
            .HasForeignKey(s => s.AttendanceScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Shift)
            .WithMany()
            .HasForeignKey(s => s.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(s => new { s.AttendanceScheduleId, s.ScheduleDayDate }).IsUnique();
        builder.HasIndex(s => s.ShiftId);
        builder.HasIndex(s => s.IsActive);
    }
}
