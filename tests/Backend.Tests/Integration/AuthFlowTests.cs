using System.Net;
using System.Net.Http.Json;

namespace backend.Tests.Integration;

[Trait("Category", "Integration")]
[Collection(IntegrationCollection.Name)]
public sealed class AuthFlowTests(IntegrationTestFactory factory) : IntegrationTestBase(factory)
{
    private readonly IntegrationTestFactory _factory = factory;

    [Fact]
    public async Task Register_Login_Me_Logout_CookieFlow()
    {
        var bare = CreateBareClient(_factory);

        using var register = await bare.PostAsJsonAsync(
            "/api/players/register",
            new { name = "alice", password = "password123" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var authCookie = Assert.Single(register.Headers.GetValues("Set-Cookie"));
        Assert.Contains("fizzbuzz_auth=", authCookie);
        Assert.Contains("httponly", authCookie, StringComparison.OrdinalIgnoreCase);

        using var logout = await bare.PostAsync("/api/players/logout", content: null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        using var meAnonymous = await bare.GetAsync("/api/players/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAnonymous.StatusCode);

        using var login = await bare.PostAsJsonAsync(
            "/api/players/login",
            new { name = "alice", password = "password123" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        Assert.Contains("fizzbuzz_auth=", Assert.Single(login.Headers.GetValues("Set-Cookie")));

        using var me = await bare.GetAsync("/api/players/me");
        me.EnsureSuccessStatusCode();
        var body = await ReadJsonAsync<AuthBody>(me);
        Assert.Equal("alice", body.Name);
        Assert.True(body.Id > 0);

        using var logoutAgain = await bare.PostAsync("/api/players/logout", content: null);
        Assert.Equal(HttpStatusCode.OK, logoutAgain.StatusCode);
        var cleared = Assert.Single(logoutAgain.Headers.GetValues("Set-Cookie"));
        Assert.Contains("fizzbuzz_auth=", cleared);

        using var meAfterLogout = await bare.GetAsync("/api/players/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfterLogout.StatusCode);
    }

    [Fact]
    public async Task Register_DuplicateName_ReturnsConflict()
    {
        var bare = CreateBareClient(_factory);
        using var first = await bare.PostAsJsonAsync(
            "/api/players/register",
            new { name = "bob", password = "password123" });
        first.EnsureSuccessStatusCode();

        using var second = await bare.PostAsJsonAsync(
            "/api/players/register",
            new { name = "bob", password = "password123" });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_BadPassword_ReturnsUnauthorized()
    {
        var bare = CreateBareClient(_factory);
        using var register = await bare.PostAsJsonAsync(
            "/api/players/register",
            new { name = "carol", password = "password123" });
        register.EnsureSuccessStatusCode();

        using var login = await bare.PostAsJsonAsync(
            "/api/players/login",
            new { name = "carol", password = "wrongpass1" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    private sealed record AuthBody(int Id, string Name);
}
