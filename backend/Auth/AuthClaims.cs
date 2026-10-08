using System.Security.Claims;

namespace backend.Auth
{
    public static class AuthClaims
    {
        public const string Subject = "sub";

        public const string Name = "name";

        public static int? GetPlayerId(this ClaimsPrincipal principal)
        {
            var value = principal.FindFirstValue(Subject)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }

        public static string? GetPlayerName(this ClaimsPrincipal principal)
        {
            return principal.FindFirstValue(Name)
                ?? principal.FindFirstValue(ClaimTypes.Name);
        }
    }
}
