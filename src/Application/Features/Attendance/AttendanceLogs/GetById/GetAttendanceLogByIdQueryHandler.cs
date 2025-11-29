using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Models;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceLogs.GetById;

internal sealed class GetAttendanceLogByIdQueryHandler(
    IApplicationDbContext context,
    IHasPermission hasPermission,
    IUserContext userContext)
    : IQueryHandler<GetAttendanceLogByIdQuery, AttendanceLogResponse>
{
    public async Task<Result<AttendanceLogResponse>> Handle(GetAttendanceLogByIdQuery query, CancellationToken cancellationToken)
    {
        // Get attendance log
        AttendanceLog? attendanceLog = await context.AttendanceLogs
            .AsNoTracking()
            .SingleOrDefaultAsync(al => al.Id == query.AttendanceLogId, cancellationToken);

        if (attendanceLog is null)
        {
            return Result.Failure<AttendanceLogResponse>(AttendanceLogErrors.NotFound(query.AttendanceLogId));
        }

        // Get employee by EmpID
        Employee? employee = await context.Employees
            .Include(e => e.OrganizationalUnit)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmpID == attendanceLog.EmpID, cancellationToken);

        // Apply permission filter: if current user is not Admin, restrict to accessible unit ids
        UserInfoDto currentUser = await userContext.GetUserAsync();
        if (currentUser.Role != Role.Admin)
        {
            IEnumerable<Guid> accessibleUnitIds = await hasPermission.GetAccessibleUnitIdsAsync(cancellationToken);

            if (employee is null ||
                !employee.OrganizationalUnitId.HasValue ||
                !accessibleUnitIds.Contains(employee.OrganizationalUnitId.Value))
            {
                return Result.Failure<AttendanceLogResponse>(AttendanceLogErrors.NotFound(query.AttendanceLogId));
            }
        }

        // Map to response
        var response = new AttendanceLogResponse
        {
            Id = attendanceLog.Id,
            DateTimeAttend = attendanceLog.DateTimeAttend,
            CardNo = attendanceLog.CardNo,
            EmpID = attendanceLog.EmpID,
            DateWork = attendanceLog.DateWork,
            TimeAttend = attendanceLog.TimeAttend,
            Direct = attendanceLog.Direct,
            DeviceName = attendanceLog.DeviceName,
            DeviceNo = attendanceLog.DeviceNo,
            EmpName = employee?.FullName ?? string.Empty,
            Employee = employee is not null ? CreateEmployeeResponse(employee) : null
        };

        return response;
    }

    private static EmployeeResponse CreateEmployeeResponse(
        Employee employee)
    {
        return new EmployeeResponse
        {
            Id = employee.Id,
            FullName = employee.FullName,
            Code = employee.Code ?? string.Empty,
            RFID = employee.RFID ?? string.Empty,
            OrganizationalUnitId = employee.OrganizationalUnitId?.ToString() ?? string.Empty,
            OrganizationalUnitName = employee.OrganizationalUnit?.UnitName ?? string.Empty,
            OrganizationalUnitCode = employee.OrganizationalUnit?.UnitCode ?? string.Empty
        };
    }
}
