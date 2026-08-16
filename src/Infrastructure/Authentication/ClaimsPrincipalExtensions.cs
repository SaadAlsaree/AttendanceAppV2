using System.Security.Claims;

namespace Infrastructure.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal? principal)
    {
        string? userId = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userId, out Guid parsedUserId) ?
            parsedUserId :
            throw new ApplicationException("User id is unavailable");
    }

    public static string? GetRole(this ClaimsPrincipal? principal)
    {
        return principal?.FindFirstValue("Role");
    }

    public static bool HasAnyRole(this ClaimsPrincipal? principal, params string[] roles)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        string? role = principal.GetRole();

        return !string.IsNullOrWhiteSpace(role) &&
            roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
