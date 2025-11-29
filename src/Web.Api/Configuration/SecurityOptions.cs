namespace Web.Api.Configuration;

/// <summary>
/// Configuration options for security middleware
/// </summary>
public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    /// <summary>
    /// Rate limiting configuration
    /// </summary>
    public RateLimitingOptions RateLimiting { get; set; } = new();

    /// <summary>
    /// IP blocking configuration
    /// </summary>
    public IpBlockingOptions IpBlocking { get; set; } = new();

    /// <summary>
    /// Request validation configuration
    /// </summary>
    public RequestValidationOptions RequestValidation { get; set; } = new();

    /// <summary>
    /// Security headers configuration
    /// </summary>
    public SecurityHeadersOptions SecurityHeaders { get; set; } = new();
}

public sealed class RateLimitingOptions
{
    /// <summary>
    /// Maximum requests per minute per client
    /// </summary>
    public int MaxRequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// Maximum requests per hour per client
    /// </summary>
    public int MaxRequestsPerHour { get; set; } = 1000;

    /// <summary>
    /// Block duration in minutes
    /// </summary>
    public int BlockDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Enable rate limiting
    /// </summary>
    public bool Enabled { get; set; } = true;
}

public sealed class IpBlockingOptions
{
    /// <summary>
    /// Maximum failed attempts before blocking
    /// </summary>
    public int MaxFailedAttemptsBeforeBlock { get; set; } = 10;

    /// <summary>
    /// Suspicious activity window in minutes
    /// </summary>
    public int SuspiciousActivityWindowMinutes { get; set; } = 15;

    /// <summary>
    /// Block duration in hours
    /// </summary>
    public int BlockDurationHours { get; set; } = 24;

    /// <summary>
    /// Enable IP blocking
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Whitelisted IP addresses
    /// </summary>
    public List<string> WhitelistedIps { get; set; } = [];
}

public sealed class RequestValidationOptions
{
    /// <summary>
    /// Maximum request size in bytes
    /// </summary>
    public int MaxRequestSize { get; set; } = 10 * 1024 * 1024; // 10MB

    /// <summary>
    /// Maximum header length in bytes
    /// </summary>
    public int MaxHeaderLength { get; set; } = 8192; // 8KB

    /// <summary>
    /// Maximum URL length in characters
    /// </summary>
    public int MaxUrlLength { get; set; } = 2048; // 2KB

    /// <summary>
    /// Enable request validation
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Paths to exclude from validation
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = [];
}

public sealed class SecurityHeadersOptions
{
    /// <summary>
    /// Enable security headers
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Content Security Policy
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = string.Empty;

    /// <summary>
    /// Strict Transport Security max age
    /// </summary>
    public int StrictTransportSecurityMaxAge { get; set; } = 31536000; // 1 year

    /// <summary>
    /// Enable HSTS preload
    /// </summary>
    public bool EnableHstsPreload { get; set; } = true;

    /// <summary>
    /// Custom security headers
    /// </summary>
    public Dictionary<string, string> CustomHeaders { get; set; } = [];
}
