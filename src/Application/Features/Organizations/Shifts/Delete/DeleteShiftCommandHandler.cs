using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Shifts.Delete;

internal sealed class DeleteShiftCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<DeleteShiftCommand>
{
    public async Task<Result> Handle(DeleteShiftCommand command, CancellationToken cancellationToken)
    {
        // Check if shift exists
        Shift shift = await context.Shifts
            .SingleOrDefaultAsync(s => s.Id == command.ShiftId, cancellationToken);

        if (shift is null)
        {
            return Result.Failure(ShiftErrors.NotFound(command.ShiftId));
        }


        // Check if shift has attendance records
        bool hasAttendanceRecords = await context.Attendances
            .AnyAsync(a => a.ShiftId == command.ShiftId, cancellationToken);

        if (hasAttendanceRecords)
        {
            return Result.Failure(ShiftErrors.CannotDeleteShiftWithAttendanceRecords());
        }

        // Check if shift is part of any employee's fixed weekly pattern
        bool assignedToEmployees = await context.EmployeeWeeklyShifts
            .AnyAsync(w => w.ShiftId == command.ShiftId, cancellationToken);

        if (assignedToEmployees)
        {
            return Result.Failure(ShiftErrors.CannotDeleteShiftInUse());
        }

        // Perform soft delete
        shift.IsDeleted = true;
        shift.DeletedAt = dateTimeProvider.GetUtcNow();
        shift.DeletedBy = userContext.UserId;

        context.Shifts.Update(shift);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
