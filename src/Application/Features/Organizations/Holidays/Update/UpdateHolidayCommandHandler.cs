using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.Holidays.Update;

internal sealed class UpdateHolidayCommandHandler(
    IApplicationDbContext context,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<UpdateHolidayCommand>
{
    public async Task<Result> Handle(UpdateHolidayCommand command, CancellationToken cancellationToken)
    {
        Holiday? holiday = await context.Holidays
            .SingleOrDefaultAsync(h => h.Id == command.HolidayId, cancellationToken);

        if (holiday is null)
        {
            return Result.Failure(OrganizationErrors.Holiday.NotFound(command.HolidayId));
        }

        // Check if holiday has already passed
        if (holiday.Date < DateOnly.FromDateTime(DateTime.Today))
        {
            return Result.Failure(OrganizationErrors.Holiday.CannotUpdatePastHoliday);
        }

        // Check if new date conflicts with existing holiday
        if (command.Date.HasValue && command.Date.Value != holiday.Date)
        {
            bool dateExists = await context.Holidays
                .AnyAsync(h => h.OrganizationId == holiday.OrganizationId &&
                               h.Date == command.Date.Value &&
                               h.Id != holiday.Id, cancellationToken);

            if (dateExists)
            {
                return Result.Failure(OrganizationErrors.Holiday.DateAlreadyExists(command.Date.Value));
            }
        }

        // Update properties if provided
        if (!string.IsNullOrEmpty(command.Name))
        {
            holiday.Name = command.Name;
        }

        if (command.Date.HasValue)
        {
            holiday.Date = command.Date.Value;
        }

        if (command.IsRecurring.HasValue)
        {
            holiday.IsRecurring = command.IsRecurring.Value;
        }

        holiday.LastUpdatedAt = dateTimeProvider.Now;

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
