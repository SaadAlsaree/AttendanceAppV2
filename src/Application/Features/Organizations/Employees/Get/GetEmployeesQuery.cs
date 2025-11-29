using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Organizations.Employees.Get;

public sealed class GetEmployeesQuery : IQuery<PaginatedResponse<EmployeeResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public Guid? OrganizationalUnitId { get; set; }
    public bool? IsManager { get; set; }
    public string? SearchTerm { get; set; }
}
