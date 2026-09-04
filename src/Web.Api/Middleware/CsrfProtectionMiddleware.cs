using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace Web.Api.Middleware;

/// <summary>
/// CSRF Protection Middleware for preventing Cross-Site Request Forgery attacks
/// </summary>
public sealed class CsrfProtectionMiddleware(RequestDelegate next, ILogger<CsrfProtectionMiddleware> logger)
{
    private const string CsrfTokenHeaderName = "X-CSRF-Token";
    private const string CsrfTokenCookieName = "__csrf_token";
    private const int TokenLength = 32;
    private static readonly TimeSpan TokenExpiry = TimeSpan.FromHours(2);

    // Methods that require CSRF protection
    private static readonly string[] ProtectedMethods = ["POST", "PUT", "DELETE", "PATCH"];

    // Paths that should be excluded from CSRF protection
    private static readonly string[] ExcludedPaths = [
        "/auth/login",
        "/users/new",
        "/auth/register",
        "/health",
        "/swagger",
        "/hangfire" // dashboard has its own Admin-only authorization filter + antiforgery handling
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (ShouldValidateCsrfToken(context))
            {
                if (!await ValidateCsrfToken(context))
                {
                    await HandleCsrfViolation(context);
                    return;
                }
            }
            else if (ShouldGenerateCsrfToken(context))
            {
                GenerateCsrfToken(context);
            }

            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in CSRF protection middleware");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal server error");
        }
    }

    private static bool ShouldValidateCsrfToken(HttpContext context)
    {
        // Only validate for protected HTTP methods
        if (!ProtectedMethods.Contains(context.Request.Method, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        // Skip validation for excluded paths
        string path = context.Request.Path.Value?.ToUpperInvariant() ?? string.Empty;
        if (ExcludedPaths.Any(excluded => path.StartsWith(excluded.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }
        // Skip validation only for endpoints that actually enforce authorization AND where the
        // caller's Bearer token authenticated successfully. Bearer-authenticated requests are not
        // CSRF-able (a forged cross-site request cannot attach the Authorization header), but a
        // merely *present* Bearer header must not bypass CSRF on endpoints that never check auth.
        // This middleware runs after UseAuthentication/UseAuthorization, so User is populated.
        if (context.GetEndpoint()?.Metadata.GetMetadata<IAuthorizeData>() is not null
            && context.User.Identity?.IsAuthenticated == true)
        {
            return false;
        }

        return true;
    }

    private static bool ShouldGenerateCsrfToken(HttpContext context)
    {
        // Generate token for GET requests that don't have one
        return context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
               !context.Request.Cookies.ContainsKey(CsrfTokenCookieName);
    }

    private async Task<bool> ValidateCsrfToken(HttpContext context)
    {
        try
        {
            // Get token from header
            string? headerToken = context.Request.Headers[CsrfTokenHeaderName].FirstOrDefault();
            if (string.IsNullOrEmpty(headerToken) && context.Request.HasFormContentType)
            {
                // Try to get token from form data
                IFormCollection form = await context.Request.ReadFormAsync();
                headerToken = form["__csrf_token"].FirstOrDefault();
            }

            if (string.IsNullOrEmpty(headerToken))
            {
                logger.LogWarning("CSRF token missing from request from IP: {ClientIp}", GetClientIp(context));
                return false;
            }

            // Get token from cookie
            string? cookieToken = context.Request.Cookies[CsrfTokenCookieName];
            if (string.IsNullOrEmpty(cookieToken))
            {
                logger.LogWarning("CSRF cookie missing from request from IP: {ClientIp}", GetClientIp(context));
                return false;
            }

            // Validate token
            if (!ValidateToken(headerToken, cookieToken))
            {
                logger.LogWarning("CSRF token validation failed from IP: {ClientIp}", GetClientIp(context));
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating CSRF token from IP: {ClientIp}", GetClientIp(context));
            return false;
        }
    }

    private void GenerateCsrfToken(HttpContext context)
    {
        try
        {
            string token = GenerateSecureToken();
            string hashedToken = HashToken(token);

            // Set cookie with the hashed token
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.Add(TokenExpiry),
                Path = "/"
            };

            context.Response.Cookies.Append(CsrfTokenCookieName, hashedToken, cookieOptions);

            // Add token to response headers for JavaScript access
            context.Response.Headers.Append("X-CSRF-Token", token);

            //logger.LogDebug("CSRF token generated for IP: {ClientIp}", GetClientIp(context));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating CSRF token");
        }
    }

    private static string GenerateSecureToken()
    {
        byte[] tokenBytes = new byte[TokenLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        return Convert.ToBase64String(tokenBytes);
    }

    private static string HashToken(string token)
    {
        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        byte[] hashBytes = SHA256.HashData(tokenBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private static bool ValidateToken(string headerToken, string cookieToken)
    {
        try
        {
            string hashedHeaderToken = HashToken(headerToken);
            return string.Equals(hashedHeaderToken, cookieToken, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private async Task HandleCsrfViolation(HttpContext context)
    {
        string clientIp = GetClientIp(context);

        logger.LogError("CSRF attack attempt detected from IP: {ClientIp}. URL: {Url}, Method: {Method}, UserAgent: {UserAgent}",
            clientIp, context.Request.Path, context.Request.Method, context.Request.Headers.UserAgent);

        context.Response.StatusCode = 403; // Forbidden
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "CSRF Protection",
            message = "Invalid or missing CSRF token",
            timestamp = DateTime.UtcNow,
            incidentId = Guid.NewGuid().ToString()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static string GetClientIp(HttpContext context)
    {
        string? realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                        context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();

        return realIp ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
