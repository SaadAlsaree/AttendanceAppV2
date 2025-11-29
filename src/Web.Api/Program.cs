using System.Reflection;
using Application;
using Hangfire;
using Hangfire.Dashboard;
using HealthChecks.UI.Client;
using Infrastructure;
using Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Web.Api;
using Web.Api.Endpoints.Security;
using Web.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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
    option.AddPolicy("AllowAll", policy =>
        policy.WithOrigins("http://localhost:3000", "http://192.168.141.156:3000", "http://fp28.inss.local", "http://fp28.inss.local:3000", "http://192.168.25.207:3000") // Add your Flutter app URL here
              .AllowAnyHeader()
              .AllowAnyMethod()
    )
);

// Register anti-forgery services
builder.Services.AddAntiforgery();

WebApplication app = builder.Build();

app.MapEndpoints();

// Add security test endpoints (only in development)
if (app.Environment.IsDevelopment())
{
    app.MapSecurityTestEndpoints();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerWithUi();

    app.ApplyMigrations();

    // Hangfire Dashboard - available only in development
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = Array.Empty<IDashboardAuthorizationFilter>() // No auth in development
    });
}



app.MapHealthChecks("health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseSerilogRequestLogging();

app.UseExceptionHandler();

// Add CORS middleware
app.UseCors("AllowAll");

app.UseAuthentication();

app.UseAuthorization();

// Add anti-forgery middleware
app.UseAntiforgery();

// Security middleware - order is important (after authentication)
app.UseSecurityMiddleware();

// REMARK: If you want to use Controllers, you'll need this.
app.MapControllers();

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
