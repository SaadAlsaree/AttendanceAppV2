using Application.Abstractions.Messaging;
using Domain.Enums;
using SharedKernel;

namespace Application.Attendance.AttendanceSchedules.GetMySchedules;

public sealed class GetMySchedulesQuery : IQuery<PaginatedResponse<AttendanceScheduleResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public ScheduleType? ScheduleType { get; set; }
    public bool? IsActive { get; set; }
    public string? SortBy { get; set; }
    public string? SortOrder { get; set; }
}
