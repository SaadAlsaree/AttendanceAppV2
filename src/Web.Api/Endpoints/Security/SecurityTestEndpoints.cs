using Microsoft.AspNetCore.Mvc;

namespace Web.Api.Endpoints.Security;

public static class SecurityTestEndpoints
{
    public static void MapSecurityTestEndpoints(this IEndpointRouteBuilder app)
    {
        // Replace 'var' with explicit type 'RouteGroupBuilder'
        RouteGroupBuilder group = app.MapGroup("api/security-test")
            .WithTags("Security Testing")
            .WithOpenApi();

        // Test endpoint for rate limiting
        group.MapGet("/rate-limit", () => Results.Ok(new
        {
            message = "Rate limit test endpoint",
            timestamp = DateTime.UtcNow,
            tip = "Send multiple requests quickly to test rate limiting"
        }))
        .WithName("TestRateLimit")
        .WithSummary("Test rate limiting functionality");


        // Test endpoint for SQL injection detection
        group.MapGet("/sql-test", (string? id) => Results.Ok(new
        {
            message = "SQL injection test endpoint",
            receivedId = id,
            timestamp = DateTime.UtcNow,
            tip = "Try: ?id=1' OR '1'='1 to test SQL injection protection"
        }))
        .WithName("TestSqlInjection")
        .WithSummary("Test SQL injection protection");

        // Test endpoint for XSS detection
        group.MapPost("/xss-test", ([FromBody] CommandData data) => Results.Ok(new
        {
            message = "XSS test endpoint",
            receivedData = data,
            timestamp = DateTime.UtcNow,
            tip = "Try sending: {\"content\": \"<script>alert('xss')</script>\"}"
        }))
        .WithName("TestXss")
        .WithSummary("Test XSS protection");

        // Test endpoint for CSRF protection
        group.MapPost("/csrf-test", ([FromBody] CommandData data) => Results.Ok(new
        {
            message = "CSRF test endpoint - request successful",
            receivedData = data,
            timestamp = DateTime.UtcNow,
            tip = "This endpoint requires CSRF token in header or is excluded for API calls with Bearer token"
        }))
        .WithName("TestCsrf")
        .WithSummary("Test CSRF protection");

        // Test endpoint for file upload validation
        // Change the async lambda to a synchronous one since there is no 'await' inside.
        group.MapPost("/file-upload-test", (IFormFile file) =>
        {
            if (file is null)
            {
                return Results.BadRequest("No file provided");
            }

            return Results.Ok(new
            {
                message = "File upload test endpoint",
                fileName = file.FileName,
                fileSize = file.Length,
                contentType = file.ContentType,
                timestamp = DateTime.UtcNow,
                tip = "Try uploading files with dangerous extensions like .exe, .php, .bat"
            });
        })
        .WithName("TestFileUpload")
        .WithSummary("Test file upload security")
        .DisableAntiforgery(); // Allow file uploads without antiforgery token for testing


        // Test endpoint for path traversal detection
        group.MapGet("/path-test", (string? path) => Results.Ok(new
        {
            message = "Path traversal test endpoint",
            receivedPath = path,
            timestamp = DateTime.UtcNow,
            tip = "Try: ?path=../../../etc/passwd to test path traversal protection"
        }))
        .WithName("TestPathTraversal")
        .WithSummary("Test path traversal protection");

        // Test endpoint for command injection detection
        group.MapPost("/command-test", ([FromBody] CommandData data) => Results.Ok(new
        {
            message = "Command injection test endpoint",
            receivedCommand = data.Command,
            timestamp = DateTime.UtcNow,
            tip = "Try sending: {\"command\": \"ls; cat /etc/passwd\"}"
        }))
        .WithName("TestCommandInjection")
        .WithSummary("Test command injection protection");


        // Endpoint to get security headers
        group.MapGet("/headers", (HttpContext context) =>
        {
            var headers = context.Response.Headers
                .Where(h => h.Key.StartsWith("X-", StringComparison.OrdinalIgnoreCase) ||
                           h.Key.Contains("Security") ||
                           h.Key.Contains("Content-Security-Policy") ||
                           h.Key.Contains("Strict-Transport-Security"))
                .ToDictionary(h => h.Key, h => h.Value.ToString());

            return Results.Ok(new
            {
                message = "Security headers test endpoint",
                securityHeaders = headers,
                timestamp = DateTime.UtcNow,
                tip = "Check the response headers to see all security headers"
            });
        })
        .WithName("TestSecurityHeaders")
        .WithSummary("View security headers");

        // Test endpoint that generates suspicious activity
        group.MapGet("/trigger-suspicious", (HttpContext context) =>
        {
            // This endpoint will trigger multiple security alerts for testing
            string userAgent = context.Request.Headers.UserAgent.ToString();

            return Results.Ok(new
            {
                message = "Suspicious activity test endpoint",
                userAgent,
                clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
                timestamp = DateTime.UtcNow,
                tip = "Use with suspicious user agents like 'sqlmap' or 'nikto' to trigger IP blocking"
            });
        })
        .WithName("TestSuspiciousActivity")
        .WithSummary("Trigger suspicious activity detection");

        // Health check endpoint (excluded from most security checks)
        group.MapGet("/health", () => Results.Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            securityMiddleware = "Active"
        }))
        .WithName("SecurityTestHealth")
        .WithSummary("Health check for security testing");
    }

    public sealed record TestData(string Content);
    public sealed record CommandData(string Command);
}
