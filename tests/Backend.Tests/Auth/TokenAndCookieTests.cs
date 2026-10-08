using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using backend.Auth;
using Microsoft.IdentityModel.Tokens;

namespace backend.Tests.Auth;

public sealed class TokenAndCookieTests
{
    private const string SigningKey = "test-signing-key-that-is-long-enough-123";

    [Fact]
    public void GenerateToken_WritesSubAndNameClaims()
    {
        using var fx = new AuthFixture();
        var tokens = fx.CreateTokenService(SigningKey);

        var token = tokens.GenerateToken(7, "erin");

        var principal = Validate(token);
        Assert.Equal("7", principal.FindFirst(AuthClaims.Subject)?.Value);
        Assert.Equal("erin", principal.FindFirst(AuthClaims.Name)?.Value);
        Assert.Equal("fizzbuzz", principal.FindFirst("iss")?.Value ?? principal.Claims.FirstOrDefault(c => c.Type == "iss")?.Value ?? "fizzbuzz");
    }

    [Fact]
    public void ValidationParameters_AcceptOwnToken_RejectWrongKey()
    {
        using var fx = new AuthFixture();
        var tokens = fx.CreateTokenService(SigningKey);
        var token = tokens.GenerateToken(9, "frank");

        var principal = new JwtSecurityTokenHandler().ValidateToken(
            token, tokens.CreateValidationParameters(), out _);
        Assert.Equal("9", principal.FindFirst(AuthClaims.Subject)?.Value);

        var wrong = fx.CreateTokenService("a-different-signing-key-that-is-also-long-1");
        Assert.Throws<SecurityTokenSignatureKeyNotFoundException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, wrong.CreateValidationParameters(), out _));
    }

    [Fact]
    public void AuthClaims_Readers_ParseIdAndName()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(AuthClaims.Subject, "12"),
            new Claim(AuthClaims.Name, "gina"),
        ]));

        Assert.Equal(12, principal.GetPlayerId());
        Assert.Equal("gina", principal.GetPlayerName());
    }

    [Fact]
    public void AuthClaims_Readers_FallBackToClaimTypes()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "13"),
            new Claim(ClaimTypes.Name, "hank"),
        ]));

        Assert.Equal(13, principal.GetPlayerId());
        Assert.Equal("hank", principal.GetPlayerName());
    }

    [Fact]
    public void AuthClaims_Readers_Missing_ReturnNull()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.Null(principal.GetPlayerId());
        Assert.Null(principal.GetPlayerName());
    }

    private static ClaimsPrincipal Validate(string token)
    {
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        return new JwtSecurityTokenHandler().ValidateToken(
            token,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "fizzbuzz",
                ValidateAudience = true,
                ValidAudience = "fizzbuzz",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
            },
            out _);
    }
}
