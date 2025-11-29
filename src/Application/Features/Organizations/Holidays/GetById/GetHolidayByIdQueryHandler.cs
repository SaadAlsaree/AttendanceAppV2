using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Organizations.Holidays.GetById;

internal sealed class GetHolidayByIdQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetHolidayByIdQuery, ApiResponse<HolidayResponse>>
{
    public async Task<Result<ApiResponse<HolidayResponse>>> Handle(GetHolidayByIdQuery query, CancellationToken cancellationToken)
    {
        Holiday? holiday = await context.Holidays
            .Include(h => h.Organization)
            .SingleOrDefaultAsync(h => h.Id == query.HolidayId, cancellationToken);

        if (holiday is null)
        {
            return Result.Failure<ApiResponse<HolidayResponse>>(OrganizationErrors.Holiday.NotFound(query.HolidayId));
        }

        var response = new HolidayResponse
        {
            Id = holiday.Id,
            OrganizationId = holiday.OrganizationId,
            OrganizationName = holiday.Organization.UnitName,
            Name = holiday.Name,
            Date = holiday.Date,
            IsRecurring = holiday.IsRecurring,
            CreatedAt = holiday.CreatedAt,
            LastUpdatedAt = holiday.LastUpdatedAt
        };

        return ApiResponse<HolidayResponse>.Success(response);
    }
}
