using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceLogs.Create;

internal sealed class CreateAttendanceLogCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateAttendanceLogCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateAttendanceLogCommand command, CancellationToken cancellationToken)
    {
        // Validate employee exists
        Employee? employee = await context.Employees.AsNoTracking()
            .SingleOrDefaultAsync(e => e.EmpID == command.EmpID, cancellationToken);

        if (employee is null)
        {
            return Result.Failure<Guid>(EmployeeErrors.NotFound(command.EmpID));
        }

        // Validate attendance if provided

        // Ensure DateTimeAttend is in UTC before saving to database
        DateTime dateTimeAttendUtc = dateTimeProvider.EnsureUtc(command.DateTimeAttend);

        var attendanceLog = new AttendanceLog
        {
            EmpID = command.EmpID,
            DateWork = command.DateWork,
            TimeAttend = command.TimeAttend,
            Direct = command.Direct,
            DeviceName = command.DeviceName,
            DeviceNo = command.DeviceNo,
            DateTimeAttend = dateTimeAttendUtc,
            CardNo = command.CardNo,
            CreatedAt = dateTimeProvider.GetUtcNow()
        };

        context.AttendanceLogs.Add(attendanceLog);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(attendanceLog.Id);
    }
}
