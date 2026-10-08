using backend.DTOs;
using backend.Gameplay;
using backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace backend.Tests.Gameplay;

public sealed class SessionTickerTests : IDisposable
{
    private readonly GameplayFixture _fx = new();

    private SessionTicker CreateTicker(Mock<IClientProxy>? groupProxy = null)
    {
        groupProxy ??= new Mock<IClientProxy>();
        var hubContext = new Mock<IHubContext<GameSessionHub>>();
        hubContext.Setup(h => h.Clients.Group(It.IsAny<string>())).Returns(groupProxy.Object);
        return new SessionTicker(
            _fx.ScopeFactory,
            hubContext.Object,
            NullLogger<SessionTicker>.Instance,
            TimeSpan.FromSeconds(15));
    }

    [Fact]
    public async Task Tick_OpenSession_AdvancesNumberAndBroadcasts()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        var group = new Mock<IClientProxy>();
        var ticker = CreateTicker(group);

        await ticker.TickAsync();

        var latest = await _fx.Store.GetSnapshotAsync(snapshot.Id);
        Assert.Equal(2, latest!.CurrentRound);
        Assert.InRange(latest.CurrentNumber, 1, 100);
        group.Verify(c => c.SendCoreAsync(
            HubMessages.NumberAdvanced,
            It.Is<object[]>(args =>
                args.Length == 1
                && ((NumberAdvancedPayload)args[0]).Round == 2
                && ((NumberAdvancedPayload)args[0]).Number == latest.CurrentNumber),
            default));
        var used = await _fx.Redis.GetUsedNumbersAsync(snapshot.Id);
        Assert.Equal(2, used!.Count);
    }

    [Fact]
    public async Task Tick_ExpiredSession_FinishesAndBroadcastsFinalScores()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 30);
        await _fx.Store.RecordAnswerAsync(snapshot.Id, ann.Id, 1, true);
        var session = await _fx.Context.Sessions.FindAsync(snapshot.Id);
        session!.EndTimeUtc = DateTime.UtcNow.AddSeconds(-1);
        await _fx.Context.SaveChangesAsync();
        _fx.Context.ChangeTracker.Clear();
        var group = new Mock<IClientProxy>();
        var ticker = CreateTicker(group);

        await ticker.TickAsync();

        var latest = await _fx.Store.GetSnapshotAsync(snapshot.Id);
        Assert.Equal(Models.SessionStatus.Finished, latest!.Status);
        group.Verify(c => c.SendCoreAsync(
            HubMessages.SessionEnded,
            It.Is<object[]>(args =>
                args.Length == 1
                && ((SessionEndedPayload)args[0]).FinalScores.Single().Score == 10),
            default));
        group.Verify(c => c.SendCoreAsync(
            HubMessages.NumberAdvanced, It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Tick_NoOpenSessions_BroadcastsNothing()
    {
        var group = new Mock<IClientProxy>();
        var ticker = CreateTicker(group);

        await ticker.TickAsync();

        group.Verify(c => c.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    public void Dispose() => _fx.Dispose();
}
