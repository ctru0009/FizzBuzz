using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using backend.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace backend.Tests.Integration;

[CollectionDefinition(IntegrationCollection.Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationTestFactory>
{
    public const string Name = "Integration";
}

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    protected IntegrationTestBase(IntegrationTestFactory factory)
    {
        Factory = factory;
    }

    protected IntegrationTestFactory Factory { get; }

    public virtual Task InitializeAsync() => Factory.ResetDatabaseAsync();

    public virtual Task DisposeAsync() => Task.CompletedTask;

    protected static HttpClient CreateBareClient(IntegrationTestFactory factory)
    {
        return factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    protected sealed record AuthenticatedClient(HttpClient Client, string CookieHeader, string AntiforgeryToken);

    protected async Task<AuthenticatedClient> RegisterPlayerAsync(string name, string password = "password123")
    {
        var cookies = new CookieContainer();
        var client = new HttpClient(new CookieTrackingHandler(Factory.Server.CreateHandler(), cookies))
        {
            BaseAddress = Factory.Server.BaseAddress,
        };
        using var register = await client.PostAsJsonAsync(
            "/api/players/register",
            new { name, password });
        register.EnsureSuccessStatusCode();

        var stored = cookies.GetCookies(new Uri("http://localhost"));
        var authCookie = stored["fizzbuzz_auth"];
        Assert.NotNull(authCookie);
        Assert.NotEmpty(authCookie.Value);

        string antiforgeryToken;
        using (var tokenResponse = await client.GetAsync("/api/antiforgery/token"))
        {
            tokenResponse.EnsureSuccessStatusCode();
            var payload = await tokenResponse.Content.ReadFromJsonAsync<AntiforgeryTokenResponse>(JsonOptions);
            Assert.NotNull(payload);
            antiforgeryToken = payload.Token;
        }

        var cookieHeader = string.Join("; ", stored.Cast<Cookie>().Select(c => $"{c.Name}={c.Value}"));
        return new AuthenticatedClient(client, cookieHeader, antiforgeryToken);
    }

    protected static HttpRequestMessage JsonPost(string url, object body, AuthenticatedClient auth)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("X-XSRF-TOKEN", auth.AntiforgeryToken);
        return request;
    }

    protected static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var text = await response.Content.ReadAsStringAsync();
        var value = JsonSerializer.Deserialize<T>(text, JsonOptions);
        Assert.NotNull(value);
        return value;
    }

    protected HubConnection CreateHubConnection(AuthenticatedClient auth)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(Factory.Server.BaseAddress, "/sessionHub"),
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                    options.Headers["Cookie"] = auth.CookieHeader;
                })
            .WithAutomaticReconnect(new HubRetryPolicy())
            .Build();
        return connection;
    }

    protected async Task<(int GameId, SessionSnapshot Session)> CreateGameAndSessionAsync(
        AuthenticatedClient auth,
        int startRange = 1,
        int endRange = 100,
        int divisibleBy = 3,
        string word = "Fizz",
        int durationSeconds = 300)
    {
        using var gameResponse = await auth.Client.SendAsync(JsonPost(
            "/api/games",
            new { name = $"Game {Guid.NewGuid():N}", startRange, endRange, rules = new[] { new { divisibleBy, replacementWord = word } } },
            auth));
        gameResponse.EnsureSuccessStatusCode();
        var game = await ReadJsonAsync<GameResponseDTO>(gameResponse);

        using var sessionResponse = await auth.Client.SendAsync(JsonPost(
            "/api/sessions",
            new { gameId = game.Id, durationSeconds },
            auth));
        sessionResponse.EnsureSuccessStatusCode();
        var session = await ReadJsonAsync<SessionSnapshot>(sessionResponse);
        return (game.Id, session);
    }

    protected static string ExpectedAnswer(int number, int divisibleBy, string word)
    {
        return number % divisibleBy == 0 ? word : number.ToString();
    }

    protected static string WrongAnswer(int number, int divisibleBy, string word)
    {
        var expected = ExpectedAnswer(number, divisibleBy, word);
        return string.Equals(expected, word, StringComparison.OrdinalIgnoreCase) ? (number + 100000).ToString() : word;
    }

    private sealed record AntiforgeryTokenResponse(string Token);
    private sealed class HubRetryPolicy : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext) => TimeSpan.FromMilliseconds(200);
    }

    private sealed class CookieTrackingHandler(HttpMessageHandler inner, CookieContainer cookies) : DelegatingHandler(inner)
    {
        private readonly CookieContainer _cookies = cookies;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri ?? new Uri("http://localhost/");
            var pending = _cookies.GetCookieHeader(uri);
            if (!string.IsNullOrEmpty(pending))
            {
                request.Headers.Remove("Cookie");
                request.Headers.Add("Cookie", pending);
            }

            var response = await base.SendAsync(request, cancellationToken);
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var value in setCookies)
                {
                    _cookies.SetCookies(uri, value);
                }
            }

            return response;
        }
    }
}
