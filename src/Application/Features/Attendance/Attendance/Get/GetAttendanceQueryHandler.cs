using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Get;

internal sealed class GetAttendanceQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetAttendanceQuery, PaginatedResponse<AttendanceResponse>>
{
    public async Task<Result<PaginatedResponse<AttendanceResponse>>> Handle(GetAttendanceQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Domain.Entities.Attendance.Attendance> attendanceQuery = context.Attendances
        .Where(a => a.CheckInTime.HasValue || a.CheckOutTime.HasValue)
            .Include(a => a.Employee)
            .ThenInclude(e => e.OrganizationalUnit)
            .Include(a => a.Shift)
            .AsNoTracking();

        // check if user role not Admin then apply accessible unit ids filter
        UserInfoDto user = await userContext.GetUserAsync();
        if (user.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);
            attendanceQuery = attendanceQuery.Where(a => accessibleUnitIds.Contains(a.Employee.OrganizationalUnitId!.Value));
        }

        // Apply filters
        if (query.EmployeeId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.EmployeeId == query.EmployeeId);
        }

        if (query.OrganizationId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.OrganizationId == query.OrganizationId);
        }

        if (query.Date.HasValue)
        {
            DateTime dateFilter = query.Date.Value.Date;
            attendanceQuery = attendanceQuery.Where(a => a.Date == dateFilter);
        }

        if (query.Status.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.Status == query.Status);
        }

        if (query.ShiftId.HasValue)
        {
            attendanceQuery = attendanceQuery.Where(a => a.ShiftId == query.ShiftId);
        }

        // Apply search
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            attendanceQuery = attendanceQuery.Where(a =>
                EF.Functions.Like(a.Employee.FullName, $"%{query.SearchTerm}%") ||
                EF.Functions.Like(a.Employee.Code, $"%{query.SearchTerm}%"));
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            string? sortOrder = query.SortOrder?.ToUpperInvariant();
            bool isDescending = sortOrder == "DESC";

            attendanceQuery = query.SortBy.ToUpperInvariant() switch
            {
                "date" => isDescending ? attendanceQuery.OrderByDescending(a => a.Date) : attendanceQuery.OrderBy(a => a.Date),
                "checkintime" => isDescending ? attendanceQuery.OrderByDescending(a => a.CheckInTime) : attendanceQuery.OrderBy(a => a.CheckInTime),
                "checkouttime" => isDescending ? attendanceQuery.OrderByDescending(a => a.CheckOutTime) : attendanceQuery.OrderBy(a => a.CheckOutTime),
                "status" => isDescending ? attendanceQuery.OrderByDescending(a => a.Status) : attendanceQuery.OrderBy(a => a.Status),
                "employeename" => isDescending ? attendanceQuery.OrderByDescending(a => a.Employee.FirstName) : attendanceQuery.OrderBy(a => a.Employee.FirstName),
                "createdat" => isDescending ? attendanceQuery.OrderByDescending(a => a.CreatedAt) : attendanceQuery.OrderBy(a => a.CreatedAt),
                _ => attendanceQuery.OrderByDescending(a => a.Date)
            };
        }
        else
        {
            attendanceQuery = attendanceQuery.OrderByDescending(a => a.Date);
        }

        // Get total count
        int totalCount = await attendanceQuery.CountAsync(cancellationToken);

        // Apply pagination
        List<AttendanceResponse> attendances = await attendanceQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new AttendanceResponse
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                OrganizationId = a.OrganizationId,
                OrganizationalName = a.Employee.OrganizationalUnit != null ? a.Employee.OrganizationalUnit.UnitName : null,
                Date = a.Date,
                CheckInTime = a.CheckInTime,
                CheckOutTime = a.CheckOutTime,
                Status = a.Status,
                ShiftId = a.ShiftId,
                WorkingMinutes = a.WorkingMinutes,
                BreakMinutes = a.BreakMinutes,
                OvertimeMinutes = a.OvertimeMinutes,
                LateMinutes = a.LateMinutes,
                EarlyLeaveMinutes = a.EarlyLeaveMinutes,
                Notes = a.Notes,
                ApprovedBy = a.ApprovedBy,
                ApprovedAt = a.ApprovedAt,
                AttendanceScheduleId = a.AttendanceScheduleId,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.LastUpdatedAt,
                FullName = a.Employee.FullName,
                Code = a.Employee.Code,

                ShiftName = a.Shift != null ? a.Shift.Name : null,
                ApproverName = null // Would need to join with Users table for approver name
            })
            .ToListAsync(cancellationToken);

        return PaginatedResponse<AttendanceResponse>.Create(
            attendances,
            totalCount,
            query.Page,
            query.PageSize);
    }
}
