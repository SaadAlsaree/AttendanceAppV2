using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using SharedKernel;

namespace Application.Attendance.AttendanceLogs.Get;

public sealed record GetAttendanceLogsQuery(
    int Page = 1,
    int PageSize = 10,

    Guid? OrganizationId = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? Direct = null,
    string? DeviceName = null,
    string? DeviceNo = null,
    string? EmpID = null,
    string? EmpName = null,
    string? SearchTerm = null,
    string? SortBy = null,
    string? SortOrder = null) : IQuery<PaginatedResponse<AttendanceLogResponse>>;
