using Domain.Entities.Attendance;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Attendance;

internal sealed class AttendanceLogConfiguration : IEntityTypeConfiguration<AttendanceLog>
{
    public void Configure(EntityTypeBuilder<AttendanceLog> builder)
    {
        builder.ToTable("AttendanceLogs", Schemas.Default);

        // Configure properties with appropriate lengths
        builder.Property(l => l.CardNo)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.CardNo)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(l => l.EmpID)
            .HasMaxLength(50);


        builder.Property(l => l.DateWork)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(l => l.TimeAttend)
            .HasColumnType("time")
            .IsRequired();
        builder.Property(l => l.Direct)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(l => l.DeviceName)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(l => l.DeviceNo)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.DateTimeAttend)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Relationships
        // Indexes
        builder.HasIndex(l => l.DateTimeAttend);
        builder.HasIndex(l => l.CardNo);
        builder.HasIndex(l => l.EmpID);
        builder.HasIndex(l => l.DateWork);
        builder.HasIndex(l => l.TimeAttend);
        builder.HasIndex(l => l.Direct);
        builder.HasIndex(l => l.DeviceName);
        builder.HasIndex(l => l.DeviceNo);
    }
}
