using Application.Abstractions.Messaging;

namespace Application.Features.Organizations.Employees.Profile;

public sealed record UpdateProfileCommand(Guid Id,
    string? ProfileImage) : ICommand;
