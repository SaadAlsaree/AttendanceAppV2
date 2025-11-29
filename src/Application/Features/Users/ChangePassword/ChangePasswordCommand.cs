using Application.Abstractions.Messaging;
using SharedKernel;

namespace Application.Features.Users.ChangePassword;

public sealed class ChangePasswordCommand : ICommand<ApiResponse<bool>>
{
    public Guid UserId { get; set; }
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }
    public string ConfirmPassword { get; set; }
}
