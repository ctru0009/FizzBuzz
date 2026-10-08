using backend.Hubs;
using backend.Interfaces;
using backend.Interfaces.Cache;
using backend.Services;
using backend.Services.Cache;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace backend.Gameplay
{
    public static class GameplayServiceExtensions
    {
        public static IServiceCollection AddGameplay(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<GameOptions>(configuration.GetSection(GameOptions.SectionName));
            services.AddScoped<IGame, GameService>();
            services.AddScoped<IGameSessionStore, GameSessionStore>();
            services.AddScoped<IGameRuleService, GameRuleService>();
            services.AddScoped<IRedisCachingService, RedisCachingService>();
            services.AddSingleton<ISessionTicker>(provider =>
                new SessionTicker(
                    provider.GetRequiredService<IServiceScopeFactory>(),
                    provider.GetRequiredService<IHubContext<GameSessionHub>>(),
                    provider.GetRequiredService<ILogger<SessionTicker>>(),
                    TimeSpan.FromSeconds(provider.GetRequiredService<IOptions<GameOptions>>().Value.NumberIntervalSeconds)));
            services.AddHostedService(provider => (SessionTicker)provider.GetRequiredService<ISessionTicker>());
            return services;
        }
    }
}
