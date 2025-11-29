using Web.Api.Middleware;

namespace Web.Api.Extensions;

/// <summary>
/// Extension methods for registering security middleware
/// </summary>
public static class SecurityMiddlewareExtensions
{
    /// <summary>
    /// Adds all security middleware to the application pipeline
    /// </summary>
    public static IApplicationBuilder UseSecurityMiddleware(this IApplicationBuilder app)
    {
        // Order is important - most restrictive first
        app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<SecurityLoggingMiddleware>();
        //app.UseMiddleware<RateLimitingMiddleware>();
        app.UseMiddleware<IpBlockingMiddleware>();
        app.UseMiddleware<CsrfProtectionMiddleware>();
        //app.UseMiddleware<RequestValidationMiddleware>();
        //app.UseMiddleware<SqlInjectionProtectionMiddleware>();

        return app;
    }

    /// <summary>
    /// Adds rate limiting middleware
    /// </summary>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RateLimitingMiddleware>();
    }

    /// <summary>
    /// Adds IP blocking middleware
    /// </summary>
    public static IApplicationBuilder UseIpBlocking(this IApplicationBuilder app)
    {
        return app.UseMiddleware<IpBlockingMiddleware>();
    }

    /// <summary>
    /// Adds request validation middleware
    /// </summary>
    //public static IApplicationBuilder UseRequestValidation(this IApplicationBuilder app)
    //{
    //    return app.UseMiddleware<RequestValidationMiddleware>();
    //}

    /// <summary>
    /// Adds SQL injection protection middleware
    /// </summary>
    public static IApplicationBuilder UseSqlInjectionProtection(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SqlInjectionProtectionMiddleware>();
    }

    /// <summary>
    /// Adds security headers middleware
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }

    /// <summary>
    /// Adds CSRF protection middleware
    /// </summary>
    public static IApplicationBuilder UseCsrfProtection(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CsrfProtectionMiddleware>();
    }

    /// <summary>
    /// Adds security logging middleware
    /// </summary>
    public static IApplicationBuilder UseSecurityLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<SecurityLoggingMiddleware>();
    }

    /// <summary>
    /// Adds request context logging middleware (already exists)
    /// </summary>
    public static IApplicationBuilder UseRequestContextLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestContextLoggingMiddleware>();
    }
}
