using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;

namespace MreHunter.Api.Middleware
{
    public class SessionCookieMiddleware
    {
        private readonly RequestDelegate _next;

        public SessionCookieMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            const string COOKIE_NAME = "mh_session";

            if (!context.Request.Cookies.ContainsKey(COOKIE_NAME))
            {
                var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16))
                    .ToLowerInvariant();
                var isHttps = context.Request.IsHttps;

                context.Response.Cookies.Append(COOKIE_NAME, sessionId, new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
                    Secure = isHttps,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    Path = "/"
                });
            }

            await _next(context);
        }
    }
}
