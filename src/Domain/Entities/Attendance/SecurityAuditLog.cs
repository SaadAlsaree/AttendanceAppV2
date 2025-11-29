using Domain.Common;
using Domain.Entities.Organizations;
using Domain.Entities.Users;

namespace Domain.Entities.Attendance;

public sealed class SecurityAuditLog : AuditableEntity<Guid>
{
    public Guid? EmployeeId { get; set; }
    public Guid? UserId { get; set; }
    public string EventType { get; set; } = string.Empty; // LOGIN, LOGOUT, FAILED_LOGIN, BIOMETRIC_MATCH, etc.
    public string EventDescription { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public string Severity { get; set; } = string.Empty; // Info, Warning, Error, Critical
    public string? AdditionalData { get; set; } // JSON data for extra context
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    public Guid? DeviceId { get; set; }
    public double? LocationLatitude { get; set; }
    public double? LocationLongitude { get; set; }
    public Guid? BiometricId { get; set; }
    public double? BiometricConfidence { get; set; }

    // Navigation Properties
    public Employee? Employee { get; set; }
    public User? User { get; set; }
}
