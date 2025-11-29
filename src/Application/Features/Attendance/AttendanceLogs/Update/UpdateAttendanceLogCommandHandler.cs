using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Attendance.AttendanceLogs.GetById;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceLogs.Update;

internal sealed class UpdateAttendanceLogCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateAttendanceLogCommand, AttendanceLogResponse>
{
    public async Task<Result<AttendanceLogResponse>> Handle(UpdateAttendanceLogCommand command, CancellationToken cancellationToken)
    {
        // Get attendance log
        AttendanceLog? attendanceLog = await context.AttendanceLogs
            .SingleOrDefaultAsync(al => al.Id == command.AttendanceLogId, cancellationToken);

        if (attendanceLog is null)
        {
            return Result.Failure<AttendanceLogResponse>(AttendanceLogErrors.NotFound(command.AttendanceLogId));
        }

        // Update properties if provided
        if (command.DateTimeAttend.HasValue)
        {
            attendanceLog.DateTimeAttend = command.DateTimeAttend.Value;
        }

        if (command.CardNo is not null)
        {
            attendanceLog.CardNo = command.CardNo;
        }

        if (command.EmpID is not null)
        {
            attendanceLog.EmpID = command.EmpID;
        }

        if (command.DateWork.HasValue)
        {
            attendanceLog.DateWork = command.DateWork.Value;
        }

        if (command.TimeAttend.HasValue)
        {
            attendanceLog.TimeAttend = command.TimeAttend;
        }

        if (command.Direct.HasValue)
        {
            attendanceLog.Direct = command.Direct.Value;
        }

        if (command.DeviceName is not null)
        {
            attendanceLog.DeviceName = command.DeviceName;
        }

        if (command.DeviceNo is not null)
        {
            attendanceLog.DeviceNo = command.DeviceNo;
        }

        attendanceLog.LastUpdatedAt = dateTimeProvider.GetUtcNow();

        await context.SaveChangesAsync(cancellationToken);

        // Get employee by EmpID
        Employee? employee = await context.Employees
            .Include(e => e.OrganizationalUnit)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.EmpID == attendanceLog.EmpID, cancellationToken);

        // Map to response using same pattern as GetById
        var response = new AttendanceLogResponse
        {
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
