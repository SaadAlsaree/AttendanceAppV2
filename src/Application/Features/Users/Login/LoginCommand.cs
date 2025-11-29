using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Users.Login;

public sealed record LoginCommand(string UserLogin, string Password) : ICommand<ApiResponse<LoginResponse>>;

