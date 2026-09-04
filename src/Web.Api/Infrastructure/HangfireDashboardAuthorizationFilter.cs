using Hangfire.Dashboard;
using Infrastructure.Authentication;

namespace Web.Api.Infrastructure;

/// <summary>
/// Allows only authenticated Admin / SuperAdmin users into the Hangfire dashboard.
/// The dashboard runs after UseAuthentication, so a Bearer token on the request
/// (used by the E2E job-trigger calls) authenticates here like anywhere else.
/// </summary>
public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private static readonly string[] AllowedRoles = ["Admin", "SuperAdmin"];

    public bool Authorize(DashboardContext context)
    {
        HttpContext httpContext = context.GetHttpContext();

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        string? role = httpContext.User.GetRole();

        return !string.IsNullOrWhiteSpace(role)
            && AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
}
