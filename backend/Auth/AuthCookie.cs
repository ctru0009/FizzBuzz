using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace backend.Auth
{
    public static class AuthCookie
    {
        public const string Name = "fizzbuzz_auth";

        public static void Write(HttpContext context, string token, JwtOptions options)
        {
            context.Response.Cookies.Append(Name, token, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddHours(options.LifetimeHours),
            });
        }

        public static void Write(HttpContext context, string token, IOptions<JwtOptions> options)
        {
            Write(context, token, options.Value);
        }

        public static void Clear(HttpContext context)
        {
            context.Response.Cookies.Delete(Name, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps,
            });
        }
    }
}
