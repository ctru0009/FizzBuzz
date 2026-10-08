using System.Security.Claims;
using backend.DTOs;
using backend.Gameplay;
using backend.Hubs;
using backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace backend.Tests.Gameplay;

public sealed class GameSessionHubTests : IDisposable
{
    private readonly GameplayFixture _fx = new();

    private GameSessionHub CreateHub(int playerId, Mock<IClientProxy>? groupProxy = null)
    {
        var hub = new GameSessionHub(_fx.Store, _fx.Rules, NullLogger<GameSessionHub>.Instance);
        var claims = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", playerId.ToString()), new Claim("name", $"Player{playerId}")],
            authenticationType: "test"));
        var clients = new Mock<IHubCallerClients>();
        groupProxy ??= new Mock<IClientProxy>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(groupProxy.Object);
        clients.Setup(c => c.Caller).Returns(new Mock<ISingleClientProxy>().Object);
        hub.Context = new Mock<HubCallerContext>().Object;
        Mock.Get(hub.Context).SetupGet(c => c.User).Returns(claims);
        Mock.Get(hub.Context).SetupGet(c => c.ConnectionId).Returns($"conn-{playerId}");
        hub.Clients = clients.Object;
        hub.Groups = new Mock<IGroupManager>().Object;
        return hub;
    }

    [Fact]
    public async Task JoinSession_ReturnsContractShape()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);

        var hub = CreateHub(ann.Id);
        var result = await hub.JoinSession(snapshot.Id);

        Assert.Equal(snapshot.Id, result.SessionId);
        Assert.Equal(snapshot.CurrentNumber, result.Number);
        Assert.Equal(1, result.Round);
        Assert.Equal(snapshot.EndTimeUtc, result.EndsAtUtc);
        Assert.Equal(0, result.Scores.Single().Score);
        Assert.False(result.AnsweredCurrentRound);
    }

    [Fact]
    public async Task JoinSession_Missing_ThrowsExactMessage()
    {
        var ann = await _fx.AddPlayerAsync("Ann");

        var hub = CreateHub(ann.Id);
        var ex = await Assert.ThrowsAsync<HubException>(() => hub.JoinSession(999));
        Assert.Equal("Session not found.", ex.Message);
    }

    [Fact]
    public async Task JoinSession_Finished_ThrowsExactMessage()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var bob = await _fx.AddPlayerAsync("Bob");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        await _fx.Store.FinishSessionAsync(snapshot.Id);

        var hub = CreateHub(bob.Id);
        var ex = await Assert.ThrowsAsync<HubException>(() => hub.JoinSession(snapshot.Id));
        Assert.Equal("Session has finished.", ex.Message);
    }

    [Fact]
    public async Task SubmitAnswer_Correct_ScoresAndBroadcasts()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 6, 6, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        var group = new Mock<IClientProxy>();
        var hub = CreateHub(ann.Id, group);

        var result = await hub.SubmitAnswer(snapshot.Id, 1, "Fizz");

        Assert.True(result.Correct);
        Assert.Equal(10, result.Score);
        group.Verify(c => c.SendCoreAsync(
            HubMessages.ScoresUpdated,
            It.Is<object[]>(args => args.Length == 1 && ((ScoresUpdatedPayload)args[0]).Scores.Single().Score == 10),
            default));
    }

    [Fact]
    public async Task SubmitAnswer_NotJoined_ThrowsExactMessage()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var bob = await _fx.AddPlayerAsync("Bob");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);

        var hub = CreateHub(bob.Id);
        var ex = await Assert.ThrowsAsync<HubException>(() => hub.SubmitAnswer(snapshot.Id, 1, "Fizz"));
        Assert.Equal("Join the session before answering.", ex.Message);
    }

    [Fact]
    public async Task SubmitAnswer_StaleRound_ThrowsExactMessage()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        await _fx.Store.AdvanceSessionAsync(snapshot.Id, 7);

        var hub = CreateHub(ann.Id);
        var ex = await Assert.ThrowsAsync<HubException>(() => hub.SubmitAnswer(snapshot.Id, 1, "Fizz"));
        Assert.Equal("Answer is for an old round.", ex.Message);
    }

    [Fact]
    public async Task SubmitAnswer_DuplicateRound_ThrowsExactMessage()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 6, 6, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);

        var hub = CreateHub(ann.Id);
        await hub.SubmitAnswer(snapshot.Id, 1, "Fizz");
        var ex = await Assert.ThrowsAsync<HubException>(() => hub.SubmitAnswer(snapshot.Id, 1, "Fizz"));
        Assert.Equal("Already answered this round.", ex.Message);
    }
    [Fact]
    public async Task SubmitAnswer_Empty_ThrowsExactMessage()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);

        var hub = CreateHub(ann.Id);
        var ex = await Assert.ThrowsAsync<HubException>(() => hub.SubmitAnswer(snapshot.Id, 1, "   "));
        Assert.Equal("Answer is required.", ex.Message);
    }

    [Fact]
    public async Task SubmitAnswer_Overlong_ThrowsExactMessage()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);

        var hub = CreateHub(ann.Id);
        var ex = await Assert.ThrowsAsync<HubException>(() => hub.SubmitAnswer(snapshot.Id, 1, new string('x', 101)));
        Assert.Equal("Answer must be at most 100 characters.", ex.Message);
    }

    [Fact]
    public async Task SubmitAnswer_MissingIdentity_ThrowsExactMessage()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);

        var hub = new GameSessionHub(_fx.Store, _fx.Rules, NullLogger<GameSessionHub>.Instance);
        var context = new Mock<HubCallerContext>();
        context.SetupGet(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity()));
        context.SetupGet(c => c.ConnectionId).Returns("conn-anon");
        hub.Context = context.Object;
        hub.Clients = new Mock<IHubCallerClients>().Object;
        hub.Groups = new Mock<IGroupManager>().Object;

        var ex = await Assert.ThrowsAsync<HubException>(() => hub.SubmitAnswer(snapshot.Id, 1, "Fizz"));
        Assert.Equal("Missing player identity.", ex.Message);
    }


    [Fact]
    public async Task SubmitAnswer_Finished_ThrowsExactMessage()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 1, 100, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        await _fx.Store.FinishSessionAsync(snapshot.Id);

        var hub = CreateHub(ann.Id);
        var ex = await Assert.ThrowsAsync<HubException>(() => hub.SubmitAnswer(snapshot.Id, 1, "Fizz"));
        Assert.Equal("Session has finished.", ex.Message);
    }

    [Fact]
    public async Task LeaveSession_RemovesFromGroupAndKeepsScore()
    {
        var ann = await _fx.AddPlayerAsync("Ann");
        var game = await _fx.AddGameAsync(ann.Id, ann.Name, 6, 6, (3, "Fizz"));
        var snapshot = await _fx.Store.CreateSessionAsync(game.Id, ann.Id, 300);
        var groups = new Mock<IGroupManager>();
        var hub = CreateHub(ann.Id);
        hub.Groups = groups.Object;
        await hub.SubmitAnswer(snapshot.Id, 1, "Fizz");

        await hub.LeaveSession(snapshot.Id);

        groups.Verify(g => g.RemoveFromGroupAsync($"conn-{ann.Id}", $"session:{snapshot.Id}", default));
        var latest = await _fx.Store.GetSnapshotAsync(snapshot.Id);
        Assert.Equal(10, latest!.Scores.Single().Score);
    }

    public void Dispose() => _fx.Dispose();
}
