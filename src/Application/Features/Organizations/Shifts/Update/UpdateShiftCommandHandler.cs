using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Shifts.Update;

internal sealed class UpdateShiftCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<UpdateShiftCommand, bool>
{
    public async Task<Result<bool>> Handle(UpdateShiftCommand command, CancellationToken cancellationToken)
    {
        // Check if shift exists
        Shift shift = await context.Shifts
            .SingleOrDefaultAsync(s => s.Id == command.ShiftId, cancellationToken);

        if (shift is null)
        {
            return Result.Failure<bool>(ShiftErrors.NotFound(command.ShiftId));
        }


        // Check if name is unique 
        if (!string.IsNullOrEmpty(command.Name) && command.Name != shift.Name)
        {
            bool nameExists = await context.Shifts
                .AnyAsync(s => s.Name == command.Name &&
                               s.Id != command.ShiftId,
                          cancellationToken);

            if (nameExists)
            {
                return Result.Failure<bool>(ShiftErrors.NameAlreadyExists(command.Name));
            }
        }

        // Block deactivation while the shift is part of any employee's fixed weekly pattern
        if (command.IsActive == false && shift.IsActive)
        {
            bool assignedToEmployees = await context.EmployeeWeeklyShifts
                .AnyAsync(w => w.ShiftId == command.ShiftId, cancellationToken);

            if (assignedToEmployees)
            {
                return Result.Failure<bool>(ShiftErrors.CannotUpdateShiftInUse());
            }
        }

        // Update shift properties
        shift.Name = command.Name;
        shift.Description = command.Description;
        shift.ShiftType = command.ShiftType ?? shift.ShiftType;
        shift.StartTime = command.StartTime ?? shift.StartTime;
        shift.EndTime = command.EndTime ?? shift.EndTime;
        shift.GracePeriodMinutes = command.GracePeriodMinutes ?? shift.GracePeriodMinutes;
        shift.IsActive = command.IsActive ?? shift.IsActive;
        shift.LastUpdatedAt = dateTimeProvider.GetUtcNow();
        shift.LastUpdatedBy = userContext.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
