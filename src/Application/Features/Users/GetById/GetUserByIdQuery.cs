using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Users.GetById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<UserResponse>;
