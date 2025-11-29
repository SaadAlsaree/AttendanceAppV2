using Domain.Enums;

namespace Application.Features.Organizations.Employees.Search;

public sealed class EmployeeResponse
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string RFID { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string SecondName { get; set; } = string.Empty;
    public string ThirdName { get; set; } = string.Empty;
    public string FourthName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid OrganizationalUnitId { get; set; }
    public string OrganizationalUnitName { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public bool IsManager { get; set; }


    public DateTime CreatedAt { get; set; }
    public string? FaceImageUrl { get; set; }
    public string? NationalIdFrontUrl { get; set; }
    public string? NationalIdBackUrl { get; set; }
    public string? ProfileImageUrl { get; set; }
    public UserStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;

}
