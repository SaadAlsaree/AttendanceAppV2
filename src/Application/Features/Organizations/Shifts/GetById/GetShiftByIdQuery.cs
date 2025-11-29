using Application.Abstractions.Messaging;
using Application.Features.Organizations.Shifts.Get;
using SharedKernel;

namespace Application.Features.Organizations.Shifts.GetById;

public sealed class GetShiftByIdQuery : IQuery<ApiResponse<ShiftResponse>>
{
    public Guid ShiftId { get; set; }
}
