using Domain.Entities.Attendance;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Attendance;

internal sealed class LeaveConfiguration : IEntityTypeConfiguration<Leave>
{
    public void Configure(EntityTypeBuilder<Leave> builder)
    {
        builder.ToTable("Leaves", Schemas.Default);

        builder.Property(l => l.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(l => l.RejectionReason)
            .HasMaxLength(500);

        builder.Property(l => l.LeaveType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Configure DateTime properties to use UTC
        builder.Property(l => l.StartDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(l => l.EndDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(l => l.ApprovedAt)
            .HasColumnType("timestamp with time zone");

        // Relationships
        builder.HasOne(l => l.Employee)
            .WithMany()
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(l => l.EmployeeId);
        builder.HasIndex(l => l.StartDate);
        builder.HasIndex(l => l.EndDate);
        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.LeaveType);
    }
}
