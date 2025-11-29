using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using AttendanceEntity = Domain.Entities.Attendance.Attendance;

namespace Application.Attendance.AttendanceBreaks.Delete;

internal sealed class DeleteAttendanceBreakCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<DeleteAttendanceBreakCommand, bool>
{
    public async Task<Result<bool>> Handle(DeleteAttendanceBreakCommand command, CancellationToken cancellationToken)
    {
        AttendanceBreak? attendanceBreak = await context.AttendanceBreaks
            .Include(b => b.Attendance)
            .SingleOrDefaultAsync(b => b.Id == command.AttendanceBreakId, cancellationToken);

        if (attendanceBreak is null)
        {
            return Result.Failure<bool>(AttendanceBreakErrors.NotFound(command.AttendanceBreakId));
        }

        // Check if attendance is approved (cannot delete breaks for approved attendance without proper authorization)
        if (attendanceBreak.Attendance.ApprovedBy.HasValue)
        {
            return Result.Failure<bool>(AttendanceErrors.AlreadyApproved(attendanceBreak.Attendance.Id));
        }

        context.AttendanceBreaks.Remove(attendanceBreak);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
