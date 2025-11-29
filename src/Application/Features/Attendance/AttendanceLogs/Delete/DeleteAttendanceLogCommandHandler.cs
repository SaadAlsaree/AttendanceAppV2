using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.AttendanceLogs.Delete;

internal sealed class DeleteAttendanceLogCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteAttendanceLogCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteAttendanceLogCommand command, CancellationToken cancellationToken)
    {
        AttendanceLog? attendanceLog = await context.AttendanceLogs
            .SingleOrDefaultAsync(al => al.Id == command.AttendanceLogId, cancellationToken);

        if (attendanceLog is null)
        {
            return Result.Failure<bool>(AttendanceLogErrors.NotFound(command.AttendanceLogId));
        }



        context.AttendanceLogs.Remove(attendanceLog);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
