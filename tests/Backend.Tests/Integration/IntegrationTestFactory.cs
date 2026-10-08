using System.Threading.RateLimiting;
using backend.Data;
using backend.Gameplay;
using backend.Hubs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace backend.Tests.Integration;

public sealed class IntegrationTestFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string SigningKey = "integration-test-signing-key-32-chars-minimum-ok";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("fizzbuzz_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder("redis:7-alpine")
        .Build();

    public string PostgresConnectionString => _postgres.GetConnectionString();

    public string RedisConnectionString => _redis.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();

        // Program.Main validates configuration eagerly at CreateBuilder time, before any
        // WebApplicationFactory config callback runs, so the fixture feeds the app through
        // environment variables, exactly like production config.
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", PostgresConnectionString);
        Environment.SetEnvironmentVariable("ConnectionStrings__Redis", RedisConnectionString);
        Environment.SetEnvironmentVariable("Jwt__SigningKey", SigningKey);
        Environment.SetEnvironmentVariable("Game__NumberIntervalSeconds", "120");
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:3000");

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BackendAppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Preserve contract HubException messages over the wire. Production gates
        // EnableDetailedErrors on Development, so the test host must match or every
        // HubException arrives masked as an unexpected error.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<HubOptions>(options => options.EnableDetailedErrors = true);

            // The auth-strict rate limit partitions by client IP, and every TestServer
            // request shares one bucket, so the suite would 429 itself. Replace the
            // policy entries with generous limits. Auth, antiforgery, and CORS stay
            // exactly as shipped.
            services.AddRateLimiterOverride();

            // Keep the prod SessionTicker type (the hosted-service cast in AddGameplay
            // requires it) but neuter the background cadence so only explicit TickAsync
            // calls in tests advance sessions. The last registration wins for both the
            // hosted service and direct resolution.
            services.AddSingleton<ISessionTicker>(provider =>
                new SessionTicker(
                    provider.GetRequiredService<IServiceScopeFactory>(),
                    provider.GetRequiredService<IHubContext<GameSessionHub>>(),
                    provider.GetRequiredService<ILogger<SessionTicker>>(),
                    TimeSpan.FromDays(1)));
        });
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BackendAppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE \"SessionAnswers\", \"SessionParticipants\", \"Sessions\", \"Rules\", \"Games\", \"Players\" RESTART IDENTITY CASCADE");
    }
}

internal static class RateLimiterTestOverrides
{
    public static IServiceCollection AddRateLimiterOverride(this IServiceCollection services)
    {
        services.Configure<RateLimiterOptions>(options =>
        {
            Func<HttpContext, RateLimitPartition<string>> generous = _ =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: "integration-tests",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100000,
                        Window = TimeSpan.FromMinutes(10),
                    });

            // Remove then re-add, because AddPolicy throws on duplicate names.
            RemovePolicy(options, Program.AuthStrictPolicy);
            options.AddPolicy(Program.AuthStrictPolicy, generous);
            RemovePolicy(options, Program.HubBasicPolicy);
            options.AddPolicy(Program.HubBasicPolicy, generous);
        });
        return services;
    }

    private static void RemovePolicy(RateLimiterOptions options, string policyName)
    {
        var property = typeof(RateLimiterOptions).GetProperty(
            "PolicyMap",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (property?.GetValue(options) is System.Collections.IDictionary map)
        {
            map.Remove(policyName);
        }
    }
}
