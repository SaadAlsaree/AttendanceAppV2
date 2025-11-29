using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Shifts.Get;

internal sealed class GetShiftsQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetShiftsQuery, PaginatedResponse<ShiftResponse>>
{
    public async Task<Result<PaginatedResponse<ShiftResponse>>> Handle(GetShiftsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Shift> shiftsQuery = context.Shifts
            .AsNoTracking()
            .AsSplitQuery()
            .AsQueryable();





        if (query.ShiftType.HasValue)
        {
            shiftsQuery = shiftsQuery.Where(s => s.ShiftType == query.ShiftType);
        }

        if (query.IsActive.HasValue)
        {
            shiftsQuery = shiftsQuery.Where(s => s.IsActive == query.IsActive);
        }
        else
        {
            // Default to active shifts only
            shiftsQuery = shiftsQuery.Where(s => s.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string searchTerm = query.SearchTerm.Trim().ToUpperInvariant();
            shiftsQuery = shiftsQuery.Where(s => s.Name.Contains(searchTerm));
        }

        // Get total count
        int totalCount = await shiftsQuery.CountAsync(cancellationToken);



        // Apply pagination
        int page = query.Page ?? 1;
        int pageSize = query.PageSize ?? 10;

        List<ShiftResponse> shifts = await shiftsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new ShiftResponse
            {
                Id = s.Id,

                Name = s.Name,
                Description = s.Description ?? "",
                ShiftType = s.ShiftType,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                GracePeriodMinutes = s.GracePeriodMinutes,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        // Create paginated response
        var paginatedResponse = new PaginatedResponse<ShiftResponse>
        {
            Data = shifts,
            Page = query.Page ?? 1,
            PageSize = query.PageSize ?? 10,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)(query.PageSize ?? 10))
        };

        return paginatedResponse;
    }
}
