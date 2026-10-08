using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace backend.Auth
{
    public class TokenService
    {
        private readonly JwtOptions _options;
        private readonly JwtSecurityTokenHandler _handler = new();

        public TokenService(IOptions<JwtOptions> options)
        {
            _options = options.Value;
        }

        public string GenerateToken(int id, string name)
        {
            var now = DateTime.UtcNow;
            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(AuthClaims.Subject, id.ToString()),
                    new Claim(AuthClaims.Name, name),
                ]),
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                NotBefore = now,
                Expires = now.AddHours(_options.LifetimeHours),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                    SecurityAlgorithms.HmacSha256),
            };

            return _handler.WriteToken(_handler.CreateToken(descriptor));
        }

        public TokenValidationParameters CreateValidationParameters()
        {
            return new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1),
            };
        }
    }
}
