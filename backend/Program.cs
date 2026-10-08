using System.Text.Json;
using System.Threading.RateLimiting;
using backend.Auth;
using backend.Data;
using backend.Gameplay;
using backend.Health;
using backend.Hubs;
using backend.Middleware;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace backend
{
    public class Program
    {
        public const string CorsPolicy = "GameCors";
        public const string AuthStrictPolicy = "auth-strict";
        public const string HubBasicPolicy = "hub-basic";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ValidateStartupConfiguration(builder.Configuration);

            builder.Services.AddControllersWithViews(options =>
            {
                options.Conventions.Add(new AuthRateLimitConvention());
                options.Conventions.Add(new AntiforgeryExemptionConvention());
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            });
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var problem = new ValidationProblemDetails(context.ModelState)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "One or more validation errors occurred.",
                        Instance = context.HttpContext.Request.Path
                    };
                    return new BadRequestObjectResult(problem)
                    {
                        ContentTypes = { "application/problem+json" }
                    };
                };
            });

            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-XSRF-TOKEN";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddSignalR(options =>
            {
                options.MaximumReceiveMessageSize = 1024 * 1024;
                options.EnableDetailedErrors = builder.Environment.IsDevelopment();
            });

            var allowedOrigins = builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? [];
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicy, policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            builder.Services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy(AuthStrictPolicy, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1)
                        }));
                options.AddPolicy(HubBasicPolicy, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 60,
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });

            builder.Services.AddDbContext<BackendAppDbContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis");
            });

            builder.Services.AddGameAuth(builder.Configuration);
            builder.Services.AddGameplay(builder.Configuration);

            builder.Services.AddHealthChecks()
                .AddCheck<PostgresHealthCheck>("postgres")
                .AddCheck<RedisHealthCheck>("redis");

            var app = builder.Build();

            app.UseMiddleware<ProblemDetailsExceptionMiddleware>();
            app.UseStatusCodePages(async statusCodeContext =>
            {
                var response = statusCodeContext.HttpContext.Response;
                if (response.HasStarted || (response.ContentLength.HasValue && response.ContentLength.Value > 0))
                {
                    return;
                }

                response.ContentType = "application/problem+json";
                var problem = new ProblemDetails
                {
                    Status = response.StatusCode,
                    Title = ReasonPhrases.GetReasonPhrase(response.StatusCode) ?? "An error occurred.",
                    Instance = statusCodeContext.HttpContext.Request.Path
                };
                await response.WriteAsync(JsonSerializer.Serialize(problem));
            });
            app.UseHttpsRedirection();
            app.UseCors(CorsPolicy);
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseAntiforgery();
            app.UseRateLimiter();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapControllers();

            app.MapGet("/api/antiforgery/token", (IAntiforgery antiforgery, HttpContext context) =>
            {
                var tokens = antiforgery.GetAndStoreTokens(context);
                return Results.Ok(new { token = tokens.RequestToken });
            }).AllowAnonymous();

            app.MapHub<GameSessionHub>("/sessionHub")
                .RequireCors(CorsPolicy)
                .RequireRateLimiting(HubBasicPolicy)
                .RequireAuthorization();

            app.MapGet("/health/live", () => Results.Ok(new { status = "live" })).AllowAnonymous();

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    var payload = new
                    {
                        status = report.Status.ToString(),
                        checks = report.Entries.Select(entry => new
                        {
                            name = entry.Key,
                            status = entry.Value.Status.ToString(),
                            description = entry.Value.Description
                        })
                    };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
                }
            }).AllowAnonymous();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<BackendAppDbContext>();
                dbContext.Database.Migrate();
            }

            app.Run();
        }

        private static void ValidateStartupConfiguration(IConfiguration configuration)
        {
            var failures = new List<string>();

            var defaultConnection = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(defaultConnection))
            {
                failures.Add("ConnectionStrings:DefaultConnection is missing or empty.");
            }

            var redis = configuration.GetConnectionString("Redis");
            if (string.IsNullOrWhiteSpace(redis))
            {
                failures.Add("ConnectionStrings:Redis is missing or empty.");
            }

            var signingKey = configuration["Jwt:SigningKey"];
            if (string.IsNullOrWhiteSpace(signingKey))
            {
                failures.Add("Jwt:SigningKey is missing. Set it via the Jwt__SigningKey environment variable.");
            }
            else if (signingKey.Length < 32)
            {
                failures.Add("Jwt:SigningKey must be at least 32 characters.");
            }

            var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? [];
            if (allowedOrigins.Length == 0 || allowedOrigins.All(string.IsNullOrWhiteSpace))
            {
                failures.Add("Cors:AllowedOrigins is missing or empty.");
            }

            // Range and default come straight from docs/CONTRACTS.md so startup stays independent of lane internals.
            var interval = configuration.GetValue<int?>("Game:NumberIntervalSeconds") ?? 15;
            if (interval < 5 || interval > 120)
            {
                failures.Add("Game:NumberIntervalSeconds must be between 5 and 120.");
            }

            if (failures.Count > 0)
            {
                var message = "Startup configuration is invalid. Refusing to boot: " + string.Join(" ", failures);
                throw new InvalidOperationException(message);
            }
        }
    }
}
