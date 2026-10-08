using System.Net;
using backend.DTOs;
using backend.Models;

namespace backend.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(IntegrationCollection.Name)]
public sealed class SessionApiTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
{
    private readonly IntegrationTestFactory _factory = factory;

    [Fact]
    public async Task CreateSession_Valid_ReturnsSnapshotShape()
    {
        var auth = await RegisterPlayerAsync($"host{Guid.NewGuid():N}");
        var (gameId, session) = await CreateGameAndSessionAsync(auth, startRange: 1, endRange: 100);

        Assert.True(session.Id > 0);
        Assert.Equal(gameId, session.GameId);
        Assert.Equal(SessionStatus.Open, session.Status);
        Assert.Equal(1, session.CurrentRound);
        Assert.InRange(session.CurrentNumber, 1, 100);
        Assert.Single(session.Scores);
        Assert.Equal(0, session.Scores[0].Score);
        Assert.True(session.EndTimeUtc > session.StartTimeUtc);

        using var get = await auth.Client.GetAsync($"/api/sessions/{session.Id}");
        get.EnsureSuccessStatusCode();
        var fetched = await ReadJsonAsync<SessionSnapshot>(get);
        Assert.Equal(session.Id, fetched.Id);
        Assert.Equal(SessionStatus.Open, fetched.Status);
        Assert.Equal(1, fetched.CurrentRound);
    }

    [Fact]
    public async Task CreateSession_MissingGame_ReturnsNotFound()
    {
        var auth = await RegisterPlayerAsync($"host{Guid.NewGuid():N}");

        using var response = await auth.Client.SendAsync(JsonPost(
            "/api/sessions",
            new { gameId = 424242, durationSeconds = 300 },
            auth));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateSession_BadDuration_ReturnsBadRequest()
    {
        var auth = await RegisterPlayerAsync($"host{Guid.NewGuid():N}");
        using var gameResponse = await auth.Client.SendAsync(JsonPost(
            "/api/games",
            new
            {
                name = "Short",
                startRange = 1,
                endRange = 10,
                rules = new[] { new { divisibleBy = 3, replacementWord = "Fizz" } },
            },
            auth));
        gameResponse.EnsureSuccessStatusCode();
        var game = await ReadJsonAsync<GameResponseDTO>(gameResponse);

        using var response = await auth.Client.SendAsync(JsonPost(
            "/api/sessions",
            new { gameId = game.Id, durationSeconds = 5 },
            auth));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task OpenList_Anonymous_ContainsCreatedSession()
    {
        var auth = await RegisterPlayerAsync($"host{Guid.NewGuid():N}");
        var (_, session) = await CreateGameAndSessionAsync(auth);

        var bare = CreateBareClient(_factory);
        using var response = await bare.GetAsync("/api/sessions/open");
        response.EnsureSuccessStatusCode();
        var open = await ReadJsonAsync<OpenSessionInfo[]>(response);
        var entry = Assert.Single(open, s => s.Id == session.Id);
        Assert.True(entry.PlayerCount >= 1);
        Assert.True(entry.EndsAtUtc > DateTime.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task GetSession_Missing_ReturnsNotFound()
    {
        var auth = await RegisterPlayerAsync($"host{Guid.NewGuid():N}");

        using var response = await auth.Client.GetAsync("/api/sessions/424242");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
