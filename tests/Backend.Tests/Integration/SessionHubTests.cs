using backend.DTOs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace backend.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(IntegrationCollection.Name)]
public sealed class SessionHubTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
{
    private readonly IntegrationTestFactory _factory = factory;

    [Fact]
    public async Task JoinSession_ReturnsContractShape()
    {
        var auth = await RegisterPlayerAsync($"hub{Guid.NewGuid():N}");
        var (_, session) = await CreateGameAndSessionAsync(auth);
        await using var hub = CreateHubConnection(auth);
        await hub.StartAsync();

        var result = await hub.InvokeAsync<JoinSessionResult>("JoinSession", session.Id);

        Assert.Equal(session.Id, result.SessionId);
        Assert.Equal(session.CurrentNumber, result.Number);
        Assert.Equal(1, result.Round);
        Assert.False(result.AnsweredCurrentRound);
        Assert.Single(result.Scores);
    }

    [Fact]
    public async Task SubmitAnswer_Correct_ScoresPlusTen()
    {
        var auth = await RegisterPlayerAsync($"hub{Guid.NewGuid():N}");
        var (_, session) = await CreateGameAndSessionAsync(auth);
        await using var hub = CreateHubConnection(auth);
        await hub.StartAsync();
        var joined = await hub.InvokeAsync<JoinSessionResult>("JoinSession", session.Id);

        var result = await hub.InvokeAsync<SubmitAnswerResult>(
            "SubmitAnswer",
            session.Id,
            joined.Round,
            ExpectedAnswer(joined.Number, 3, "Fizz"));

        Assert.True(result.Correct);
        Assert.Equal(10, result.Score);
    }

    [Fact]
    public async Task SubmitAnswer_Wrong_ScoresZero()
    {
        var auth = await RegisterPlayerAsync($"hub{Guid.NewGuid():N}");
        var (_, session) = await CreateGameAndSessionAsync(auth);
        await using var hub = CreateHubConnection(auth);
        await hub.StartAsync();
        var joined = await hub.InvokeAsync<JoinSessionResult>("JoinSession", session.Id);

        var result = await hub.InvokeAsync<SubmitAnswerResult>(
            "SubmitAnswer",
            session.Id,
            joined.Round,
            WrongAnswer(joined.Number, 3, "Fizz"));

        Assert.False(result.Correct);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public async Task SubmitAnswer_StaleRound_ThrowsExactMessage()
    {
        var auth = await RegisterPlayerAsync($"hub{Guid.NewGuid():N}");
        var (_, session) = await CreateGameAndSessionAsync(auth);
        await using var hub = CreateHubConnection(auth);
        await hub.StartAsync();
        var joined = await hub.InvokeAsync<JoinSessionResult>("JoinSession", session.Id);

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.InvokeAsync<SubmitAnswerResult>("SubmitAnswer", session.Id, joined.Round + 99, "Fizz"));

        AssertHubError(ex, "Answer is for an old round.");
    }

    [Fact]
    public async Task SubmitAnswer_NotJoined_ThrowsExactMessage()
    {
        var host = await RegisterPlayerAsync($"host{Guid.NewGuid():N}");
        var (_, session) = await CreateGameAndSessionAsync(host);
        var outsider = await RegisterPlayerAsync($"outsider{Guid.NewGuid():N}");
        await using var hub = CreateHubConnection(outsider);
        await hub.StartAsync();

        var ex = await Assert.ThrowsAsync<HubException>(() =>
            hub.InvokeAsync<SubmitAnswerResult>("SubmitAnswer", session.Id, 1, "Fizz"));

        AssertHubError(ex, "Join the session before answering.");
    }

    [Fact]
    public async Task SubmitAnswer_ConcurrentDuplicate_SecondRejectedByUniqueConstraint()
    {
        var auth = await RegisterPlayerAsync($"hub{Guid.NewGuid():N}");
        var (_, session) = await CreateGameAndSessionAsync(auth);
        await using var hub = CreateHubConnection(auth);
        await hub.StartAsync();
        var joined = await hub.InvokeAsync<JoinSessionResult>("JoinSession", session.Id);
        var answer = ExpectedAnswer(joined.Number, 3, "Fizz");

        var attempts = Enumerable.Range(0, 8)
            .Select(_ => InvokeSubmitAsync(hub, session.Id, joined.Round, answer))
            .ToArray();
        var outcomes = await Task.WhenAll(attempts);

        var successes = outcomes.OfType<SubmitAnswerResult>().ToArray();
        var failures = outcomes.OfType<HubException>().ToArray();
        Assert.Single(successes);
        Assert.Equal(10, successes[0].Score);
        Assert.NotEmpty(failures);
        Assert.All(failures, f => AssertHubError(f, "Already answered this round."));

        using var snapshot = await auth.Client.GetAsync($"/api/sessions/{session.Id}");
        snapshot.EnsureSuccessStatusCode();
        var state = await ReadJsonAsync<SessionSnapshot>(snapshot);
        Assert.Equal(10, state.Scores.Single().Score);
    }

    [Fact]
    public async Task SubmitAnswer_BroadcastsScoresUpdated()
    {
        var auth = await RegisterPlayerAsync($"hub{Guid.NewGuid():N}");
        var (_, session) = await CreateGameAndSessionAsync(auth);
        await using var hub = CreateHubConnection(auth);
        var broadcast = new TaskCompletionSource<ScoresUpdatedPayload>(TaskCreationOptions.RunContinuationsAsynchronously);
        hub.On<ScoresUpdatedPayload>("ScoresUpdated", payload => broadcast.TrySetResult(payload));
        await hub.StartAsync();
        var joined = await hub.InvokeAsync<JoinSessionResult>("JoinSession", session.Id);

        await hub.InvokeAsync<SubmitAnswerResult>(
            "SubmitAnswer",
            session.Id,
            joined.Round,
            ExpectedAnswer(joined.Number, 3, "Fizz"));

        var completed = await Task.WhenAny(broadcast.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.Same(broadcast.Task, completed);
        var payload = await broadcast.Task;
        Assert.Equal(10, payload.Scores.Single().Score);
    }

    private static void AssertHubError(HubException ex, string contractMessage)
    {
        // SignalR wraps thrown HubExceptions with an invocation prefix, even with
        // detailed errors on. The contract message must survive verbatim as the suffix.
        Assert.EndsWith(contractMessage, ex.Message);
    }

    private static async Task<object> InvokeSubmitAsync(HubConnection hub, int sessionId, int round, string answer)
    {
        try
        {
            return await hub.InvokeAsync<SubmitAnswerResult>("SubmitAnswer", sessionId, round, answer);
        }
        catch (HubException ex)
        {
            return ex;
        }
    }
}
