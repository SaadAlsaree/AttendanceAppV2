using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Organizations.EmployeeWeeklyShifts.Get;

public sealed class GetEmployeeWeeklyShiftsQuery : IQuery<PaginatedResponse<EmployeeWeeklyShiftsResponse>>
{
    public int? Page { get; set; } = 1;
    public int? PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
}
