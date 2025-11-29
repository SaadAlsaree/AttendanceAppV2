using Domain.Entities.Users;

namespace Application.Abstractions.Authentication;

public interface ITokenProvider
{
    string Create(User user);
}
