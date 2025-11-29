using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceBreaks.Get;

internal sealed class GetAttendanceBreaksQueryHandler(
    IApplicationDbContext context)
    : IQueryHandler<GetAttendanceBreaksQuery, PaginatedResponse<AttendanceBreakResponse>>
{
    public async Task<Result<PaginatedResponse<AttendanceBreakResponse>>> Handle(GetAttendanceBreaksQuery query, CancellationToken cancellationToken)
    {
        IQueryable<AttendanceBreak> breaksQuery = context.AttendanceBreaks
            .Include(b => b.Attendance)
            .ThenInclude(a => a.Employee)
            .AsNoTracking()
            .AsQueryable();

        if (query.EmployeeId.HasValue)
        {
            breaksQuery = breaksQuery.Where(b => b.Attendance.EmployeeId == query.EmployeeId.Value);
        }
        if (query.AttendanceId.HasValue)
        {
            breaksQuery = breaksQuery.Where(b => b.AttendanceId == query.AttendanceId.Value);
        }
        if (query.StartDate.HasValue)
        {
            breaksQuery = breaksQuery.Where(b => b.StartTime >= query.StartDate.Value);
        }
        if (query.EndDate.HasValue)
        {
            breaksQuery = breaksQuery.Where(b => b.StartTime <= query.EndDate.Value);
        }
        if (query.BreakType.HasValue)
        {
            breaksQuery = breaksQuery.Where(b => b.BreakType == query.BreakType.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTerm = query.SearchTerm.ToUpperInvariant();
            breaksQuery = breaksQuery.Where(b => b.Attendance.Employee.FirstName.ToUpperInvariant().Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || b.BreakType.ToString().ToUpperInvariant().Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        // Sorting
        bool isDescending = (query.SortOrder?.ToString().ToUpperInvariant() ?? "DESCENDING") == "DESCENDING";
        breaksQuery = query.SortBy?.ToUpperInvariant() switch
        {
            "STARTTIME" => isDescending ? breaksQuery.OrderByDescending(b => b.StartTime) : breaksQuery.OrderBy(b => b.StartTime),
            "ENDTIME" => isDescending ? breaksQuery.OrderByDescending(b => b.EndTime) : breaksQuery.OrderBy(b => b.EndTime),
            "DURATIONMINUTES" => isDescending ? breaksQuery.OrderByDescending(b => b.DurationMinutes) : breaksQuery.OrderBy(b => b.DurationMinutes),
            "BREAKTYPE" => isDescending ? breaksQuery.OrderByDescending(b => b.BreakType) : breaksQuery.OrderBy(b => b.BreakType),
            "CREATEDAT" => isDescending ? breaksQuery.OrderByDescending(b => b.CreatedAt) : breaksQuery.OrderBy(b => b.CreatedAt),
            _ => breaksQuery.OrderByDescending(b => b.CreatedAt)
        };

        int totalCount = await breaksQuery.CountAsync(cancellationToken);
        List<AttendanceBreakResponse> items = await breaksQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(b => new AttendanceBreakResponse
            {
                Id = b.Id,
                AttendanceId = b.AttendanceId,
                EmployeeId = b.Attendance.EmployeeId,
                EmployeeName = b.Attendance.Employee.FirstName + " " + b.Attendance.Employee.FamilyName,
                StartTime = b.StartTime,
                EndTime = b.EndTime,
                DurationMinutes = b.DurationMinutes,
                BreakType = b.BreakType,
                Notes = b.Notes,
                CreatedAt = b.CreatedAt,
                LastUpdatedAt = b.LastUpdatedAt,
                AttendanceDate = b.Attendance.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)
            })
            .ToListAsync(cancellationToken);

        return PaginatedResponse<AttendanceBreakResponse>.Create(
            items,
            totalCount,
            query.Page,
            query.PageSize);
    }
}
