using Domain.Enums;

namespace Application.Features.Organizations.Employees.Get;

public sealed class EmployeeResponse
{
    public Guid Id { get; set; }
    public string EmpId { get; set; } = string.Empty;
    public string RFID { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public Guid OrganizationalUnitId { get; set; }
    public string OrganizationalUnitName { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsManager { get; set; }

    /// <summary>True when the employee has a fixed weekly shift pattern (تثبيت الدوام) assigned.</summary>
    public bool HasFixedShift { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? FaceImageUrl { get; set; }
    public string? NationalIdFrontUrl { get; set; }
    public string? NationalIdBackUrl { get; set; }
    public string? ProfileImageUrl { get; set; }
    public UserStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;

}
