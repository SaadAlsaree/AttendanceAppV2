using System.Globalization;
using Domain.Entities.Attendance;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Attendance;

internal sealed class AttendanceScheduleConfiguration : IEntityTypeConfiguration<AttendanceSchedule>
{
    public void Configure(EntityTypeBuilder<AttendanceSchedule> builder)
    {
        builder.ToTable("AttendanceSchedules", Schemas.Default);

        builder.Property(s => s.Notes)
            .HasMaxLength(500);

        builder.Property(s => s.ScheduleType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.ExcludedDates)
            .HasConversion(
                v => string.Join(',', v.Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(d => DateOnly.Parse(d, CultureInfo.InvariantCulture))
                    .ToList())
            .HasMaxLength(1000)
            .Metadata.SetValueComparer(ValueComparers.GetDateOnlyListComparer());

        // Relationships
        builder.HasOne(s => s.Employee)
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);



        builder.HasMany(s => s.ScheduleDays)
            .WithOne(sd => sd.AttendanceSchedule)
            .HasForeignKey(sd => sd.AttendanceScheduleId)
            .OnDelete(DeleteBehavior.Cascade);


        // Indexes
        builder.HasIndex(s => new { s.EmployeeId, s.StartDate });
        builder.HasIndex(s => s.EndDate);
        builder.HasIndex(s => s.ScheduleType);
        builder.HasIndex(s => s.IsActive);
    }
}
