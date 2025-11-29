using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Attendance;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Attendance.Delete;

internal sealed class DeleteAttendanceCommandHandler(
    IApplicationDbContext context)
    : ICommandHandler<DeleteAttendanceCommand>
{
    public async Task<Result> Handle(DeleteAttendanceCommand command, CancellationToken cancellationToken)
    {
        Domain.Entities.Attendance.Attendance? attendance = await context.Attendances
            .SingleOrDefaultAsync(a => a.Id == command.AttendanceId, cancellationToken);

        if (attendance is null)
        {
            return Result.Failure(AttendanceErrors.NotFound(command.AttendanceId));
        }

        // Cannot delete approved attendance records without proper authorization
        if (attendance.ApprovedBy.HasValue)
        {
            return Result.Failure(AttendanceErrors.AlreadyApproved(command.AttendanceId));
        }

        // Check if there are any related attendance breaks that need to be handled
        bool hasBreaks = await context.AttendanceBreaks
            .AnyAsync(b => b.AttendanceId == command.AttendanceId, cancellationToken);

        if (hasBreaks)
        {
            return Result.Failure(AttendanceErrors.HasRelatedBreaks(command.AttendanceId));
        }

        // Note: AttendanceLogs are not directly related to Attendance records
        // They are separate entities for tracking employee check-ins/check-outs

        context.Attendances.Remove(attendance);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
