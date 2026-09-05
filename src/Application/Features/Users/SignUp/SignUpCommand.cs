using Application.Abstractions.Messaging;
using Domain.Enums;
using SharedKernel;

namespace Application.Features.Users.SignUp;

public sealed record SignUpCommand(
    string Username,
    string UserLogin,
    string Password,
    string ConfirmPassword,
    Role Role = Role.User,
    Guid OrganizationalUnitId = default,
    Guid? SiteId = null) : ICommand<ApiResponse<SignUpResponse>>;
