using System.Collections.Concurrent;
using System.Net;

namespace Web.Api.Middleware;

/// <summary>
/// Middleware for rate limiting to prevent DDoS attacks and excessive requests
/// </summary>
public sealed class RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
{
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();

    private const int MaxRequestsPerMinute = 200;
    private const int MaxRequestsPerHour = 1000;
    private const int BlockDurationMinutes = 5;

    public async Task InvokeAsync(HttpContext context)
    {
        string clientId = GetClientIdentifier(context);

        if (IsRateLimited(clientId))
        {
            logger.LogWarning("Rate limit exceeded for client: {ClientId}", clientId);

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers.Append("Retry-After", "60");

            await context.Response.WriteAsync("Rate limit exceeded. Please try again later.");
            return;
        }

        await next(context);
    }

    private static string GetClientIdentifier(HttpContext context)
    {
        // Try to get real IP from headers first (for reverse proxy scenarios)
        string? realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                        context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();

        return realIp ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool IsRateLimited(string clientId)
    {
        DateTime now = DateTime.UtcNow;

        ClientRequestInfo clientInfo = _clients.AddOrUpdate(clientId,
            new ClientRequestInfo { FirstRequest = now, RequestCount = 1, LastRequest = now },
            (key, existing) =>
            {
                // Reset if hour window has passed
                if (now.Subtract(existing.FirstRequest).TotalHours >= 1)
                {
                    return new ClientRequestInfo { FirstRequest = now, RequestCount = 1, LastRequest = now };
                }

                // Check if client is blocked
                if (existing.IsBlocked && now.Subtract(existing.BlockedUntil).TotalMinutes < BlockDurationMinutes)
                {
                    return existing;
                }

                // Update request info
                existing.RequestCount++;
                existing.LastRequest = now;

                // Block if limits exceeded
                if (existing.RequestCount > MaxRequestsPerHour ||
                    now.Subtract(existing.LastRequest).TotalMinutes <= 1 && existing.RequestCount > MaxRequestsPerMinute)
                {
                    existing.IsBlocked = true;
                    existing.BlockedUntil = now;
                }

                return existing;
            });

        return clientInfo.IsBlocked && now.Subtract(clientInfo.BlockedUntil).TotalMinutes < BlockDurationMinutes;
    }


    private sealed class ClientRequestInfo
    {
        public DateTime FirstRequest { get; set; }
        public int RequestCount { get; set; }
        public DateTime LastRequest { get; set; }
        public bool IsBlocked { get; set; }
        public DateTime BlockedUntil { get; set; }
    }
}
