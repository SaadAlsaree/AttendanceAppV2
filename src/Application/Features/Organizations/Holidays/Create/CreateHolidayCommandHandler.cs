using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.Holidays.Create;

internal sealed class CreateHolidayCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateHolidayCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateHolidayCommand command, CancellationToken cancellationToken)
    {
        OrganizationalUnit? organization = await context.OrganizationalUnits.AsNoTracking()
            .SingleOrDefaultAsync(o => o.Id == command.OrganizationId, cancellationToken);

        if (organization is null)
        {
            return Result.Failure<Guid>(OrganizationErrors.OrganizationalUnit.NotFound(command.OrganizationId));
        }

        // Check if holiday with same date already exists for this organization
        bool holidayExists = await context.Holidays
            .AnyAsync(h => h.OrganizationId == command.OrganizationId && h.Date == command.Date, cancellationToken);

        if (holidayExists)
        {
            return Result.Failure<Guid>(OrganizationErrors.Holiday.DateAlreadyExists(command.Date));
        }

        var holiday = new Holiday
        {
            OrganizationId = organization.Id,
            Name = command.Name,
            Date = command.Date,
            IsRecurring = command.IsRecurring,
            CreatedAt = dateTimeProvider.Now
        };

        holiday.Raise(new HolidayCreatedDomainEvent(holiday));

        context.Holidays.Add(holiday);

        await context.SaveChangesAsync(cancellationToken);

        return holiday.Id;
    }
}
