using System.Security.Claims;
using backend.Auth;
using backend.Controllers;
using backend.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace backend.Tests.Auth;

public sealed class PlayersControllerTests : IDisposable
{
    private const string SigningKey = "test-signing-key-that-is-long-enough-123";
    private readonly AuthFixture _fx = new();

    [Fact]
    public async Task Register_Returns201AndSetsCookie()
    {
        var controller = CreateController();

        var result = await controller.Register(
            new RegisterRequest { Name = "ivan", Password = "password123" }, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var body = Assert.IsType<AuthResult>(created.Value);
        Assert.Equal("ivan", body.Name);
        Assert.True(body.Id > 0);
        Assert.Contains(AuthCookie.Name, controller.Response.Headers.SetCookie.ToString());
        Assert.Contains("httponly", controller.Response.Headers.SetCookie.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_DuplicateName_Returns409()
    {
        var controller = CreateController();
        await controller.Register(
            new RegisterRequest { Name = "judy", Password = "password123" }, CancellationToken.None);

        var second = CreateController();
        var result = await second.Register(
            new RegisterRequest { Name = "judy", Password = "anotherpass1" }, CancellationToken.None);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatusCodes.Status409Conflict, conflict.StatusCode);
    }

    [Fact]
    public async Task Login_Success_Returns200AndSetsCookie()
    {
        var setup = CreateController();
        await setup.Register(
            new RegisterRequest { Name = "kate", Password = "password123" }, CancellationToken.None);

        var controller = CreateController();
        var result = await controller.Login(
            new LoginRequest { Name = "kate", Password = "password123" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<AuthResult>(ok.Value);
        Assert.Equal("kate", body.Name);
        Assert.Contains(AuthCookie.Name, controller.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task Login_BadCredentials_Returns401()
    {
        var setup = CreateController();
        await setup.Register(
            new RegisterRequest { Name = "liam", Password = "password123" }, CancellationToken.None);

        var controller = CreateController();
        var result = await controller.Login(
            new LoginRequest { Name = "liam", Password = "wrongpass1" }, CancellationToken.None);

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Me_WithAuthCookie_ReturnsIdAndName()
    {
        var setup = CreateController();
        var registered = await setup.Register(
            new RegisterRequest { Name = "mia", Password = "password123" }, CancellationToken.None);
        var created = Assert.IsType<CreatedAtActionResult>(registered);
        var identity = Assert.IsType<AuthResult>(created.Value);

        var controller = CreateController(identity.Id, identity.Name);
        var result = await controller.Me(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        var body = Assert.IsType<AuthResult>(ok.Value);
        Assert.Equal(identity.Id, body.Id);
        Assert.Equal("mia", body.Name);
    }

    [Fact]
    public async Task Me_WithoutAuth_Returns401()
    {
        var controller = CreateController();

        var result = await controller.Me(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void Logout_ClearsCookie()
    {
        var controller = CreateController();

        var result = controller.Logout();

        Assert.IsType<OkObjectResult>(result);
        var header = controller.Response.Headers.SetCookie.ToString();
        Assert.Contains(AuthCookie.Name, header);
        Assert.Contains("expires=", header, StringComparison.OrdinalIgnoreCase);
    }

    private PlayersController CreateController(int? playerId = null, string? playerName = null)
    {
        var options = Options.Create(_fx.CreateJwtOptions(SigningKey));
        var controller = new PlayersController(_fx.Players, _fx.CreateTokenService(SigningKey), options)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };

        if (playerId is not null)
        {
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(AuthClaims.Subject, playerId.Value.ToString()),
                new Claim(AuthClaims.Name, playerName ?? string.Empty),
            ]));
        }

        return controller;
    }

    public void Dispose() => _fx.Dispose();
}
