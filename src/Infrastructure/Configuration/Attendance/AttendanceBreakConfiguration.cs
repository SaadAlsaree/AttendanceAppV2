using Domain.Entities.Attendance;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Attendance;

internal sealed class AttendanceBreakConfiguration : IEntityTypeConfiguration<AttendanceBreak>
{
    public void Configure(EntityTypeBuilder<AttendanceBreak> builder)
    {
        builder.ToTable("AttendanceBreaks", Schemas.Default);

        builder.Property(b => b.Notes)
            .HasMaxLength(500);

        builder.Property(b => b.BreakType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Relationships
        builder.HasOne(b => b.Attendance)
            .WithMany(a => a.Breaks)
            .HasForeignKey(b => b.AttendanceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(b => b.AttendanceId);
        builder.HasIndex(b => b.StartTime);
        builder.HasIndex(b => b.EndTime);
        builder.HasIndex(b => b.BreakType);
    }
}
