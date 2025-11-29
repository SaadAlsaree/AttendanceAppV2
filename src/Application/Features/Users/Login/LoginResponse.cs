namespace Application.Features.Users.Login;

public sealed class LoginResponse
{
    public string Token { get; set; }
    public Guid UserId { get; set; }
    public DateTime LastLoginDate { get; set; }
}
