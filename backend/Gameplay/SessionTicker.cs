using backend.DTOs;
using backend.Hubs;
using backend.Interfaces.Cache;
using Microsoft.AspNetCore.SignalR;

namespace backend.Gameplay
{
    public sealed class SessionTicker : BackgroundService, ISessionTicker
    {
        private static readonly TimeSpan RedisGracePeriod = TimeSpan.FromMinutes(5);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<GameSessionHub> _hub;
        private readonly ILogger<SessionTicker> _logger;
        private readonly TimeSpan _interval;

        public SessionTicker(
            IServiceScopeFactory scopeFactory,
            IHubContext<GameSessionHub> hub,
            ILogger<SessionTicker> logger,
            TimeSpan interval)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _logger = logger;
            _interval = interval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await TickAsync(stoppingToken);
                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        public async Task TickAsync(CancellationToken cancellationToken = default)
        {
            using var scope = _scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IGameSessionStore>();
            var rules = scope.ServiceProvider.GetRequiredService<IGameRuleService>();
            var redis = scope.ServiceProvider.GetRequiredService<IRedisCachingService>();

            IReadOnlyList<OpenSessionState> open;
            try
            {
                open = await store.ListOpenSessionStatesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session ticker failed to list open sessions.");
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var state in open)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    await TickSessionAsync(store, rules, redis, state, now, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Session ticker failed for session {SessionId}.", state.SessionId);
                }
            }
        }

        private async Task TickSessionAsync(
            IGameSessionStore store,
            IGameRuleService rules,
            IRedisCachingService redis,
            OpenSessionState state,
            DateTime now,
            CancellationToken cancellationToken)
        {
            if (now >= state.EndTimeUtc)
            {
                var finalScores = await store.FinishSessionAsync(state.SessionId, cancellationToken);
                if (finalScores is not null)
                {
                    await _hub.Clients.Group(SessionGroups.Name(state.SessionId))
                        .SendAsync(HubMessages.SessionEnded, new SessionEndedPayload(finalScores), cancellationToken);
                }

                return;
            }

            var used = await redis.GetUsedNumbersAsync(state.SessionId, cancellationToken) ?? [];
            var number = rules.PickNumber(state.StartRange, state.EndRange, used);
            var advanced = await store.AdvanceSessionAsync(state.SessionId, number, cancellationToken);
            if (advanced is null)
            {
                return;
            }

            var ttl = state.EndTimeUtc - now + RedisGracePeriod;
            await redis.AddUsedNumberAsync(state.SessionId, number, ttl, cancellationToken);
            var advancesAtUtc = DateTime.UtcNow + _interval;
            await _hub.Clients.Group(SessionGroups.Name(state.SessionId))
                .SendAsync(HubMessages.NumberAdvanced, new NumberAdvancedPayload(advanced.Number, advanced.Round, advanced.EndsAtUtc, advancesAtUtc), cancellationToken);
        }
    }
}
