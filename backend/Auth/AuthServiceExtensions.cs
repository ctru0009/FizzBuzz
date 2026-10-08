using System.IdentityModel.Tokens.Jwt;
using System.Text;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace backend.Auth
{
    public static class AuthServiceExtensions
    {
        public static IServiceCollection AddGameAuth(this IServiceCollection services, IConfiguration configuration)
        {
            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            services.Configure<JwtOptions>(options =>
            {
                options.Issuer = configuration["Jwt:Issuer"] ?? "fizzbuzz";
                options.Audience = configuration["Jwt:Audience"] ?? "fizzbuzz";
                options.SigningKey = configuration["Jwt:SigningKey"] ?? string.Empty;
                options.LifetimeHours = configuration.GetValue<int?>("Jwt:LifetimeHours") ?? 8;
            });

            services.AddSingleton<TokenService>();
            services.AddScoped<IPasswordHasher<Player>, PasswordHasher<Player>>();
            services.AddScoped<IPlayer, PlayerService>();

            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<Microsoft.Extensions.Options.IOptions<JwtOptions>>((bearer, jwt) =>
                {
                    var value = jwt.Value;
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = value.Issuer,
                        ValidateAudience = true,
                        ValidAudience = value.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(value.SigningKey)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                    };
                    bearer.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var token = context.Request.Cookies[AuthCookie.Name];
                            if (string.IsNullOrEmpty(token)
                                && context.Request.Headers.TryGetValue("Authorization", out var header))
                            {
                                var auth = header.ToString();
                                if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                                {
                                    token = auth["Bearer ".Length..].Trim();
                                }
                            }

                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(token)
                                && (path.StartsWithSegments("/api") || path.StartsWithSegments("/sessionHub")))
                            {
                                context.Token = token;
                            }

                            return Task.CompletedTask;
                        },
                    };
                });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            services.AddAuthorization();

            return services;
        }
    }
}
