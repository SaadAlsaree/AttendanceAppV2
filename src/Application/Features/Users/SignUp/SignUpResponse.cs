using Domain.Enums;

namespace Application.Features.Users.SignUp;

public sealed class SignUpResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string UserLogin { get; set; } = string.Empty;
    public Role Role { get; set; }
    public DateTime CreatedDate { get; set; }
}
