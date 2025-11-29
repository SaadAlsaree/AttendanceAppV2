using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Users.Delete;

public sealed record DeleteUserCommand(Guid UserId, Guid DeletedBy) : ICommand<bool>;
