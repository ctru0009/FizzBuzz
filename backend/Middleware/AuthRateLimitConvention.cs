using backend;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.RateLimiting;

namespace backend.Middleware
{
    public sealed class AuthRateLimitConvention : IActionModelConvention
    {
        public void Apply(ActionModel action)
        {
            if (!action.Controller.ControllerName.Equals("Players", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var template = action.Selectors
                .Select(selector => selector.AttributeRouteModel?.Template)
                .FirstOrDefault(route => route is not null) ?? string.Empty;
            var isAuthRoute = template.EndsWith("/register", StringComparison.OrdinalIgnoreCase)
                || template.EndsWith("/login", StringComparison.OrdinalIgnoreCase);
            var isAuthAction = action.ActionName.Contains("Register", StringComparison.OrdinalIgnoreCase)
                || action.ActionName.Contains("Login", StringComparison.OrdinalIgnoreCase);
            if (isAuthRoute || isAuthAction)
            {
                foreach (var selector in action.Selectors)
                {
                    selector.EndpointMetadata.Add(new EnableRateLimitingAttribute(Program.AuthStrictPolicy));
                }
            }
        }
    }
}
