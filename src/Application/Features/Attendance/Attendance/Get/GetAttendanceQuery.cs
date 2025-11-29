using Application.Abstractions.Messaging;
using Domain.Enums;
using SharedKernel;

namespace Application.Attendance.Get;

public sealed class GetAttendanceQuery : IQuery<PaginatedResponse<AttendanceResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? EmployeeId { get; set; }
    public Guid? OrganizationId { get; set; }
    public DateTime? Date { get; set; }
    public AttendanceStatus? Status { get; set; }
    public Guid? ShiftId { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
}
