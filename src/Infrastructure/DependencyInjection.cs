using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Caching;
using Application.Abstractions.Data;
using Application.Abstractions.Storage;
using Azure.Storage.Blobs;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.BackgroundJobs;
using Infrastructure.Caching;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.Services;
using Infrastructure.Storage;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration) =>
        services
            .AddServices()
            .AddDatabase(configuration)
            .AddHealthChecks(configuration)
            .AddAuthenticationInternal(configuration)
            .AddAuthorizationInternal()
            .AddHangfire(configuration)
            .AddHangfireJobs();

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        // يمكن تكوين المنطقة الزمنية من appsettings.json
        services.AddSingleton<IDateTimeProvider>(sp =>
        {
            IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
            string? timeZoneId = configuration["TimeZone:Id"] ?? "Asia/Baghdad";
            return new DateTimeProvider(timeZoneId);
        });

        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        // Attendance Services
        services.AddScoped<Infrastructure.Services.Interfaces.IAttendanceProcessingService, AttendanceProcessingService>();
        services.AddScoped<Infrastructure.Services.Interfaces.IAttendanceValidationService, AttendanceValidationService>();
        services.AddScoped<Infrastructure.Services.Interfaces.IAttendanceDataSyncService, AttendanceDataSyncService>();
        services.AddScoped<Infrastructure.Services.Interfaces.IEmployeeExemptionService, EmployeeExemptionService>();

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? sqlServerConnectionString = configuration.GetConnectionString("sqlServer");

        string? connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<ApplicationDbContext>(
            options => options
                .UseNpgsql(connectionString, npgsqlOptions =>
                    npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default))
                .UseSnakeCaseNamingConvention());

        // SQL Server - External Database (Read-Only for EventTab)
        services.AddDbContext<ExternalAttendanceDbContext>(options =>
            options.UseSqlServer(sqlServerConnectionString));


        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddSingleton<IBlobService, BlobService>();
        services.AddSingleton(_ => new BlobServiceClient(configuration.GetConnectionString("BlobStorage")));

        string redisConnectionString = configuration.GetConnectionString("Cache")!;

        services.AddStackExchangeRedisCache(options =>
            options.Configuration = redisConnectionString);

        services.AddSingleton<ICacheService, CacheService>();


        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Database")!);

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<IHasPermission, HasPermission>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenProvider, TokenProvider>();

        return services;
    }

    private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddScoped<PermissionProvider>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddTransient<IAuthorizationHandler, MultiplePermissionsAuthorizationHandler>();

        // Register HttpClient for HikvisionService
        services.AddHttpClient<IHikvisionService, HikvisionService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30); // مهلة زمنية مناسبة للأجهزة
            client.DefaultRequestHeaders.Add("User-Agent", "AttendanceApp/1.0");
        });

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }
}
