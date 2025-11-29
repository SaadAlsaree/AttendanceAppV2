using Application.Abstractions.Messaging;
using Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SharedKernel;

namespace Application.Attendance.AttendanceSchedules.Get;

public sealed class GetAttendanceSchedulesQuery : IQuery<PaginatedResponse<AttendanceScheduleResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? EmployeeId { get; set; }
    public Guid? ShiftId { get; set; }
    public ScheduleType? ScheduleType { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public SortOrder? SortOrder { get; set; }
}
