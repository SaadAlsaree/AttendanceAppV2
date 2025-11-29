using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Web.Api.Middleware;

/// <summary>
/// Advanced SQL Injection Protection Middleware with pattern detection and prevention
/// </summary>
public sealed class SqlInjectionProtectionMiddleware(RequestDelegate next, ILogger<SqlInjectionProtectionMiddleware> logger)
{
    // Advanced SQL injection detection patterns
    private static readonly Regex[] AdvancedSqlPatterns =
    [
        // Union-based injection
        new(@"\b(UNION\s+(ALL\s+)?SELECT)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Boolean-based blind injection
        new(@"(\s+(AND|OR)\s+\d+\s*[=<>!]+\s*\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"(\s+(AND|OR)\s+['""]?\w+['""]?\s*[=<>!]+\s*['""]?\w+['""]?)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Time-based blind injection
        new(@"\b(WAITFOR\s+DELAY|SLEEP\s*\(|BENCHMARK\s*\(|PG_SLEEP\s*\()\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Error-based injection
        new(@"\b(EXTRACTVALUE\s*\(|UPDATEXML\s*\(|XMLTYPE\s*\()\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Stacked queries
        new(@";\s*(SELECT|INSERT|UPDATE|DELETE|DROP|CREATE|ALTER)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Information schema access
        new(@"\b(INFORMATION_SCHEMA\.(TABLES|COLUMNS|SCHEMATA))\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // System tables access
        new(@"\b(SYS\.(TABLES|COLUMNS|OBJECTS)|SYSOBJECTS|SYSCOLUMNS|MSysObjects)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Database fingerprinting
        new(@"\b(@@VERSION|VERSION\s*\(\)|SQLITE_VERSION|MYSQL_VERSION)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Comment-based evasion
        new(@"(/\*.*?\*/|--\s|#)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // String concatenation attacks
        new(@"(\|\||CONCAT\s*\(|CHR\s*\(|CHAR\s*\()", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Hex/Binary encoding attacks
        new(@"(0x[0-9a-fA-F]+|BINARY\s*\()", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Function-based injection
        new(@"\b(LOAD_FILE\s*\(|INTO\s+OUTFILE|INTO\s+DUMPFILE)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Conditional statements
        new(@"\b(CASE\s+WHEN|IF\s*\(.*,.*,.*\))\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        
        // Database-specific functions
        new(@"\b(EXEC\s*\(|EXECUTE\s*\(|SP_EXECUTESQL)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)
    ];

    // Dangerous SQL keywords that should trigger alerts
    private static readonly string[] DangerousKeywords =
    [
        "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "INSERT", "UPDATE",
        "EXEC", "EXECUTE", "SP_", "XP_", "OPENROWSET", "OPENDATASOURCE",
        "BULK", "CMDSHELL", "SHUTDOWN", "BACKUP", "RESTORE"
    ];

    // Common SQL injection payloads
    private static readonly string[] CommonPayloads =
    [
        "' OR '1'='1", "' OR 1=1--", "' OR 'a'='a", "\" OR \"1\"=\"1",
        "' UNION SELECT", "'; DROP TABLE", "'; DELETE FROM",
        "' AND (SELECT", "' OR (SELECT", "admin'--", "admin'/*",
        "1' AND '1'='1", "1' OR '1'='1", "' HAVING 1=1--",
        "' GROUP BY", "' ORDER BY", "' UNION ALL SELECT"
    ];

    // Paths that should be excluded from SQL injection protection
    private static readonly string[] ExcludedPaths = [
        "/auth/login",
        "/auth/register",
        "/users/new",
        "/users/reset-password",
        "/health",
        "/swagger"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Skip SQL injection protection for excluded paths
            if (ShouldSkipProtection(context))
            {
                await next(context);
                return;
            }

            // Check URL and query string
            if (ContainsSqlInjection(context.Request.Path + context.Request.QueryString.ToString()))
            {
                await HandleSqlInjectionAttempt(context, "URL/Query String");
                return;
            }

            // Check headers
            foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in context.Request.Headers)
            {
                if (ContainsSqlInjection(header.Value.ToString()))
                {
                    await HandleSqlInjectionAttempt(context, $"Header: {header.Key}");
                    return;
                }
            }

            // Check form data
            if (context.Request.HasFormContentType)
            {
                try
                {
                    IFormCollection form = await context.Request.ReadFormAsync();
                    foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> field in form)
                    {
                        if (ContainsSqlInjection(field.Value.ToString()))
                        {
                            await HandleSqlInjectionAttempt(context, $"Form Field: {field.Key}");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error reading form data for SQL injection check from IP: {ClientIp}", GetClientIp(context));
                }
            }

            // Check JSON body
            if (context.Request.ContentType?.Contains("application/json") == true && context.Request.ContentLength > 0)
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);
                string body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (ContainsSqlInjection(body))
                {
                    await HandleSqlInjectionAttempt(context, "JSON Body");
                    return;
                }
            }

            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SQL injection protection middleware");
            context.Response.StatusCode = 500;
            await context.Response.WriteAsync("Internal server error");
        }
    }

    private bool ContainsSqlInjection(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        string normalizedInput = NormalizeInput(input);

        // Check against advanced patterns
        foreach (Regex pattern in AdvancedSqlPatterns)
        {
            if (pattern.IsMatch(normalizedInput))
            {
                logger.LogWarning("SQL injection pattern detected: {Pattern} in input: {Input}",
                    pattern.ToString(), input.Length > 100 ? input[..100] + "..." : input);
                return true;
            }
        }

        // Check against common payloads
        foreach (string payload in CommonPayloads)
        {
            if (normalizedInput.Contains(payload, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Common SQL injection payload detected: {Payload} in input", payload);
                return true;
            }
        }

        // Check for dangerous keywords in suspicious contexts
        if (ContainsDangerousKeywords(normalizedInput))
        {
            logger.LogWarning("Dangerous SQL keywords detected in input");
            return true;
        }

        return false;
    }

    private static string NormalizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        // Convert to uppercase for case-insensitive matching
        string normalized = input.ToUpperInvariant();

        // Remove common encoding/evasion techniques
        normalized = Uri.UnescapeDataString(normalized);
        normalized = normalized.Replace("%20", " ");
        normalized = normalized.Replace("+", " ");
        normalized = normalized.Replace("\t", " ");
        normalized = normalized.Replace("\n", " ");
        normalized = normalized.Replace("\r", " ");

        // Remove extra spaces
        normalized = Regex.Replace(normalized, @"\s+", " ");

        return normalized;
    }

    private bool ContainsDangerousKeywords(string input)
    {
        foreach (string keyword in DangerousKeywords)
        {
            // Look for keywords in SQL contexts (with spaces, parentheses, etc.)
            string pattern = $@"\b{Regex.Escape(keyword)}\b";
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase) && HasSqlContext(input))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasSqlContext(string input)
    {
        string[] sqlIndicators = ["SELECT", "FROM", "WHERE", "UNION", "INSERT", "UPDATE", "DELETE", "TABLE", "DATABASE"];

        foreach (string indicator in sqlIndicators)
        {
            if (input.Contains(indicator, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Check for SQL-like syntax patterns
        return Regex.IsMatch(input, @"[=<>!]+|[\(\)';]", RegexOptions.IgnoreCase);
    }

    private async Task HandleSqlInjectionAttempt(HttpContext context, string location)
    {
        string clientIp = GetClientIp(context);

        logger.LogError("SQL injection attempt detected from IP: {ClientIp} in {Location}. URL: {Url}",
            clientIp, location, context.Request.Path);

        // Log additional details for forensics
        logger.LogInformation("SQL injection attempt details - User-Agent: {UserAgent}, Referer: {Referer}",
            context.Request.Headers.UserAgent.ToString(),
            context.Request.Headers.Referer.ToString());

        context.Response.StatusCode = 403; // Forbidden
        context.Response.ContentType = "application/json";

        object response = new
        {
            error = "Security Violation",
            message = "SQL injection attempt detected and blocked",
            timestamp = DateTime.UtcNow,
            incidentId = Guid.NewGuid().ToString()
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }

    private static bool ShouldSkipProtection(HttpContext context)
    {
        string path = context.Request.Path.Value?.ToUpperInvariant() ?? string.Empty;
        return ExcludedPaths.Any(excluded =>
            path.StartsWith(excluded.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));
    }

    private static string GetClientIp(HttpContext context)
    {
        string? realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                        context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();

        return realIp ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
