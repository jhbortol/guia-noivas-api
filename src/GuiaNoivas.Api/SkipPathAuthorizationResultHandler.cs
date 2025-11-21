using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace GuiaNoivas.Api
{
    // Bypass the authorization enforcement for specific paths like /swagger and /hangfire.
    public class SkipPathAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
    {
        private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

        public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
        {
            var path = context.Request.Path.Value;
            if (!string.IsNullOrEmpty(path) && (path.StartsWith("/swagger") || path.StartsWith("/hangfire")))
            {
                // Let the request continue without enforcing the fallback policy
                await next(context);
                return;
            }

            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
        }
    }
}
