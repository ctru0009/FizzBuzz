using backend.Data;
using backend.DTOs;
using backend.Gameplay;
using backend.Interfaces.Cache;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests.Gameplay;

public sealed class FakeRedisCachingService : IRedisCachingService
{
    private readonly Dictionary<int, List<int>> _used = [];

    public Task<IReadOnlyList<int>?> GetUsedNumbersAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<int>? result = _used.TryGetValue(sessionId, out var list) ? list.ToList() : null;
        return Task.FromResult(result);
    }

    public Task AddUsedNumberAsync(int sessionId, int number, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (!_used.TryGetValue(sessionId, out var list))
        {
            list = [];
            _used[sessionId] = list;
        }

        list.Add(number);
        return Task.CompletedTask;
    }
}

public sealed class GameplayFixture : IDisposable
{
    private readonly string _databaseName = $"gameplay-{Guid.NewGuid()}";

    public GameplayFixture()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<BackendAppDbContext>(options =>
            options.UseInMemoryDatabase(_databaseName));
        services.AddScoped<IGameRuleService, GameRuleService>();
        services.AddSingleton<IRedisCachingService, FakeRedisCachingService>();
        services.AddScoped<IGameSessionStore, GameSessionStore>();
        Provider = services.BuildServiceProvider();
        Scope = Provider.CreateScope();
        Context = Scope.ServiceProvider.GetRequiredService<BackendAppDbContext>();
        Rules = Scope.ServiceProvider.GetRequiredService<IGameRuleService>();
        Store = Scope.ServiceProvider.GetRequiredService<IGameSessionStore>();
        Redis = (FakeRedisCachingService)Scope.ServiceProvider.GetRequiredService<IRedisCachingService>();
        ScopeFactory = Provider.GetRequiredService<IServiceScopeFactory>();
    }

    public ServiceProvider Provider { get; }

    public IServiceScope Scope { get; }

    public BackendAppDbContext Context { get; }

    public IGameRuleService Rules { get; }

    public IGameSessionStore Store { get; }

    public FakeRedisCachingService Redis { get; }

    public IServiceScopeFactory ScopeFactory { get; }

    public async Task<Player> AddPlayerAsync(string name)
    {
        var player = new Player
        {
            Name = name,
            PasswordHash = "test-hash",
            CreatedAt = DateTime.UtcNow,
        };
        Context.Players.Add(player);
        await Context.SaveChangesAsync();
        return player;
    }

    public async Task<Game> AddGameAsync(
        int playerId,
        string authorName,
        int start = 1,
        int end = 100,
        params (int DivisibleBy, string Word)[] rules)
    {
        var game = new Game
        {
            Name = "Test game",
            PlayerId = playerId,
            AuthorName = authorName,
            StartRange = start,
            EndRange = end,
            CreatedAt = DateTime.UtcNow,
        };
        Context.Games.Add(game);
        await Context.SaveChangesAsync();
        foreach (var (divisibleBy, word) in rules)
        {
            Context.Rules.Add(new Rule
            {
                GameId = game.Id,
                DivisibleBy = divisibleBy,
                ReplacementWord = word,
            });
        }

        await Context.SaveChangesAsync();
        return game;
    }

    public void Dispose()
    {
        Scope.Dispose();
        Provider.Dispose();
    }
}
