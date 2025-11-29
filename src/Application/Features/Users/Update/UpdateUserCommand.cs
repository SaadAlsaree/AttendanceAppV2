using Application.Abstractions.Messaging;
using Domain.Enums;

namespace Application.Features.Users.Update;

public sealed class UpdateUserCommand : ICommand
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string UserLogin { get; set; } = string.Empty;
    public Role Role { get; set; }
    public UserStatus Status { get; set; }
    public bool IsActive { get; set; }
    public Guid? OrganizationalUnitId { get; set; }
}

