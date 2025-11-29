using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Organizations.Employees.GetById;

public sealed class GetEmployeeByIdQuery : IQuery<ApiResponse<GetEmployeeByIdVm>>
{
    public Guid Id { get; set; }
}
