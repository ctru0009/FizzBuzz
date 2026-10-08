using System.Net;
using System.Net.Http.Json;
using backend.DTOs;

namespace backend.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(IntegrationCollection.Name)]
public sealed class GameApiTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
{
    private readonly IntegrationTestFactory _factory = factory;

    [Fact]
    public async Task CreateGame_Valid_ReturnsCreatedShape()
    {
        var auth = await RegisterPlayerAsync($"gamer{Guid.NewGuid():N}");

        using var response = await auth.Client.SendAsync(JsonPost(
            "/api/games",
            new
            {
                name = "Fizz Five",
                startRange = 1,
                endRange = 100,
                rules = new[] { new { divisibleBy = 3, replacementWord = "Fizz" }, new { divisibleBy = 5, replacementWord = "Buzz" } },
            },
            auth));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var game = await ReadJsonAsync<GameResponseDTO>(response);
        Assert.True(game.Id > 0);
        Assert.Equal("Fizz Five", game.Name);
        Assert.Equal(1, game.StartRange);
        Assert.Equal(100, game.EndRange);
        Assert.Equal(2, game.Rules.Length);
        Assert.NotEmpty(game.AuthorName);

        using var list = await auth.Client.GetAsync("/api/games");
        list.EnsureSuccessStatusCode();
        var games = await ReadJsonAsync<GameResponseDTO[]>(list);
        Assert.Contains(games, g => g.Id == game.Id && g.Rules.Length == 2);
    }

    [Fact]
    public async Task CreateGame_StartNotLessThanEnd_ReturnsBadRequest()
    {
        var auth = await RegisterPlayerAsync($"gamer{Guid.NewGuid():N}");

        using var response = await auth.Client.SendAsync(JsonPost(
            "/api/games",
            new
            {
                name = "Bad Range",
                startRange = 10,
                endRange = 10,
                rules = new[] { new { divisibleBy = 3, replacementWord = "Fizz" } },
            },
            auth));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_DivisibleByZero_ReturnsBadRequest()
    {
        var auth = await RegisterPlayerAsync($"gamer{Guid.NewGuid():N}");

        using var response = await auth.Client.SendAsync(JsonPost(
            "/api/games",
            new
            {
                name = "Bad Rule",
                startRange = 1,
                endRange = 10,
                rules = new[] { new { divisibleBy = 0, replacementWord = "Fizz" } },
            },
            auth));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_MissingAntiforgeryToken_ReturnsBadRequest()
    {
        var auth = await RegisterPlayerAsync($"gamer{Guid.NewGuid():N}");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/games")
        {
            Content = JsonContent.Create(new
            {
                name = "No Token",
                startRange = 1,
                endRange = 10,
                rules = new[] { new { divisibleBy = 3, replacementWord = "Fizz" } },
            }),
        };

        using var response = await auth.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGame_Anonymous_ReturnsUnauthorized()
    {
        var bare = CreateBareClient(_factory);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/games")
        {
            Content = JsonContent.Create(new
            {
                name = "Anon",
                startRange = 1,
                endRange = 10,
                rules = new[] { new { divisibleBy = 3, replacementWord = "Fizz" } },
            }),
        };

        using var response = await bare.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
