using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Organizations.Employees.Search;

public sealed class SearchEmployeeQuery : IQuery<PaginatedResponse<EmployeeResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SearchTerm { get; set; } = string.Empty;
}
