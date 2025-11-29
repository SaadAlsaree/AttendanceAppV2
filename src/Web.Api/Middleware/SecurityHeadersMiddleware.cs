namespace Web.Api.Middleware;

/// <summary>
/// Middleware for adding security headers to prevent various attacks
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // Add security headers before processing the request
        AddSecurityHeaders(context);

        // Remove sensitive headers that might leak server information
        RemoveSensitiveHeaders(context);

        await next(context);
    }

    private static void AddSecurityHeaders(HttpContext context)
    {
        // Don't modify headers if response has already started
        if (context.Response.HasStarted)
        {
            return;
        }

        IHeaderDictionary headers = context.Response.Headers;

        // Prevent clickjacking attacks
        headers.Append("X-Frame-Options", "DENY");

        // Prevent MIME type sniffing
        headers.Append("X-Content-Type-Options", "nosniff");

        // Enable XSS protection
        headers.Append("X-XSS-Protection", "1; mode=block");

        // Enforce HTTPS
        headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");

        // Content Security Policy - restrictive policy
        string cspPolicy = string.Join("; ", "default-src 'self'",
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'", // May need adjustment based on your app
            "style-src 'self' 'unsafe-inline'",
            "img-src 'self' data: https:",
            "font-src 'self'",
            "connect-src 'self'",
            "frame-ancestors 'none'",
            "base-uri 'self'",
            "form-action 'self'",
            "object-src 'none'",
            "media-src 'self'",
            "worker-src 'self'",
            "manifest-src 'self'");
        headers.Append("Content-Security-Policy", cspPolicy);

        // Referrer Policy
        headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions Policy (formerly Feature Policy)
        string permissionsPolicy = string.Join(", ", "camera=()",
            "microphone=()",
            "geolocation=()",
            "interest-cohort=()",
            "payment=()",
            "usb=()",
            "magnetometer=()",
            "accelerometer=()",
            "gyroscope=()",
            "fullscreen=(self)",
            "picture-in-picture=()");
        headers.Append("Permissions-Policy", permissionsPolicy);

        // Cross-Origin policies
        headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
        headers.Append("Cross-Origin-Opener-Policy", "same-origin");
        headers.Append("Cross-Origin-Resource-Policy", "same-origin");

        // Cache control for sensitive pages
        if (IsSensitivePath(context.Request.Path))
        {
            headers.Append("Cache-Control", "no-store, no-cache, must-revalidate, private");
            headers.Append("Pragma", "no-cache");
            headers.Append("Expires", "0");
        }

        // Expect-CT header for certificate transparency
        headers.Append("Expect-CT", "max-age=86400, enforce");

        // Additional security headers for API endpoints
        if (IsApiPath(context.Request.Path))
        {
            headers.Append("X-Permitted-Cross-Domain-Policies", "none");
            headers.Append("X-Download-Options", "noopen");
        }
    }

    private static void RemoveSensitiveHeaders(HttpContext context)
    {
        // Don't modify headers if response has already started
        if (context.Response.HasStarted)
        {
            return;
        }

        IHeaderDictionary headers = context.Response.Headers;

        // Remove headers that might reveal server information
        string[] sensitiveHeaders = [
            "Server",
            "X-Powered-By",
            "X-AspNet-Version",
            "X-AspNetMvc-Version",
            "X-SourceFiles"
        ];

        foreach (string header in sensitiveHeaders)
        {
            headers.Remove(header);
        }
    }

    private static bool IsSensitivePath(PathString path)
    {
        string[] sensitivePaths = [
            "/admin",
            "/login",
            "/register",
            "/password",
            "/profile",
            "/settings",
            "/api/auth",
            "/api/users"
        ];

        return sensitivePaths.Any(sensitivePath =>
            path.StartsWithSegments(sensitivePath, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsApiPath(PathString path)
    {
        return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);
    }
}
