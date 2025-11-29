using Domain.Entities.Attendance;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Attendance;

internal sealed class ScheduleIssueConfiguration : IEntityTypeConfiguration<ScheduleIssue>
{
    public void Configure(EntityTypeBuilder<ScheduleIssue> builder)
    {
        builder.ToTable("ScheduleIssues", Schemas.Default);

        builder.Property(s => s.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.ExceptionType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.AttendanceSchedule)
            .WithMany(a => a.Exceptions)
            .HasForeignKey(s => s.AttendanceScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Shift)
            .WithMany()
            .HasForeignKey(s => s.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(s => new { s.AttendanceScheduleId, s.Date }).IsUnique();
        builder.HasIndex(s => s.Date);
        builder.HasIndex(s => s.ExceptionType);
    }
}
