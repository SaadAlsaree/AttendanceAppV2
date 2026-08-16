using Infrastructure.Authentication;

namespace Web.Api.Middleware;

/// <summary>
/// Enforces a strict view-only (عرض فقط) boundary for security officers (ضباط الامن).
/// Any non-safe HTTP method (anything other than GET/HEAD/OPTIONS) is rejected with 403,
/// regardless of the per-endpoint role allowlists. This is a defense-in-depth backstop so
/// the role cannot mutate state even on endpoints that have no explicit authorization
/// (e.g. the unauthenticated Files endpoints).
/// </summary>
public sealed class ViewOnlyRoleMiddleware(RequestDelegate next)
{
    private static readonly string[] SafeMethods = [HttpMethods.Get, HttpMethods.Head, HttpMethods.Options];

    public async Task InvokeAsync(HttpContext context)
    {
        bool isUnsafeMethod = !SafeMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase);

        if (isUnsafeMethod && context.User.HasAnyRole(nameof(Domain.Enums.Role.SecurityOfficer)))
        {
            IResult problem = Results.Problem(
                title: "Forbidden",
                detail: "ضباط الامن لديهم صلاحية العرض فقط ولا يمكنهم تنفيذ أي عملية تعديل.",
                type: "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                statusCode: StatusCodes.Status403Forbidden);

            await problem.ExecuteAsync(context);
            return;
        }

        await next(context);
    }
}
