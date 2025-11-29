using Application.Abstractions.Messaging;
using Domain.Enums;
using SharedKernel;

namespace Application.Features.Users.UpdateRole;

public sealed class UpdateUserRoleCommand : ICommand<ApiResponse<bool>>
{
    public Guid UserId { get; set; }
    public Role NewRole { get; set; }
    public Guid UpdatedBy { get; set; }
}
