using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Web.Api.Middleware;

/// <summary>
/// Middleware for validating incoming requests and detecting potential attacks
/// </summary>
public sealed class RequestValidationMiddleware(RequestDelegate next, ILogger<RequestValidationMiddleware> logger)
{
    // SQL Injection patterns
    private static readonly Regex[] SqlInjectionPatterns =
    [
        new(@"(\b(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|EXEC|EXECUTE)\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(\b(UNION|HAVING|GROUP\s+BY|ORDER\s+BY)\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(--|\/\*|\*\/|;|'|""|\b(OR|AND)\b\s*\d+\s*=\s*\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(\b(INFORMATION_SCHEMA|SYSOBJECTS|SYSCOLUMNS)\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    // XSS patterns
    private static readonly Regex[] XssPatterns =
    [
        new(@"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"javascript:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"on\w+\s*=", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"<iframe\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"<object\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"<embed\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"<link\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"<meta\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    // Path traversal patterns
    private static readonly Regex[] PathTraversalPatterns =
    [
        new(@"\.\.[\\/]", RegexOptions.Compiled),
        new(@"%2e%2e[\\/]", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\.\.%2f", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"%2e%2e%2f", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    // Command injection patterns
    private static readonly Regex[] CommandInjectionPatterns =
    [
        new(@"(\||&|;|`|\$\(|\${)", RegexOptions.Compiled),
        new(@"\b(cat|ls|dir|type|echo|whoami|id|uname|pwd|cd)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\b(wget|curl|nc|netcat|telnet|ssh|ftp)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    // File upload validation patterns
    private static readonly string[] DangerousFileExtensions =
    [
        ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".jar",
        ".php", ".asp", ".aspx", ".jsp", ".py", ".pl", ".rb", ".sh", ".ps1"
    ];

    private const int MaxRequestSize = 10 * 1024 * 1024; // 10MB
    private const int MaxHeaderLength = 8192; // 8KB
    private const int MaxUrlLength = 2048; // 2KB

    // Paths that should be excluded from request body validation
    private static readonly string[] ExcludedBodyValidationPaths = [
        "/auth/login",
        "/auth/register",
        "/users/new",
        "/users/reset-password"
    ];

    // Headers that should be excluded from malicious pattern validation
    private static readonly string[] ExcludedHeaders = [
        "User-Agent",
        "Accept",
        "Accept-Language",
        "Accept-Encoding",
        "Content-Type",
        "Content-Length",
        "Authorization",
        "X-Requested-With",
        "X-CSRF-Token",
        "Referer",
        "Origin",
        "Cache-Control",
        "Pragma",
        "Connection",
        "Upgrade-Insecure-Requests",
        "Sec-Fetch-Site",
        "Sec-Fetch-Mode",
        "Sec-Fetch-Dest",
        "Sec-Fetch-User",
        "Host",
        "DNT",
        "Keep-Alive",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Via",
        "Warning",
        "Date",
        "Server",
        "X-Powered-By",
        "X-AspNet-Version",
        "X-AspNetMvc-Version",
        "X-SourceFiles",
        "X-Forwarded-For",
        "X-Forwarded-Proto",
        "X-Real-IP",
        "CF-Connecting-IP",
        "CF-IPCountry",
        "CF-Ray",
        "CF-Visitor",
        "Cookie",
        "Set-Cookie"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Validate request size
            if (context.Request.ContentLength > MaxRequestSize)
            {
                logger.LogWarning("Request size too large: {Size} bytes from IP: {ClientIp}",
                    context.Request.ContentLength, GetClientIp(context));
                await RejectRequest(context, "Request size too large");
                return;
            }

            // Validate URL length
            string fullUrl = context.Request.Path + context.Request.QueryString;
            if (fullUrl.Length > MaxUrlLength)
            {
                logger.LogWarning("URL too long: {Length} characters from IP: {ClientIp}",
                    fullUrl.Length, GetClientIp(context));
                await RejectRequest(context, "URL too long");
                return;
            }

            // Validate headers
            if (!ValidateHeaders(context))
            {
                logger.LogWarning("Header validation failed for request from IP: {ClientIp}, Path: {Path}, Method: {Method}",
                    GetClientIp(context), context.Request.Path, context.Request.Method);
                await RejectRequest(context, "Invalid headers detected");
                return;
            }

            // Validate URL and query parameters
            if (!ValidateUrl(context))
            {
                await RejectRequest(context, "Suspicious URL pattern detected");
                return;
            }

            // Validate request body if present (skip for excluded paths)
            if (context.Request.ContentLength > 0 && context.Request.Body.CanRead &&
                !ShouldSkipBodyValidation(context) && !await ValidateRequestBody(context))
            {
                await RejectRequest(context, "Suspicious request content detected");
                return;
            }

            // Validate file uploads
            if (context.Request.HasFormContentType && context.Request.Form.Files.Count > 0 && !ValidateFileUploads(context))
            {
                await RejectRequest(context, "Dangerous file upload detected");
                return;
            }

            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in request validation middleware");
            await RejectRequest(context, "Request validation failed");
        }
    }

    private bool ValidateHeaders(HttpContext context)
    {
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in context.Request.Headers)
        {
            // Check header length
            if (header.Value.ToString().Length > MaxHeaderLength)
            {
                logger.LogWarning("Header too long: {HeaderName} from IP: {ClientIp}",
                    header.Key, GetClientIp(context));
                return false;
            }

            // Skip malicious pattern validation for excluded headers
            if (ExcludedHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            // Check for malicious patterns in headers
            string headerValue = header.Value.ToString();
            if (ContainsMaliciousPatterns(headerValue))
            {
                logger.LogWarning("Malicious pattern in header {HeaderName}: {HeaderValue} from IP: {ClientIp}",
                    header.Key, headerValue, GetClientIp(context));
                return false;
            }
        }

        return true;
    }

    private bool ValidateUrl(HttpContext context)
    {
        string url = context.Request.Path + context.Request.QueryString;

        if (ContainsMaliciousPatterns(url))
        {
            logger.LogWarning("Malicious pattern in URL: {Url} from IP: {ClientIp}",
                url, GetClientIp(context));
            return false;
        }

        return true;
    }

    private async Task<bool> ValidateRequestBody(HttpContext context)
    {
        try
        {
            // Enable buffering to allow multiple reads
            context.Request.EnableBuffering();

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
            string body = await reader.ReadToEndAsync();

            // Reset position for next middleware
            context.Request.Body.Position = 0;

            if (ContainsMaliciousPatterns(body))
            {
                logger.LogWarning("Malicious pattern in request body from IP: {ClientIp}. Body: {RequestBody}",
                    GetClientIp(context), body);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating request body from IP: {ClientIp}", GetClientIp(context));
            return false;
        }
    }

    private bool ValidateFileUploads(HttpContext context)
    {
        foreach (IFormFile file in context.Request.Form.Files)
        {
            // Check file extension
            string extension = Path.GetExtension(file.FileName).ToUpperInvariant();
            if (DangerousFileExtensions.Contains(extension))
            {
                logger.LogWarning("Dangerous file upload attempt: {FileName} from IP: {ClientIp}",
                    file.FileName, GetClientIp(context));
                return false;
            }

            // Check for suspicious file names
            if (ContainsMaliciousPatterns(file.FileName))
            {
                logger.LogWarning("Suspicious file name: {FileName} from IP: {ClientIp}",
                    file.FileName, GetClientIp(context));
                return false;
            }
        }

        return true;
    }

    private static bool ContainsMaliciousPatterns(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        // Check SQL injection patterns
        foreach (Regex pattern in SqlInjectionPatterns)
        {
            if (pattern.IsMatch(input))
            {
                return true;
            }
        }

        // Check XSS patterns
        foreach (Regex pattern in XssPatterns)
        {
            if (pattern.IsMatch(input))
            {
                return true;
            }
        }

        // Check path traversal patterns
        foreach (Regex pattern in PathTraversalPatterns)
        {
            if (pattern.IsMatch(input))
            {
                return true;
            }
        }

        // Check command injection patterns
        foreach (Regex pattern in CommandInjectionPatterns)
        {
            if (pattern.IsMatch(input))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldSkipBodyValidation(HttpContext context)
    {
        string path = context.Request.Path.Value?.ToUpperInvariant() ?? string.Empty;
        return ExcludedBodyValidationPaths.Any(excluded =>
            path.StartsWith(excluded.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));
    }

    private static string GetClientIp(HttpContext context)
    {
        string? realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                        context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();

        return realIp ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static async Task RejectRequest(HttpContext context, string reason)
    {
        context.Response.StatusCode = 400; // Bad Request
        context.Response.ContentType = "application/json";

        object response = new
        {
            error = "Request Validation Failed",
            message = reason,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
