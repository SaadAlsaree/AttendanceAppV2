using Domain.Common;
using Domain.Entities.Attendance;
using Domain.Entities.Users;

namespace Domain.Entities.Organizations;

public sealed class Employee : AuditableEntity<Guid>
{
    public string EmpID { get; set; }
    public string? Code { get; set; }
    public string RFID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;
    public string ThirdName { get; set; } = string.Empty;
    public string? FourthName { get; set; } = string.Empty;
    public string? FamilyName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty; // FirstName + SecondName + ThirdName + FourthName + FamilyName
    public string? Email { get; set; } = string.Empty;
    public bool? IsManager { get; set; }
    public string? FaceImageUrl { get; set; } = string.Empty;
    public string? NationalIdFrontUrl { get; set; } = string.Empty;
    public string? NationalIdBackUrl { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; } = string.Empty;


    public Guid? OrganizationalUnitId { get; set; }
    public OrganizationalUnit? OrganizationalUnit { get; set; }

    public Guid? ManagerId { get; set; }
    public Employee? Manager { get; set; }

    public Guid? UserId { get; set; }
    public User? User { get; set; }
    public List<AttendanceSchedule> AttendanceSchedules { get; set; } = new List<AttendanceSchedule>();
    public List<Attendance.Attendance> Attendances { get; set; } = new List<Attendance.Attendance>();

    // Navigation Properties
    public List<Employee> Subordinates { get; set; } = new List<Employee>();
    public List<OrganizationalUnit> ManagedUnits { get; set; } = new List<OrganizationalUnit>();
}
