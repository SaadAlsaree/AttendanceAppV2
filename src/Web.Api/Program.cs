using System.Reflection;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Application;
using Hangfire;
using Hangfire.Dashboard;
using HealthChecks.UI.Client;
using Infrastructure;
using Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Web.Api;
using Web.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Do not advertise the server implementation (information disclosure hardening).
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddApplication()
    .AddPresentation(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

// Add Swagger only in development environment
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGenWithAuth();
}

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

builder.Services.AddCors(option =>
    option.AddPolicy("RestrictedOrigins", policy =>
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:3003", // local dev/E2E frontend (3000 may be taken by another stack)
            "http://10.42.10.67:3000",
            "http://fp28.inss.local",
            "https://fp28.inss.local",
            "http://fp28.inss.local:3000",
            "http://192.168.25.207:3000") // Add your Flutter app URL here
              .AllowAnyHeader()
              .AllowAnyMethod()
    )
);

builder.Services.AddRateLimiter(cfg =>
{
    cfg.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    cfg.AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 5;
        options.Window = TimeSpan.FromMinutes(1);

    });

    cfg.AddPolicy("per-user", httpContext =>
    {
        string? userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrWhiteSpace(userId))
        {
            return RateLimitPartition.GetTokenBucketLimiter(userId, _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 50,
                TokensPerPeriod = 25,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1)
            });
        }

        return RateLimitPartition.GetFixedWindowLimiter("anonymous", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1)
        });
    });
});

// Register anti-forgery services
builder.Services.AddAntiforgery();

WebApplication app = builder.Build();

app.MapEndpoints();

// Add security test endpoints (only in development)


if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();

    app.ApplyMigrations();
}



app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

// Add CORS middleware
app.UseCors("RestrictedOrigins");

app.UseAuthentication();

app.UseAuthorization();

// Hangfire Dashboard - Development only, Admin/SuperAdmin only. Must be registered AFTER
// UseAuthentication/UseAuthorization so the dashboard authorization filter sees the
// authenticated principal (a Bearer token works, which the E2E trigger calls rely on).
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new Web.Api.Infrastructure.HangfireDashboardAuthorizationFilter()],
        IgnoreAntiforgeryToken = true
    });
}

// Add anti-forgery middleware
app.UseAntiforgery();

// Security middleware - order is important (after authentication)
app.UseSecurityMiddleware();

// REMARK: If you want to use Controllers, you'll need this.
app.MapControllers();

// Rate limiting is disabled in Development so the local E2E/test workflow (rapid
// scripted requests) isn't throttled. Without this middleware the per-endpoint
// .RequireRateLimiting metadata is a no-op. Production keeps the limiter.
if (!app.Environment.IsDevelopment())
{
    app.UseRateLimiter();
}

// Schedule Hangfire recurring jobs
using (IServiceScope scope = app.Services.CreateScope())
{
    HangfireJobScheduler jobScheduler = scope.ServiceProvider.GetRequiredService<HangfireJobScheduler>();
    jobScheduler.ScheduleRecurringJobs();
}

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
namespace Web.Api
{
    public partial class Program;
}
