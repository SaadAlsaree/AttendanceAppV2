using Application.Abstractions.Messaging;
using Domain.Enums;
using SharedKernel;

namespace Application.Features.Organizations.Shifts.Get;

public sealed class GetShiftsQuery : IQuery<PaginatedResponse<ShiftResponse>>
{
    public int? Page { get; set; } = 1;
    public int? PageSize { get; set; } = 10;
    public ShiftType? ShiftType { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
    public string? SortBy { get; set; } = "Name";
}
