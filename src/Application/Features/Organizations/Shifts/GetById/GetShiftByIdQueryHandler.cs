using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Application.Features.Organizations.Shifts.Get;
using Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Features.Organizations.Shifts.GetById;

internal sealed class GetShiftByIdQueryHandler(IApplicationDbContext context
  )
    : IQueryHandler<GetShiftByIdQuery, ApiResponse<ShiftResponse>>
{
    public async Task<Result<ApiResponse<ShiftResponse>>> Handle(GetShiftByIdQuery query, CancellationToken cancellationToken)
    {
        Shift shift = await context.Shifts
            .AsNoTracking()

            .SingleOrDefaultAsync(s => s.Id == query.ShiftId, cancellationToken);

        if (shift is null)
        {
            return Result.Failure<ApiResponse<ShiftResponse>>(ShiftErrors.NotFound(query.ShiftId));
        }


        var response = new ShiftResponse
        {
            Id = shift.Id,

            Name = shift.Name,
            Description = shift.Description ?? "",
            ShiftType = shift.ShiftType,
            StartTime = shift.StartTime,
            EndTime = shift.EndTime,
            GracePeriodMinutes = shift.GracePeriodMinutes,
            IsActive = shift.IsActive,
            CreatedAt = shift.CreatedAt
        };

        return Result.Success(ApiResponse<ShiftResponse>.Success(response));
    }
}
