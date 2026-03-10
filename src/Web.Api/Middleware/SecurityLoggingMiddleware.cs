namespace Web.Api.Middleware;

/// <summary>
/// Advanced security logging middleware for monitoring and forensics
/// </summary>
public sealed class SecurityLoggingMiddleware(RequestDelegate next, ILogger<SecurityLoggingMiddleware> logger)
{
    private static readonly string[] SensitiveHeaders = [
        "Authorization", "Cookie", "X-API-Key", "X-Auth-Token", "X-Access-Token"
    ];

    private static readonly string[] SuspiciousUserAgents = [
        "sqlmap", "nikto", "nmap", "masscan", "zap", "burp", "gobuster", "dirb"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        // Start security logging
        await LogSecurityInfo(context);

        // Monitor response
        Stream originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await next(context);

            // Log security response info
            await LogSecurityResponse(context, responseBody);

            // Copy response back
            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
        }
        finally
        {
            // Ensure original stream is restored even if an exception occurs
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task LogSecurityInfo(HttpContext context)
    {
        try
        {
            HttpRequest request = context.Request;
            string clientIp = GetClientIp(context);

            // Log basic request info
            //logger.LogInformation("Security Request: {Method} {Path} from {ClientIp} - UserAgent: {UserAgent}",
            //    request.Method, request.Path, clientIp, request.Headers.UserAgent.ToString());

            // Check for suspicious user agents
            string userAgent = request.Headers.UserAgent.ToString().ToUpperInvariant();
            if (SuspiciousUserAgents.Any(agent => userAgent.Contains(agent)))
            {
                logger.LogWarning("Suspicious User-Agent detected: {UserAgent} from {ClientIp}",
                    userAgent, clientIp);
            }

            // Log authentication info
            if (request.Headers.ContainsKey("Authorization"))
            {
                //string authType = request.Headers.Authorization.ToString().Split(' ')[0];
                //logger.LogInformation("Authentication attempt with {AuthType} from {ClientIp}", authType, clientIp);
            }

            // Log potential attack indicators
            await LogPotentialAttacks(context);

            // Log geolocation if available (simplified)
            LogGeolocation(context, clientIp);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in security logging");
        }
    }

    private async Task LogPotentialAttacks(HttpContext context)
    {
        HttpRequest request = context.Request;
        string clientIp = GetClientIp(context);

        // Check for common attack patterns in URL
        string fullUrl = request.Path + request.QueryString;
        if (ContainsAttackPatterns(fullUrl))
        {
            logger.LogWarning("Potential attack pattern in URL: {Url} from {ClientIp}", fullUrl, clientIp);
        }

        // Check headers for attacks
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in request.Headers)
        {
            if (!SensitiveHeaders.Contains(header.Key) && ContainsAttackPatterns(header.Value.ToString()))
            {
                logger.LogWarning("Potential attack pattern in header {HeaderName}: {HeaderValue} from {ClientIp}",
                    header.Key, header.Value, clientIp);
            }
        }

        // Check form data if present
        if (request.HasFormContentType && request.ContentLength < 1024 * 1024) // Only for small forms
        {
            try
            {
                IFormCollection form = await request.ReadFormAsync();
                foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> field in form)
                {
                    if (ContainsAttackPatterns(field.Value.ToString()))
                    {
                        logger.LogWarning("Potential attack pattern in form field {FieldName} from {ClientIp}",
                            field.Key, clientIp);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not read form data for security logging");
            }
        }
    }

    private async Task LogSecurityResponse(HttpContext context, MemoryStream responseBody)
    {
        try
        {
            string clientIp = GetClientIp(context);
            int statusCode = context.Response.StatusCode;

            // Log security-relevant status codes
            if (statusCode == 401 || statusCode == 403 || statusCode == 429)
            {
                logger.LogWarning("Security response {StatusCode} sent to {ClientIp} for {Path}",
                    statusCode, clientIp, context.Request.Path);
            }

            // Log failed authentication attempts
            if (statusCode == 401)
            {
                logger.LogWarning("Authentication failed from {ClientIp} - Path: {Path}, UserAgent: {UserAgent}",
                    clientIp, context.Request.Path, context.Request.Headers.UserAgent);
            }

            // Log access denied
            if (statusCode == 403)
            {
                logger.LogWarning("Access denied for {ClientIp} - Path: {Path}, Method: {Method}",
                    clientIp, context.Request.Path, context.Request.Method);
            }

            // Log error responses that might indicate attacks
            if (statusCode >= 400 && statusCode < 500)
            {
                try
                {
                    long originalPosition = responseBody.Position;
                    responseBody.Seek(0, SeekOrigin.Begin);

                    using var reader = new StreamReader(responseBody, leaveOpen: true);
                    string responseContent = await reader.ReadToEndAsync();

                    // Reset position for later use
                    responseBody.Seek(originalPosition, SeekOrigin.Begin);

                    if (responseContent.Length > 0 && responseContent.Length < 1000) // Only log small responses
                    {
                        //logger.LogInformation("Client error {StatusCode} response to {ClientIp}: {Response}",
                        //    statusCode, clientIp, responseContent);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "Could not read response body for security logging");
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error logging security response");
        }
    }

    private static bool ContainsAttackPatterns(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        string upperInput = input.ToUpperInvariant();

        // Common attack patterns
        string[] patterns = [
            "<script", "javascript:", "onload=", "onerror=",
            "union select", "drop table", "' or '1'='1",
            "../", "%2e%2e", "eval(", "exec(",
            "cmd.exe", "/bin/sh", "cat /etc/passwd",
            "<?php", "<%", "%00"
        ];

        return patterns.Any(pattern => upperInput.Contains(pattern));
    }

    private void LogGeolocation(HttpContext context, string clientIp)
    {
        try
        {
            // Check for CloudFlare headers
            string? country = context.Request.Headers["CF-IPCountry"].FirstOrDefault();
            if (!string.IsNullOrEmpty(country))
            {
                //logger.LogInformation("Request from {Country} - IP: {ClientIp}", country, clientIp);
            }

            // Check for other common geolocation headers
            string? geoInfo = context.Request.Headers["X-Forwarded-Country"].FirstOrDefault() ??
                             context.Request.Headers["X-Country-Code"].FirstOrDefault();

            if (!string.IsNullOrEmpty(geoInfo))
            {
                //logger.LogInformation("Geo info: {GeoInfo} for IP: {ClientIp}", geoInfo, clientIp);
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Error logging geolocation info");
        }
    }

    private static string GetClientIp(HttpContext context)
    {
        string? realIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault() ??
                        context.Request.Headers["X-Real-IP"].FirstOrDefault() ??
                        context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim();

        return realIp ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
