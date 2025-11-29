using Application.Models;

namespace Application.Abstractions.Authentication;

public interface IUserContext
{
    Guid UserId { get; }

    Task<UserInfoDto> GetUserAsync();
}
