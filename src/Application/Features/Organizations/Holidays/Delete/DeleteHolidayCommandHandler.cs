using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.Holidays.Delete;

internal sealed class DeleteHolidayCommandHandler(IApplicationDbContext context)
    : ICommandHandler<DeleteHolidayCommand>
{
    public async Task<Result> Handle(DeleteHolidayCommand command, CancellationToken cancellationToken)
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
            return Result.Failure(OrganizationErrors.Holiday.CannotDeletePastHoliday);
        }

        context.Holidays.Remove(holiday);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
