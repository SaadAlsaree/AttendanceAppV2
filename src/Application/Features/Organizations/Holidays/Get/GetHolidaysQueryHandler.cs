using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;
using System.Globalization;

namespace Application.Organizations.Holidays.Get;

internal sealed class GetHolidaysQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetHolidaysQuery, PaginatedResponse<HolidayResponse>>
{
    public async Task<Result<PaginatedResponse<HolidayResponse>>> Handle(GetHolidaysQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Holiday> holidaysQuery = context.Holidays
            .Include(h => h.Organization)
            .AsQueryable();

        // Apply filters
        if (query.OrganizationId.HasValue)
        {
            holidaysQuery = holidaysQuery.Where(h => h.OrganizationId == query.OrganizationId.Value);
        }

        if (query.StartDate.HasValue)
        {
            holidaysQuery = holidaysQuery.Where(h => h.Date >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            holidaysQuery = holidaysQuery.Where(h => h.Date <= query.EndDate.Value);
        }

        if (query.IsRecurring.HasValue)
        {
            holidaysQuery = holidaysQuery.Where(h => h.IsRecurring == query.IsRecurring.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            holidaysQuery = holidaysQuery.Where(h => h.Name.Contains(query.SearchTerm));
        }

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(query.SortBy))
        {
            string sortBy = query.SortBy.ToUpperInvariant();
            string? sortOrder = query.SortOrder?.ToUpperInvariant();
            bool isDescending = sortOrder == "DESC";

            holidaysQuery = sortBy switch
            {
                "NAME" => isDescending ? holidaysQuery.OrderByDescending(h => h.Name) : holidaysQuery.OrderBy(h => h.Name),
                "DATE" => isDescending ? holidaysQuery.OrderByDescending(h => h.Date) : holidaysQuery.OrderBy(h => h.Date),
                "CREATEDAT" => isDescending ? holidaysQuery.OrderByDescending(h => h.CreatedAt) : holidaysQuery.OrderBy(h => h.CreatedAt),
                _ => holidaysQuery.OrderBy(h => h.Date)
            };
        }
        else
        {
            holidaysQuery = holidaysQuery.OrderBy(h => h.Date);
        }

        // Get total count for pagination
        int totalCount = await holidaysQuery.CountAsync(cancellationToken);

        // Apply pagination
        List<HolidayResponse> holidays = await holidaysQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(h => new HolidayResponse
            {
                Id = h.Id,
                OrganizationId = h.OrganizationId,
                OrganizationName = h.Organization.UnitName,
                Name = h.Name,
                Date = h.Date,
                IsRecurring = h.IsRecurring,
                CreatedAt = h.CreatedAt,
                LastUpdatedAt = h.LastUpdatedAt
            })
            .ToListAsync(cancellationToken);

        return PaginatedResponse<HolidayResponse>.Create(
            holidays,
            totalCount,
            query.Page,
            query.PageSize);
    }
}
