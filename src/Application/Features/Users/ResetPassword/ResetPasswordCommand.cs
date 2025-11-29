using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Users.ResetPassword;

public sealed record ResetPasswordCommand(Guid UserId, string NewPassword, string ConfirmPassword) : ICommand<bool>;
