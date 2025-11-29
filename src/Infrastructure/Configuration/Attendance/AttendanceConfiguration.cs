using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Attendance;

internal sealed class AttendanceConfiguration : IEntityTypeConfiguration<Domain.Entities.Attendance.Attendance>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Attendance.Attendance> builder)
    {
        builder.ToTable("Attendances", Schemas.Default);

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.CheckInMethod)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(a => a.CheckOutMethod)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Configure DateTime properties to use UTC
        builder.Property(a => a.Date)
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.CheckInTime)
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.CheckOutTime)
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.ApprovedAt)
            .HasColumnType("timestamp with time zone");

        // Relationships
        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Shift)
            .WithMany()
            .HasForeignKey(a => a.ShiftId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.AttendanceSchedule)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.AttendanceScheduleId)
            .OnDelete(DeleteBehavior.SetNull);



        // Indexes
        builder.HasIndex(a => new { a.EmployeeId, a.Date })
            .IsUnique();
        builder.HasIndex(a => a.Date);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => a.ShiftId);
        builder.HasIndex(a => a.AttendanceScheduleId);
    }
}
