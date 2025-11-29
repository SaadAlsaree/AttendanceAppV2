using Application.Abstractions.Messaging;
using Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SharedKernel;

namespace Application.Attendance.AttendanceBreaks.Get;

public sealed class GetAttendanceBreaksQuery : IQuery<PaginatedResponse<AttendanceBreakResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? EmployeeId { get; set; }
    public Guid? AttendanceId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public BreakType? BreakType { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; }
    public SortOrder? SortOrder { get; set; }
}
