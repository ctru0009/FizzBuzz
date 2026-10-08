using backend.Auth;
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests.Auth;

public sealed class AuthFixture : IDisposable
{
    public AuthFixture()
    {
        var databaseName = $"auth-{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<BackendAppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IPasswordHasher<Player>, PasswordHasher<Player>>();
        services.AddScoped<IPlayer, PlayerService>();
        Provider = services.BuildServiceProvider();
        Scope = Provider.CreateScope();
        Context = Scope.ServiceProvider.GetRequiredService<BackendAppDbContext>();
        Players = Scope.ServiceProvider.GetRequiredService<IPlayer>();
    }

    public ServiceProvider Provider { get; }

    public IServiceScope Scope { get; }

    public BackendAppDbContext Context { get; }

    public IPlayer Players { get; }

    public TokenService CreateTokenService(string signingKey)
    {
        return new TokenService(Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "fizzbuzz",
            Audience = "fizzbuzz",
            SigningKey = signingKey,
            LifetimeHours = 8,
        }));
    }

    public JwtOptions CreateJwtOptions(string signingKey)
    {
        return new JwtOptions
        {
            Issuer = "fizzbuzz",
            Audience = "fizzbuzz",
            SigningKey = signingKey,
            LifetimeHours = 8,
        };
    }

    public void Dispose()
    {
        Scope.Dispose();
        Provider.Dispose();
    }
}

public sealed class PlayerServiceTests : IDisposable
{
    private readonly AuthFixture _fx = new();

    [Fact]
    public async Task RegisterAsync_HashesPasswordAndPersists()
    {
        var request = new RegisterRequest { Name = "alice", Password = "password123" };

        var player = await _fx.Players.RegisterAsync(request);

        Assert.True(player.Id > 0);
        Assert.Equal("alice", player.Name);
        Assert.NotNull(player.PasswordHash);
        Assert.NotEqual("password123", player.PasswordHash);
        var stored = await _fx.Context.Players.AsNoTracking().SingleAsync(p => p.Name == "alice");
        Assert.Equal(player.Id, stored.Id);
        Assert.Equal(player.PasswordHash, stored.PasswordHash);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateName_ThrowsTypedException()
    {
        await _fx.Players.RegisterAsync(new RegisterRequest { Name = "bob", Password = "password123" });

        await Assert.ThrowsAsync<DuplicatePlayerNameException>(() =>
            _fx.Players.RegisterAsync(new RegisterRequest { Name = "bob", Password = "anotherpass1" }));
    }

    [Fact]
    public async Task ValidateCredentialsAsync_CorrectPassword_ReturnsPlayer()
    {
        var registered = await _fx.Players.RegisterAsync(
            new RegisterRequest { Name = "carol", Password = "password123" });

        var player = await _fx.Players.ValidateCredentialsAsync(
            new LoginRequest { Name = "carol", Password = "password123" });

        Assert.NotNull(player);
        Assert.Equal(registered.Id, player.Id);
    }

    [Theory]
    [InlineData("dave", "wrongpass1")]
    [InlineData("missing", "password123")]
    public async Task ValidateCredentialsAsync_BadCredentials_ReturnsNull(string name, string password)
    {
        await _fx.Players.RegisterAsync(new RegisterRequest { Name = "dave", Password = "password123" });

        var player = await _fx.Players.ValidateCredentialsAsync(
            new LoginRequest { Name = name, Password = password });

        Assert.Null(player);
    }

    [Fact]
    public async Task GetByIdAsync_Missing_ReturnsNull()
    {
        var player = await _fx.Players.GetByIdAsync(4242);

        Assert.Null(player);
    }

    public void Dispose() => _fx.Dispose();
}
