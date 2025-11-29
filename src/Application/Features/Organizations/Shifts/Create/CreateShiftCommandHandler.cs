using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Shifts.Create;

internal sealed class CreateShiftCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider,
    IUserContext userContext)
    : ICommandHandler<CreateShiftCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateShiftCommand command, CancellationToken cancellationToken)
    {


        // Check if shift name is unique 
        bool shiftNameExists = await context.Shifts
            .AnyAsync(s => s.Name == command.Name,
                      cancellationToken);

        if (shiftNameExists)
        {
            return Result.Failure<Guid>(ShiftErrors.NameAlreadyExists(command.Name));
        }

        // Create the shift
        var shift = new Shift
        {
            Name = command.Name,
            Description = command.Description,
            ShiftType = command.ShiftType,
            StartTime = command.StartTime,
            EndTime = command.EndTime,
            GracePeriodMinutes = command.GracePeriodMinutes,
            IsActive = command.IsActive,
            CreatedAt = dateTimeProvider.GetUtcNow(),
            CreatedBy = userContext.UserId
        };

        // Raise domain event
        shift.Raise(new ShiftCreatedDomainEvent(shift.Id));

        // Add to database
        context.Shifts.Add(shift);
        await context.SaveChangesAsync(cancellationToken);

        return shift.Id;
    }
}
