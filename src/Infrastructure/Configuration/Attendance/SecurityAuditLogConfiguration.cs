using Domain.Entities.Attendance;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Configuration.Attendance;

internal sealed class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
    {
        builder.ToTable("SecurityAuditLogs", Schemas.Default);

        builder.Property(s => s.EventType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.EventDescription)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.IpAddress)
            .HasMaxLength(45);  // IPv6 max length

        builder.Property(s => s.UserAgent)
            .HasMaxLength(500);

        builder.Property(s => s.Severity)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.AdditionalData)
            .HasMaxLength(2000);

        builder.Property(s => s.FailureReason)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(s => s.Employee)
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(s => s.Timestamp);
        builder.HasIndex(s => s.EventType);
        builder.HasIndex(s => s.Severity);
        builder.HasIndex(s => s.IsSuccessful);
        builder.HasIndex(s => s.EmployeeId);
        builder.HasIndex(s => s.DeviceId);
    }
}
