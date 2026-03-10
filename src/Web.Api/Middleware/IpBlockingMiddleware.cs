using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace Web.Api.Middleware;

/// <summary>
/// Middleware for blocking suspicious IPs and preventing attacks
/// </summary>
public sealed class IpBlockingMiddleware(RequestDelegate next, ILogger<IpBlockingMiddleware> logger)
{
    private static readonly ConcurrentDictionary<string, SuspiciousActivity> _suspiciousIps = new();
    private static readonly HashSet<string> _blockedIps = [];

    private const int MaxFailedAttemptsBeforeBlock = 10;
    private const int SuspiciousActivityWindowMinutes = 15;
    private const int BlockDurationHours = 24;

    // Known suspicious patterns
    private static readonly string[] SuspiciousUserAgents =
    [
        "sqlmap", "nikto", "nmap", "masscan", "zap", "burp", "gobuster", "dirb", "dirbuster",
        "wfuzz", "ffuf", "hydra", "nessus", "openvas", "acunetix", "w3af", "skipfish"
    ];

    private static readonly string[] SuspiciousUrls =
    [
        "/wp-admin", "/phpmyadmin", "/admin", "/.env", "/config", "/backup", "/test",
        "/shell", "/cmd", "/eval", "/proc/", "/etc/passwd", "/var/log", "../",
        "SELECT", "UNION", "DROP", "INSERT", "UPDATE", "DELETE", "<script", "javascript:",
        "<?php", "<%", "%00", "%2e%2e", "%252f", "%c0%af"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        string clientIp = GetClientIp(context);

        // Check if IP is blocked
        if (IsIpBlocked(clientIp))
        {
            logger.LogWarning("Blocked IP attempted access: {ClientIp}", clientIp);
            await BlockRequest(context, "IP address is blocked due to suspicious activity");
            return;
        }

        // Check for suspicious activity
        if (IsSuspiciousRequest(context))
        {
            await HandleSuspiciousActivity(context, clientIp);
            return;
        }

        await next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        // Try to get real IP from headers first (for reverse proxy scenarios)
        string? realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                        context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() ??
                        context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();

        return realIp ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool IsIpBlocked(string clientIp)
    {
        return _blockedIps.Contains(clientIp);
    }

    private bool IsSuspiciousRequest(HttpContext context)
    {
        string userAgent = context.Request.Headers.UserAgent.ToString().ToUpperInvariant();
        string url = context.Request.Path.ToString().ToUpperInvariant();
        string query = context.Request.QueryString.ToString().ToUpperInvariant();
        string fullUrl = url + query;

        // Check user agent
        if (SuspiciousUserAgents.Any(agent => userAgent.Contains(agent)))
        {
            logger.LogWarning("Suspicious user agent detected: {UserAgent} from IP: {ClientIp}",
                userAgent, GetClientIp(context));
            return true;
        }

        // Check URL patterns
        if (SuspiciousUrls.Any(pattern => fullUrl.Contains(pattern)))
        {
            logger.LogWarning("Suspicious URL pattern detected: {Url} from IP: {ClientIp}",
                fullUrl, GetClientIp(context));
            return true;
        }

        // Check for empty user agent (common in bots)
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            logger.LogWarning("Empty user agent from IP: {ClientIp}", GetClientIp(context));
            return true;
        }

        // Check for too long URLs (potential buffer overflow attempts)
        if (fullUrl.Length > 2000)
        {
            logger.LogWarning("Extremely long URL detected from IP: {ClientIp}", GetClientIp(context));
            return true;
        }

        // Check request headers for common attack patterns
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in context.Request.Headers)
        {
            string headerValue = header.Value.ToString().ToUpperInvariant();
            if (SuspiciousUrls.Any(pattern => headerValue.Contains(pattern)))
            {
                logger.LogWarning("Suspicious header pattern detected in {HeaderName}: {HeaderValue} from IP: {ClientIp}",
                    header.Key, headerValue, GetClientIp(context));
                return true;
            }
        }

        return false;
    }

    private async Task HandleSuspiciousActivity(HttpContext context, string clientIp)
    {
        DateTime now = DateTime.UtcNow;

        SuspiciousActivity activity = _suspiciousIps.AddOrUpdate(clientIp,
            new SuspiciousActivity { FirstSeen = now, Count = 1, LastSeen = now },
            (key, existing) =>
            {
                // Reset if time window has passed
                if (now.Subtract(existing.FirstSeen).TotalMinutes > SuspiciousActivityWindowMinutes)
                {
                    return new SuspiciousActivity { FirstSeen = now, Count = 1, LastSeen = now };
                }

                existing.Count++;
                existing.LastSeen = now;
                return existing;
            });

        // Block IP if threshold reached
        if (activity.Count >= MaxFailedAttemptsBeforeBlock)
        {
            _blockedIps.Add(clientIp);
            logger.LogError("IP blocked due to repeated suspicious activity: {ClientIp} (Count: {Count})",
                clientIp, activity.Count);

            // Schedule unblocking
            _ = Task.Delay(TimeSpan.FromHours(BlockDurationHours)).ContinueWith(_ =>
            {
                _blockedIps.Remove(clientIp);
                //logger.LogInformation("IP unblocked after timeout: {ClientIp}", clientIp);
            }, TaskScheduler.Default);
        }

        await BlockRequest(context, "Suspicious activity detected");
    }

    private static async Task BlockRequest(HttpContext context, string reason)
    {
        context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "Access Denied",
            message = reason,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }


    private sealed class SuspiciousActivity
    {
        public DateTime FirstSeen { get; set; }
        public int Count { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
